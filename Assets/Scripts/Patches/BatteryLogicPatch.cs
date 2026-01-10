using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using HarmonyLib;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(Battery))]
    internal class BatteryLogicPatch
    {

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Battery.CanLogicRead))]
        public static bool CanLogicReadPatch(LogicType logicType, Battery __instance, ref bool __result)
        {
            if (!ReVolt.enableBatteryLogicAddition.Value)
                return true;

            if (logicType == LogicType.ExportQuantity || logicType == LogicType.ImportQuantity)
            {
                __result = true;
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Battery.GetLogicValue))]
        public static bool GetLogicValuePatch(LogicType logicType, Battery __instance, ref double __result, float ____powerProvided)
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
