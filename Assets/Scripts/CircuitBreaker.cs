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
        public Collider TooltipCollider;
        public InfoScreenComponent InfoScreen;

        [SerializeField]
        private ReVoltMultiStateAnimator _breakerStateAnimator;
        [SerializeField]
        private ReVoltMultiStateAnimator _breakerHandleAnimator;

        private float _tripPoint;

        protected const int FLAG_TRIPSP = 1024;
        protected const int FLAG_MODE = 2048;

        public const int MODE_ON = 2;
        public const int MODE_TRIPPED = 1;
        public const int MODE_OFF = 0;

        public override string[] ModeStrings => _breakerModeStrings;
        static readonly string[] _breakerModeStrings = { "Breaker is Open", "Breaker is Tripped", "Breaker is Closed" };


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
        }

        public void PatchPrefab()
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

                PlayPooledAudioSound(Defines.Sounds.ShutterCloseStop, Vector3.zero);
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
        }

        public override string GetContextualName(Interactable interactable)
        {
            switch (interactable.Action)
            {
                case InteractableType.Button1:
                    return $"Trip Point: {Setting}";

                case InteractableType.Mode:
                    return ModeStrings[InteractMode.State];

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

                    PlayPooledAudioSound(Defines.Sounds.ScrewdriverSound, Vector3.zero);

                    return action;

                case InteractableType.OnOff: // Toggle Breaker
                    if (Mode == MODE_TRIPPED)
                        action.AppendStateMessage(ReVoltStrings.ResetBreakerToClear);

                    if (!doAction)
                        return action.Succeed();

                    if (Mode == MODE_TRIPPED)
                    {
                        OnOff = false;
                        Error = 0;
                    }
                    else
                        OnOff = !OnOff;

                    PlayPooledAudioSound(OnOff ? Defines.Sounds.ApcOn : Defines.Sounds.ApcOff, Vector3.zero);
                    UpdateMode();

                    return action;

                case InteractableType.Mode: // View state
                    if (Mode != MODE_TRIPPED)
                        return action.Succeed();

                    action.AppendStateMessage(ReVoltStrings.ResetBreakerToClear);
                    return action.Fail();

                case InteractableType.Button2: // Close Breaker
                    return action.Fail("Not Implemented");

                case InteractableType.Button3: // Open Breaker
                    return action.Fail("Not Implemented");

                case InteractableType.Button4: // Trip Test
                    return action.Fail("Not Implemented");

                case InteractableType.Button5: // Cycle Input Conn.
                    return action.Fail("Not Implemented");

                case InteractableType.Button6: // Cycle Output Conn.
                    return action.Fail("Not Implemented");

                case InteractableType.Button7: // Cycle Data Conn.
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
            // NOP out - let powered state be set by the PowerTick, if at all
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
            {
                RefreshAnimState();
            }
        }

        protected void UpdateMode() => UpdateMode(Error > 0 ? MODE_TRIPPED : (OnOff ? MODE_ON : MODE_OFF));

        protected virtual string StateTooltip()
        {
            var sb = new StringBuilder();

            sb.Append(ModeStrings[Mode]);
            if (Mode == MODE_TRIPPED)
                sb.AppendLine(ReVoltStrings.ResetBreakerToClear);

            return sb.ToString();

        }
        
        protected virtual string InfoScreenTooltip()
        {
            if (!Powered)
                return "The screen is dark";

            var sb = new StringBuilder();

            sb.AppendLine(ModeStrings[Mode]);
            if (Mode == MODE_TRIPPED)
                sb.AppendLine(ReVoltStrings.ResetBreakerToClear);

            if (OutputNetwork != null)
            {
                sb.Append(GameStrings.CableAnalyserActual.AsString(OutputNetwork.CurrentLoad.ToStringPrefix("W", "yellow")));
                sb.AppendLine();
                sb.Append(GameStrings.CableAnalyserRequired.AsString(OutputNetwork.RequiredLoad.ToStringPrefix("W", "yellow")));
                sb.AppendLine();
                sb.Append(GameStrings.CableAnalyserPotential.AsString(OutputNetwork.PotentialLoad.ToStringPrefix("W", "yellow")));

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
        }

        public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
        {
            base.ProcessUpdate(reader, networkUpdateType);

            if (IsNetworkUpdateRequired(FLAG_TRIPSP, networkUpdateType))
                Setting = (float)reader.ReadDouble();

            if (IsNetworkUpdateRequired(FLAG_MODE, networkUpdateType))
                UpdateMode(reader.ReadInt32());
        }

        public override void SerializeOnJoin(RocketBinaryWriter writer)
        {
            base.SerializeOnJoin(writer);

            writer.WriteDouble(Setting);
            writer.WriteDouble(_transferredLast);
            writer.WriteInt32(Mode);
        }

        public override void DeserializeOnJoin(RocketBinaryReader reader)
        {
            base.DeserializeOnJoin(reader);

            Setting = (float)reader.ReadDouble();
            _transferredLast = (float)reader.ReadDouble();
            var NewMode = reader.ReadInt32();

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
            base.DeserializeSave(baseData);
            if (baseData is not CircuitBreakerSaveData saveData)
                return;

            Setting = saveData.TripPoint;
            _transferredLast = saveData.TransferredLast;

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
                case LogicType.On:
                case LogicType.Mode:
                case LogicType.Error:
                    return isSmartBreaker;

                default:
                    return base.CanLogicRead(logicType);
            }
        }

        public override bool CanLogicWrite(LogicType logicType)
        {
            return (logicType == LogicType.Setting || logicType == LogicType.Activate || logicType == LogicType.On) && isSmartBreaker;
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
                case LogicType.PowerActual:
                    return _transferredLast;
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
