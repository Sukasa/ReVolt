using Assets.Scripts;
using UnityEngine;

namespace ReVolt
{
    internal class ReVoltStrings
    {
        internal static readonly int HoldForPreviousTripHash = Animator.StringToHash("HoldForPreviousTrip");
        internal static readonly int ResetBreakerToClearHash = Animator.StringToHash("ResetBreakerToClear");

        public static string HoldForPreviousTrip => Localization.GetInterface(HoldForPreviousTripHash);

        public static string ResetBreakerToClear => Localization.GetInterface(ResetBreakerToClearHash);
    }
}
