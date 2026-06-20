using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using LibConstruct;
using ReVolt.Interfaces;
using System.Collections.Generic;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Motherboards;
using LaunchPadBooster.Utils;
using static ReVolt.Interfaces.ISwitchgearComponent;
namespace ReVolt
{
    public class BusTie : Device, ISwitchgearComponent, IPatchable
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

        public void OnMemberAdded(ISwitchgearComponent member)
        {
            // Do nothing
        }

        public void OnMemberRemoved(ISwitchgearComponent member)
        {
            
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

        public override void OnAddCableNetwork(CableNetwork newNetwork)
        {
            base.OnAddCableNetwork(newNetwork);
            foreach (var switchgearComponent in ReVolt.SwitchgearNetwork.MemberNetwork(this).Members)
            {
                switchgearComponent.OnBusConnectionChanged(this);
            }
        }

        public override void OnRemoveCableNetwork(CableNetwork oldNetwork)
        {
            base.OnRemoveCableNetwork(oldNetwork);
            foreach (var switchgearComponent in ReVolt.SwitchgearNetwork.MemberNetwork(this).Members)
            {
                switchgearComponent.OnBusConnectionChanged(this);
            }
        }

        PseudoNetwork<ISwitchgearComponent> IPseudoNetworkMember<ISwitchgearComponent>.Network { get; } = ReVolt.SwitchgearNetwork.Join();
        public SwitchgearComponentType ComponentType => BusType;
        
        public void OnBusConnectionChanged(Device BusTie)
        {
            // Bus tie does not care if a tie cable changes
        }

        public SwitchgearComponentType BusType;
        
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
    }
}
