using Assets.Scripts.Networks;

namespace ReVolt.Interfaces
{
    public interface IBreaker
    {
        float LimitCurrent { get; }

        void Trip();

        bool CanSupplyPower(CableNetwork cableNetwork);
    }
}