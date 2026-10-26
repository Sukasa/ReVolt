using Assets.Scripts.Networks;

namespace ReVolt.Interfaces
{
    public interface IBreaker
    {
        /// <summary>Limit power (in Watts), or <c>null</c> if not expressible in Watts.</summary>
        float? LimitCurrent { get; }

        void Trip();

        bool CanSupplyPower(CableNetwork cableNetwork);
    }
}