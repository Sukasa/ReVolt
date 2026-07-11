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
        public MeshRenderer RendererUp;
        public MeshRenderer RendererDown;

        protected int connFlags;
        protected bool reapplyAppearance;

        protected const int FLAG_CONNECTIONS = 1024;
        
        public override void OnRegistered(Cell cell)
        {
            base.OnRegistered(cell);
            RebuildExclusions.Clear();
            ReVolt.CableTrayNetwork.RebuildNetworkCreate(this);
            UpdateJunctionConnections();
        }

        public override void OnDeregistered()
        {
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

        public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
        {
            base.BuildUpdate(writer, networkUpdateType);
            
            if (IsNetworkUpdateRequired(FLAG_CONNECTIONS, networkUpdateType))
                writer.WriteInt32(connFlags);

            
        }

        public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
        {
            base.ProcessUpdate(reader, networkUpdateType);

            if (IsNetworkUpdateRequired(FLAG_CONNECTIONS, networkUpdateType))
            {
                connFlags = reader.ReadInt32();
                reapplyAppearance = true;
            }
        }

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
            foreach (var Cable in MatchCables(Metric))
                if (Cable?.CableNetwork != null && !List.Contains(Cable.CableNetwork))
                    List.Add(Cable.CableNetwork);
        }

        public override void OnNeighborPlaced(SmallGrid neighbor)
        {
            base.OnNeighborPlaced(neighbor);
            UpdateJunctionConnections();
        }

        public override void OnNeighborRemoved(SmallGrid neighbor)
        {
            base.OnNeighborRemoved(neighbor);
            UpdateJunctionConnections();
        }

        public override void OnServerTick(float deltaTime)
        {
            UpdateEnds();
        }

        public void UpdateEnds()
        {
            if (!reapplyAppearance || OpenEnds.Count < 8)
                return;
            
            if (RendererUp != null)
                RendererUp.enabled = (connFlags & 1) == 1;

            if (RendererDown != null)
                RendererDown.enabled = (connFlags & 2) == 2;

            reapplyAppearance = false;
        }

        public override string GetStationpediaCategoryKey() => StationpediaCategoryStrings.CableCategory;
        public override string GetStationpediaCategory() => Localization.GetInterface(StationpediaCategoryStrings.CableCategory);

        private bool IsConnectedToTray(Connection OpenEnd)
        {
            var smallCell = GridController.GetSmallCell(GridController.WorldToLocalGrid(OpenEnd.Transform.position, SmallGridSize, SmallGridOffset));
            return smallCell is { Other: CableTray } && smallCell.Other.IsConnected(OpenEnd);
        }

        public void UpdateJunctionConnections()
        {
            if (!GameManager.RunSimulation || OpenEnds.Count < 8)
                return;
            
            if (GameManager.GameState == GameState.Loading)
            {
                if (!_isDeferringUpdate && OpenEnds.Count > 8)
                    DeferredUJC().Forget();
                return;
            }

            connFlags = 0;
            
            if (RendererUp != null && IsConnectedToTray(OpenEnds[8]))
                connFlags |= 1;

            if (RendererDown != null && OpenEnds.Count > 10 && IsConnectedToTray(OpenEnds[10]))
                connFlags |= 2;

            reapplyAppearance = true;
            
            if (NetworkManager.IsServer)
                NetworkUpdateFlags |= FLAG_CONNECTIONS;
        }

        private bool _isDeferringUpdate;

        private async UniTaskVoid DeferredUJC()
        {
            if (OpenEnds.Count < 8 || _isDeferringUpdate)
                return;

            _isDeferringUpdate = true;
            do
            {
                await UniTask.NextFrame();
            } while (GameManager.GameState == GameState.Loading);

            await UniTask.SwitchToMainThread();
            
            UpdateJunctionConnections();
            _isDeferringUpdate = false;
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