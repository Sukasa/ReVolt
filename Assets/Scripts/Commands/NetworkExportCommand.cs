using System.Collections.Generic;
using Assets.Scripts.Networks;
using Util.Commands;

namespace ReVolt.Commands
{
    public class NetworkExportCommand : CommandBase
    {
        public override string Execute(string[] args)
        {
            // Incomplete, need to finalize DTO object structure and grab the spec from Baugn to implement properly
            List<NetworkExport> Exports = new();
            
            foreach (var network in CableNetwork.AllCableNetworks.ToList())
            {
                
            }
            
            return null;
        }

        public override string HelpText => "Dump all cable networks to a .json file";
        public override string[] Arguments { get; } = null;
        public override bool IsLaunchCmd { get; } = false;
    }
}