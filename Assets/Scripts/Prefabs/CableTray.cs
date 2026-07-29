using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using LaunchPadBooster.Utils;
using LibConstruct;
using ReVolt.Interfaces;
using UnityEngine;

namespace ReVolt
{
    public class CableTray : SmallSingleGrid, ICableTrayComponent, IPatchable, ISmartRotatable
    {
        public static readonly List<CableTray> AllTrays = new(); // Master list used for the meson scanners as PseudoNetworks are not tracked globally
        
        public override void OnRegistered(Cell cell)
        {
            base.OnRegistered(cell);
            AllTrays.Add(this);
            RebuildExclusions.Clear();
            ReVolt.CableTrayNetwork.RebuildNetworkCreate(this);
        }

        public override void OnDeregistered()
        {
            AllTrays.Remove(this);
            base.OnDeregistered();
            RebuildExclusions.Clear();
            ReVolt.CableTrayNetwork.RebuildNetworkDestroy(this);
        }

        public void OnMemberAdded(ICableTrayComponent member)
        {
            RebuildExclusions.Clear();
        }

        public void OnMemberRemoved(ICableTrayComponent member)
        {
            RebuildExclusions.Clear();
            Span<SmallCellRef> buf = stackalloc SmallCellRef[32];
            var count = 0;
            FillConnected<Cable>(buf, ref count);
            var span = buf[..count];
            for (var index = 0; index < span.Length; ++index)
            {
                var cable = span[index].Get<Cable>();
                cable.CableNetwork?.Remove(cable);
                cable.CableNetwork = null;
            }
        }

        private static readonly HashSet<int> RebuildExclusions = new();
        

        public void OnMembersChanged()
        {
            // If sim not running, cable networks will be rebuilt externally later
            if (GameManager.GameState == GameState.Loading || !GameManager.RunSimulation)
                return;

            Span<SmallCellRef> buf = stackalloc SmallCellRef[32];
            var count = 0;
            FillConnected<Cable>(buf, ref count);
            var span = buf[..count];


            for (var index = 0; index < span.Length; ++index)
            {
                var cable = span[index].Get<Cable>();

                var col = GameManager.GetColorIndex(cable.CustomColor);
                var volt = (int)cable.MaxVoltage;

                if (!RebuildExclusions.Add(col + volt * 64))
                    continue;

                CableNetwork.RebuildCableNetworkServer(span[index]);
            }
        }

        public List<Cable> MatchCables(Cable Metric)
        {
            var Result = new List<Cable>();

            var wantColour = GameManager.GetColorIndex(Metric.CustomColor);
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
                    var cableCol = GameManager.GetColorIndex(Cable.CustomColor);

                    if (Cable != Metric &&
                        Mathf.Approximately(Cable.MaxVoltage, Metric.MaxVoltage) &&
                        cableCol == wantColour &&
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

        private bool IsConnectedToTray(Connection OpenEnd)
        {
            var smallCell = GridController.GetSmallCell(GridController.WorldToLocalGrid(OpenEnd.Transform.position, SmallGridSize, SmallGridOffset));
            return smallCell is { Other: CableTray } && smallCell.Other.IsConnected(OpenEnd);
        }

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