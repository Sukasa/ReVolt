using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Pipes;
using HarmonyLib;
using UnityEngine;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(Device))]
    public class DevicePatches
    {

        [HarmonyPrefix]
        [HarmonyPatch("AssessPower")]
        public static bool AssessPower(CableNetwork cableNetwork, bool isOn, Device __instance)
        {
            if (cableNetwork == null || !isOn)
            {
                if (__instance.Powered)
                    SetPower(__instance, cableNetwork, false);
            }
            else
            {
                if (cableNetwork.PowerTick is not RevoltTick Ticker)
                    return true;

                if (Ticker.GetPowerState(RevoltTick.ClassifyDevice(__instance)))
                {
                    var usedPower = __instance.GetUsedPower(cableNetwork);
                    if (usedPower <= 0.0)
                        return false;

                    if (usedPower > (double)cableNetwork.EstimatedRemainingLoad)
                    {
                        cableNetwork.DuringTickLoad += Mathf.Min(usedPower, cableNetwork.EstimatedRemainingLoad);
                        if (__instance.Powered)
                            SetPower(__instance, cableNetwork, false);
                    }
                    else
                    {
                        cableNetwork.DuringTickLoad += usedPower;
                        if (!__instance.Powered)
                            SetPower(__instance, cableNetwork, true);
                    }
                }
                else
                {
                    SetPower(__instance, cableNetwork, false);
                }
            }
            return false;
        }

        [HarmonyReversePatch, HarmonyPatch("SetPower")]
        public static void SetPower(Device instance, CableNetwork cableNetwork, bool hasPower)
        {
            // Stub
        }
    }
}
