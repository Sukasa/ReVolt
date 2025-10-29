using Assets.Scripts;
using UnityEngine;

namespace ReVolt
{
    internal class ReVoltStrings
    {
        internal static readonly int HoldForPreviousTripHash = Animator.StringToHash("HoldForPreviousTrip");
        internal static readonly int ResetBreakerToClearHash = Animator.StringToHash("ResetBreakerToClear");
        internal static readonly int SmartBreakerNoCableFoundHash = Animator.StringToHash("SmartBreakerNoCableFound");

        internal static readonly int RevoltBreakerTrippedHash = Animator.StringToHash("RevoltBreakerTripped");
        internal static readonly int RevoltBreakerOpenHash = Animator.StringToHash("RevoltBreakerOpen");
        internal static readonly int RevoltBreakerClosedHash = Animator.StringToHash("RevoltBreakerClosed");
        internal static readonly int RevoltBreakerTrippedNetworkHash = Animator.StringToHash("RevoltBreakerTrippedNetwork");

        internal static readonly int RevoltBreakerNotOpenHash = Animator.StringToHash("RevoltBreakerNotOpen");
        internal static readonly int RevoltBreakerAlreadyOpenHash = Animator.StringToHash("RevoltBreakerAlreadyOpen");

        public static string HoldForPreviousTrip => Localization.GetInterface(HoldForPreviousTripHash);

        public static string ResetBreakerToClear => Localization.GetInterface(ResetBreakerToClearHash);

        public static string SmartBreakerNoCableFound => Localization.GetInterface(SmartBreakerNoCableFoundHash);

        public static string RevoltBreakerTripped => Localization.GetInterface(RevoltBreakerTrippedHash);
        public static string RevoltBreakerOpen => Localization.GetInterface(RevoltBreakerOpenHash);
        public static string RevoltBreakerClosed => Localization.GetInterface(RevoltBreakerClosedHash);
        public static string RevoltBreakerTrippedNetwork => Localization.GetInterface(RevoltBreakerTrippedNetworkHash);
        public static string RevoltBreakerNotOpen => Localization.GetInterface(RevoltBreakerNotOpenHash);
        public static string RevoltBreakerAlreadyOpen => Localization.GetInterface(RevoltBreakerAlreadyOpenHash);
    }
}
