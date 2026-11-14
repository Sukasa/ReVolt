using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using LaunchPadBooster.Utils;
using LibConstruct;
using ReVolt.Interfaces;
using ReVolt.ModCompatibility;

namespace ReVolt
{
    public class CableTray : SmallSingleGrid, ICableTrayComponent, IPatchable, ISmartRotatable
    {
        public static readonly List<CableTray> AllTrays = new(); // Master list used for the meson scanners as PseudoNetworks are not tracked globally

        private static IEqualityComparer<Cable> _cableComparer = new ColorVoltageCableComparer();
        /// <summary>
        /// Comparer used to determine if two cables can be joined through trays.
        /// </summary>
        /// <remarks>Defaults to <see cref="ColorVoltageCableComparer"/>.</remarks>
        public static IEqualityComparer<Cable> CableComparer
        {
            get => _cableComparer;
            set
            {
                _cableComparer = value ?? throw new ArgumentNullException(nameof(value));
                RebuildExclusions = new(_cableComparer);
            }
        }

        private static HashSet<Cable> RebuildExclusions = new(_cableComparer);
        private static readonly HashSet<long> SeenTrays = new();

        public override void OnRegistered(Cell cell)
        {
            base.OnRegistered(cell);
            AllTrays.Add(this);
            ReVolt.CableTrayNetwork.RebuildNetworkCreate(this);
            SeenTrays.Clear();
            PropagateNetworkUpdate(Network.Members);
        }

        public override void OnDeregistered()
        {
            AllTrays.Remove(this);

            Span<SmallCellRef> buf = stackalloc SmallCellRef[32];
            var count = 0;
            FillConnected<CableTray>(buf, ref count);

            base.OnDeregistered();
            ReVolt.CableTrayNetwork.RebuildNetworkDestroy(this);
            SeenTrays.Clear();

            var span = buf[..count];
            for (var index = 0; index < span.Length; ++index)
                PropagateNetworkUpdate(span[index].Get<CableTray>().Network.Members);
        }

        public void TriggerPropagation()
        {
            SeenTrays.Clear();
            PropagateNetworkUpdate(Network.Members);
        }
        
        private static void PropagateNetworkUpdate(IEnumerable<ICableTrayComponent> TraySet)
        {
            if (GameManager.GameState == GameState.Loading || !GameManager.RunSimulation)
                return;
            
            Span<SmallCellRef> buf = stackalloc SmallCellRef[32];

            foreach (var Tray in TraySet)
            {
                if (!SeenTrays.Add(Tray.ReferenceId))
                    continue;

                var count = 0;
                Tray.FillConnected<Cable>(buf, ref count);
                var span = buf[..count];

                for (var index = 0; index < span.Length; ++index)
                {
                    var cable = span[index].Get<Cable>();

                    if (cable == null)
                        continue;

                    if (cable.CableNetwork == null)
                        continue;

                    if (!RebuildExclusions.Add(cable))
                        continue;

                    cable.CableNetwork ??= new CableNetwork(cable);

                    CableNetwork.RebuildCableNetworkServer(cable);
                }
            }
            RebuildExclusions.Clear();
        }

        public void OnMemberAdded(ICableTrayComponent member)
        {
        }

        public void OnMemberRemoved(ICableTrayComponent member)
        {
        }


        public void OnMembersChanged()
        {
        }

        public List<Cable> MatchCables(Cable Metric)
        {
            var Result = new List<Cable>();

            Span<SmallCellRef> buf = stackalloc SmallCellRef[32];

            foreach (var Component in Network.Members)
            {
                var Tray = Component as CableTray;

                if (Tray == null)
                {
                    ConsoleWindow.PrintError("Null tray member!");
                    continue;
                }

                var count = 0;
                Tray.FillConnected<Cable>(buf, ref count);

                var span = buf[..count];
                for (var cableIndex = 0; cableIndex < span.Length; ++cableIndex)
                {
                    var Cable = span[cableIndex].Get<Cable>();
                    if (Cable != Metric &&
                        CableComparer.Equals(Metric, Cable) &&
                        !Result.Contains(Cable))
                    {
                        Result.Add(Cable);
                    }
                }
            }

            return Result;
        }

        public void MatchCableNetworks(List<CableNetwork> List, Cable Metric)
        {
            var list = MatchCables(Metric);
            for (var index = list.Count - 1; index >= 0; index--)
            {
                var Cable = list[index];
                if (Cable?.CableNetwork != null && !List.Contains(Cable.CableNetwork))
                    List.Add(Cable.CableNetwork);
            }
        }

        public override string GetStationpediaCategoryKey() => StationpediaCategoryStrings.CableCategory;
        public override string GetStationpediaCategory() => Localization.GetInterface(StationpediaCategoryStrings.CableCategory);

        public IEnumerable<Connection> Connections
        {
            get
            {
                for (var idx = OpenEnds.Count - 1; idx >= 0; --idx)
                {
                    var openEnd = OpenEnds[idx];
                    if (openEnd.ConnectionType == NetworkType.LandingPad || (openEnd.ConnectionType & ReVolt.CableTrayNetwork.ConnectionType) != NetworkType.None)
                        yield return openEnd;
                }
            }
        }

        public PseudoNetwork<ICableTrayComponent> Network { get; } = ReVolt.CableTrayNetwork.Join();

        public void PatchPrefab()
        {
            ReVolt.CableTrayNetwork.PatchConnections(this);

            ReVolt.MOD.SetupPrefabs(PrefabName)
                .SetBlueprintMaterials()
                .SetPaintableColor(ColorType.Red)
                .SetExitTool(PrefabNames.Wrench);
        }

        public int[] OpenEndsPermutation =
        {
            0,
            1,
            2,
            3,
            4,
            5
        };

        public int[] GetOpenEndsPermutation() => (int[])OpenEndsPermutation.Clone();

        public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

        public SmartRotate.ConnectionType GetConnectionType() => ConnectionType;

        public void SetOpenEndsPermutation(int[] permutation)
        {
            OpenEndsPermutation = (int[])permutation.Clone();
        }

        public void SetConnectionType(SmartRotate.ConnectionType connectionType)
        {
            ConnectionType = connectionType;
        }
    }
}