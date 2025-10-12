using HarmonyLib;
using ReVolt.Assets.Scripts.patches;
using StationeersMods.Interface;
using System.Diagnostics;
namespace ReVolt
{
    [StationeersMod("ReVolt", "Re-Volt [StationeersMods]", "0.0.1")]
    public class ReVolt : ModBehaviour
    {
        // Configuration vars
        internal static ConfigEntry<float> configMaxBatteryChargeRate;
        internal static ConfigEntry<float> configMaxBatteryDischargeRate;
        internal static ConfigEntry<float> configCableBurnFactor;
        internal static ConfigEntry<bool> configEnablePrefabs;
        public override void OnLoaded(ContentHandler contentHandler)
        {
            UnityEngine.Debug.Log("Re-Volt is loading");
            // Balancing config
            configMaxBatteryChargeRate = Config.Bind("Balancing", "Max Battery charge rate", 0.01f, "Maximum Stationary battery charge rate, in % of max charge");
            configMaxBatteryDischargeRate = Config.Bind("Balancing", "Max Battery discharge rate", 0.05f, "Maximum Stationary battery discharge rate, in % of max charge");
            configCableBurnFactor = Config.Bind("Balancing", "Cable burn factor", 1.0f, "Increase or decrease this to affect how likely a cable is to burn out each tick.  Set to 0.0 to disable cable burn entirely.");

            // Content config
            configEnablePrefabs = Config.Bind("Content", "Enable Custom Objects", true, "Enable Re-Volt circuit breakers");

            UnityEngine.Debug.Log(new StackTrace().GetFrame(0).GetMethod().ReflectedType.Assembly.FullName);
            UnityEngine.Debug.Log("Applying ReVolt patches...");
            Harmony harmony = new("ReVolt");
            PrefabPatch.prefabs = contentHandler.prefabs;
            harmony.PatchAll();

            UnityEngine.Debug.Log("Re-Volt Loaded with " + contentHandler.prefabs.Count + " prefab(s)");
        }
    }
}
