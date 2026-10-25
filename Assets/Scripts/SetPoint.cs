using System;

using Assets.Scripts.Objects.Pipes;

namespace ReVolt
{
    /// <summary>
    /// Represents an abstract set point with a bounded range.
    /// </summary>
    public abstract class SetPoint
    {
        /// <param name="parent">The parent device associated with the set point.</param>
        /// <param name="unitString">The unit of measurement for the set point.</param>
        /// <param name="lowerBound">The minimum allowable value.</param>
        /// <param name="upperBound">The maximum allowable value.</param>
        /// <exception cref="ArgumentException">Lower bound cannot be greater than upper bound.</exception>
        public SetPoint(Device parent, string unitString, float lowerBound, float upperBound)
        {
            if (lowerBound > upperBound)
                throw new ArgumentException(
                    "Lower bound cannot be greater than upper bound.",
                    paramName: nameof(lowerBound) + "," + nameof(upperBound));

            Parent = parent;

            UnitString = unitString;

            LowerBound = lowerBound;
            UpperBound = upperBound;
        }

        /// <summary>Device this set point belongs to.</summary>
        public Device Parent { get; }

        /// <summary>Unit of measurement.</summary>
        /// <remarks>Prefix values are generated, so the string should accept any SI prefix.</remarks>
        /// <example><c>"m/ms"</c>, but not <c>"km/s"</c></example>
        public string UnitString { get; }

        /// <summary>Lower bound (inclusive) of the range.</summary>
        public float LowerBound { get; }
        /// <summary>Upper bound (inclusive) of the range.</summary>
        public float UpperBound { get; }

        /// <summary><c>true</c> if the range of valid values is continuous; <c>false</c> if discrete.</summary>
        public abstract bool IsContinuous { get; }

        /// <summary>Current value of the set point, clamped to the nearest valid value.</summary>
        public virtual float CurrentValue { get; set; }

        /// <summary>Next higher step value, or <c>null</c> if already at the upper bound.</summary>
        public abstract float? NextStepUp { get; }
        /// <summary>Next lower step value, or <c>null</c> if already at the lower bound.</summary>
        public abstract float? NextStepDown { get; }

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
