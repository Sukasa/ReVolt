using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using HarmonyLib;
using UnityEngine;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(AreaPowerControl))]
    internal class AreaPowerControllerPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(AreaPowerControl.ReceivePower))]
        public static bool ReceivePowerPatch(CableNetwork cableNetwork, float powerAdded, AreaPowerControl __instance, ref float ____powerProvided)
        {
            if (!ReVolt.enableAreaPowerControlFix.Value)
                return true;

            if (__instance.InputNetwork != null && cableNetwork != __instance.InputNetwork || __instance.InputNetwork == null)
                return false;

            powerAdded -= __instance.UsedPower;

            // If we're not getting any power to the APC as a result of the quiescent current being the only supplied power, skip
            if (powerAdded <= 0.0f)
                return false;

            ____powerProvided -= powerAdded;

            // Return if the APC is power-positive but can't charge its battery
            if ((double)____powerProvided >= 0.0 || !(bool)__instance.Battery || __instance.Battery.IsCharged)
                return false;

            float num = Mathf.Min(__instance.Battery.PowerDelta, __instance.BatteryChargeRate, powerAdded);
            __instance.Battery.PowerStored += num;
            ____powerProvided += num;


            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(AreaPowerControl.GetUsedPower))]
        public static bool GetUsedPowerPatch(CableNetwork cableNetwork, AreaPowerControl __instance, ref float __result, float ____powerProvided, CableNetwork ___InputNetwork)
        {
            if (!ReVolt.enableAreaPowerControlFix.Value)
                return true;

            __result = 0.0f;

            if (__instance.InputNetwork == null || cableNetwork != __instance.InputNetwork)
                return false;

            float usedPower = 0.0f;
            if (__instance.OnOff && __instance.OutputNetwork != null)
            {
                usedPower = ____powerProvided + __instance.UsedPower;

                if ((bool)__instance.Battery && !__instance.Battery.IsCharged)
                    usedPower += Mathf.Min(__instance.BatteryChargeRate, __instance.Battery.PowerDelta);
            }


            __result = usedPower;
            return false;
        }

    }
}
