using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Effects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ReVolt.Assets.Scripts.Components
{
    internal class BreakerStatusScreen
    {
        [SerializeField]
        private MaterialChanger _materialChanger;

        [SerializeField]
        private Collider infoTrigger;

        public Collider InfoTrigger => infoTrigger;


        public static readonly int TripPowered = UnityEngine.Animator.StringToHash("TripPowered");

        public void RefreshState(Device parent)
        {
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
                        _materialChanger.ChangeState(Defines.Animator.OnPowered);
                        break;

                    case CircuitBreaker.MODE_TRIPPED:
                        _materialChanger.ChangeState(TripPowered);
                        break;
                }
            }
        }
    }
}
