using System;
using System.Collections.Generic;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using HarmonyLib;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(SmallGrid))]
    public class SmallGridPatches
    {
        // Patch to allow cable trays and cables to intersect with their open ends
        [HarmonyPrefix, HarmonyPatch(nameof(SmallGrid.IsPipeEndCollision))]
        public static bool IsPipeEndCollisionPatch(SmallGrid smallGrid, SmallGrid __instance, ref bool __result)
        {
            if (!ReVolt.enablePrefabContent.Value)
                return true;

            if (!((__instance is Cable && smallGrid is CableTray) | (__instance is CableTray && smallGrid is Cable)))
                return true;

            __result = false;
            return false;
        }
    }
}