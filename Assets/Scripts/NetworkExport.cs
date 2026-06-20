using System;
using System.Collections.Generic;
using System.Linq;

namespace ReVolt
{
    public class NetworkExport
    {
        public class CableInfo : FuseInfo // Inherit from 'FuseInfo' because cables also have a max power limit
        {
            public List<int> Connections { get; set; } = new();
        }

        public class ThingInfo
        {
            public int RefId { get; set; }
        }

        public class DeviceInfo : ThingInfo
        {
            public string StructureTypeName { get; set; }
        }

        public class FuseInfo : ThingInfo
        {
            public int MaxVoltage { get; set; }
        }

        public Dictionary<string, List<FuseInfo>> FusesByType { get; set; }  = new();
        public Dictionary<string, List<CableInfo>> CablesByType { get; set; } = new();
        public List<DeviceInfo> Devices { get; set; } = new();

        [NonSerialized]
        private Dictionary<int, ThingInfo> _things;
        public Dictionary<int, ThingInfo> ThingsById
        {
            get
            {
                if (_things is not null)
                    return _things;
                
                _things = new();
                foreach (var fuse in FusesByType.Values.SelectMany(x => x))
                    _things.Add(fuse.RefId, fuse);
                    
                foreach (var cable in CablesByType.Values.SelectMany(x => x))
                    _things.Add(cable.RefId, cable);
                    
                foreach (var device in Devices)
                    _things.Add(device.RefId, device);

                return _things;
            }
        }
    }
}