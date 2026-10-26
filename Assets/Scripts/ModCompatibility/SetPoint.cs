using System;

using Assets.Scripts.Objects.Pipes;

using UnityEngine;

namespace ReVolt.ModCompatibility
{
    /// <summary>
    /// Represents a set point with a bounded range.
    /// </summary>
    public class SetPoint
    {
        /// <param name="unitString">The unit of measurement for the set point.</param>
        /// <param name="lowerBound">The minimum allowable value.</param>
        /// <param name="upperBound">The maximum allowable value.</param>
        /// <param name="minorStep">The small step size.</param>
        /// <param name="majorStep">The large step size.</param>
        /// <exception cref="ArgumentException">Lower bound cannot be greater than upper bound.</exception>
        public SetPoint(string unitString, float lowerBound, float upperBound, float minorStep, float? majorStep = null)
        {
            if (lowerBound > upperBound)
                throw new ArgumentException(
                    "Lower bound cannot be greater than upper bound.",
                    paramName: nameof(lowerBound) + "," + nameof(upperBound));

            UnitString = unitString;

            LowerBound = lowerBound;
            UpperBound = upperBound;
            MinorStepDelta = minorStep;
            MajorStepDelta = majorStep ?? minorStep;
        }

        /// <summary>Unit of measurement.</summary>
        /// <remarks>Prefix values are generated, so the string should accept any SI prefix.</remarks>
        /// <example><c>"m/ms"</c>, but not <c>"km/s"</c></example>
        public string UnitString { get; }

        /// <summary>Lower bound (inclusive) of the range.</summary>
        public float LowerBound { get; }
        /// <summary>Upper bound (inclusive) of the range.</summary>
        public float UpperBound { get; }

        /// <summary>Large, incremental step size.</summary>
        public float MajorStepDelta { get; }
        /// <summary>Small, incremental step size.</summary>
        public float MinorStepDelta { get; }

        private float _currentValue;
        /// <summary>Current value of the set point, clamped to the nearest valid value.</summary>
        public virtual float CurrentValue
        {
            get => _currentValue;
            set
            {
                if (float.IsNaN(value)) return;
                _currentValue = Mathf.Clamp(value, LowerBound, UpperBound);
            }
        }

        /// <summary>Next step up, or <c>null</c> if already at the upper bound.</summary>
        public virtual float? NextStepUp
            => CurrentValue < UpperBound ? Math.Min(CurrentValue + MinorStepDelta, UpperBound) : null;
        /// <summary>Next step down, or <c>null</c> if already at the lower bound.</summary>
        public virtual float? NextStepDown
            => CurrentValue > LowerBound ? Math.Max(CurrentValue - MinorStepDelta, LowerBound) : null;

        /// <summary>Next major step up, or <c>null</c> if already at the upper bound.</summary>
        public virtual float? NextMajorStepUp
            => CurrentValue < UpperBound ? Math.Min(CurrentValue + MajorStepDelta, UpperBound) : null;
        /// <summary>Next major step down, or <c>null</c> if already at the lower bound.</summary>
        public virtual float? NextMajorStepDown
            => CurrentValue > LowerBound ? Math.Max(CurrentValue - MajorStepDelta, LowerBound) : null;

        /// <summary>
        /// Attempts to convert the current value to the specified target unit.
        /// </summary>
        /// <param name="targetUnit">The unit to which the value should be converted.</param>
        /// <param name="convertedValue">Converted value if the conversion succeeded; otherwise, <c>0f</c>.</param>
        /// <returns><c>true</c> if the conversion was successful; otherwise, <c>false</c>.</returns>
        public virtual bool TryConvertValue(string targetUnit, out float convertedValue)
        {
            if (targetUnit == UnitString)
            {
                convertedValue = CurrentValue;
                return true;
            }
            convertedValue = 0f;
            return false;
        }
    }
}
