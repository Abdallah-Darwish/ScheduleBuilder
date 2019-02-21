using System;
using System.Collections.Generic;
using System.Linq;

namespace ScheduleBuilder.Core
{
    public class DaysEqualityComparer : IEqualityComparer<IReadOnlyList<DayOfWeek>>
    {
        public bool Equals(IReadOnlyList<DayOfWeek> x, IReadOnlyList<DayOfWeek> y)
        {
            if (x is null && y is null) { return true; }
            else if (x is null ^ y is null) { return false; }
            return x.Count == y.Count && x.Except(y).Count() == 0;
        }

        public int GetHashCode(IReadOnlyList<DayOfWeek> obj)
        {
            return obj?.Aggregate(0, (hash, day) => HashCodeHelper.CombineHashCodes(hash, day.GetHashCode())) ?? 0;
        }
    }
}