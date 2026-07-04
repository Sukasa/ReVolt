using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using LaunchPadBooster.Utils;
using ReVolt.Components;
using ReVolt.Interfaces;
using System.Collections.Generic;
using UnityEngine;
using static ReVolt.RevoltTick;

namespace ReVolt
{
    internal class LoadCenter : Device, IPatchable, ILoadCenter
    {
        public List<ReVoltMultiStateAnimator> ToggleSwitches;
        public List<AssignableBinaryPoweredMaterialChanger> IndicatorLights;
        public ReVoltMultiStateAnimator Door;

        public override string GetStationpediaCategory()
        {
            return Localization.GetInterface(StationpediaCategoryStrings.CableCategory);
        }

        public void PatchPrefab()
        {
            ReVolt.MOD.SetupPrefabs(PrefabName)
                .SetBlueprintMaterials()
                .SetPaintableColor(ColorType.White)
                .SetExitTool(PrefabNames.Wrench);
        }

        #region Powernet Integration

        public bool HasConflict
        {
            get => Error > 0;
            set => Error = value ? 1 : 0;
        }

        public PowerUsage[] LoadControlData { private get; set; }

        public bool EnableLights => GetInteractable(InteractableType.Button1).State > 0;

        public bool EnableDoors => GetInteractable(InteractableType.Button2).State > 0;

        public bool EnableAtmos => GetInteractable(InteractableType.Button3).State > 0;

        public bool EnableEquip => GetInteractable(InteractableType.Button4).State > 0;

        public bool EnableLogic => GetInteractable(InteractableType.Button5).State > 0;

        #endregion

        #region Animations & Interations

        protected async UniTaskVoid RefreshAnimStateFromThread()
        {
            await UniTask.SwitchToMainThread();
            RefreshAnimState();
        }

        protected override void RefreshAnimState(bool skipAnimation = false)
        {
            if (ThreadedManager.IsThread)
            {
                RefreshAnimStateFromThread().Forget();
                return;
            }
            base.RefreshAnimState(skipAnimation);

            for (var i = ToggleSwitches.Count - 1; i >= 0; i--)
                ToggleSwitches[i].RefreshState(skipAnimation);

            for (var i = IndicatorLights.Count - 1; i >= 0; i--)
                IndicatorLights[i].RefreshState(skipAnimation);

            Door.RefreshState(skipAnimation);

        }

        public override void OnInteractableStateChanged(Interactable interactable, int newState, int oldState)
        {
            base.OnInteractableStateChanged(interactable, newState, oldState);

            // Update cached values for animator(s)
            if (interactable != GetInteractable(interactable.Action))
                GetInteractable(interactable.Action).Interact(interactable.State);
        }

        public override void OnFinishedInteractionSync(Interactable interactable)
        {
            base.OnFinishedInteractionSync(interactable);
            if (interactable != GetInteractable(interactable.Action))
                GetInteractable(interactable.Action).Interact(interactable.State);
        }

        public override string GetContextualName(Interactable interactable)
        {
            return interactable.Action switch
            {
                InteractableType.Button1 or InteractableType.Button2 or InteractableType.Button3 or InteractableType.Button4 or InteractableType.Button5 =>
                    $"{base.GetContextualName(interactable)} {(interactable.State > 0 ? "Off" : "On")}",
                _ => base.GetContextualName(interactable)
            };
        }

        public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
        {
            if (interactable == null)
                return null;

            var action = new DelayedActionInstance
            {
                Duration = 0f,
                ActionMessage = interactable.ContextualName,
            };

            switch (interactable.Action)
            {
                case InteractableType.Open: // Open door
                    if (!doAction)
                        return action.Succeed();

                    IsOpen = !IsOpen;
                    PlayPooledAudioSound(IsOpen ? Defines.Sounds.LeverUp : Defines.Sounds.LeverDown, new Vector3(0f, 0f, 0.125f));

                    return action;

                case InteractableType.Button1:
                case InteractableType.Button2:
                case InteractableType.Button3:
                case InteractableType.Button4:
                case InteractableType.Button5:
                case InteractableType.Button6:
                    if (!doAction)
                        return action.Succeed();

                    PlayPooledAudioSound(interactable.State == 0 ? Defines.Sounds.SwitchOn : Defines.Sounds.SwitchOff, new Vector3(0f, 0f, 0.125f)); 
                    interactable.Interact(interactable.State > 0 ? 0 : 1);

                    return action.Succeed();

                default:
                    return base.InteractWith(interactable, interaction, doAction);

            }
        }

