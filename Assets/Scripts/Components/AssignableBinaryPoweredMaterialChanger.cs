using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;

namespace ReVolt.Components
{
    public class AssignableBinaryPoweredMaterialChanger : GameBase
    {
        [SerializeField]
        protected Thing parentThing;

        [SerializeField]
        private InteractableType interactableAction;
        private Interactable _interactable;

        [SerializeField]
        private MaterialChanger _materialChanger;

        public Mesh OffMesh;
        public Mesh OnMesh;

        [SerializeField]
        private Collider infoTrigger;

        private bool _initialised;

        public Collider InfoTrigger => infoTrigger;

        private void Awake() => Init();

        private Task _errorTask;
        public virtual void Init()
        {
            if (!_initialised && parentThing == null)
                    parentThing = GetComponentInParent<Thing>();
            
            _initialised = true;

            if (_interactable != null || parentThing == null)
                return;

            _interactable = parentThing.GetInteractable(interactableAction);
            
        }

        private async UniTask ErrorBlink()
        {
            bool _tickOffset = false;
            while (parentThing != null && parentThing.Powered && parentThing.Error > 0)
            {
                _tickOffset = !_tickOffset;
                _materialChanger.ChangeState(_tickOffset ? Defines.Animator.Error1 : Defines.Animator.Error0);
                await UniTask.Delay(500);
            }
            _errorTask = null;
        }

        public void RefreshState(bool skipAnimation = false)
        {
            var Powered = parentThing.Powered;
            var useState = _interactable == null ? false : _interactable.State > 0;
            var Errored = parentThing.Error > 0;

            if (Powered && useState && !Errored)
                GetComponent<MeshFilter>().mesh = OnMesh;
            else
                GetComponent<MeshFilter>().mesh = OffMesh;

            if (!Powered)
                _materialChanger.ChangeState(Defines.Animator.NotPowered);
            else
            {
                if (Errored)
                {
                    if (_errorTask == null || _errorTask.IsCompleted)
                        _errorTask = ErrorBlink().AsTask();
                }
                else if (useState)
                    _materialChanger.ChangeState(Defines.Animator.OnPowered);
                else
                    _materialChanger.ChangeState(Defines.Animator.OffPowered);
            }
            

        }
    }
}
