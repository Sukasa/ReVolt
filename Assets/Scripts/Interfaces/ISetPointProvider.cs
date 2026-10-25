using Assets.Scripts.Objects.Pipes;

namespace ReVolt.Interfaces
{
    public interface ISetPointProvider<in TDevice, out TSetPoint>
        where TDevice : Device
        where TSetPoint : SetPoint
    {
        TSetPoint CreateSetPoint(TDevice parent);
    }
}
