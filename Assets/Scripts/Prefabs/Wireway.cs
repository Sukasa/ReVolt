using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using LaunchPadBooster.Utils;
using LibConstruct;
using ReVolt.Interfaces;
using static ReVolt.Interfaces.ISwitchgearComponent;

namespace ReVolt
{
    public class Wireway : SmallGrid, ISwitchgearComponent, IPatchable
    {
        public void PatchPrefab()
        {
            ReVolt.SwitchgearNetwork.PatchConnections(this);
            ReVolt.MOD.SetupPrefabs(PrefabName)
                .SetBlueprintMaterials()
                .SetPaintableColor(ColorType.Red)
                .SetExitTool(PrefabNames.Drill);
        }
        
        public void OnMembersChanged()
        {
        }
        
        public override string GetStationpediaCategory()
        {
            return Localization.GetInterface(StationpediaCategoryStrings.CableCategory);
        }

        public IEnumerable<Connection> Connections // Literally just Tom's reference code because that's all I actually need right now
        {
            get
            {
                foreach (var openEnd in OpenEnds)
                    if (openEnd.ConnectionType == NetworkType.LandingPad || openEnd.ConnectionType == ReVolt.SwitchgearNetwork.ConnectionType)
                        yield return openEnd;
            }
        }
        
        PseudoNetwork<ISwitchgearComponent> IPseudoNetworkMember<ISwitchgearComponent>.Network { get; } = ReVolt.SwitchgearNetwork.Join();

        public void OnMemberAdded(ISwitchgearComponent member)
        {
        }

        public void OnMemberRemoved(ISwitchgearComponent member)
        {
        }

        public override void OnRegistered(Cell cell)
        {
            base.OnRegistered(cell);
            ReVolt.SwitchgearNetwork.RebuildNetworkCreate(this);
        }

        public override void OnDeregistered()
        {
            base.OnDeregistered();
            ReVolt.SwitchgearNetwork.RebuildNetworkDestroy(this);
        }

        public SwitchgearComponentType ComponentType => SwitchgearComponentType.Decorative;
        
        public void OnBusConnectionChanged(Device BusTie)
        {
        }
    }
}