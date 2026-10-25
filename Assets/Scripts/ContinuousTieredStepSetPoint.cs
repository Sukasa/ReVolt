using System;

using Assets.Scripts.Objects.Pipes;

using UnityEngine;

namespace ReVolt
{
    /// <summary>
    /// Represents a continuous set point with two tiers of default step increments within a specified range.
    /// </summary>
    public class ContinuousTieredStepSetPoint : TieredSetPoint
    {
        /// <inheritdoc cref="SetPoint.SetPoint" />
        /// <param name="minorDelta">Small incremental step size.</param>
        /// <param name="majorDelta">Large incremental step size.</param>
        public ContinuousTieredStepSetPoint(Device parent, string unitString, float min, float max, float minorDelta, float majorDelta)
            : base(parent, unitString, min, max)
        {
            if (minorDelta < 0) throw new ArgumentOutOfRangeException(nameof(minorDelta), minorDelta, "Minor delta cannot be a negative value.");
            if (majorDelta < 0) throw new ArgumentOutOfRangeException(nameof(majorDelta), majorDelta, "Major delta cannot be a negative value.");
            MajorStepDelta = majorDelta;
            MinorStepDelta = minorDelta;

            CurrentValue = LowerBound; // Initialize to the minimum value
        }

        /// <summary>Large, incremental step size.</summary>
        public float MajorStepDelta { get; }
        /// <summary>Small, incremental step size.</summary>
        public float MinorStepDelta { get; }

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
        public override float? NextMajorStepUp => CurrentValue < UpperBound ? Math.Min(CurrentValue + MajorStepDelta, UpperBound) : (float?)null;

        /// <inheritdoc />
        public override float? NextMajorStepDown => CurrentValue > LowerBound ? Math.Max(CurrentValue - MajorStepDelta, LowerBound) : (float?)null;

        /// <inheritdoc />
        public override float? NextStepUp => CurrentValue < UpperBound ? Math.Min(CurrentValue + MinorStepDelta, UpperBound) : (float?)null;

        /// <inheritdoc />
        public override float? NextStepDown => CurrentValue > LowerBound ? Math.Max(CurrentValue - MinorStepDelta, LowerBound) : (float?)null;
    }
}
