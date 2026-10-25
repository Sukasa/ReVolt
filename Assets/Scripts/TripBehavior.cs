namespace ReVolt
{
    /// <summary>Defines the behavior for determining breaker trips.</summary>
    public abstract class TripBehavior
    {
        /// <summary>Determines the chance that a trip should occur this tick.
        /// </summary>
        /// <param name="transferred">Transferred watts during the current tick.</param>
        /// <returns>Probability between <c>0f</c> and <c>1f</c>; or negative if not applicable.</returns>
        public abstract float GetTripChance(float transferred);

        /// <summary>
        /// Determines whether a trip should occur this tick.
        /// </summary>
        /// <param name="RNG">Random number generator to use for probabilistic trips.</param>
        /// <param name="transferred">Transferred watts during the current tick.</param>
        /// <returns>true if a trip event should occur; otherwise, false.</returns>
        public virtual bool ShouldTrip(System.Random RNG, float transferred)
            => (float)RNG.NextDouble() < GetTripChance(transferred);
    }
}
