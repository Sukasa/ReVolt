using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using HarmonyLib;
using System;
using Assets.Scripts.Objects.Motherboards;
using UnityEngine;

namespace ReVolt.Patches
{
    
    [HarmonyPatch(typeof(Battery))]
    internal class BatteryPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Battery.GetUsedPower))]
        public static void LimitMaxChargeRate(Battery __instance, ref float __result)
        {
            if (ReVolt.enableBatteryLimitsPatch.Value)
                __result = MathF.Min(__result, __instance.PowerMaximum * ReVolt.configMaxBatteryChargeRate.Value);
        }

        private const float MaxBatterySelfDischargeRate = 40f;
        
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Battery.ReceivePower))]
        public static bool ChargeEfficiencyControl(Battery __instance, CableNetwork cableNetwork, float powerAdded)
        {
            if (!ReVolt.enableBatteryLimitsPatch.Value)
                return true;
            
            if (__instance.Error == 1 || !__instance.OnOff || cableNetwork != __instance.InputNetwork || !GetIsOperable(__instance)) return false;
            var charged = powerAdded > MaxBatterySelfDischargeRate ? ReVolt.configBatteryChargeEfficiency.Value * (powerAdded - MaxBatterySelfDischargeRate) + MaxBatterySelfDischargeRate : powerAdded;

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

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Battery.CanLogicRead))]
        public static bool CanLogicReadPatch(LogicType logicType, Battery __instance, ref bool __result)
        {
            if (!ReVolt.enableBatteryLogicAddition.Value)
                return true;

            if (logicType is not (LogicType.ExportQuantity or LogicType.ImportQuantity))
                return true;
            
            __result = true;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Battery.GetLogicValue))]
        public static bool GetLogicValuePatch(LogicType logicType, Battery __instance, ref double __result)
        {
            if (!ReVolt.enableBatteryLogicAddition.Value)
                return true;

            switch (logicType)
            {
                case LogicType.ExportQuantity:
                    __result = __instance.PowerMaximum * ReVolt.configMaxBatteryDischargeRate.Value;
                    return false;
                case LogicType.ImportQuantity:
                    __result = __instance.PowerMaximum * ReVolt.configMaxBatteryChargeRate.Value;
                    return false;
                default:
                    return true;
            }
        }
    }
}
