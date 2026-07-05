using System;
using System.Collections.Generic;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Pipes;
using HarmonyLib;
using ReVolt.Prefabs;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(Logicable))]
    public class LogicablePatches
    {
        [HarmonyPostfix, HarmonyPatch(nameof(Logicable.RecalculateSortedDevicesList))]
        public static void RecalculateSortedDevicesListPatch(CableNetwork cableNetwork, ref List<ILogicable> __result)
        {
            if (cableNetwork == null)
                return;

            var hits = 0;
            for (var index = __result.Count - 1; index >= 0; --index)
            {
                if (__result[index] is not StructureDataBridge DB)
                    continue;

                __result.AddRange(DB.GetBridgedDevices(cableNetwork));
                hits++;
            }

            switch (hits)
            {
                case 0:
                    return; // No data bridges, so we don't need to re-sort or de-duplicate
                
                case > 1: // If we added more than one data bridge, de-dup just in case they bridged the same network by accident
                    for (var index = __result.Count - 2; index >= 0; --index)
                    {
                        for (var index2 = __result.Count - 1; index2 > index; --index2)
                        {
                            if (ReferenceEquals(__result[index2], __result[index]))
                                __result.RemoveAt(index2);
                        }
                    }
                    break;
            }

            __result.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal));
        }
    }
}