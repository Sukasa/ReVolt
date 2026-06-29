using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using HarmonyLib;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(Stationpedia))]
    internal class StationpediaPatches
    {
        private const string ElectronicsPage = "Electronics"; // Stationpedia._electronicsPage

        [HarmonyPrefix, HarmonyPatch(nameof(Stationpedia.AddElectronicsStationpedia))]
        public static bool AddElectronicsStationpediaPatch(Thing dynamicThing, StationCategoryInsert insert, ref bool __result)
        {
            switch (dynamicThing)
            {
                case BusTie _:
                case CableTray _:
                case Wireway _:
                    Stationpedia.DataHandler.AddNewListItem(ElectronicsPage, dynamicThing.GetStationpediaCategory(), insert);
                    __result = true;
                    return false;

                default:
                    return true;
            }
        }
    }
}