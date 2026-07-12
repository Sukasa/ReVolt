using Assets.Scripts.Objects.Motherboards;
using ReVolt.Interfaces;
using StationeersObjects = Assets.Scripts.Objects;

namespace ReVolt
{
    public class SingleConstructor : StationeersObjects.Constructor, IDefaultColour
    {
        public ColorType DefaultColour;
        
        public ColorType DefaultColor => DefaultColour;
        // Forward the base game type so I can use it in my ItemKits
    }
}
