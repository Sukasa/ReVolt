namespace ReVolt
{
    public class InstantTripBehavior : TripBehavior
    {
        public InstantTripBehavior(CircuitBreaker parent) => Parent = parent;

        public CircuitBreaker Parent { get; }

        public override bool ShouldTrip(System.Random RNG, float transferred) => ShouldTrip(transferred);
        public bool ShouldTrip(float transferred)
        {
            // If the trip point cannot be expressed in Watts, no meaningful trip exists.
            if (!Parent.TripPoint.TryConvertValue("W", out var tripWatts))
                return false;

            // If the transferred power is below the trip point, or the breaker is not on, it will not trip.
            if (transferred <= tripWatts || Parent.Mode != CircuitBreaker.MODE_ON)
                return false;

            return true;
        }

        public override float GetTripChance(float transferred) => ShouldTrip(transferred) ? 1.0f : -1.0f;
    }
}
