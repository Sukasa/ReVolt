using HarmonyLib;
using ReVolt.Assets.Scripts.patches;
using StationeersMods.Interface;
using System.Diagnostics;
namespace ReVolt
{
    [StationeersMod("ReVolt", "Re-Volt [StationeersMods]", "0.0.1")]
    public class ReVolt : ModBehaviour
    {
        // private ConfigEntry<bool> configBool;
    
        public override void OnLoaded(ContentHandler contentHandler)
        {
            UnityEngine.Debug.Log("Re-Volt is loading");

            //Config example
            // configBool = Config.Bind("Input",
            //     "Boolean",
            //     true,
            //     "Boolean description");

            UnityEngine.Debug.Log(new StackTrace().GetFrame(0).GetMethod().ReflectedType.Assembly.FullName);
            UnityEngine.Debug.Log("Applying ReVolt patches...");
            Harmony harmony = new("ReVolt");
            PrefabPatch.prefabs = contentHandler.prefabs;
            harmony.PatchAll();

            UnityEngine.Debug.Log("Re-Volt Loaded with " + contentHandler.prefabs.Count + " prefab(s)");
        }
    }
}
