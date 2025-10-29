using BepInEx.Configuration;
using HarmonyLib;
using LaunchPadBooster;
using ReVolt.Assets.Scripts;
using ReVolt.Patches;
using StationeersMods.Interface;
using UnityEngine;
 
namespace ReVolt
{
    [StationeersMod("Re-Volt", "Re-Volt [StationeersMods]", "1.3.1")]
    public class ReVolt : ModBehaviour
    {
        // Configuration vars
        internal static ConfigEntry<float> configMaxBatteryChargeRate;
        internal static ConfigEntry<float> configMaxBatteryDischargeRate;
        internal static ConfigEntry<float> configCableBurnFactor;
        internal static ConfigEntry<bool> enableRecursiveNetworkLimits;

        internal static ConfigEntry<bool> enablePrefabContent;

        internal static ConfigEntry<bool> enableTransformerExploitMitigation;
        internal static ConfigEntry<bool> enableAreaPowerControlFix;
        internal static ConfigEntry<bool> enableBatteryLimitsPatch;

        public static readonly Mod MOD = new("Re-Volt", "1.3.1");

        public override void OnLoaded(ContentHandler contentHandler)
        {
            Debug.Log("Re-Volt loading");

            // Balancing config
            configMaxBatteryChargeRate = Config.Bind("Balancing", "Max Battery charge rate", 0.002f, "Maximum Stationary battery charge rate, in % of max charge");
            configMaxBatteryDischargeRate = Config.Bind("Balancing", "Max Battery discharge rate", 0.007f, "Maximum Stationary battery discharge rate, in % of max charge");
            configCableBurnFactor = Config.Bind("Balancing", "Cable burn factor", 1.0f, "Increase or decrease this to affect how likely a cable is to burn out each tick.  Set to 0.0 to disable cable burn entirely.");
            enableRecursiveNetworkLimits = Config.Bind("Balancing", "Enable Recursive Network Limits", false, "Re-enables the check that force-burns cables out if the power grid forms a loop through multiple transformers or batteries");

            // Patches config
            enableTransformerExploitMitigation = Config.Bind("Patches", "Enable Transformer Exploit Mitigation", true, "Patch transformers to mitigate the free-power exploit, and restore quiescent current draw");
            enableAreaPowerControlFix = Config.Bind("Patches", "Enable APC Power Fix", true, "Patch APCs to mitigate a 10W free-power discrepancy, and restore quiescent current draw");
            enableBatteryLimitsPatch = Config.Bind("Patches", "Enable Battery Limits", true, "Patch station batteries to limit charge/discharge rate");

            // Content config
            enablePrefabContent = Config.Bind("Content", "Enable Custom Objects", true, "Enable Re-Volt circuit breakers");

            // Now set up the patches and content loader (via patch)
            Harmony harmony = new("ReVolt");
            PrefabPatcher.PrefabContent = contentHandler.prefabs;
            harmony.PatchAll();

            MOD.AddSaveDataType<CircuitBreakerSaveData>();
            MOD.SetMultiplayerRequired();
        }
    }
}
