using Assets.Scripts;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using ReVolt.Assets.Scripts;
using ReVolt.Components;
using ReVolt.Interfaces;
using StationeersMods.Interface;
using System.Text;
using UnityEngine;

namespace ReVolt
{
    public class CircuitBreaker : ElectricalInputOutput, IPatchable, ISetable, IBreaker
    {
        public float MaxTripCurrent;
        public float MinTripCurrent;
        public float DeltaTripCurrent;
        public float MaxInterruptCurrent;
        public bool isSmartBreaker;
        public bool canRemoteControl;
        public Collider TooltipCollider;
        public InfoScreenComponent InfoScreen;
        public BreakerStatusScreen StatusIndicator;

        [SerializeField]
        private ReVoltMultiStateAnimator _breakerStateAnimator;
        [SerializeField]
        private ReVoltMultiStateAnimator _breakerHandleAnimator;

        private float _tripPoint;

        protected const int FLAG_TRIPSP = 1024;
        protected const int FLAG_MODE = 2048;
        protected const int FLAG_CONNECTIONS = 4096;

        public const int MODE_ON = 2;
        public const int MODE_TRIPPED = 1;
        public const int MODE_OFF = 0;

        public override string[] ModeStrings => _breakerModeStrings;
        static string[] _breakerModeStrings = { };
        static string[] _ColouredModeStrings = { };

        // Moved here from Heavy Breaker.  This way I'm not having to have overrides on overrides in the serialization code (just keeps things cleaner)
        protected readonly long[] ConnectionRefIds = new long[3];


        public float LimitCurrent => Mode == MODE_ON ? (float)Setting : 0.0f;

        [ByteArraySync]
        public double Setting
        {
            get => _tripPoint;
            set
            {
                _tripPoint = Mathf.Clamp((float)value, 1000.0f, MaxTripCurrent);
                if (NetworkManager.IsServer)
                    NetworkUpdateFlags |= FLAG_TRIPSP;
            }
        }

        private float _deficit;
        private float _transferred;
        private float _transferredLast;
        private System.Random RNG;

        public override void Awake()
        {
            base.Awake();
            RNG = new System.Random((int)ReferenceId);
            if (Setting == 0.0)
                Setting = MinTripCurrent;

            if (_breakerModeStrings.Length == 0)
            {
                _breakerModeStrings = new string[3] {
                    ReVoltStrings.RevoltBreakerOpen,
                    ReVoltStrings.RevoltBreakerTrippedNetwork,
                    ReVoltStrings.RevoltBreakerClosed,
                };

                _ColouredModeStrings = new string[3] {
                    ReVoltStrings.RevoltBreakerOpen,
                    ReVoltStrings.RevoltBreakerTripped,
                    ReVoltStrings.RevoltBreakerClosed,
                };
            }
        }

        public virtual void PatchPrefab()
        {
            BuildStates[0].Tool.ToolExit = StationeersModsUtility.FindTool(StationeersTool.WRENCH);
            BuildStates[1].Tool.ToolEntry = StationeersModsUtility.FindTool(StationeersTool.DRILL);
            BuildStates[1].Tool.ToolExit = StationeersModsUtility.FindTool(StationeersTool.DRILL);
        }

        public override string GetStationpediaCategory()
        {
            return Localization.GetInterface(StationpediaCategoryStrings.CableCategory);
        }

        #region Power Simulation

        public override bool AllowSetPower(CableNetwork cableNetwork)
        {
            return InputNetwork == cableNetwork;
        }

        public void Trip()
        {
            if (Mode == MODE_ON)
            {
                UpdateModeNextFrame(MODE_TRIPPED).Forget();

                PlayPooledAudioSound(Defines.Sounds.ShutterCloseStop, new Vector3(0f, 0f, 0.125f));
            }
        }

        public bool CanSupplyPower(CableNetwork cableNetwork) => Mode == MODE_ON && (cableNetwork == OutputNetwork) && (InputNetwork.PotentialLoad - _deficit) > 0.0f;

