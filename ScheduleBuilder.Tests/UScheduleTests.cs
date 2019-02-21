using System;
using System.Linq;
using Xunit;

namespace ScheduleBuilder.Tests
{
    public class UScheduleTests
    {
        private static UClass[] s_testClasses = new UClass[]
            {
             new UClass(new UCourse(0,1,"a"),"a",new DayOfWeek[] {DayOfWeek.Sunday,DayOfWeek.Monday},TimeSpan.FromHours(8),TimeSpan.FromHours(9)),
             new UClass(new UCourse(2,1,"b"),"b",new DayOfWeek[] {DayOfWeek.Monday,DayOfWeek.Tuesday},TimeSpan.FromHours(10),TimeSpan.FromHours(11)),
             new UClass(new UCourse(3,1,"c"),"c",new DayOfWeek[] {DayOfWeek.Tuesday,DayOfWeek.Wednesday},TimeSpan.FromHours(15),TimeSpan.FromHours(16)),
            };

        private USchedule GetTestSchedule()
        {
            var result = new USchedule();
            foreach (var uClass in s_testClasses)
            {
                result.Add(uClass);
            }
            return result;
        }

        [Fact]
        public void MaximumBreak_UScheduleHasMultipleBreaks_ReturnsMaxBreak()
        {
            USchedule schedule = GetTestSchedule();

            Assert.Equal(schedule.MaximumBreaksTotal, TimeSpan.FromHours(4));
        }

        [Fact]
        public void LongestDayDuration_UScheduleHasMultipleCalsses_ReturnsLongestDayDuration()
        {
            var schedule = GetTestSchedule();

            Assert.Equal(schedule.LongestDayDuration, TimeSpan.FromHours(6));
        }

        [Fact]
        public void FirstStartTime_UScheduleHasMultipleCalsses_ReturnsFirstStartTime()
        {
            var schedule = GetTestSchedule();

            Assert.Equal(TimeSpan.FromHours(8), schedule.FirstStartTime);
        }

        [Fact]
        public void LastEndTime_UScheduleHasMultipleCalsses_ReturnsLastEndTime()
        {
            var schedule = GetTestSchedule();

            Assert.Equal(schedule.LastEndTime, TimeSpan.FromHours(16));
        }

        [Fact]
        public void Days_UScheduleHasRepeatedDays_ReturnsDistinctOrderedDayes()
        {
            var schedule = GetTestSchedule();

            Assert.Equal(schedule.Days, new DayOfWeek[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday });
        }

        [Fact]
        public void Add_AddedClassIntersectWithExisitingClass_ReturnsFalse()
        {
            var schedule = GetTestSchedule();

            Assert.False(schedule.Add(new UClass(course: new UCourse(1234, 3, "kjrhgkagkulhrwg"), instructorName: "llkwdj;jfd", days: new DayOfWeek[] { DayOfWeek.Monday }, startTime: TimeSpan.FromHours(8.5), endTime: TimeSpan.FromHours(10))));
        }

        [Fact]
        public void Add_AddedClassIsNull_ThrowsArguemntNullException()
        {
            var schedule = GetTestSchedule();

            Assert.Throws<ArgumentNullException>(() => schedule.Add(null));
        }

        [Fact]
        public void Remove_RemovedClassIsNull_ThrowsArgumentNullException()
        {
            var schedule = GetTestSchedule();

            Assert.Throws<ArgumentNullException>(() => schedule.Remove(null));
        }

        [Fact]
        public void Add_CheckIfItsWorking_AddClass()
        {
            var schedule = GetTestSchedule();
            var classDays = new DayOfWeek[] { DayOfWeek.Sunday, DayOfWeek.Monday };
            TimeSpan classStartTime = schedule.Classes.Where(c => classDays.Intersect(c.Days).Any()).Max(c => c.EndTime) + TimeSpan.FromMinutes(10);
            TimeSpan classEndTime = classStartTime + TimeSpan.FromHours(1);
            var newClass = new UClass(course: new UCourse(schedule.Classes.Max(c => c.Course.ID) + 1, 3, "acvbnm,."), teacherName: "asdfghjkl", days: classDays, startTime: classStartTime, endTime: classEndTime);
            Assert.True(schedule.Add(newClass));
            Assert.Contains(newClass, schedule.Classes);
        }

        [Fact]
        public void Remove_CheckIfItsWorking_RemoveClass()
        {
            var schedule = GetTestSchedule();
            int originalCount = schedule.Classes.Count;
            schedule.Remove(schedule.Classes.First());
            Assert.True(schedule.Classes.Count == originalCount - 1);
        }

        [Fact]
        public void HasSubject_CheckedSubjectIsNull_ThrowsArgumentNullException()
        {
            var schedule = GetTestSchedule();

            Assert.Throws<ArgumentNullException>(() => schedule.HasSubject(null));
        }

        [Fact]
        public void HasSubject_CheckedSubjectDoesntExist_ReturnsFalse()
        {
            var schedule = GetTestSchedule();

            Assert.False(schedule.HasSubject(new UCourse(schedule.Classes.Max(c => c.Course.ID) + 1, 9999, "qwertyuiopasdfghjklzxcvbnm,.")));
        }

        [Fact]
        public void HasSubject_CheckedSubjectExists_ReturnsTrue()
        {
            var schedule = GetTestSchedule();

            Assert.True(schedule.HasSubject(schedule.Classes.First().Course));
        }

        [Fact]
        public void HasSubjects_CheckedSubjectsAreNull_ThrowsArgumentNullException()
        {
            var schedule = GetTestSchedule();

            Assert.Throws<ArgumentNullException>(() => schedule.HasSubjects(null));
        }

        [Fact]
        public void HasSubjects_CheckedSubjectsDontExist_ReturnsFalse()
        {
            var schedule = GetTestSchedule();

            Assert.False(schedule.HasSubjects(new UCourse[] { new UCourse(schedule.Classes.Max(c => c.Course.ID) + 1, 9999, "qwertyuiopasdfghjklzxcvbnm,.") }));
        }

        [Fact]
        public void HasSubjects_CheckedSubjectsExists_ReturnsTrue()
        {
            var schedule = GetTestSchedule();

            Assert.True(schedule.HasSubjects(new UCourse[] { schedule.Classes.First().Course }));
        }

        [Fact]
        public void GetHashCode_BothSchedulesHaveTheSameData_ReturnsTheSameHashCodes()
        {
            Assert.Equal(GetTestSchedule().GetHashCode(), GetTestSchedule().GetHashCode());
        }

        [Fact]
        public void GetHashCode_SchedulesHaveDifferentData_ReturnsDifferentHashCodes()
        {
            var schedule1 = GetTestSchedule();
            var schedule2 = GetTestSchedule();
            schedule2.Remove(schedule2.Classes.First());
            Assert.NotEqual(schedule1.GetHashCode(), schedule2.GetHashCode());
        }

        [Fact]
        public void Equals_ComparedSchedulesHaveTheSameData_ReturnsTrue()
        {
            var schedule1 = GetTestSchedule();
            var schedule2 = GetTestSchedule();

            Assert.True(schedule1.Equals(schedule1));
        }

        [Fact]
        public void Equals_ComparedSchedulesHaveDifferentData_ReturnsFalse()
        {
            var schedule1 = GetTestSchedule();
            var schedule2 = GetTestSchedule();
            schedule2.Remove(schedule2.Classes.First());
            Assert.False(schedule1.Equals(schedule2));
        }
    }
}