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