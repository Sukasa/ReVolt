using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using LibConstruct;
using ReVolt.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Motherboards;
using LaunchPadBooster.Utils;
using UnityEngine;
using static ReVolt.Interfaces.ISwitchgearComponent;

namespace ReVolt.Prefabs
{
    public class HeavyBreaker : CircuitBreaker, ISwitchgearComponent
    {
        public SwitchgearComponentType ComponentType => SwitchgearComponentType.Breaker;


        [ReadOnly] public CableNetwork DataNetwork;

        public void OnBusConnectionChanged(Device BusTie)
        {
            // TOOD check that the bus tie being updated is one we're linked to, so we don't waste CPU cycles (more than we already are...)
            CheckConnections();
        }

        PseudoNetwork<ISwitchgearComponent> IPseudoNetworkMember<ISwitchgearComponent>.Network { get; } = ReVolt.SwitchgearNetwork.Join();

        private List<long> PowerEntries;
        private List<long> DataEntries;

        private bool DirtyLists = true;
        private bool _hasRepatched = false;

        private readonly int[] ConnectionIndices = new int[3] { 0, 0, 0 };

        private static readonly float[] SettingOffsets = new float[4] { -10000.0f, -1000.0f, 1000.0f, 10000.0f };
        private static readonly string[] ConnectionNames = new string[3] { "Input", "Output", "Data" };

        public InteractableAnimComponent[] AnimationComponents;

        protected override void OnBuildStateUpdated(int newState, int previousState)
        {
            base.OnBuildStateUpdated(newState, previousState);
            _breakerStateAnimator?.SetActive(newState > 0);
        }

        public override void OnFinishedLoad()
        {
            base.OnFinishedLoad();
            _breakerStateAnimator?.SetActive(CurrentBuildStateIndex > 0);
        }

        public override void PatchPrefab()
        {
            ReVolt.SwitchgearNetwork.PatchConnections(this);
            ReVolt.MOD.SetupPrefabs(PrefabName)
                .SetBlueprintMaterials()
                .SetPaintableColor(ColorType.Red)
                .SetExitTool(PrefabNames.Drill)
                .SetEntryTool(PrefabNames.Screwdriver, 1)
                .SetEntry2Tool(PrefabNames.ElectronicParts, 1)
                .SetExitTool(PrefabNames.Drill, 1);
        }

        protected override void RefreshAnimState(bool skipAnimation = false)
        {
            base.RefreshAnimState(skipAnimation);
            _breakerStateAnimator?.SetActive(CurrentBuildStateIndex > 0);
            for (var i = AnimationComponents.Length - 1; i >= 0; i--)
            {
                AnimationComponents[i].RefreshState(skipAnimation);
            }
        }

        public IEnumerable<Connection> Connections // Literally just Tom's reference code because that's all I actually need right now
        {
            get
            {
                foreach (var openEnd in OpenEnds)
                    if (openEnd.ConnectionType == NetworkType.LandingPad || openEnd.ConnectionType == ReVolt.SwitchgearNetwork.ConnectionType)
                        yield return openEnd;
            }
        }

        public override string GetContextualName(Interactable interactable)
        {
            switch (interactable.Action)
            {
                case InteractableType.Button6:
                case InteractableType.Button7:
                case InteractableType.Button8: // For connections, the contextual name is just the current value
                    return
                        $"{ConnectionNames[interactable.Action - InteractableType.Button6]} Connection: {NameByRefId(ConnectionRefIds[interactable.Action - InteractableType.Button6]) ?? "Not Connected"}";

                case InteractableType.Button9: // Setting buttons show the current setting
                case InteractableType.Button10:
                case InteractableType.Button11:
                case InteractableType.Button12:
                    return $"Trip Point: {Setting}";

                default:
                    return base.GetContextualName(interactable);
            }
        }

        public override void OnPowerTick()
        {
            if (!_hasRepatched)
            {
                CheckConnections();
                _hasRepatched = true;
            }

            base.OnPowerTick();
        }

