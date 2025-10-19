using Assets.Scripts.Networks;
using static ReVolt.RevoltTick;

namespace ReVolt.Interfaces
{
    public interface ILoadCenter
    {
        public bool HasConflict { get; set; }

        public PowerUsage[] LoadControlData { set; }

        public bool EnableLights { get; }
        public bool EnableDoors { get; }
        public bool EnableAtmos { get; }
        public bool EnableEquip { get; }
        public bool EnableLogic { get; }
    }
}
