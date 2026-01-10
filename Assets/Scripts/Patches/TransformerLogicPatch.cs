using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using HarmonyLib;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(Transformer))]
    internal class TransformerLogicPatch
    {

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Transformer.CanLogicRead))]
        public static bool CanLogicReadPatch(LogicType logicType, Transformer __instance, ref bool __result)
        {
            if (!ReVolt.enableTransformerLogicAddition.Value)
                return true;

            if (logicType == LogicType.PowerActual)
            {
                __result = true;
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Transformer.GetLogicValue))]
        public static bool GetLogicValuePatch(LogicType logicType, Transformer __instance, ref double __result, float ____powerProvided)
        {
            if (!ReVolt.enableTransformerLogicAddition.Value)
                return true;

            if (logicType == LogicType.PowerActual)
            {
                __result = ____powerProvided;
                return false;
            }

            return true;
        }
    }
}
