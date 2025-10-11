using Assets.Scripts.Networks;
using HarmonyLib;

namespace ReVolt.Assets.Scripts.patches
{
    [HarmonyPatch]
    public class PowertickCalculateStatePatch
    {
        [HarmonyPatch(typeof(PowerTick), nameof(PowerTick.CalculateState))]
        public static bool Prefix(PowerTick __instance)
        {
            if (__instance is RevoltTick revoltTick)
                revoltTick.CalculateState_New();
            else
            {
                // If we got here, something went wrong with our Revolt injector
                UnityEngine.Debug.LogWarning("PowerTick was not replaced!");
                return true; 
            }
                

            return false;
        }
    }
}
