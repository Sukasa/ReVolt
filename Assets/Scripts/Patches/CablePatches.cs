using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using Cysharp.Threading.Tasks;
using HarmonyLib;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(Cable))]
    public class CablePatches
    {
        public static bool GateTriggerRepeatRegistration;
        public static bool RetriggerRegistration;

        [HarmonyPrefix, HarmonyPatch(nameof(Cable.OnRegistered))]
        public static bool BeforeOnRegistered(Cell cell, Cable __instance)
        {
            if (!ReVolt.enablePrefabContent.Value)
                return true;

            if (GameManager.GameState == GameState.Loading || !GameManager.RunSimulation)
                return true;

            GateTriggerRepeatRegistration = true;

            return true;
        }
         
        [HarmonyPostfix, HarmonyPatch(nameof(Cable.OnRegistered))]
        public static void AfterOnRegistered(Cell cell, Cable __instance)
        {
            if (RetriggerRegistration)
            {
                RetriggerRegistration = false;
                GateTriggerRepeatRegistration = false;
                DeferredCableRegistration(__instance).Forget();
            }
            else
                GateTriggerRepeatRegistration = false;
        }

        [HarmonyFinalizer, HarmonyPatch(nameof(Cable.OnRegistered))]
        public static void ForceRegisterCleanup()
        {
            RetriggerRegistration = false;
            GateTriggerRepeatRegistration = false;
        }

        public static async UniTaskVoid DeferredCableRegistration(Cable cable)
        {
            await UniTask.Yield();

            if (cable?.IsBeingDestroyed ?? true)
                return;
            
            var cableNetwork = CableNetwork.Merge(CableNetwork.ConnectedNetworks(cable));
            if (cableNetwork != null)
                cableNetwork.Add(cable);
            else
                _ = new CableNetwork(cable);
        }
    }
}