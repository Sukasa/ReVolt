using UnityEngine;

namespace ReVolt
{
    public class ThermalTripBehavior : TripBehavior
    {
        public ThermalTripBehavior(CircuitBreaker parent) => Parent = parent;

        public CircuitBreaker Parent { get; }

        public override float GetTripChance(float transferred)
        {
            // If the trip point cannot be expressed in Watts, no meaningful trip chance exists.
            if (!Parent.TripPoint.TryConvertValue("W", out var tripWatts))
                return -1.0f;

            // If the transferred power is below the trip point, or the breaker is not on, no trip chance exists.
            if (transferred <= tripWatts || Parent.Mode != CircuitBreaker.MODE_ON)
                return -1.0f;

            // Do a cheap calculation to get the % chance to trip the breaker
            var tripChance = Mathf.Pow(transferred / tripWatts, 1.25f) - 1.0f;

            return tripChance * ReVolt.configCableBurnFactor.Value;
        }
    }
}
