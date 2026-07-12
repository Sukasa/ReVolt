using System.Collections.Generic;
using System.Reflection.Emit;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using HarmonyLib;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(SPUMesonScanner))]
    public class SPUMesonScannerPatches
    {
        [HarmonyTranspiler, HarmonyPatch("RenderMeshes")]
        public static IEnumerable<CodeInstruction> RenderMeshesPatch(IEnumerable<CodeInstruction> instructions)
        {
            var found = false;
            foreach (var instruction in instructions)
            {
                if (instruction.IsStloc())
                {
                    var localIndex = instruction.operand switch
                    {
                        LocalBuilder lb => lb.LocalIndex,
                        int index => index,
                        _ => -1
                    };

                    if (localIndex == 5)
                    {
                        if (!found) // Patch the second assignment to sourceIndex, not the first
                            found = true;
                        else
                        {
                            yield return new CodeInstruction(OpCodes.Ldloc, 5);
                            yield return new CodeInstruction(OpCodes.Add);
                        }
                    }
                }

                yield return instruction;
            }
        }
        
        [HarmonyTranspiler, HarmonyPatch(nameof(SPUMesonScanner.Render))]
        public static IEnumerable<CodeInstruction> RenderPatch(IEnumerable<CodeInstruction> instructions)
        {
            var RenderFunc = AccessTools.Method(typeof(SPUMesonScanner), "RenderMeshes");
            var CacheTrays = SymbolExtensions.GetMethodInfo(() => CacheCableTrays());
            
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(RenderFunc))
                    yield return new CodeInstruction(OpCodes.Call, CacheTrays);
                
                yield return instruction;
            }
        }

        [HarmonyReversePatch, HarmonyPatch("AddToBatch")]
        private static void AddToBatch(Structure structure)
        {
        }

        public static void CacheCableTrays()
        {
            for (var index = CableTray.AllTrays.Count - 1; index >= 0; index--)
            {
                var tray = CableTray.AllTrays[index];
                if (!tray.IsOccluded)
                    AddToBatch(tray);
            }
        }
    }
}