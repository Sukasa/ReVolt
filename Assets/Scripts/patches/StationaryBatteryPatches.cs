using Assets.Scripts.Objects.Electrical;
using HarmonyLib;
using System;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(Battery))]
    internal class StationaryBatteryPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Battery.GetUsedPower))]
        public static void LimitMaxChargeRate(Battery __instance, ref float __result)
        {
            if (ReVolt.enableBatteryLimitsPatch.Value)
                __result = MathF.Min(__result, __instance.PowerMaximum * ReVolt.configMaxBatteryChargeRate.Value);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Battery.GetGeneratedPower))]
        public static void LimitMaxDischargeRate(Battery __instance, ref float __result)
        {
            if (ReVolt.enableBatteryLimitsPatch.Value)
                __result = MathF.Min(__result, __instance.PowerMaximum * ReVolt.configMaxBatteryDischargeRate.Value);
        }
    }
}
