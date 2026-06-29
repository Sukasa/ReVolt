using System;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(AreaPowerControl))]
    internal class AreaPowerControllerPatches
    {
        [HarmonyPrefix, HarmonyPatch(nameof(AreaPowerControl.ReceivePower))]
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

            // Power that was drawn last tick should be removed from the incoming packet
            if (____powerProvided > powerAdded)
            {
                ____powerProvided -= powerAdded;
                powerAdded = 0;
            }
            else
            {
                powerAdded -= ____powerProvided;
                ____powerProvided = 0;
            }

            // If we didn't make up enough power to charge the battery or if there's no battery or if it's fully charged, then just return now and discard the remaining power
            if (powerAdded <= 0.0 || !(bool)__instance.Battery || __instance.Battery.IsCharged)
                return false;

            // Otherwise we charge the battery at max rate or as far as we can based on remaining power
            float num = Mathf.Min(__instance.Battery.PowerDelta, __instance.BatteryChargeRate, powerAdded);
            __instance.Battery.PowerStored += num; // Add to battery

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(AreaPowerControl.UsePower))]
        public static bool UsePowerPatch(CableNetwork cableNetwork, float powerUsed, AreaPowerControl __instance, ref float ____powerProvided)
        {
            if (!ReVolt.enableAreaPowerControlFix.Value)
                return true;

            if (cableNetwork != __instance.OutputNetwork)
                return false;

            if (____powerProvided == 0)
            {
                // APC is being fed enough power that the upstream is fully supplying it each tick
                // So this tick we won't immediately draw from the battery and instead mark down the power use for next tick
            }
            else
            {
                // We didn't get enough power last tick so we need to draw from battery
                if (__instance.Battery && !__instance.Battery.IsEmpty)
                {
                    var num = Mathf.Min(__instance.Battery.PowerStored, powerUsed);
                    __instance.Battery.PowerStored -= num;
                    powerUsed -= num;
                }
            }
            
            

            ____powerProvided += powerUsed;

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(AreaPowerControl.AvailablePower), MethodType.Getter)]
        public static bool AvailablePowerGetterPatch(AreaPowerControl __instance, ref float __result, float ____powerProvided)
        {
            if (!ReVolt.enableAreaPowerControlFix.Value)
                return true;
            
            var availablePower = __instance.InputNetwork?.PotentialLoad ?? 0.0f;
            __result = Math.Max(0.0f, availablePower - ____powerProvided - __instance.UsedPower);
            
            if (__instance.Battery != null && !__instance.Battery.IsEmpty)
                __result += __instance.Battery.PowerStored;

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