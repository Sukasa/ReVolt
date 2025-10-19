using Assets.Scripts.Objects;
using HarmonyLib;
using UnityEngine;

namespace ReVolt.Patches
{
    [HarmonyPatch(typeof(Thing))]
    public class ThingPatches
    {

        [HarmonyPostfix]
        [HarmonyPatch("HasState")]
        public static void FixStateIssues(string stateName, ref Interactable interactable, Thing __instance, ref bool __result)
        {
            int num = Animator.StringToHash(stateName);

            // Bugfix: original code returned InteractButton1 instead of InteractButton3
            if (num == Interactable.Button3State && __instance.HasButton3State)
                interactable = __instance.InteractButton3;
            
            // Feature add: support Button4 and Button5 states
            if (num == Animator.StringToHash("Button4"))
            {
                interactable = __instance.GetInteractable(InteractableType.Button4);
                __result = true;
            }

            if (num == Animator.StringToHash("Button5"))
            {
                interactable = __instance.GetInteractable(InteractableType.Button5);
                __result = true;
            }
        }
    }
}
