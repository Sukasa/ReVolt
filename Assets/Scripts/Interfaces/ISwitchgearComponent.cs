using Assets.Scripts.Objects.Pipes;
using LibConstruct;

namespace ReVolt.Interfaces
{
    public interface ISwitchgearComponent : IPseudoNetworkMember<ISwitchgearComponent>
    {
        public enum SwitchgearComponentType
        {
            Breaker,
            Decorative,
            Power,
            Data
        }

        public SwitchgearComponentType ComponentType { get; }

        public void OnBusConnectionChanged(Device BusTie);
    }
}