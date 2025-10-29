using Assets.Scripts.Objects;
using LibConstruct;
using ReVolt.Interfaces;
using System;
using System.Collections.Generic;
using static ReVolt.Interfaces.ISwitchgearComponent;

namespace ReVolt.Prefabs
{
    public class HeavyBreaker : CircuitBreaker, ISwitchgearComponent
    {
        public static PseudoNetworkType<SwitchgearNetwork> SwitchgearNetwork = new();

        public SwitchgearComponentType ComponentType => SwitchgearComponentType.Breaker;

        PseudoNetwork<ISwitchgearComponent> IPseudoNetworkMember<ISwitchgearComponent>.Network { get; }

        public IEnumerable<Connection>  Connections
        {
            get
            {
                foreach (var openEnd in this.OpenEnds)
                    // set connections to LandingPad type in unity to mark them for replacement with new connection type
                    if (openEnd.ConnectionType == NetworkType.LandingPad || openEnd.ConnectionType == MyNetworkType.ConnectionType)
                        yield return openEnd;
            }
        }

        public void OnMembersChanged()
        {

        }

        void IPseudoNetworkMember<ISwitchgearComponent>.OnMemberAdded(ISwitchgearComponent member)
        {
            
        }

        void IPseudoNetworkMember<ISwitchgearComponent>.OnMemberRemoved(ISwitchgearComponent member)
        {
            
        }
    }
}
