using System;
using System.Collections.Generic;
using System.Text;

namespace ScheduleBuilder.Core
{
    public class UBreak : IEquatable<UBreak>
    {
        public TimeSpan StartTime { get; }
        public TimeSpan EndTime { get; }
        public UBreak(TimeSpan startTime, TimeSpan endTime)
        {
            if (startTime > endTime)
            {
                throw new ArgumentException("Break start time can't be after its end time.");
            }
            StartTime = startTime;
            EndTime = endTime;
        }

        public bool Equals(UBreak other)
        {
            if (other is null) { return false; }
            return other.StartTime == StartTime && other.EndTime == EndTime;
        }
        public override bool Equals(object obj)
        {
            if (obj is UBreak ubreak) { return Equals(ubreak); }
            return false;
        }
        public override int GetHashCode() => HashCodeHelper.CombineHashCodes(StartTime.GetHashCode(), EndTime.GetHashCode());
        public override string ToString() => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    }
}
