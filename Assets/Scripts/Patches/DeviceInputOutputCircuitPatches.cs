using Assets.Scripts.Objects.Pipes;
using HarmonyLib;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(DeviceInputOutputCircuit))]
    public class DeviceInputOutputCircuitPatches
    {
        [HarmonyPrefix, HarmonyPatch(nameof(DeviceInputOutputCircuit.GetLogicableFromIndex))]
        public static bool GetLogicableFromIndexPatch(int deviceIndex, int networkIndex, DeviceInputOutputCircuit __instance, ref ILogicable __result)
        {
            if (deviceIndex == int.MaxValue)
            {
                __result = networkIndex != int.MinValue ? __instance.GetNetwork(networkIndex) : __instance;
                return false;
            }

            if (__instance.DataCableNetwork != null && !__instance.DataNetworkDevicesSorted.Contains(__instance.Devices[deviceIndex] as Device))
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

        [HarmonyPrefix, HarmonyPatch(nameof(DeviceInputOutputCircuit.GetLogicableFromId))]
        public static bool GetLogicableFromIdPatch(int deviceId, int networkIndex, DeviceInputOutputCircuit __instance, ref ILogicable __result)
        {
            if (deviceId == 0)
            {
                __result = null;
                return false;
            }

            var logicableFromId = Referencable.Find<ILogicable>(deviceId);

            if (__instance.DataCableNetwork != null && !__instance.DataNetworkDevicesSorted.Contains(logicableFromId))
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