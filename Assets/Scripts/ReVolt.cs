using BepInEx.Configuration;
using HarmonyLib;
using LaunchPadBooster;
using ReVolt.Assets.Scripts;
using ReVolt.patches;
using StationeersMods.Interface;
using UnityEngine;

namespace ReVolt
{
    [StationeersMod("Re-Volt", "Re-Volt [StationeersMods]", "1.0.1")]
    public class ReVolt : ModBehaviour
    {
        // Configuration vars
        internal static ConfigEntry<float> configMaxBatteryChargeRate;
        internal static ConfigEntry<float> configMaxBatteryDischargeRate;
        internal static ConfigEntry<float> configCableBurnFactor;
        internal static ConfigEntry<bool> configEnablePrefabs;

        public static readonly Mod MOD = new("Re-Volt", "1.0.1");

        public override void OnLoaded(ContentHandler contentHandler)
        {
            Debug.Log("Re-Volt loading");

            // Balancing config
            configMaxBatteryChargeRate = Config.Bind("Balancing", "Max Battery charge rate", 0.01f, "Maximum Stationary battery charge rate, in % of max charge");
            configMaxBatteryDischargeRate = Config.Bind("Balancing", "Max Battery discharge rate", 0.05f, "Maximum Stationary battery discharge rate, in % of max charge");
            configCableBurnFactor = Config.Bind("Balancing", "Cable burn factor", 1.0f, "Increase or decrease this to affect how likely a cable is to burn out each tick.  Set to 0.0 to disable cable burn entirely.");

            // Content config
            configEnablePrefabs = Config.Bind("Content", "Enable Custom Objects", true, "Enable Re-Volt circuit breakers");

            // Now set up the patches and content loader (via patch)
            Harmony harmony = new("ReVolt");
            PrefabPatcher.PrefabContent = contentHandler.prefabs;
            harmony.PatchAll();

            MOD.AddSaveDataType<CircuitBreakerSaveData>();
            MOD.SetMultiplayerRequired();
        }
    }
}
