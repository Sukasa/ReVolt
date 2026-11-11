using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using HarmonyLib;

namespace ReVolt.Patches
{
    
    [HarmonyPatch]
    public static class NetworkAnalyzerPatches
    {
        [HarmonyPatch(typeof(Cartridge), nameof(Cartridge.OnPreScreenUpdate))]
        public static void BaseOnPreScreenUpdate(Cartridge instance)
        {
            // Filled in by Harmony
        }
        
        [HarmonyPatch(typeof(NetworkAnalyzer), nameof(NetworkAnalyzer.OnPreScreenUpdate))]
        public static bool OnPreScreenUpdatePatch(NetworkAnalyzer __instance, ref bool ____isActiveHand, ref CableNetwork ____scannedNetwork)
        {
            BaseOnPreScreenUpdate(__instance);
            
            if (!____isActiveHand)
                return false;
            
            __instance._scannedNetwork = __instance.GetScannedNetwork();
            if (____scannedNetwork != null)
            { 
                __instance._selectedText = $"{Localization.GetInterface("NetworkAnalyserNetwork")} {StringManager.Get(____scannedNetwork.ReferenceId)}";
                __instance._actualValueText = __instance._scannedNetwork.CurrentLoad.ToStringPrefix(Localization.GetInterface("WattEnergyMeasurement"));
                __instance._potentialValueText = __instance._scannedNetwork.PotentialLoad.ToStringPrefix(Localization.GetInterface("WattEnergyMeasurement"));
                __instance._requiredValueText = __instance._scannedNetwork.RequiredLoad.ToStringPrefix(Localization.GetInterface("WattEnergyMeasurement"));
                __instance._devicesValueText = __instance._scannedNetwork.DeviceList.Count.ToString();
                __instance._tempDeviceOutput = string.Empty;
                if (__instance._lastScannedNetwork != __instance._scannedNetwork)
                    __instance._needTopScroll = true;
                __instance._lastScannedNetwork = __instance._scannedNetwork;
                var num = 0;
                foreach (Device device in __instance._scannedNetwork.DeviceList) 
                {
                    ++num;
                    __instance._tempDeviceOutput += $"\n{StringManager.Get(num)}.{device.DisplayName}";
                    __instance._tempDeviceOutput += string.Format("<#A9A9A9> ");
                    if (device.HasOnOffState)
                        __instance._tempDeviceOutput = $"{__instance._tempDeviceOutput}...{(device.OnOff ? ActionStrings.On : ActionStrings.Off)}";
                    if (device.HasPowerState)
                        __instance._tempDeviceOutput =
                            $"{__instance._tempDeviceOutput}...{(device.Powered ? device.GetUsedPower(device.PowerCableNetwork).ToStringPrefix(Localization.GetInterface("WattEnergyMeasurement")) : ActionStrings.Unpowered)}";
                    if (device.HasOpenState)
                        __instance._tempDeviceOutput = $"{__instance._tempDeviceOutput}...{(device.IsOpen ? ActionStrings.Opened : ActionStrings.Closed)}";
                    if (device.HasErrorState && device.Error >= 1)
                        __instance._tempDeviceOutput = $"{__instance._tempDeviceOutput}...{ActionStrings.Error}";
                    __instance._tempDeviceOutput += string.Format(" </color>");
                }

                __instance._outputText = __instance._tempDeviceOutput;
            }
            else
            {
                __instance._selectedText = Localization.GetInterface("NotApplicableString");
                __instance._actualValueText = Localization.GetInterface("NotApplicableString");
                __instance._potentialValueText = Localization.GetInterface("NotApplicableString");
                __instance._devicesValueText = Localization.GetInterface("NotApplicableString");
                __instance._requiredValueText = Localization.GetInterface("NotApplicableString");
                __instance._outputText = string.Empty;
            }
        }
    }
}