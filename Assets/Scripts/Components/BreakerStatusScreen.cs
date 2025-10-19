using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;

namespace ReVolt.Components
{
    public class BreakerStatusScreen : GameBase
    {
        [SerializeField]
        protected Thing parentThing;

        [SerializeField]
        private MaterialChanger _materialChanger;

        [SerializeField]
        private Collider infoTrigger;

        [SerializeField]
        private InteractableType interactableAction;

        public Mesh OnMesh;

        public Collider InfoTrigger => infoTrigger;

        private Task _trippedAnimTask;

        CircuitBreaker _cachedParent;

        public Mesh BaseMesh;


        public static readonly int TripPowered = Animator.StringToHash("TripPowered");

        public static readonly int TripPowered2 = Animator.StringToHash("TripPowered2");

        private async UniTask TrippedAnim()
        {
            bool _tickOffset = false;
            while (_cachedParent != null && _cachedParent.Powered && _cachedParent.Mode == CircuitBreaker.MODE_TRIPPED)
            {
                _tickOffset = !_tickOffset;
                _materialChanger.ChangeState(_tickOffset ? TripPowered : TripPowered2);
                await UniTask.Delay(500);
            }
            _trippedAnimTask = null;
        }

        public void RefreshState(CircuitBreaker parent = null)
        {
            if (parent == null)
                parent = _cachedParent;

            _cachedParent = parent;

            if (parent == null)
                return;

            GetComponent<MeshFilter>().mesh = BaseMesh;

            if (!parent.Powered)
            {
                _materialChanger.ChangeState(Defines.Animator.NotPowered);
            }
            else
            {
                switch (parent.Mode)
                {
                    case CircuitBreaker.MODE_OFF:
                        _materialChanger.ChangeState(Defines.Animator.OffPowered);
                        break;

                    case CircuitBreaker.MODE_ON:
                        GetComponent<MeshFilter>().mesh = OnMesh;
                        _materialChanger.ChangeState(Defines.Animator.OnPowered);
                        break;

                    case CircuitBreaker.MODE_TRIPPED:
                        if (_trippedAnimTask == null || _trippedAnimTask.IsCompleted)
                            _trippedAnimTask = TrippedAnim().AsTask();
                        break;
                }
            }
        }
    }
}
