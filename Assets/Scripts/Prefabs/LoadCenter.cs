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
            PrefabUtils.SetExitTool(this, PrefabNames.Wrench);
        }

        #region Powernet Integration

        public bool HasConflict
        {
            get
            {
                return Error > 0;
            }
            set
            {
                Error = value ? 1 : 0;
            }
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

            for (int i = ToggleSwitches.Count - 1; i >= 0; i--)
                ToggleSwitches[i].RefreshState(skipAnimation);

            for (int i = IndicatorLights.Count - 1; i >= 0; i--)
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
            switch(interactable.Action)
            {
                case InteractableType.Button1:
                case InteractableType.Button2:
                case InteractableType.Button3:
                case InteractableType.Button4:
                case InteractableType.Button5:
                    return $"{base.GetContextualName(interactable)} {(interactable.State > 0 ? "Off" : "On")}";

                default:
                    return base.GetContextualName(interactable);
            }
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
                    PlayPooledAudioSound(IsOpen ? Defines.Sounds.LeverUp : Defines.Sounds.LeverDown, Vector3.zero);

                    return action;

                case InteractableType.Button1:
                case InteractableType.Button2:
                case InteractableType.Button3:
                case InteractableType.Button4:
                case InteractableType.Button5:
                case InteractableType.Button6:
                    if (!doAction)
                        return action.Succeed();

                    PlayPooledAudioSound(interactable.State == 0 ? Defines.Sounds.SwitchOn : Defines.Sounds.SwitchOff, Vector3.zero); 
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
            switch (logicSlotType)
            {
                case LogicSlotType.On:
                    return true;

                default:
                    return base.CanLogicWrite(logicSlotType, slotId);
            }
        }

        public override bool CanLogicRead(LogicSlotType logicSlotType, int slotId)
        {
            switch (logicSlotType)
            {
                case LogicSlotType.On:
                case LogicSlotType.Quantity:
                    return true;

                default:
                    return base.CanLogicWrite(logicSlotType, slotId);
            }
        }

        public override bool CanLogicWrite(LogicType logicType)
        {
            if (logicType == LogicType.On)
                return false;
            return base.CanLogicWrite(logicType);
        }

        public override bool CanLogicRead(LogicType logicType)
        {
            if (logicType == LogicType.On)
                return false;
            return base.CanLogicRead(logicType);
        }

        public override void SetLogicValue(LogicSlotType logicSlotType, int slotId, double value)
        {
            if (logicSlotType == LogicSlotType.On)
            {
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
                switch ((PowerClass)slotId)
                {
                    case PowerClass.Lights:
                        return GetInteractable(InteractableType.Button1).State;
                    case PowerClass.Doors:
                        return GetInteractable(InteractableType.Button2).State;
                    case PowerClass.Atmospherics:
                        return GetInteractable(InteractableType.Button3).State;
                    case PowerClass.Equipment:
                        return GetInteractable(InteractableType.Button4).State;
                    case PowerClass.Logic:
                        return GetInteractable(InteractableType.Button5).State;

                    default:
                        return 0.0;
                }
            }

            if (logicSlotType == LogicSlotType.Quantity || logicSlotType == (LogicSlotType)LogicType.PowerActual)
            {
                if (LoadControlData == null)
                    return 0.0;


                double sum = 0;
                for (int i = LoadControlData.Length - 1; i >= 0; i--)
                    if (LoadControlData[i].Category == (PowerClass)slotId)
                        sum += LoadControlData[i].PowerUsed;
                return sum;
            }

            return base.GetLogicValue(logicSlotType, slotId);
        }

        #endregion
    }
}
