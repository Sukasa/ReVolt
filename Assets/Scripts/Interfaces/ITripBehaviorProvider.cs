using Assets.Scripts.Objects.Pipes;

namespace ReVolt.Interfaces
{
    public interface ITripBehaviorProvider<in TDevice, out TTripBehavior>
        where TDevice : Device
        where TTripBehavior : TripBehavior
    {
        TTripBehavior CreateTripBehavior(TDevice parent);
    }
}
