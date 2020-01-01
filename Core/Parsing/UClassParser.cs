using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ScheduleBuilder.Core.Parsing
{
    internal static class UClassParser
    {
        private static readonly string
            slc_classId = @"td>span[id*=CourseNo]",
            slc_classSection = @"td>span[id*=Section]",
            slc_classesTable = @"table[id*=Courses]";

        private static readonly Regex
            //proposed courses page only
            rgx_classStartTime = new Regex("lblGvStartTime_\\d+\"\\>(?<hour>\\d+):(?<minute>\\d+)\\<", GlobalRegexOptions.Value),
            rgx_classEndTime = new Regex("lblGvEndTime_\\d+\"\\>(?<hour>\\d+):(?<minute>\\d+)\\<", GlobalRegexOptions.Value),

            //Registered courses page only
            rgx_registeredClassTime = new Regex("lblGvLecTime_\\d+\"\\>(?<startHour>\\d+):(?<startMinute>\\d+)\\s+(?<endHour>\\d+):(?<endMinute>\\d+)\\<", GlobalRegexOptions.Value),
            //rgx_registrationEventTarget = new Regex("lbtnAddCourse_\\d+\"\\shref=\"javascript:__doPostBack\\(\\&\\#39\\;([^\\&]+)", GlobalRegexOptions.Value),

            //avilable for regestration
            rgx_avilableClassTime = new Regex("lblGvStartTime_\\d+\"\\>(?<startHour>\\d+):(?<startMinute>\\d+)\\s+(?<endHour>\\d+):(?<endMinute>\\d+)\\<", GlobalRegexOptions.Value);

        private static (TimeSpan StartTime, TimeSpan EndTime) ParseClassTime(string classSource)
        {
            int startTimeHour = 0, endTimeHour = 0, startTimeMinute = 0, endTimeMinute = 0;
            if (classSource.Contains("lblGvTimeSeparator"))
            {
                Match startTimeMatch = rgx_classStartTime.Match(classSource), endTimeMatch = rgx_classEndTime.Match(classSource);

                int.TryParse(startTimeMatch.Groups["hour"].Value, out startTimeHour);
                int.TryParse(startTimeMatch.Groups["minute"].Value, out startTimeMinute);

                int.TryParse(endTimeMatch.Groups["hour"].Value, out endTimeHour);
                int.TryParse(endTimeMatch.Groups["minute"].Value, out endTimeMinute);
            }
            else if (classSource.Contains("gvRegistrationCoursesSchedule"))
            {
                Match timeMatch = rgx_avilableClassTime.Match(classSource);

                int.TryParse(timeMatch.Groups["startHour"].Value, out startTimeHour);
                int.TryParse(timeMatch.Groups["startMinute"].Value, out startTimeMinute);

                int.TryParse(timeMatch.Groups["endHour"].Value, out endTimeHour);
                int.TryParse(timeMatch.Groups["endMinute"].Value, out endTimeMinute);
            }
            else if (classSource.Contains("gvRegisteredCourses"))
            {
                Match timeMatch = rgx_registeredClassTime.Match(classSource);

                int.TryParse(timeMatch.Groups["startHour"].Value, out startTimeHour);
                int.TryParse(timeMatch.Groups["startMinute"].Value, out startTimeMinute);

                int.TryParse(timeMatch.Groups["endHour"].Value, out endTimeHour);
                int.TryParse(timeMatch.Groups["endMinute"].Value, out endTimeMinute);
            }
            return (new TimeSpan(startTimeHour, startTimeMinute, 0), new TimeSpan(endTimeHour, endTimeMinute, 0));

        }
        public static UClass ParseClass(IHtmlTableRowElement classRow, int classYear = 0, USemester classSemester = USemester.Unknown)
        {
            int courseId = int.Parse(classRow.QuerySelector<IHtmlSpanElement>(slc_classId).TextContent);
            string courseName = classRow.QuerySelector<IHtmlSpanElement>("td>span[id*=CourseName]").TextContent;
            string classInstructor = classRow.QuerySelector<IHtmlSpanElement>("td>span[id*=Instructor]").TextContent;
            int classSection = int.Parse(classRow.QuerySelector<IHtmlSpanElement>(slc_classSection).TextContent);
            string classDaysString = classRow.QuerySelector<IHtmlSpanElement>("td>span[id*=Day]").TextContent;
            DayOfWeek[] classDays = DayOfWeekConverter.ToDays(classDaysString).ToArray();
            var (classStartTime, classEndTime) = ParseClassTime(classRow.OuterHtml);
            int.TryParse(classRow.QuerySelector<IHtmlSpanElement>("td>span[id*=MaxStNo]")?.TextContent, out int classCapacity);
            int.TryParse(classRow.QuerySelector<IHtmlSpanElement>("td>span[id*=RegStNo]")?.TextContent, out int classRegisterdStudentsCount);

            //To detecet Labs
            int classFinancialHours = (int)((classDays.Length == 1 ? 1 : (classEndTime - classStartTime).TotalHours) * classDays.Length);

            classInstructor = classInstructor.Trim();
            courseName = courseName.Trim();

            return new UClass(course: new UCourse(courseId, classFinancialHours, courseName),
                instructorName: classInstructor,
                days: classDays,
                startTime: classStartTime,
                endTime: classEndTime,
                section: classSection,
                capacity: classCapacity,
                numberOfRegisteredStudents: classRegisterdStudentsCount);
        }
        private static (int ClassId, string ClassRegistrationEventTarget) ParseClassIdAndRegistrationEventTarget(IHtmlTableRowElement classRow)
        {
            int courseId = int.Parse(classRow.QuerySelector<IHtmlSpanElement>(slc_classId).TextContent);
            int classSection = int.Parse(classRow.QuerySelector<IHtmlSpanElement>(slc_classSection).TextContent);

            string registrationAnchorHref = classRow.QuerySelector<IHtmlAnchorElement>("a[id*=btnAddCourse]").Href;
            int hrefFirstQuoteIndex = registrationAnchorHref.IndexOf('\''), hrefSecondQuoteIndex = registrationAnchorHref.IndexOf('\'', hrefFirstQuoteIndex + 1);
            string registrationEventTarget = registrationAnchorHref.Substring(hrefFirstQuoteIndex + 1, hrefSecondQuoteIndex - hrefFirstQuoteIndex - 1);

            return (UClass.Identify(courseId, classSection), registrationEventTarget);
        }

        public static UClass[] ParseClassesTable(IHtmlTableElement classesTable, int classYear = 0, USemester classSemester = USemester.Unknown)
        {
            var classes = new ConcurrentBag<UClass>();
            Parallel.ForEach(classesTable.Rows.Skip(1), row => classes.Add(ParseClass(row, classYear, classSemester)));
            return classes.ToArray();
        }

        public static Dictionary<int, string> ParseRegestrationEventTargets(IHtmlTableElement classesTable)
        {
            ConcurrentBag<(int ClassId, string ClassRegistrationEventTarget)> targets = new ConcurrentBag<(int, string)>();
            Parallel.ForEach(classesTable.Rows.Skip(1), row => targets.Add(ParseClassIdAndRegistrationEventTarget(row)));
            return targets.ToDictionary(c => c.ClassId, c => c.ClassRegistrationEventTarget);
        }
    }
}
