using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ReVolt
{
    // Only the power tick commits an applied revision. Topology notifications can
    // come from another thread, including during Re-Volt's refresh.
    internal sealed class RevisionClock
    {
        private long _revision = 1;
        private long _applied;

        internal long Revision => Interlocked.Read(ref _revision);
        internal bool Pending => Revision != Interlocked.Read(ref _applied);
        internal void Changed() => Interlocked.Increment(ref _revision);

        internal bool TryCommit(long observed)
        {
            if (Revision != observed)
                return false;
            Interlocked.Exchange(ref _applied, observed);
            return Revision == observed;
        }
    }

    internal sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
    {
        internal static readonly ReferenceComparer<T> Instance = new ReferenceComparer<T>();
        public bool Equals(T x, T y) => ReferenceEquals(x, y);
        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }

    internal static class CacheAudit
    {
        internal static bool Matches<T>(IEnumerable<T> expected, IEnumerable<T> cached) where T : class
        {
            var counts = new Dictionary<T, int>(ReferenceComparer<T>.Instance);
            foreach (T item in expected)
            {
                counts.TryGetValue(item, out int count);
                counts[item] = count + 1;
            }

            foreach (T item in cached)
            {
                if (item == null || !counts.TryGetValue(item, out int count))
                    return false;
                if (count == 1)
                    counts.Remove(item);
                else
                    counts[item] = count - 1;
            }

            return counts.Count == 0;
        }

        // Compare exact object identity, multiplicity, and rating, not just list
        // lengths. Replacing a cable with another rating must also be noticed.
        internal static bool MatchesRated<T>(IEnumerable<T> expected,
            SortedList<float, List<T>> cached, Func<T, float> rating) where T : class
        {
            var counts = new Dictionary<T, int>(ReferenceComparer<T>.Instance);
            foreach (T item in expected)
            {
                counts.TryGetValue(item, out int count);
                counts[item] = count + 1;
            }

            foreach (KeyValuePair<float, List<T>> group in cached)
            {
                foreach (T item in group.Value)
                {
                    if (item == null || !group.Key.Equals(rating(item)) || !counts.TryGetValue(item, out int count))
                        return false;
                    if (count == 1)
                        counts.Remove(item);
                    else
                        counts[item] = count - 1;
                }
            }

            return counts.Count == 0;
        }
    }
}
