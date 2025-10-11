using Assets.Scripts.Networks;
using HarmonyLib;

namespace ReVolt.Assets.Scripts.patches
{
    [HarmonyPatch(typeof(PowerTick), nameof(PowerTick.Initialise))]
    public class PowerTickInitializePatch
    {
        public static bool Prefix(PowerTick __instance, CableNetwork cableNetwork)
        {
            if (__instance is RevoltTick revoltTick)
                revoltTick.Initialize_New(cableNetwork);
            else
                return true; // If we got here, something went wrong with our Revolt injector.  I'll add error logging to this later.  (or just make this patch the issue)

            return false;
        }
    }
}
