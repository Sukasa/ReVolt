using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using HarmonyLib;
using System.Diagnostics.CodeAnalysis;
using Assets.Scripts.Objects.Motherboards;
using UnityEngine;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(Transformer))]
    internal class TransformerPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Transformer.GetGeneratedPower))]
        public static bool GetGeneratedPowerPatch(CableNetwork cableNetwork, Transformer __instance, ref float __result, float ____powerProvided, CableNetwork ___InputNetwork)
        {
            if (!ReVolt.enableTransformerExploitMitigation.Value)
                return true;

            if (__instance.OutputNetwork == null || __instance.Error == 1 || cableNetwork != __instance.OutputNetwork || !__instance.OnOff || __instance.InputNetwork == null)
            {
                __result = 0f;
                return false;
            }
            
            __result =  Mathf.Clamp(__instance.InputNetwork.PotentialLoad - ____powerProvided, 0f, (float)__instance.Setting);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Transformer.GetUsedPower))]
        public static bool GetUsedPowerPatch([NotNull] CableNetwork cableNetwork, Transformer __instance, ref float __result, float ____powerProvided)
        {
            if (!ReVolt.enableTransformerExploitMitigation.Value)
                return true;

            if (__instance.InputNetwork == null || __instance.OutputNetwork == null || cableNetwork != __instance.InputNetwork)
            {
                __result = 0f;
                return false;
            }

            __result = __instance.Error == 1 ? (!__instance.OnOff ? 0.0f : __instance.UsedPower) : (!__instance.OnOff ? 0.0f : Mathf.Min((float)__instance.Setting, ____powerProvided) + __instance.UsedPower);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Transformer.ReceivePower))]
        public static bool ReceivePowerPatch([NotNull] CableNetwork cableNetwork, float powerAdded, Transformer __instance, ref float ____powerProvided)
        {
            if (!ReVolt.enableTransformerExploitMitigation.Value)
                return true;

            if (__instance.InputNetwork != null && cableNetwork != __instance.InputNetwork || !__instance.OnOff || __instance.InputNetwork == null)
                return false;

            powerAdded -= __instance.UsedPower;

            if (powerAdded < 0.0f)
                return false;

            ____powerProvided -= powerAdded;
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Transformer.CanLogicRead))]
        public static bool CanLogicReadPatch(LogicType logicType, Transformer __instance, ref bool __result)
        {
            if (!ReVolt.enableTransformerLogicAddition.Value)
                return true;

            if (logicType != LogicType.PowerActual)
                return true;
            
            __result = true;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Transformer.GetLogicValue))]
        public static bool GetLogicValuePatch(LogicType logicType, Transformer __instance, ref double __result, float ____powerProvided)
        {
            if (!ReVolt.enableTransformerLogicAddition.Value)
                return true;

            if (logicType != LogicType.PowerActual)
                return true;
            
            __result = ____powerProvided;
            return false;
        }
    }
}
