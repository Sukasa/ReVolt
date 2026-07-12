using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
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
        public static readonly PseudoNetworkType<ICableTrayComponent> CableTrayNetwork = new();
        
        // Configuration vars
        internal static ConfigEntry<float> configMaxBatteryChargeRate;
        internal static ConfigEntry<float> configMaxBatteryDischargeRate;
        internal static ConfigEntry<float> configBatteryChargeEfficiency;
        internal static ConfigEntry<float> configBatteryCapacityFactor;
        internal static ConfigEntry<float> configCableBurnFactor;
        internal static ConfigEntry<bool> enableRecursiveNetworkLimits;
        internal static ConfigEntry<float> heavyBreakerMaxTripSetting;
        internal static ConfigEntry<float> mediumTransformerMaxSetting;
        internal static ConfigEntry<float> largeTransformerMaxSetting;

        internal static ConfigEntry<bool> enablePrefabContent;

        internal static ConfigEntry<bool> enableTransformerExploitMitigation;
        internal static ConfigEntry<bool> enableTransformerLogicAddition;
        internal static ConfigEntry<bool> enableBatteryLogicAddition;
        internal static ConfigEntry<bool> enableAreaPowerControlFix;
        internal static ConfigEntry<bool> enableBatteryLimitsPatch;

        public static readonly Mod MOD = new("Re-Volt", "1.7.6");

        [UsedImplicitly]
        public void OnLoaded(ConfigFile config, List<GameObject> prefabs)
        {
            Debug.Log("Re-Volt is loading");

            // Battery Balancing config
            configMaxBatteryChargeRate = config.Bind(
                new ConfigDefinition("Battery Balancing", "Max Charge Rate"), 0.002f,
                new ConfigDescription("Maximum Stationary battery charge rate, as ratio of max charge capacity", new AcceptableValueRange<float>(0.001f, 1.0f)));
            
            configBatteryCapacityFactor = config.Bind(
                new ConfigDefinition("Battery Balancing", "Max capacity factor"), 1.0f,
                new ConfigDescription("Multiplier for Battery and Large Battery capacity", new AcceptableValueRange<float>(0.1f, 10.0f)));
            
            configMaxBatteryDischargeRate = config.Bind(
                new ConfigDefinition("Battery Balancing", "Max discharge rate"), 0.007f,
                new ConfigDescription("Maximum Stationary battery discharge rate, as ratio of max charge", new AcceptableValueRange<float>(0.001f, 1.0f)));
            
            configBatteryChargeEfficiency = config.Bind(
                new ConfigDefinition("Battery Balancing", "Charge Efficiency"), 1.0f,
                new ConfigDescription("Battery charging efficiency.  Reduce this to lose energy to charging inefficiencies, if you really want to.", new AcceptableValueRange<float>(0.001f, 1.0f)));
            
            // Power Balancing Config
            configCableBurnFactor = config.Bind(
                new ConfigDefinition("Power Balancing", "Cable burn Chance"), 1.0f,
                new ConfigDescription("Increase or decrease this to affect how likely a cable is to burn out each tick.  Set to 0.0 to disable cable burn entirely.", new AcceptableValueRange<float>(0.000f, 2.0f)));
            
            heavyBreakerMaxTripSetting = config.Bind(
                new ConfigDefinition("Power Balancing", "Heavy Breaker Maximum Trip Setting"), 500000.0f,
                new ConfigDescription("Maximum configurable trip current for a Heavy Breaker.", new AcceptableValueRange<float>(100000f, 500000f)));
            
            mediumTransformerMaxSetting = config.Bind(
                new ConfigDefinition("Power Balancing", "Medium Transformer Maximum Setting"), 50000.0f,
                new ConfigDescription("Re-scale medium transformer current limit to match new cables", new AcceptableValueRange<float>(25000f, 100000f)));
            
            largeTransformerMaxSetting = config.Bind(
                new ConfigDefinition("Power Balancing", "Large Transformer Maximum Setting"), 125000.0f,
                new ConfigDescription("Re-scale large transformer current limit to match new cables", new AcceptableValueRange<float>(50000f, 500000f)));

            // Patches config
            enableTransformerExploitMitigation = config.Bind(
                new ConfigDefinition("Patches", "Enable Transformer Exploit Mitigation"), true,
                new ConfigDescription("Patch transformers to mitigate the free-power exploit, and restore quiescent current draw"));
            
            enableRecursiveNetworkLimits = config.Bind(
                new ConfigDefinition("Patches", "Enable Recursive Network Check"), false,
                new ConfigDescription("Re-enables the check that force-burns cables out if the power grid forms a loop through multiple enabled transformers or batteries"));
            
            enableTransformerLogicAddition = config.Bind(
                new ConfigDefinition("Patches", "Enable Transformer Logic Additions"), true, 
                new ConfigDescription("Patch transformers to add mid-tier power monitoring"));
           
            enableBatteryLogicAddition = config.Bind(
                new ConfigDefinition("Patches", "Enable Battery Logic Additions"), true, 
                new ConfigDescription("Patch station batteries to add charge/discharge-limit logic values"));
         
            enableAreaPowerControlFix = config.Bind(
                new ConfigDefinition("Patches", "Enable APC Power Fix"), true,
                new ConfigDescription("Patch APCs to mitigate a 10W free-power discrepancy, and restore quiescent current draw"));
           
            enableBatteryLimitsPatch = config.Bind(
                new ConfigDefinition("Patches", "Enable Battery Limits"), true,
                new ConfigDescription("Patch station batteries to limit charge/discharge rate, charge efficiency, and maximum charge"));

            // Content config
            enablePrefabContent = config.Bind(
                new ConfigDefinition("Content", "Enable Custom Objects"), true,
                new ConfigDescription("Enable Re-Volt Circuit Breakers, Heavy Breakers, Load Center, and Cable Tray"));

            // Now set up the patches and content loader (via patch)
            Debug.Log("Re-Volt config loaded; patching...");
            
            Harmony harmony = new("ReVolt");
            harmony.PatchAll();
            Debug.Log("Re-Volt patches implemented, loading prefabs...");
            
            if (enablePrefabContent.Value)
            {
                MOD.AddPrefabs(prefabs);

                for (var index = prefabs.Count - 1; index >= 0; index--)
                {
                    var prefab = prefabs[index];
                    var prefabThing = prefab.GetComponent<Thing>();
                    if (prefabThing == null)
                        continue;

                    if (prefabThing is IPatchable y)
                        y.PatchPrefab();
                    else
                        MOD.SetupPrefabs(prefabThing.PrefabName).SetBlueprintMaterials().SetPaintableColor(prefabThing is IDefaultColour IDC ? IDC.DefaultColor : ColorType.White);
                }
            }
            
            Debug.Log("Re-Volt loaded prefabs");

            MOD.AddSaveDataType<CircuitBreakerSaveData>();
            MOD.Networking.Required = true;
        }
    }
}