using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ScheduleBuilder.Core.Parsing
{
    internal static class UClassParser
    {
        private static readonly Regex
            //Common
            rgx_classes = new Regex("\\<tr\\sclass=\"GridView(?:Alternate)?RowStyle\"\\>((?:[\\s]|.)*?)\\<\\/tr\\>", GlobalRegexOptions.Value),
            rgx_courseId = new Regex("lblGvCourseNo_\\d+\"\\>(\\d+)\\<", GlobalRegexOptions.Value),
            rgx_courseName = new Regex("lblGvCourseNameAr?_\\d+\"\\>([^\\<]+)\\<", GlobalRegexOptions.Value),
            rgx_classInstructor = new Regex("lblGvInstructorAr?_\\d+\"\\>([^\\<]+)\\<", GlobalRegexOptions.Value),
            rgx_classSection = new Regex("lblGvSections_\\d+\"\\>(\\d+)\\<", GlobalRegexOptions.Value),
            rgx_classDays = new Regex("lblGvDayAr?_\\d+\"\\>([^\\<]+)\\<", GlobalRegexOptions.Value),

            //proposed courses page only
            rgx_classStartTime = new Regex("lblGvStartTime_\\d+\"\\>(?<hour>\\d+):(?<minute>\\d+)\\<", GlobalRegexOptions.Value),
            rgx_classEndTime = new Regex("lblGvEndTime_\\d+\"\\>(?<hour>\\d+):(?<minute>\\d+)\\<", GlobalRegexOptions.Value),
            rgx_classCapacity = new Regex("lblGvMaxStNo_\\d+\"\\>(\\d+)\\<", GlobalRegexOptions.Value),
            rgx_classRegisterdStudentsCount = new Regex("lblGvRegStNo_\\d+\"\\>(\\d+)\\<", GlobalRegexOptions.Value),

            //Registered courses page only
            rgx_registeredClassTime = new Regex("lblGvLecTime_\\d+\"\\>(?<startHour>\\d+):(?<startMinute>\\d+)\\s+(?<endHour>\\d+):(?<endMinute>\\d+)\\<", GlobalRegexOptions.Value),
            //rgx_registrationEventTarget = new Regex("lbtnAddCourse_\\d+\"\\shref=\"javascript:__doPostBack\\(\\&\\#39\\;([^\\&]+)", GlobalRegexOptions.Value),

            //avilable for regestration
            rgx_avilableClassTime = new Regex("lblGvStartTime_\\d+\"\\>(?<startHour>\\d+):(?<startMinute>\\d+)\\s+(?<endHour>\\d+):(?<endMinute>\\d+)\\<", GlobalRegexOptions.Value),
            rgx_registrationEventTarget = new Regex("lbtnAddCourse_\\d+\"\\shref=\"javascript:__doPostBack\\(\\&\\#39\\;([^\\&]+)", GlobalRegexOptions.Value);

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
        public static UClass ParseClass(string classSource, int classYear = 0, USemester classSemester = USemester.Unknown)
        {
            int courseId = int.Parse(rgx_courseId.Match(classSource).Groups[1].Value);
            string courseName = rgx_courseName.Match(classSource).Groups[1].Value;
            string classInstructor = rgx_classInstructor.Match(classSource).Groups[1].Value;
            int classSection = int.Parse(rgx_classSection.Match(classSource).Groups[1].Value);
            string classDaysString = rgx_classDays.Match(classSource).Groups[1].Value;
            DayOfWeek[] classDays = DayOfWeekConverter.ToDays(classDaysString).ToArray();
            var (classStartTime, classEndTime) = ParseClassTime(classSource);
            int.TryParse(rgx_classCapacity.Match(classSource).Groups[1].Value, out int classCapacity);
            int.TryParse(rgx_classRegisterdStudentsCount.Match(classSource).Groups[1].Value, out var classRegisterdStudentsCount);

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
        private static KeyValuePair<int, string> ParseClassIdAndRegistrationEventTarget(string classSource)
        {
            int courseId = int.Parse(rgx_courseId.Match(classSource).Groups[1].Value);
            int classSection = int.Parse(rgx_classSection.Match(classSource).Groups[1].Value);
            string registrationEventTarget = rgx_registrationEventTarget.Match(classSource).Groups[1].Value;
            return new KeyValuePair<int, string>(UClass.Identify(courseId, classSection), registrationEventTarget);
        }
        public static UClass[] ParseClassesTable(string pageSource, int classYear = 0, USemester classSemester = USemester.Unknown)
        {
            return rgx_classes.Matches(pageSource).OfType<Match>().Select(m => ParseClass(m.Groups[1].Value, classYear, classSemester)).ToArray();
        }
        public static Dictionary<int, string> ParseRegestrationEventTargets(string pageSource)
        {
            var targets = rgx_classes.Matches(pageSource).OfType<Match>().Select(m => ParseClassIdAndRegistrationEventTarget(m.Groups[1].Value));
            Dictionary<int, string> result = new Dictionary<int, string>();
            foreach (var target in targets)
            {
                result.Add(target.Key, target.Value);
            }
            return result;
        }
    }
}
