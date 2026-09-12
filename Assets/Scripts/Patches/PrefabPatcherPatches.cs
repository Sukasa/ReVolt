using System;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using HarmonyLib;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(Prefab))]
    public class PrefabPatcherPatches
    {
        
        [HarmonyPrefix, HarmonyPatch(nameof(Prefab.LoadAll))]
        public static void LoadAllPatch()
        {
            foreach (var sourcePrefab in WorldManager.Instance.SourcePrefabs)
            {
                if (sourcePrefab?.GetComponent<Thing>() is Battery BatteryPrefab && ReVolt.enableBatteryLimitsPatch.Value)
                {
                    BatteryPrefab.PowerMaximum *= ReVolt.configBatteryCapacityFactor.Value;
                }

                if (sourcePrefab?.GetComponent<Thing>() is CableToolBelt beltPrefab)
                {
                    if (!ReVolt.enablePrefabContent.Value)
                        continue;

                    int len;
                    for (var i = 0; i < beltPrefab.SlotCount; i++)
                        if (beltPrefab.Slots[i].SpecificTypePrefabHashes is not null && (len = beltPrefab.Slots[i].SpecificTypePrefabHashes.Length) > 0)
                        {
                            Array.Resize(ref beltPrefab.Slots[i].SpecificTypePrefabHashes, len + 1);
                            beltPrefab.Slots[i].SpecificTypePrefabHashes[len] = -935097351; // Hardcoded hash for ItemKitCableTray
                        }
                
                }
                
                if (sourcePrefab?.GetComponent<Thing>() is Transformer transformerPrefab)
                {
                    transformerPrefab.OutputMaximum = transformerPrefab.PrefabName switch
                    {
                        "StructureTransformer" => ReVolt.largeTransformerMaxSetting.Value,
                        "StructureTransformerMedium" or "StructureTransformerMedium(Reversed)" => ReVolt.mediumTransformerMaxSetting.Value,
                        _ => transformerPrefab.OutputMaximum
                    };
                }
            }
        }
    }
}