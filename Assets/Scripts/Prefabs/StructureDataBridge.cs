using System;
using System.Collections.Generic;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using LaunchPadBooster.Utils;
using Objects.Pipes;
using ReVolt.Interfaces;

namespace ReVolt.Prefabs
{
    public class StructureDataBridge : LogicUnitBase, IPatchable
    {
        private bool _updateFlag;
        public bool IsOneWay;
        
        private readonly List<Device>[] _cachedLists = new List<Device>[4];
        
        public override void OnNetworkChange()
        {
            _updateFlag = true;
        }

        public override bool ShowStateTooltip => false;

        public unsafe List<Device> GetBridgedDevices(CableNetwork excludeNetwork)
        {
            Span<SmallCellRef> cables = stackalloc SmallCellRef[4];
            Span<long> seenNetworks = stackalloc long[4];
            var count = 0;
            FillConnected<Cable>(cables, ref count);
            var span = cables[..count];
            var cacheIndex = -1;
            var seenIdx = 0;
            
            
            
            for (var index = 0; index < span.Length; ++index)
                if (cables[index].Get<Cable>().CableNetwork == excludeNetwork)
                    cacheIndex = index;

            List<Device> Devices = new();
            
            if (IsOneWay && cacheIndex == 0) // For the data diode, input 0 cannot see any other networks
                return Devices;
            
            if (cacheIndex >= 0 && _cachedLists[cacheIndex] != null)
                return _cachedLists[cacheIndex];
            
            for (var index = 0; index < span.Length; ++index)
            {
                var cable = span[index].Get<Cable>();
                if (cable.CableNetwork is not { } cableNetwork || cableNetwork == excludeNetwork)
                    continue;
                
                for(var sidx = 0; sidx < seenIdx; sidx++)
                    if (seenNetworks[sidx] == cableNetwork.ReferenceId)
                        goto already_added; // continue outer for if we've seen the same CableNetwork twice
                
                seenNetworks[seenIdx++] = cableNetwork.ReferenceId;
                Devices.AddRange(cableNetwork.DataDeviceList);
                Devices.Remove(this);
                    
                already_added: ;
            }
            
            if (cacheIndex >= 0)
                _cachedLists[cacheIndex] = Devices;
            
            return Devices;
        }
        
        public override unsafe void OnPowerTick()
        {
            if (_updateFlag)
            {
                for (var i = 0; i < 4; i++)
                    _cachedLists[i] = null;
                
                // Trigger all connected LogicUnitBase devices on all networks to refresh their device lists
                Span<SmallCellRef> cables = stackalloc SmallCellRef[4];
                var count = 0;
                FillConnected<Cable>(cables, ref count);
                var span = cables[..count];
                for (var index = 0; index < span.Length; ++index)
                {
                    var _list = span[index].Get<Cable>().CableNetwork.DataDeviceList;
                    for (var i = _list.Count - 1; i >= 0; --i)
                    {
                        switch (_list[i])
                        {
                            case LogicUnitBase device:
                                device.OnNetworkChange();
                                break;
                            case DeviceInputOutputCircuit device2:
                                device2.OnNetworkedRefresh(null);
                                break;
                            case DeviceInputOutputImportExportCircuit device3:
                                device3.OnNetworkedRefresh(null);
                                break;
                            case DeviceInputOutputImportCircuit device4:
                                device4.OnNetworkedRefresh(null);
                                break;
                            // TODO identify (mostly via testing) if there are other device types that need to be refreshed
                        }
                    }
                }

                _updateFlag = false;   
            }
            base.OnPowerTick();
        }
        
        public void PatchPrefab()
        {
            ReVolt.MOD.SetupPrefabs(PrefabName)
                .SetBlueprintMaterials()
                .SetPaintableColor(ColorType.Black)
                .AddToMultiConstructorKit("ItemKitLogicInputOutput")
                .SetEntryTool("ItemKitLogicInputOutput")
                .SetExitTool(PrefabNames.Drill);
        }
    }
}