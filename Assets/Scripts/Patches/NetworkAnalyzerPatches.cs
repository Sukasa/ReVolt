using System.Text;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Util;
using HarmonyLib;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(NetworkAnalyser))]
    public static class NetworkAnalyzerPatches
    {
        [HarmonyPostfix, HarmonyPatch(nameof(NetworkAnalyser.OnPreScreenUpdate))]
        public static void AfterPreScreenUpdate(NetworkAnalyser __instance, ref CableNetwork ____scannedNetwork, ref string ____outputText)
        {
            if (____scannedNetwork == null)
                return;
            
            StringBuilder Output = new(____outputText);
            var num = ____scannedNetwork.DeviceList.Count;
            foreach (var device in CableNetworkPatches.GetExtendedDeviceList(____scannedNetwork))
            {
                if (device.PowerCableNetwork == ____scannedNetwork)
                    continue;
                
                ++num;

                Output.Append(device.DataCableNetwork == ____scannedNetwork ? "<#10cc10>" : "<#1b751b>");
                Output.Append(StringManager.Get(num));
                Output.Append(".");
                Output.Append(device.DisplayName);
                Output.AppendLine("</color>");
            }

            ____outputText = Output.ToString();
        }
        
    }
}