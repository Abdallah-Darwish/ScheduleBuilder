using ScheduleBuilder.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace WindowsUI
{
    class UScheduleListModel
    {
        private readonly DaysEqualityComparer s_daysEqualityComparer = new DaysEqualityComparer();
        public USchedule Source { get; }
        public string Info { get; }
        public UClassListModel[] ClassesModels { get; }
        public string Days { get; }
        public string FinancialHours { get; }
        public string FirstStartTime { get; }
        public string LastEndTime { get; }
        public string LongestDayDuration { get; }
        public string MaximumBreaksTotal { get; }
        public UScheduleListModel(USchedule src)
        {

            Source = src;
            DayOfWeek[] srcDays = src.Days.ToArray();
            Info = $"{srcDays.Length} day{(srcDays.Length == 1 ? string.Empty : "s")} | {Source.FinancialHours} hour";
            ClassesModels = Source.Classes.OrderBy(x => s_daysEqualityComparer.GetHashCode(x.Days)).ThenBy(c => c.StartTime).ThenBy(c => c.EndTime).Select(c => new UClassListModel(c)).ToArray();
            Days = DayOfWeekConverter.ToString(srcDays);
            FinancialHours = Source.FinancialHours.ToString();
            FirstStartTime = Source.FirstStartTime.ToString("hh\\:mm");
            LastEndTime = Source.LastEndTime.ToString("hh\\:mm");
            LongestDayDuration = Source.LongestDayDuration.ToString("hh\\:mm");
            MaximumBreaksTotal = Source.MaximumBreaksTotal.ToString("hh\\:mm");
        }
    }
}
