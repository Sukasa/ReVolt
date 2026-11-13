using System;
using System.Collections.Generic;

using Assets.Scripts.Objects.Electrical;

using UnityEngine;

namespace ReVolt.ModCompatibility
{
    /// <summary>
    /// Compares two <see cref="Cable"/> objects based on their <see cref="Cable.CustomColor"/> and <see cref="Cable.MaxVoltage"/>.
    /// </summary>
    public class ColorVoltageCableComparer : IEqualityComparer<Cable>
    {
        /// <inheritdoc />
        public bool Equals(Cable x, Cable y)
        {
            if (x == y) return true;
            if (!x || !y) return false;
            return
                Mathf.Approximately(x.MaxVoltage, y.MaxVoltage) &&
                x.CustomColor == y.CustomColor;
        }

        /// <inheritdoc />
        public int GetHashCode(Cable obj) =>
            HashCode.Combine(
                (int)obj.MaxVoltage,
                obj.CustomColor);
    }
}
