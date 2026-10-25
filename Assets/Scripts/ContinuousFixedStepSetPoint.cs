using System;

using Assets.Scripts.Objects.Pipes;

using UnityEngine;

namespace ReVolt
{
    /// <summary>
    /// Represents a continuous set point with fixed default step increments within a specified range.
    /// </summary>
    public class ContinuousFixedStepSetPoint : SetPoint
    {
        /// <inheritdoc cref="SetPoint.SetPoint" />
        /// <param name="delta">Incremental step size.</param>
        public ContinuousFixedStepSetPoint(Device parent, string unitString, float min, float max, float delta)
            : base(parent, unitString, min, max)
        {
            if (delta < 0) throw new ArgumentOutOfRangeException(nameof(delta), delta, "Delta cannot be a negative value.");
            StepDelta = delta;

            CurrentValue = LowerBound; // Initialize to the minimum value
        }

        /// <summary>Incremental step size.</summary>
        public float StepDelta { get; }

        /// <inheritdoc />
        public override bool IsContinuous => true;

        /// <inheritdoc />
        public override float CurrentValue
        {
            get => base.CurrentValue;
            set
            {
                if (float.IsNaN(value)) return;
                base.CurrentValue = Mathf.Clamp(value, LowerBound, UpperBound);
            }
        }

        /// <inheritdoc />
        public override float? NextStepUp => CurrentValue < UpperBound ? Math.Min(CurrentValue + StepDelta, UpperBound) : (float?)null;

        /// <inheritdoc />
        public override float? NextStepDown => CurrentValue > LowerBound ? Math.Max(CurrentValue - StepDelta, LowerBound) : (float?)null;
    }
}
