using System.Collections.Generic;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using BepInEx.Configuration;
using HarmonyLib;
using LaunchPadBooster;
using LibConstruct;
using ReVolt.Assets.Scripts;
using ReVolt.Interfaces;
using UnityEngine;

namespace ReVolt
{
    public class ReVolt : MonoBehaviour
    {
        public static readonly PseudoNetworkType<ISwitchgearComponent> SwitchgearNetwork = new();
        
        // Configuration vars
        internal static ConfigEntry<float> configMaxBatteryChargeRate;
        internal static ConfigEntry<float> configMaxBatteryDischargeRate;
        internal static ConfigEntry<float> configBatteryChargeEfficiency;
        internal static ConfigEntry<float> configCableBurnFactor;
        internal static ConfigEntry<bool> enableRecursiveNetworkLimits;
        internal static ConfigEntry<float> heavyBreakerMaxTripSetting;

        internal static ConfigEntry<bool> enablePrefabContent;

        internal static ConfigEntry<bool> enableTransformerExploitMitigation;
        internal static ConfigEntry<bool> enableTransformerLogicAddition;
        internal static ConfigEntry<bool> enableBatteryLogicAddition;
        internal static ConfigEntry<bool> enableAreaPowerControlFix;
        internal static ConfigEntry<bool> enableBatteryLimitsPatch;

        public static readonly Mod MOD = new("Re-Volt", "1.5.0");

        public void OnLoaded(ConfigFile config, List<GameObject> prefabs)
        {
            Debug.Log("Re-Volt is loading");

            // Balancing config
            configMaxBatteryChargeRate = config.Bind("Balancing", "Max Battery charge rate", 0.002f, "Maximum Stationary battery charge rate, in % of max charge");
            configMaxBatteryDischargeRate = config.Bind("Balancing", "Max Battery discharge rate", 0.007f, "Maximum Stationary battery discharge rate, in % of max charge");
            configBatteryChargeEfficiency = config.Bind("Balancing", "Battery Charge Efficiency", 1.0f,
                "Battery charging efficiency.  Reduce this to lose energy to charging inefficiencies.  If you really want to.");
            configCableBurnFactor = config.Bind("Balancing", "Cable burn factor", 1.0f,
                "Increase or decrease this to affect how likely a cable is to burn out each tick.  Set to 0.0 to disable cable burn entirely.");
            enableRecursiveNetworkLimits = config.Bind("Balancing", "Enable Recursive Network Limits", false,
                "Re-enables the check that force-burns cables out if the power grid forms a loop through multiple transformers or batteries");
            heavyBreakerMaxTripSetting = config.Bind("Balancing", "Heavy Breaker Maximum Trip Setting", 500000.0f,
                "Maximum configurable trip current for a Heavy Breaker.  Adjust if you have cable mods installed");

            // Patches config
            enableTransformerExploitMitigation = config.Bind("Patches", "Enable Transformer Exploit Mitigation", true,
                "Patch transformers to mitigate the free-power exploit, and restore quiescent current draw");
            enableTransformerLogicAddition = config.Bind("Patches", "Enable Transformer Logic Additions", true, "Patch transformers to add mid-tier power monitoring");
            enableBatteryLogicAddition = config.Bind("Patches", "Enable Battery Logic Additions", true, "Patch station batteries to add charge/discharge-limit logic values");
            enableAreaPowerControlFix = config.Bind("Patches", "Enable APC Power Fix", true,
                "Patch APCs to mitigate a 10W free-power discrepancy, and restore quiescent current draw");
            enableBatteryLimitsPatch = config.Bind("Patches", "Enable Battery Limits", true, "Patch station batteries to limit charge/discharge rate");

            // Content config
            enablePrefabContent = config.Bind("Content", "Enable Custom Objects", true, "Enable Re-Volt circuit breakers");

            // Now set up the patches and content loader (via patch)
            Debug.Log("Re-Volt config loaded; patching...");
            
            Harmony harmony = new("ReVolt");
            harmony.PatchAll();
            Debug.Log("Re-Volt patches implemented, loading prefabs...");
            
            if (enablePrefabContent.Value)
            {
                MOD.AddPrefabs(prefabs);

                foreach (var prefab in prefabs)
                {
                    var prefabThing = prefab.GetComponent<Thing>();
                    if (prefabThing == null)
                        continue;
                    
                    if (prefabThing is IPatchable y)
                        y.PatchPrefab();
                    else
                        MOD.SetupPrefabs(prefabThing.PrefabName).SetBlueprintMaterials().SetPaintableColor(ColorType.White);
                    
                }
            }

            Debug.Log("Re-Volt loaded prefabs");

            MOD.AddSaveDataType<CircuitBreakerSaveData>();
            MOD.SetMultiplayerRequired();
        }
    }
}