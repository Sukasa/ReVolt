using Assets.Scripts.Networks;

namespace ReVolt.Interfaces
{
    public interface IBreaker
    {
        /// <summary>Limit power (in Watts), or <c>null</c> if not expressible in Watts.</summary>
        float? LimitPower { get; }

        /// <summary>Limit current (in <see cref="LimitUnits"/>)</summary>
        float LimitCurrent { get; }
        /// <summary>Native units the breaker is limited in.</summary>
        string LimitUnits { get; }

        void Trip();

        bool CanSupplyPower(CableNetwork cableNetwork);
    }
}