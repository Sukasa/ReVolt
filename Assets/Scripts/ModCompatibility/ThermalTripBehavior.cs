using UnityEngine;

namespace ReVolt.ModCompatibility
{
    public class ThermalTripBehavior
    {
        public ThermalTripBehavior(CircuitBreaker parent) => Parent = parent;

        public CircuitBreaker Parent { get; }

        /// <summary>Determines the chance that a trip should occur this tick.
        /// </summary>
        /// <param name="transferred">Transferred watts during the current tick.</param>
        /// <returns>Probability between <c>0f</c> (never) and <c>1f</c> (always). Can exceed <c>1f</c> when well over the trip point. Can be negative if not applicable.</returns>
        protected virtual float GetTripChance(float transferred)
        {
            // If the trip point cannot be expressed in Watts, no meaningful trip chance exists.
            if (!Parent.TripPoint.TryConvertValue("W", out var tripWatts))
                return -1.0f;

            // If the transferred power is below the trip point, or the breaker is not on, no trip chance exists.
            if (transferred <= tripWatts || Parent.Mode != CircuitBreaker.MODE_ON)
                return -1.0f;

            // Do a cheap calculation to get the % chance to trip the breaker
            var tripChance = Mathf.Pow(transferred / tripWatts, 1.25f) - 1.0f;
            return tripChance;
        }

        /// <summary>
        /// Determines whether a trip should occur this tick.
        /// </summary>
        /// <param name="RNG">Random number generator to use for probabilistic trips.</param>
        /// <param name="transferred">Transferred watts during the current tick.</param>
        /// <returns>true if a trip event should occur; otherwise, false.</returns>
        public virtual bool ShouldTrip(System.Random RNG, float transferred)
        {
            var burnChance = GetTripChance(transferred) * ReVolt.configCableBurnFactor.Value;
            return (float)RNG.NextDouble() < burnChance;
        }
    }
}
