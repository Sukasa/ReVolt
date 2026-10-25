using Assets.Scripts.Objects.Pipes;

namespace ReVolt
{
    public abstract class TieredSetPoint : SetPoint
    {
        public TieredSetPoint(Device parent, string unitString, float lowerBound, float upperBound)
            : base(parent, unitString, lowerBound, upperBound) { }

        /// <summary>Next major set point up, or <c>null</c> if already at the upper bound.</summary>
        public abstract float? NextMajorStepUp { get; }

        /// <summary>Next major set point down, or <c>null</c> if already at the lower bound.</summary>
        public abstract float? NextMajorStepDown { get; }

    }
}
