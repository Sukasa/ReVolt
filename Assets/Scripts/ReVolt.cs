using HarmonyLib;
using ReVolt.Assets.Scripts.patches;
using StationeersMods.Interface;

namespace ReVolt
{
    [StationeersMod("ReVolt", "Re-Volt [StationeersMods]", "1.0.0")]
    public class ReVolt : ModBehaviour
    {
        // Configuration vars
        internal static ConfigEntry<float> configMaxBatteryChargeRate;
        internal static ConfigEntry<float> configMaxBatteryDischargeRate;
        internal static ConfigEntry<float> configCableBurnFactor;
        internal static ConfigEntry<bool> configEnablePrefabs;
        public override void OnLoaded(ContentHandler contentHandler)
        {
            UnityEngine.Debug.Log("Re-Volt is charging up!");
            // Balancing config
            configMaxBatteryChargeRate = Config.Bind("Balancing", "Max Battery charge rate", 0.01f, "Maximum Stationary battery charge rate, in % of max charge");
            configMaxBatteryDischargeRate = Config.Bind("Balancing", "Max Battery discharge rate", 0.05f, "Maximum Stationary battery discharge rate, in % of max charge");
            configCableBurnFactor = Config.Bind("Balancing", "Cable burn factor", 1.0f, "Increase or decrease this to affect how likely a cable is to burn out each tick.  Set to 0.0 to disable cable burn entirely.");

            // Content config
            configEnablePrefabs = Config.Bind("Content", "Enable Custom Objects", true, "Enable Re-Volt circuit breakers");

            // Now set up the patches and content loader (via patch)
            Harmony harmony = new("ReVolt");
            PrefabPatch.prefabs = contentHandler.prefabs;
            harmony.PatchAll();

            UnityEngine.Debug.Log("Re-Volt is loaded and ready");
        }
    }
}
