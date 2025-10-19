using Assets.Scripts.Objects;
using System.Xml.Serialization;

namespace ReVolt.Assets.Scripts
{
    [XmlInclude(typeof(CircuitBreakerSaveData))]
    public class CircuitBreakerSaveData : StructureSaveData
    {
        [XmlElement]
        public float TripPoint;

        [XmlElement]
        public float TransferredLast;

        [XmlElement]
        public int Mode;
    }
}
