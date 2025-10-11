using Assets.Scripts.Networks;
using HarmonyLib;

namespace ReVolt.Assets.Scripts.patches
{
    [HarmonyPatch]
    public class PowertickApplyStatePatch
    {
        [HarmonyPatch(typeof(PowerTick), nameof(PowerTick.ApplyState))]
        public static bool Prefix(PowerTick __instance)
        {
            if (__instance is RevoltTick revoltTick)
                revoltTick.ApplyState_New();
            else
                return true; // If we got here, something went wrong with our Revolt injector.  I'll add error logging to this later.  (or just make this patch the issue)

            return false;
        }
    }
}