        public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
        {
            var action = new DelayedActionInstance
            {
                Duration = 0f,
                ActionMessage = interactable.ContextualName,
            };

            switch (interactable.Action)
            {
                case InteractableType.Mode:
                    if (!doAction)
                        return action.Succeed();


                    return action;

                case InteractableType.Slot1:
                case InteractableType.Slot2:
                    if (!interaction.SourceSlot.Contains<Screwdriver>())
                        return action.Fail(GameStrings.RequiresScrewdriver);

                    if (InteractOpen.State > 0)
                        return action.Fail(ReVoltStrings.RevoltDoorMustBeClosed);

                    if (!doAction)
                        return action.Succeed();


                    interactable.State = 1 - interactable.State;

                    if (NetworkManager.IsServer)
                        NetworkUpdateFlags |= FLAG_LOCKINGBOLTS;

                    ConsoleWindow.PrintAction($"{interactable.Action} now {interactable.State}");
                    return action;

                case InteractableType.Open:

                    if (GetInteractable(InteractableType.Slot1).State != 1 || GetInteractable(InteractableType.Slot2).State != 1)
                        return action.Fail(ReVoltStrings.RevoltDoorMustBeUnlocked);

                    if (!doAction)
                        return action.Succeed();

                    interactable.State = 1 - interactable.State;

                    return action;

                case InteractableType.Button6: // Cycle Input Conn.
                case InteractableType.Button7: // Cycle Output Conn.
                case InteractableType.Button8: // Cycle Data Conn.
                    UpdateEntryLists();

                    if (!interaction.SourceSlot.Contains<Screwdriver>())
                        return action.Fail(GameStrings.RequiresScrewdriver);

                    // We get which connection from the interactable type (they're in linear contiguous order), then figure out the next in the list.
                    // Then display and/or apply it
                    int conIdx = interactable.Action - InteractableType.Button6;
                    int dir = 1;
                    if (KeyManager.GetButton(KeyMap.QuantityModifier))
                        dir = -1;
                    else
                        action.ExtendedMessage = ReVoltStrings.HoldForPreviousTrip;

                    List<long> conList = conIdx == 2 ? DataEntries : PowerEntries;

                    if (conList.Count == 0)
                        return action.Fail(ReVoltStrings.HeavyBreakerNoConnectionAvailable);

                    int nextIdx = (ConnectionIndices[conIdx] + dir + conList.Count) % conList.Count;

                    var newName = NameByRefId(conList[nextIdx]);
                    action.AppendStateMessage(GameStrings.GlobalChangeSettingTo, newName);

                    if (!doAction)
                        return action.Succeed();

                    ConnectionIndices[conIdx] = nextIdx;
                    ConnectionRefIds[conIdx] = conList[nextIdx];
                    CheckConnections();

                    // We should probably let clients know?
                    if (NetworkManager.IsServer)
                        NetworkUpdateFlags |= FLAG_CONNECTIONS;

                    return action;

                case InteractableType.Button9: // Setting buttons
                case InteractableType.Button10:
                case InteractableType.Button11:
                case InteractableType.Button12:
                    var adjIdx = interactable.Action - InteractableType.Button9;
                    var by = SettingOffsets[adjIdx];
                    var newSetting = Mathf.Clamp((float)Setting + by, 1000.0f, ReVolt.heavyBreakerMaxTripSetting.Value);

                    action.AppendStateMessage(GameStrings.GlobalChangeSettingTo, newSetting.ToString());

                    if (!doAction)
                        return action.Succeed();

                    ConsoleWindow.PrintAction($"Trip Point: {Setting} => {newSetting}, adjIdx: {adjIdx}");

                    Setting = newSetting;
                    return action;

                default:
                    return base.InteractWith(interactable, interaction, doAction);
            }
        }

        private ISwitchgearComponent ThingByRefId(long RefId) => ReVolt.SwitchgearNetwork.MemberNetwork(this).Members.FirstOrDefault(x => x.ReferenceId == RefId);

        private string NameByRefId(long RefId) => ThingByRefId(RefId)?.DisplayName;

        private void UpdateEntryLists(bool Force = false)
        {
            if (!Force && !DirtyLists)
                return;

            DirtyLists = false;

            PowerEntries = ReVolt.SwitchgearNetwork.MemberNetwork(this).Members.Where(x => x.ComponentType == SwitchgearComponentType.Power).Select(x => x.ReferenceId).ToList();
            DataEntries = ReVolt.SwitchgearNetwork.MemberNetwork(this).Members.Where(x => x.ComponentType == SwitchgearComponentType.Data).Select(x => x.ReferenceId).ToList();
            ConnectionIndices[0] = Math.Max(PowerEntries.IndexOf(ConnectionRefIds[0]), 0);
            ConnectionIndices[1] = Math.Max(PowerEntries.IndexOf(ConnectionRefIds[1]), 0);
            ConnectionIndices[2] = Math.Max(DataEntries.IndexOf(ConnectionRefIds[2]), 0);

            CheckConnections();
        }

