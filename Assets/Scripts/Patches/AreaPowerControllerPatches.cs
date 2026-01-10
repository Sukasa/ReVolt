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

            // Check that the network we're receiving power from is also the network we SHOULD be receiving power from
            if (__instance.InputNetwork == null || cableNetwork != __instance.InputNetwork)
                return false;

            // Eat quiescent current from the incoming power
            powerAdded -= __instance.UsedPower;

            // If we're not getting any power to the APC as a result of the quiescent current being the only supplied power, skip
            if (powerAdded <= 0.0f)
                return false;

            // Try to satisfy power load
            ____powerProvided -= powerAdded;

            // If we didn't make up enough power to charge the battery or if there's no battery or if it's fully charged, then just return now and discard the remaining power
            if (____powerProvided >= 0.0 || !(bool)__instance.Battery || __instance.Battery.IsCharged)
                return false;

            // Otherwise we charge the battery at max rate or as far as we can based on remaining power
            float num = Mathf.Min(__instance.Battery.PowerDelta, __instance.BatteryChargeRate, powerAdded);
            __instance.Battery.PowerStored += num; // Add to battery
            ____powerProvided += num; // ... Add to the power we've provided to downstream?  I don't think we need this anymore.

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(AreaPowerControl.GetUsedPower))]
        public static bool UsePowerPatch(CableNetwork cableNetwork, float powerUsed, AreaPowerControl __instance, ref float ____powerProvided)
        {
            if (!ReVolt.enableAreaPowerControlFix.Value)
                return true;
            
            if (cableNetwork != __instance.OutputNetwork)
                return false;
            
            if (__instance.Battery && !__instance.Battery.IsEmpty)
            {
                float num = Mathf.Min(__instance.Battery.PowerStored, powerUsed);
                __instance.Battery.PowerStored -= num;
                powerUsed -= num;
            }
            
            ____powerProvided += powerUsed;

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
            if (__instance.OnOff)
            {
                usedPower += __instance.UsedPower;
                
                if (__instance.OutputNetwork != null)
                    usedPower += ____powerProvided;

                if ((bool)__instance.Battery && !__instance.Battery.IsCharged)
                    usedPower += Mathf.Min(__instance.BatteryChargeRate, __instance.Battery.PowerDelta);
            }

            __result = usedPower;
            return false;
        }

    }
}
