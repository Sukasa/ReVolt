using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
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
        public static bool IsPipeEndCollisionPatch(SmallGrid  smallGrid, SmallGrid __instance, ref bool __result)
        {
            if (!ReVolt.enablePrefabContent.Value)
                return true;

            if (!((__instance is Cable && smallGrid is CableTray) | (__instance is CableTray && smallGrid is Cable)))
                return true;
            
            __result = false;
            return false;
        }

        [HarmonyPostfix, HarmonyPatch(nameof(SmallGrid.ConnectedCables), new Type[] {})]
        public static void ConnectedCablesPostfix(ref List<Cable> __result, SmallGrid __instance)
        {
            if (__instance is not Cable cable)
                return;
            
            foreach (var openEnd in cable.OpenEnds)
            {
                var smallCell = cable.GridController.GetSmallCell(cable.GridController.WorldToLocalGrid(openEnd.Transform.position, SmallGrid.SmallGridSize,
                    SmallGrid.SmallGridOffset));
                
                if (smallCell is not { Other: CableTray Tray } || smallCell.Other == cable || !smallCell.Other.IsConnected(openEnd))
                    continue;
                
                if (CablePatches.GateTriggerRepeatRegistration)
                    CablePatches.RetriggerRegistration = true;
                else
                    Tray.MatchCables(__result, cable);
            }
        }
        
    }
}