        protected override bool IsOperable => (Mode == MODE_ON) && base.IsOperable;

        public override void OnPowerTick()
        {
            base.OnPowerTick();

            if (_transferred > Setting && Mode == MODE_ON)
            {
                // Do a cheap calculation to get the % chance to trip the breaker
                var tripChance = Mathf.Pow(_transferred / (float)Setting, 1.25f) - 1.0f;

                // If we fail the chance then trip, *or* if we're a smart breaker then trip instantly.
                if (isSmartBreaker || (float)RNG.NextDouble() <= tripChance * ReVolt.configCableBurnFactor.Value)
                    Trip();

            }
            _transferredLast = _transferred;
            _transferred = 0f;
        }

        public override void UsePower(CableNetwork cableNetwork, float powerUsed)
        {
            if (!IsOperable || cableNetwork != OutputNetwork)
                return;

            _transferred += powerUsed;
            _deficit += powerUsed;
        }

        public override void ReceivePower(CableNetwork cableNetwork, float powerAdded)
        {
            if (!IsOperable || cableNetwork != InputNetwork)
                return;

            _deficit -= powerAdded - UsedPower;
        }

        public override float GetUsedPower(CableNetwork cableNetwork)
        {
            if (cableNetwork != InputNetwork)
                return 0.0f;

            if (!IsOperable)
                return base.IsOperable && isSmartBreaker ? UsedPower : 0.0f;

            return _deficit + UsedPower;
        }

        public override float GetGeneratedPower(CableNetwork cableNetwork)
        {
            if (!IsOperable || cableNetwork != OutputNetwork || InputNetwork == null)
                return 0f;

            return InputNetwork.PotentialLoad - _deficit;
        }

        #endregion

        #region Animations & Interactions

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
            _breakerStateAnimator?.RefreshState(skipAnimation);
            _breakerHandleAnimator?.RefreshState(skipAnimation);
            InfoScreen?.RefreshState(this);
            StatusIndicator?.RefreshState(this);
        }

        public override string GetContextualName(Interactable interactable)
        {
            return interactable.Action switch
            {
                InteractableType.Button1 => $"Trip Point: {Setting}",
                InteractableType.Mode => ModeStrings[InteractMode.State],
                _ => base.GetContextualName(interactable),
            };
        }

