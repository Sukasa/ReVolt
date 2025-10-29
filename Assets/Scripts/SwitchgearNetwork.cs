using Assets.Scripts.Objects;
using LibConstruct;
using System.Collections.Generic;

namespace ReVolt
{
    public class SwitchgearNetwork : IPseudoNetworkMember<SwitchgearNetwork>
    {
        public IEnumerable<Connection> Connections => throw new System.NotImplementedException();

        public PseudoNetwork<SwitchgearNetwork> Network => throw new System.NotImplementedException();

        public string DisplayName => "Switchgear Network";

        public ushort NetworkUpdateFlags { get; set; }
        public long ReferenceId { get; set; }
        public bool BeingDestroyed { get; set; }

        public void OnAssignedReference()
        {

        }

        public void OnMemberAdded(SwitchgearNetwork member)
        {
        }

        public void OnMemberRemoved(SwitchgearNetwork member)
        {
        }

        public void OnMembersChanged()
        {
        }

        public void PrintDebugInfo(bool verbose = false)
        {
        }
    }
}
