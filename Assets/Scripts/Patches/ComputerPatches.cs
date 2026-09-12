using System.Collections.Generic;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using HarmonyLib;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(Computer))]
    public class ComputerPatches
    {
        [HarmonyPrefix, HarmonyPatch(nameof(Computer.DeviceList))]
        public static bool DeviceListPatch(Computer __instance, ref List<ILogicable> __result)
        {
            if (__instance.DataCable == null)
                __instance.FindDataCable();
            __result = Logicable.RecalculateSortedDevicesList(__instance.DataCableNetwork);
            return false;
        }
    }
}