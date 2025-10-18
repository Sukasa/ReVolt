using Assets.Scripts.Networks;
using static ReVolt.RevoltTick;

namespace ReVolt.Interfaces
{
    public interface ILoadController
    {
        public bool InterfaceConflict { get; set; }

        public PowerUsage[] LoadControlData { set; }

        public bool IsInterfaceFor(CableNetwork Network);

        public void OnRevoltPowerTick(RevoltTick Tick);
    }
}
