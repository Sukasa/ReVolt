using Assets.Scripts.Networks;

namespace ReVolt
{
    public interface IBreaker
    {
        float LimitCurrent { get; }

        void Trip();

        bool CanSupplyPower(CableNetwork cableNetwork);
    }
}