        public override void OnAddCableNetwork(CableNetwork newNetwork)
        {
            // Stub out to avoid recursing into CheckConnections
        }

        public override void OnRemoveCableNetwork(CableNetwork newNetwork)
        {
            // Stub out to avoid recursing into CheckConnections
        }


        protected override void CheckConnections()
        {
            Device inputDevice = ThingByRefId(ConnectionRefIds[0]) as Device;
            Device outputDevice = ThingByRefId(ConnectionRefIds[1]) as Device;
            Device dataDevice = ThingByRefId(ConnectionRefIds[2]) as Device;

            var previousNetworks = ConnectedCableNetworks.ToList();

            AttachedCables.Clear();
            ConnectedCableNetworks.Clear();
            PowerCables.Clear();
            DataCables.Clear();

            if (inputDevice != null)
            {
                var InPowerCable = inputDevice.PowerCable;
                if (InPowerCable != null)
                {
                    InputNetwork = InPowerCable.CableNetwork;

                    AttachedCables.Add(InPowerCable);
                    ConnectedCableNetworks.Add(InputNetwork);
                    PowerCables.Add(InPowerCable);

                    if (InputNetwork != null && !previousNetworks.Contains(InputNetwork))
                        InputNetwork.AddDevice(InPowerCable, this);
                }
                else
                {
                    InputNetwork = null;
                }
            }
            else
            {
                InputNetwork = null;
            }

            if (outputDevice != null)
            {
                var OutpowerCable = outputDevice.PowerCable;

                if (OutpowerCable != null)
                {
                    OutputNetwork = OutpowerCable.CableNetwork;
                    AttachedCables.Add(OutpowerCable);
                    PowerCables.Add(OutpowerCable);
                    ConnectedCableNetworks.Add(OutputNetwork);

                    if (OutputNetwork != null && !previousNetworks.Contains(OutputNetwork))
                        OutputNetwork.AddDevice(outputDevice.PowerCable, this);
                }
                else
                {
                    OutputNetwork = null;
                }
            }
            else
            {
                OutputNetwork = null;
            }

            if (dataDevice != null)
            {
                DataCable = dataDevice.DataCable;
                if (DataCable != null)
                {
                    AttachedCables.Add(DataCable);
                    DataCables.Add(DataCable);
                    DataNetwork = DataCable.CableNetwork;
                    ConnectedCableNetworks.Add(DataNetwork);

                    if (DataNetwork != null && !previousNetworks.Contains(DataNetwork))
                        DataNetwork.AddDevice(DataCable, this);
                }
                else
                {
                    DataNetwork = null;
                }
            }
            else
            {
                DataCable = null;
                DataNetwork = null;
            }

            for (var i = previousNetworks.Count - 1; i >= 0; i--)
                if (previousNetworks[i] != null && !ConnectedCableNetworks.Contains(previousNetworks[i]))
                    previousNetworks[i].RemoveDevice(this);
        }

        public void OnMembersChanged()
        {
            DirtyLists = true;
        }

        public void OnMemberAdded(ISwitchgearComponent member)
        {
            // Do nothing
        }

        public void OnMemberRemoved(ISwitchgearComponent member)
        {
            int idx = Array.IndexOf(ConnectionRefIds, member.ReferenceId);
            if (idx > -1)
            {
                ConnectionRefIds[idx] = 0;
                ConnectionIndices[idx] = 0;

                UpdateEntryLists(true);
                CheckConnections();
            }
        }

        public override void OnRegistered(Cell cell)
        {
            base.OnRegistered(cell);
            ReVolt.SwitchgearNetwork.RebuildNetworkCreate(this);
        }

        public override void OnDeregistered()
        {
            base.OnDeregistered();
            ReVolt.SwitchgearNetwork.RebuildNetworkDestroy(this);
        }
    }
}