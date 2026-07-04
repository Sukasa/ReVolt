using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using HarmonyLib;
using System;
using UnityEngine;

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

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Battery.ReceivePower))]
        public static bool ChargeEfficiencyControl(Battery __instance, CableNetwork cableNetwork, float powerAdded)
        {
            if (!ReVolt.enableBatteryLimitsPatch.Value)
                return true;
            
            if (__instance.Error == 1 || !__instance.OnOff || cableNetwork != __instance.InputNetwork || !GetIsOperable(__instance)) return false;
            var charged = ReVolt.configBatteryChargeEfficiency.Value * powerAdded;
            if (powerAdded < 50) // At below a 50 power differential, just fully charge the battery (just enough to keep up with maximum ambient discharge rate plus a tiny bit)
                charged = powerAdded;

            __instance.PowerStored = Mathf.Clamp(charged + __instance.PowerStored, 0f, __instance.PowerMaximum);

            return false;
        }

        [HarmonyReversePatch]
        [HarmonyPatch("get_IsOperable")]
        public static bool GetIsOperable(Battery __instance)
        {
            return false;
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
