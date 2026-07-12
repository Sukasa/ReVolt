using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
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
            var num = Animator.StringToHash(stateName);

            // Bugfix: original code returned InteractButton1 instead of InteractButton3
            if (num == Interactable.Button3State && __instance.HasButton3State)
                interactable = __instance.InteractButton3;

            // Feature add: support Button4 and Button5 states
            if (num == Animator.StringToHash("Button4"))
            {
                interactable = __instance.GetInteractable(InteractableType.Button4);
                __result = interactable != null; // Bugfix per tom_is_unlucky: return true only if interactable not null
            }

            if (num == Animator.StringToHash("Button5"))
            {
                interactable = __instance.GetInteractable(InteractableType.Button5);
                __result = interactable != null; // Bugfix per tom_is_unlucky: return true only if interactable not null
            }

            if (num == Animator.StringToHash("Slot1"))
            {
                interactable = __instance.GetInteractable(InteractableType.Slot1);
                __result = interactable != null;
            }

            if (num == Animator.StringToHash("Slot2"))
            {
                interactable = __instance.GetInteractable(InteractableType.Slot2);
                __result = interactable != null;
            }
        }

        // Sprayer fix - patches where Thing calls ISprayer.DoSpray() because I cannot directly patch an interface static method in harmony

        [HarmonyTranspiler, HarmonyPatch(nameof(Thing.AttackWith))]
        public static IEnumerable<CodeInstruction> RenderPatch(IEnumerable<CodeInstruction> instructions)
        {
            var BaseFunc = AccessTools.Method(typeof(ISprayer), nameof(ISprayer.DoSpray));
            var InjectFunc = SymbolExtensions.GetMethodInfo(() => DoSprayPostfix(null, null, false));

            foreach (var instruction in instructions)
            {
                if (instruction.Calls(BaseFunc))
                    instruction.operand = InjectFunc;

                yield return instruction;
            }
        }

        [UsedImplicitly] // Transpiled postfix on an interface static method
        public static Thing.DelayedActionInstance DoSprayPostfix(Thing thing, ISprayer sprayer, bool doAction)
        {
            var result = ISprayer.DoSpray(thing, sprayer, doAction);

            if (thing is not Cable cable || !doAction || !GameManager.RunSimulation || result.IsDisabled)
                return result;

            Span<SmallCellRef> cableTrays = new SmallCellRef[6];
            var count = 0;
            cable.FillConnected<CableTray>(cableTrays, ref count);

            if (count > 0) // Defer the repatch so that the network painter / whatever else have a chance to finish
                DeferredRepatch(cable).Forget();

            return result;
        }

        public static async UniTaskVoid DeferredRepatch(Cable cable)
        {
            await UniTask.Yield();

            Operation();
            return;

            unsafe void Operation() // Local function wrapper to satisfy no-span-in-async compiler constraint
            {
                Span<SmallCellRef> cells = stackalloc SmallCellRef[6];
                var count = 0;
                cable.FillConnected<CableTray>(cells, ref count);

                for (var idx = count - 1; idx >= 0; idx--)
                {
                    var tray = cells[idx].Get<CableTray>();
                    if (tray is null) continue;

                    tray.OnMemberRemoved(null);
                    tray.OnMembersChanged();
                }
            }
        }
    }
}