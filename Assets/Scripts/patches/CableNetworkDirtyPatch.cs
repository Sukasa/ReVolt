using Assets.Scripts.Networks;
using HarmonyLib;
using System;

namespace ReVolt.Assets.Scripts.patches
{
    [HarmonyPatch]
    public class CableNetworkDirtyPatch
    {
        [HarmonyPatch(typeof(CableNetwork), nameof(CableNetwork.DirtyPowerAndDataDeviceLists))]
        public static void Postfix(CableNetwork __instance)
        {
            if (__instance.PowerTick is RevoltTick revoltTick)
                revoltTick.IsDirty = true;
        }
    }
}
