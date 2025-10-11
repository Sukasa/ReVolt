using Assets.Scripts.Networks;
using HarmonyLib;

namespace ReVolt.Assets.Scripts.patches
{
    [HarmonyPatch]
    public class PowerTickCacheStatePatch
    {
        [HarmonyReversePatch, HarmonyPatch(typeof(PowerTick), "CacheState")]
        public static void CacheState(PowerTick instance)
        {
            // Stub
        }
    }
}
