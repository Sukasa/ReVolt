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
using System.Linq;
using Assets.Scripts.Objects.Motherboards;
using LaunchPadBooster.Utils;
using UnityEngine;
using static ReVolt.Interfaces.ISwitchgearComponent;

namespace ReVolt.Prefabs
{
    public class HeavyBreaker : CircuitBreaker, ISwitchgearComponent
    {
        public SwitchgearComponentType ComponentType => SwitchgearComponentType.Breaker;
        
        
        public void OnBusConnectionChanged(Device BusTie)
        {
            // TOOD check that the bus tie being updated is one we're linked to, so we don't waste CPU cycles (more than we already are...)
            CheckConnections();
        }

        PseudoNetwork<ISwitchgearComponent> IPseudoNetworkMember<ISwitchgearComponent>.Network { get; } = ReVolt.SwitchgearNetwork.Join();

        private List<long> PowerEntries;
        private List<long> DataEntries;

        private bool DirtyLists = true;

        private readonly int[] ConnectionIndices = new int[3] { 0, 0, 0 };

        private static readonly float[] SettingOffsets = new float[4] { -10000.0f, -1000.0f, 1000.0f, 10000.0f };
        private static readonly string[] ConnectionNames = new string[3] { "Input", "Output", "Data" };
        public GameObject Buttons;

        public InteractableAnimComponent[] AnimationComponents;

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

        protected override void OnBuildStateUpdated(int newState, int previousState)
        {
            base.OnBuildStateUpdated(newState, previousState);
            Buttons.SetActive(newState > 0);
        }

        protected override void RefreshAnimState(bool skipAnimation = false)
        {
            base.RefreshAnimState(skipAnimation);
            for (int i = 0; i < AnimationComponents.Length; i++)
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

        public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
        {
            var action = new DelayedActionInstance
            {
                Duration = 0f,
                ActionMessage = interactable.ContextualName,
            };

            switch (interactable.Action)
            {
                case InteractableType.Slot1:
                case InteractableType.Slot2:
                    if (!interaction.SourceSlot.Contains<Screwdriver>())
                        return action.Fail(GameStrings.RequiresScrewdriver);

                    if (InteractOpen.State > 0)
                        return action.Fail(ReVoltStrings.RevoltDoorMustBeClosed);

                    if (!doAction)
                        return action.Succeed();


                    interactable.State = 1 - interactable.State;

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
                    int adjIdx = interactable.Action - InteractableType.Button9;
                    float by = SettingOffsets[adjIdx];
                    float newSetting = Mathf.Clamp((float)Setting + by, 1000.0f, ReVolt.heavyBreakerMaxTripSetting.Value);

                    action.AppendStateMessage(GameStrings.GlobalChangeSettingTo, newSetting.ToString());

                    if (!doAction)
                        return action.Succeed();

                    ConsoleWindow.PrintAction($"Trip Point: {Setting} => {newSetting}, adjIdx: {adjIdx}");

                    Setting = newSetting;
                    return action;

                default:
                    var t = base.InteractWith(interactable, interaction, doAction);

                    if (doAction)
                        ConsoleWindow.PrintAction($"{interactable.DisplayName} fired in base");

                    return t;
            }
        }

        private ISwitchgearComponent ThingByRefId(long ReferenceId) => ReVolt.SwitchgearNetwork.MemberNetwork(this).Members.FirstOrDefault(x => x.ReferenceId == ReferenceId);

        private string NameByRefId(long ReferenceId) => ThingByRefId(ReferenceId)?.DisplayName;

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
        }

        // This is almost certainly buggy somewhere, since it's not tested yet.
        // This function is expanded in order to "patch" all the cable networks it's indirectly connected to.
        // It's probably going to break somehow, but I'll deal with it.
        // This works i the breaker is set up afterwards.  I don't know how it's going to work
        // if/when cables are adjusted on the bus tie.
        // I may want to rework this to just grab the cable(s) from the bus tie(s) every tick?
        protected override void CheckConnections()
        {
            // The "as" checks should always pass, but be robust anyways
            Device inputDevice = ThingByRefId(ConnectionRefIds[0]) as Device;
            Device outputDevice = ThingByRefId(ConnectionRefIds[1]) as Device;
            Device dataDevice = ThingByRefId(ConnectionRefIds[2]) as Device;

            var previousNetworks = ConnectedCableNetworks.ToList(); // Clone list for re-use after

            AttachedCables.Clear();
            ConnectedCableNetworks.Clear();

            if (inputDevice != null)
            {
                InputConnection = inputDevice.OpenEnds[0];
                InputNetwork = inputDevice.PowerCable != null ? inputDevice.PowerCable.CableNetwork : null;
                AttachedCables.Add(inputDevice.PowerCable);
                ConnectedCableNetworks.Add(InputNetwork);

                if (InputNetwork != null && !previousNetworks.Contains(InputNetwork))
                    InputNetwork.AddDevice(inputDevice.PowerCable, this);
            }
            else
            {
                InputNetwork = null;
                InputConnection = new(this);
            }

            if (outputDevice != null)
            {
                OutputConnection = outputDevice.OpenEnds[0];
                OutputNetwork = outputDevice.PowerCable != null ? outputDevice.PowerCable.CableNetwork : null;
                AttachedCables.Add(outputDevice.PowerCable);
                ConnectedCableNetworks.Add(OutputNetwork);

                if (OutputNetwork != null && !previousNetworks.Contains(OutputNetwork))
                    OutputNetwork.AddDevice(outputDevice.PowerCable, this);
            }
            else
            {
                OutputNetwork = null;
                OutputConnection = new(this);
            }

            if (dataDevice != null)
            {
                DataCable = dataDevice.DataCable;
                if (DataCable != null)
                {
                    AttachedCables.Add(DataCable);
                    ConnectedCableNetworks.Add(DataCable.CableNetwork);

                    if (!previousNetworks.Contains(DataCable.CableNetwork))
                        DataCable.CableNetwork.AddDevice(DataCable, this);
                }
            }
            else
                DataCable = null;

            foreach (var Network in previousNetworks)
                if (!ConnectedCableNetworks.Contains(Network))
                    Network.RemoveDevice(this);
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