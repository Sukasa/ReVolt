using Assets.Scripts;
using Assets.Scripts.GridSystem;
using HarmonyLib;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(GameManager))]
    public class GameManagerPatches
    {
        [HarmonyPostfix, HarmonyPatch("OnGameStateChanged")]
        public static void OnGameStateChangedPatch(GameState oldState, GameState newState)
        {
            if (newState is GameState.Loading or GameState.None)
                CableTray.AllTrays.Clear();
        }
    }
}