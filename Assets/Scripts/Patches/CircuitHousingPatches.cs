using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using HarmonyLib;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(CircuitHousing))]
    public class CircuitHousingPatches
    {
        [HarmonyPrefix, HarmonyPatch(nameof(CircuitHousing.GetLogicableFromIndex))]
        public static bool GetLogicableFromIndexPatch(int deviceIndex, int networkIndex, CircuitHousing __instance, ref ILogicable __result)
        {
            if (deviceIndex == int.MaxValue)
            {
                __result = networkIndex != int.MinValue ? __instance.GetNetwork(networkIndex) : __instance;
                return false;
            }

            if (__instance.InputNetwork1 != null && !__instance.InputNetwork1DevicesSorted.Contains(__instance.Devices[deviceIndex] as Device))
            {
                __result = null;
                return false;
            }

            if (networkIndex == int.MinValue)
            {
                __result = __instance.Devices[deviceIndex];
                return false;
            }

            __result = __instance.Devices[deviceIndex] is not IConnected device ? null : (ILogicable)device.GetNetwork(networkIndex);

            return false;
        }

        [HarmonyPrefix, HarmonyPatch(nameof(CircuitHousing.GetLogicableFromId))]
        public static bool GetLogicableFromIdPatch(int deviceId, int networkIndex, CircuitHousing __instance, ref ILogicable __result)
        {
            if (deviceId == 0)
            {
                __result = null;
                return false;
            }

            var logicableFromId = Referencable.Find<ILogicable>(deviceId);

            if (__instance.InputNetwork1 != null && !__instance.InputNetwork1DevicesSorted.Contains(logicableFromId))
            {
                __result = null;
                return false;
            }

            if (networkIndex == int.MinValue)
            {
                __result = logicableFromId;
                return false;
            }

            if (logicableFromId is Device d)
                __result = d == null ? null : (ILogicable)d.GetNetwork(networkIndex);

            return false;
        }
    }
}