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
    }
}