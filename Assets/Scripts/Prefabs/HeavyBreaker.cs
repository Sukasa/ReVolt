using Assets.Scripts.Objects;
using LibConstruct;
using ReVolt.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using static ReVolt.Interfaces.ISwitchgearComponent;

namespace ReVolt.Prefabs
{
    public class HeavyBreaker : CircuitBreaker, ISwitchgearComponent
    {
        public static PseudoNetworkType<ISwitchgearComponent> SwitchgearNetwork = new();

        public SwitchgearComponentType ComponentType => SwitchgearComponentType.Breaker;

        PseudoNetwork<ISwitchgearComponent> IPseudoNetworkMember<ISwitchgearComponent>.Network { get; }

        private List<long> PowerEntries;
        private List<long> DataEntries;

        private bool DirtyLists = true;

        private readonly int[] ConnectionIndices = new int[3] { 0, 0, 0 };

        private static readonly float[] SettingOffsets = new float[4] { -10000.0f, -1000.0f, 1000.0f, 10000.0f };

        public IEnumerable<Connection> Connections
        {
            get
            {
                foreach (var openEnd in OpenEnds)
                    if (openEnd.ConnectionType == NetworkType.LandingPad || openEnd.ConnectionType == SwitchgearNetwork.ConnectionType)
                        yield return openEnd;
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
                case InteractableType.Button6: // Cycle Input Conn.
                case InteractableType.Button7: // Cycle Output Conn.
                case InteractableType.Button8: // Cycle Data Conn.
                    UpdateEntryLists();

                    int conIdx = (int)interactable.Action - (int)InteractableType.Button6;
                    int dir = 1;
                    if (KeyManager.GetButton(KeyMap.QuantityModifier))
                        dir = -1;

                    List<long> conList = interactable.Action == InteractableType.Button8 ? DataEntries : PowerEntries;
                    int nextIdx = (conIdx + dir + conList.Count) % conList.Count;

                    if (!doAction)
                        return action.Fail("Not Implemented");



                    return action.Fail("Not Implemented");

                case InteractableType.Button9:  // Setting buttons
                case InteractableType.Button10:
                case InteractableType.Button11:
                case InteractableType.Button12:
                    int adjIdx = (int)interactable.Action - (int)InteractableType.Button9;
                    float by = SettingOffsets[adjIdx];

                    return action.Fail("Not Implemented");

                // TODO cases for the four setting buttons

                default:
                    return base.InteractWith(interactable, interaction, doAction);
            }
        }

        private void UpdateEntryLists(bool Force = false) { 
            if (Force || !DirtyLists)
                return;
            DirtyLists = false;

            PowerEntries = SwitchgearNetwork.MemberNetwork(this).Members.Where(x => x.ComponentType == SwitchgearComponentType.Power).Select(x => x.ReferenceId).ToList();
            DataEntries = SwitchgearNetwork.MemberNetwork(this).Members.Where(x => x.ComponentType == SwitchgearComponentType.Data).Select(x => x.ReferenceId).ToList();
            ConnectionIndices[0] = Math.Max(PowerEntries.IndexOf(ConnectionRefIds[0]), 0);
            ConnectionIndices[1] = Math.Max(DataEntries.IndexOf(ConnectionRefIds[1]), 0);
            ConnectionIndices[2] = Math.Max(PowerEntries.IndexOf(ConnectionRefIds[2]), 0);
        }

        private void UpdateConnections()
        {

        }

        protected override void CheckConnections()
        {

        }

        public void OnMembersChanged()
        {
            DirtyLists = true;
        }

        public void OnMemberAdded(ISwitchgearComponent member)
        {

        }

        public void OnMemberRemoved(ISwitchgearComponent member)
        {
            if (ConnectionRefIds.Contains(member.ReferenceId))
            {
                UpdateEntryLists(true);
                UpdateConnections();   
            }
        }
    }
}
