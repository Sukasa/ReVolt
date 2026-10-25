using System;

namespace ReVolt.ModCompatibility
{
    public class InstantTripBehavior : ThermalTripBehavior
    {
        public InstantTripBehavior(CircuitBreaker parent) : base(parent) { }

        protected override float GetTripChance(float transferred)
        {
            if (base.GetTripChance(transferred) > 0)
                return 1.0f;
            return -1.0f;
        }

        public override bool ShouldTrip(Random RNG, float transferred)
            => GetTripChance(transferred) > 0;
    }
}