        #endregion

        #region Data Networking

        public override bool CanLogicWrite(LogicSlotType logicSlotType, int slotId)
        {
            return logicSlotType switch
            {
                LogicSlotType.On => true,
                _ => base.CanLogicWrite(logicSlotType, slotId)
            };
        }

        public override bool CanLogicRead(LogicSlotType logicSlotType, int slotId)
        {
            return logicSlotType switch
            {
                LogicSlotType.On or LogicSlotType.Quantity => true,
                _ => base.CanLogicRead(logicSlotType, slotId)
            };
        }

        public override bool CanLogicWrite(LogicType logicType) => logicType != LogicType.On && base.CanLogicWrite(logicType);

        public override bool CanLogicRead(LogicType logicType) => logicType != LogicType.On && base.CanLogicRead(logicType);

        public override void SetLogicValue(LogicSlotType logicSlotType, int slotId, double value)
        {
            if (logicSlotType == LogicSlotType.On)
            {
                if (HasConflict)
                    return;
                
                switch ((PowerClass)slotId)
                {
                    case PowerClass.Lights:
                        GetInteractable(InteractableType.Button1).Interact(value > 0 ? 1 : 0);
                        break;
                    case PowerClass.Doors:
                        GetInteractable(InteractableType.Button2).Interact(value > 0 ? 1 : 0);
                        break;
                    case PowerClass.Atmospherics:
                        GetInteractable(InteractableType.Button3).Interact(value > 0 ? 1 : 0);
                        break;
                    case PowerClass.Equipment:
                        GetInteractable(InteractableType.Button4).Interact(value > 0 ? 1 : 0);
                        break;
                    case PowerClass.Logic:
                        GetInteractable(InteractableType.Button5).Interact(value > 0 ? 1 : 0);
                        break;
                }

                return;
            }


            base.SetLogicValue(logicSlotType, slotId, value);
        }

        public override double GetLogicValue(LogicSlotType logicSlotType, int slotId)
        {
            if (logicSlotType == LogicSlotType.On)
            {
                if (HasConflict)
                    return 0f;

                return (PowerClass)slotId switch
                {
                    PowerClass.Lights => GetInteractable(InteractableType.Button1).State,
                    PowerClass.Doors => GetInteractable(InteractableType.Button2).State,
                    PowerClass.Atmospherics => GetInteractable(InteractableType.Button3).State,
                    PowerClass.Equipment => GetInteractable(InteractableType.Button4).State,
                    PowerClass.Logic => GetInteractable(InteractableType.Button5).State,
                    _ => 0.0
                };
            }

            // LogicSlotType contains no available "power actual". and since these slots CANNOT contain actual items, I don't worry about PowerActual being 'ReferenceId'.
            // Instead, if we see an attempt to read Quantity or ReferenceId from a slot, we know the user is REALLY trying to read quantity / power actual of that category
            // so we do not call into the base GetLogicValue.  This way if a user tries to use LogicType.PowerActual, we don't just throw an error
            if (logicSlotType != LogicSlotType.Quantity && logicSlotType != (LogicSlotType)LogicType.PowerActual)
                return base.GetLogicValue(logicSlotType, slotId);
            
            if (LoadControlData == null)
                return 0.0;

            double sum = 0;
            for (var i = LoadControlData.Length - 1; i >= 0; i--)
                if (LoadControlData[i].Category == (PowerClass)slotId)
                    sum += LoadControlData[i].PowerUsed;
            return sum;

        }

        #endregion
    }
}