        public override void UpdateStateVisualizer(bool visualOnly = false)
        {
            base.UpdateStateVisualizer(visualOnly);
            if (CurrentBuildStateIndex == 1)
                RefreshAnimState();
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
                case InteractableType.Button1: // Cycle Setpoint
                    if (!interaction.SourceSlot.Contains<Screwdriver>())
                        return action.Fail(GameStrings.RequiresScrewdriver);

                    var dir = DeltaTripCurrent;

                    if (KeyManager.GetButton(KeyMap.QuantityModifier))
                        dir = -DeltaTripCurrent;
                    else
                        action.ExtendedMessage = ReVoltStrings.HoldForPreviousTrip;

                    var newSetting = Setting + dir;
                    if (newSetting > MaxTripCurrent)
                        newSetting = MinTripCurrent;
                    if (newSetting < MinTripCurrent)
                        newSetting = MaxTripCurrent;

                    action.AppendStateMessage(GameStrings.GlobalChangeSettingTo, newSetting.ToString());

                    if (!doAction)
                        return action.Succeed();

                    Setting = newSetting;

                    PlayPooledAudioSound(Defines.Sounds.ScrewdriverSound, new Vector3(0f, 0f, 0.125f));

                    return action;

                case InteractableType.OnOff: // Toggle Breaker
                    if (Mode == MODE_TRIPPED)
                        action.AppendStateMessage(ReVoltStrings.ResetBreakerToClear);

                    if (!doAction)
                        return action.Succeed();

                    var newOnOff = Mode == MODE_OFF;

                    if (Mode == MODE_TRIPPED)
                        Error = 0;

                    OnOff = newOnOff;

                    PlayPooledAudioSound(OnOff ? Defines.Sounds.ApcOn : Defines.Sounds.ApcOff, new Vector3(0f, 0f, 0.125f));
                    UpdateMode();

                    return action;

                case InteractableType.Mode: // View state
                    if (Mode != MODE_TRIPPED)
                        return action.Succeed();

                    action.AppendStateMessage(ReVoltStrings.ResetBreakerToClear);
                    return action.Fail();

                case InteractableType.Button2: // Close Breaker
                    if (Mode != MODE_OFF)
                        action.Fail(ReVoltStrings.RevoltBreakerNotOpen);

                    if (!doAction)
                        return action.Succeed();

                    OnOff = true;

                    PlayPooledAudioSound(Defines.Sounds.ApcOn, new Vector3(0f, 0f, 0.25f));
                    UpdateMode();

                    return action;


                case InteractableType.Button3: // Open Breaker
                    if (Mode == MODE_OFF)
                        action.Fail(ReVoltStrings.RevoltBreakerAlreadyOpen);

                    if (!doAction)
                        return action.Succeed();

                    if (Mode != MODE_OFF)
                    {
                        OnOff = false;
                        Error = 0;
                    }

                    PlayPooledAudioSound(Defines.Sounds.ApcOff, new Vector3(0f, 0f, 0.25f));
                    UpdateMode();

                    return action;

                case InteractableType.Button4: // Test (trip) Breaker
                    return action.Fail("Not Implemented");                           

                default:
                    return base.InteractWith(interactable, interaction, doAction);

            }
        }

        public override void OnFinishedInteractionSync(Interactable interactable)
        {
            base.OnFinishedInteractionSync(interactable);

            if (NetworkManager.IsServer)
                NetworkUpdateFlags |= FLAG_MODE;

            if (NetworkManager.IsClient)
            {
                if (interactable != GetInteractable(interactable.Action))
                    GetInteractable(interactable.Action).Interact(interactable.State);
            }
        }

        protected async UniTaskVoid UpdateModeNextFrame(int NewMode, bool SkipAnimation = false)
        {
            await UniTask.NextFrame();
            UpdateMode(NewMode, SkipAnimation);
        }

        protected override void AssessPower(CableNetwork cableNetwork, bool isOn)
        {
            // NOP out
        }

        protected void UpdateMode(int NewMode, bool SkipAnimation = false)
        {
            OnServer.Interact(GetInteractable(InteractableType.Mode), NewMode, SkipAnimation);

            var _error = NewMode == MODE_TRIPPED ? 1 : 0;
            var _onOff = NewMode > 0;

            if (Error != _error)
                Error = _error;

            if (OnOff != _onOff)
                OnOff = _onOff;

            if (Mode != NewMode)
                Mode = NewMode;

            if (NetworkManager.IsServer)
                NetworkUpdateFlags |= FLAG_MODE;

            if (NetworkManager.IsClient)
                RefreshAnimState();
        }

        protected void UpdateMode() => UpdateMode(Error > 0 ? MODE_TRIPPED : (OnOff ? MODE_ON : MODE_OFF));

        protected virtual string StateTooltip()
        {
            var sb = new StringBuilder();

            sb.AppendLine(_ColouredModeStrings[Mode]);
            if (Mode == MODE_TRIPPED)
                sb.AppendLine(ReVoltStrings.ResetBreakerToClear);

            return sb.ToString();

        }

        protected virtual string IndicatorTooltip()
        {
            if (!Powered)
                return "The screen is dark";

            var sb = new StringBuilder();

            sb.AppendLine(_ColouredModeStrings[Mode]);
            if (Mode == MODE_TRIPPED)
                sb.AppendLine(ReVoltStrings.ResetBreakerToClear);

            return sb.ToString();

        }

        protected virtual string InfoScreenTooltip()
        {
            if (!Powered)
                return "The screen is dark";

            var sb = new StringBuilder();

            sb.AppendLine(_ColouredModeStrings[Mode]);
            if (Mode == MODE_TRIPPED)
                sb.AppendLine(ReVoltStrings.ResetBreakerToClear);

            sb.Append("Current Trip Point is <color=yellow>");
            sb.Append(Setting.ToStringPrefix("W", "yellow", true));
            sb.AppendLine("</color>");

            if (OutputNetwork != null)
            {
                sb.Append("Providing ");
                sb.AppendLine(_transferredLast.ToStringPrefix("W", "yellow"));
                sb.AppendLine();
                sb.AppendLine("Network:");
                sb.Append(GameStrings.CableAnalyserActual.AsString(OutputNetwork.CurrentLoad.ToStringPrefix("W", "yellow")));
                sb.AppendLine();
                sb.Append(GameStrings.CableAnalyserRequired.AsString(OutputNetwork.RequiredLoad.ToStringPrefix("W", "yellow")));
                sb.AppendLine();
                sb.Append(GameStrings.CableAnalyserPotential.AsString(OutputNetwork.PotentialLoad.ToStringPrefix("W", "yellow")));

            }
            else
            {
                sb.Append(ReVoltStrings.SmartBreakerNoCableFound);
            }


            return sb.ToString();

        }

        public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
        {
            if (InfoScreen != null && hitCollider == InfoScreen.InfoTrigger)
                return new PassiveTooltip(toDefault: true)
                {
                    Title = DisplayName,
                    Extended = InfoScreenTooltip()
                };
            if (StatusIndicator != null && hitCollider == StatusIndicator.InfoTrigger)
                return new PassiveTooltip(toDefault: true)
                {
                    Title = DisplayName,
                    Extended = IndicatorTooltip()
                };

            if (hitCollider == TooltipCollider)
                return new PassiveTooltip(toDefault: true)
                {
                    Title = DisplayName,
                    Extended = StateTooltip()
                };


            return base.GetPassiveTooltip(hitCollider);
        }

        #endregion

        #region Persistence & Sync

        public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
        {
            base.BuildUpdate(writer, networkUpdateType);

            if (IsNetworkUpdateRequired(FLAG_TRIPSP, networkUpdateType))
                writer.WriteDouble(Setting);

            if (IsNetworkUpdateRequired(FLAG_MODE, networkUpdateType))
                writer.WriteInt32(Mode);

            if (IsNetworkUpdateRequired(FLAG_CONNECTIONS, networkUpdateType))
            {
                writer.WriteInt64(ConnectionRefIds[0]);
                writer.WriteInt64(ConnectionRefIds[1]);
                writer.WriteInt64(ConnectionRefIds[2]);
            }
        }

        public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
        {
            base.ProcessUpdate(reader, networkUpdateType);

            if (IsNetworkUpdateRequired(FLAG_TRIPSP, networkUpdateType))
                Setting = (float)reader.ReadDouble();

            if (IsNetworkUpdateRequired(FLAG_MODE, networkUpdateType))
                UpdateMode(reader.ReadInt32());

            if (IsNetworkUpdateRequired(FLAG_CONNECTIONS, networkUpdateType))
            {
                ConnectionRefIds[0] = reader.ReadInt64();
                ConnectionRefIds[1] = reader.ReadInt64();
                ConnectionRefIds[2] = reader.ReadInt64();
            }
        }

        public override void SerializeOnJoin(RocketBinaryWriter writer)
        {
            base.SerializeOnJoin(writer);

            writer.WriteDouble(Setting);
            writer.WriteDouble(_transferredLast);
            writer.WriteInt32(Mode);
            writer.WriteInt64(ConnectionRefIds[0]);
            writer.WriteInt64(ConnectionRefIds[1]);
            writer.WriteInt64(ConnectionRefIds[2]);
        }

        public override void DeserializeOnJoin(RocketBinaryReader reader)
        {
            base.DeserializeOnJoin(reader);

            Setting = (float)reader.ReadDouble();
            _transferredLast = (float)reader.ReadDouble();
            var NewMode = reader.ReadInt32();

            ConnectionRefIds[0] = reader.ReadInt64();
            ConnectionRefIds[1] = reader.ReadInt64();
            ConnectionRefIds[2] = reader.ReadInt64();

            UpdateModeNextFrame(NewMode).Forget();
        }

        public override ThingSaveData SerializeSave()
        {
            var saveData = new CircuitBreakerSaveData();
            var baseData = saveData as ThingSaveData;
            InitialiseSaveData(ref baseData);
            return saveData;
        }

        public override void DeserializeSave(ThingSaveData baseData)
        {
            if (baseData is not CircuitBreakerSaveData saveData)
                return;

            base.DeserializeSave(baseData);

            Setting = saveData.TripPoint;
            _transferredLast = saveData.TransferredLast;

            if (saveData.ConnectionRefs != null)
            {
                ConnectionRefIds[0] = saveData.ConnectionRefs[0];
                ConnectionRefIds[1] = saveData.ConnectionRefs[1];
                ConnectionRefIds[2] = saveData.ConnectionRefs[2];
            }

            UpdateModeNextFrame(saveData.Mode).Forget();
        }

        protected override void InitialiseSaveData(ref ThingSaveData baseData)
        {
            base.InitialiseSaveData(ref baseData);
            if (baseData is not CircuitBreakerSaveData saveData)
                return;

            saveData.TripPoint = (float)Setting;
            saveData.TransferredLast = _transferredLast;
            saveData.Mode = Mode;
            saveData.ConnectionRefs = ConnectionRefIds;
        }

        public override void OnFinishedLoad()
        {
            base.OnFinishedLoad();

            if (Setting > MaxTripCurrent)
                Setting = MaxTripCurrent;
            if (Setting < MinTripCurrent)
                Setting = MinTripCurrent;

            UpdateMode();
        }
        #endregion

        #region Data Networking

        public override bool CanLogicRead(LogicType logicType)
        {
            switch (logicType)
            {
                case LogicType.Setting:
                case LogicType.Maximum:
                case LogicType.Ratio:
                case LogicType.PowerActual:
                case LogicType.PowerGeneration:
                case LogicType.PowerPotential:
                case LogicType.PowerRequired:
                case LogicType.RequiredPower:
                case LogicType.On:
                case LogicType.Mode:
                case LogicType.Error:
                case LogicType.Power:
                    return isSmartBreaker;

                default:
                    return base.CanLogicRead(logicType);
            }
        }

        public override bool CanLogicWrite(LogicType logicType)
        {
            return (logicType == LogicType.Setting || logicType == LogicType.Activate || logicType == LogicType.On) && canRemoteControl;
        }

        public override double GetLogicValue(LogicType logicType)
        {
            switch (logicType)
            {
                case LogicType.Setting:
                    return Setting;
                case LogicType.Maximum:
                    return MaxTripCurrent;
                case LogicType.Ratio:
                    return Setting / MaxTripCurrent;
                case LogicType.PowerGeneration:
                    return _transferredLast;
                case LogicType.PowerActual:
                    return OutputNetwork.CurrentLoad;
                case LogicType.PowerRequired:
                    return OutputNetwork.RequiredLoad;
                case LogicType.PowerPotential:
                    return OutputNetwork.PotentialLoad;
                case LogicType.RequiredPower:
                    return UsedPower;
                default:
                    return base.GetLogicValue(logicType);
            }
        }

        public override void SetLogicValue(LogicType logicType, double value)
        {
            if (!isSmartBreaker)
                return;

            base.SetLogicValue(logicType, value);

            if (logicType != LogicType.Setting)
                return;

            Setting = value.Clamp(0.0, MaxTripCurrent);
        }

        #endregion

    }
}
