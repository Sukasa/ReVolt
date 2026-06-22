using Assets.Scripts.Networks;
using HarmonyLib;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(PowerTick))]
    public class PowerTickPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(PowerTick.Initialise))]
        public static bool InitialisePatch(PowerTick __instance, CableNetwork cableNetwork)
        {
            // If the injection failed, run the original code
            if (__instance is not RevoltTick revoltTick)
                return true;

            revoltTick.Initialize_New(cableNetwork);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(PowerTick.CalculateState))]
        public static bool CalculateStatePatch(PowerTick __instance)
        {
            // If the injection failed, run the original code
            if (__instance is not RevoltTick revoltTick)
                return true;

            revoltTick.CalculateState_New();
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(PowerTick.ApplyState))]
        public static bool ApplyStatePatch(PowerTick __instance)
        {
            // If the injection failed, run the original code
            if (__instance is not RevoltTick revoltTick)
                return true;

            revoltTick.ApplyState_New();
            return false;
        }

        [HarmonyReversePatch]
        [HarmonyPatch("CacheState")]
        public static void CacheState(PowerTick _)
        {
            // Stub
        }

        [HarmonyReversePatch]
        [HarmonyPatch("CheckForRecursiveProviders")]
        public static void CheckForRecursiveProviders(PowerTick _)
        {
            // Stub
        }


    }
}
