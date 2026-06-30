using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using HarmonyLib;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(Prefab))]
    public class PrefabPatcherPatches
    {
        [HarmonyPrefix, HarmonyPatch(nameof(Prefab.LoadAll))]
        public static void LoadPatchesPatch()
        {
            foreach (var sourcePrefab in WorldManager.Instance.SourcePrefabs)
            {
                if (sourcePrefab?.GetComponent<Thing>() is Battery BatteryPrefab)
                {
                    BatteryPrefab.PowerMaximum *= ReVolt.configBatteryCapacityFactor.Value;
                }
            }
        }
    }
}