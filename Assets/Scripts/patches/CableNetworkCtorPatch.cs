using Assets.Scripts.Networks;
using HarmonyLib;
using System;

namespace ReVolt
{
    [HarmonyPatch]
    public class CableNetworkCtorPatch
    {
        [HarmonyPatch(typeof(CableNetwork), MethodType.Constructor)]
        public static void Postfix(CableNetwork __instance)
        {
            UnityEngine.Debug.LogWarning("Replacing PowerTick instance with RevoltTick instance...");
            if (__instance.PowerTick is not RevoltTick)
            {
                // PowerTick is a readonly variable, so use reflection to ignore that
                var fieldInfo = typeof(CableNetwork).GetField(nameof(CableNetwork.PowerTick));
                fieldInfo.SetValue(new RevoltTick(), __instance);
                UnityEngine.Debug.LogWarning("Replaced PowerTick instance with RevoltTick instance");
                UnityEngine.Debug.Log("Replaced PowerTick instance with RevoltTick instance");
            }
        }
    }
}
