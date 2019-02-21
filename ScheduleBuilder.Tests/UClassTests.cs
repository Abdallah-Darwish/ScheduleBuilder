using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ScheduleBuilder.Tests
{
    public class UClassTests
    {
      

        private UClass GetTestUClass(UCourse subject = null, string teacherName = null, IEnumerable<DayOfWeek> days = null, TimeSpan? startTime = null, TimeSpan? endTime = null, int? section = null, int? capacity = null, int? numberOfRegisteredStudents = null, int? year = null, USemester? semester = null)
        {
            subject = subject ?? new UCourse(0, 1, "qwetyuiop[");

            teacherName = teacherName ?? "wertyuio";
            days = days ?? new DayOfWeek[] { DayOfWeek.Sunday, DayOfWeek.Tuesday, DayOfWeek.Thursday };
            startTime = startTime ?? TimeSpan.FromHours(8);
            endTime = endTime ?? TimeSpan.FromHours(9);
            section = section ?? 1;
            capacity = capacity ?? 30;
            numberOfRegisteredStudents = numberOfRegisteredStudents ?? 27;
            year = year ?? 2018;
            semester = semester ?? USemester.First;
            return new UClass(course: subject, instructorName: teacherName, days: days, startTime: startTime.Value, endTime: endTime.Value, section: section.Value, capacity: capacity.Value, numberOfRegisteredStudents: numberOfRegisteredStudents.Value, year: year.Value, semester: semester.Value);
        }

        [Fact]
        public void Day_RepeatedDays_ReturnsDistinctDays()
        {
            DayOfWeek[] days = new DayOfWeek[3]
            {
                DayOfWeek.Sunday,
                DayOfWeek.Monday,
                DayOfWeek.Sunday
            };

            var testClass = GetTestUClass(days: days);
            Assert.Equal(2, testClass.Days.Count);
        }

        [Fact]
        public void Intersects_CheckedClassDoesntIntersect_ReturnsFalse()
        {
            var class1 = GetTestUClass();
            var class2 = GetTestUClass(startTime: class1.EndTime + TimeSpan.FromHours(1), endTime: class1.EndTime + TimeSpan.FromHours(2), days: Enum.GetValues(typeof(DayOfWeek)).OfType<DayOfWeek>().Except(class1.Days));

            Assert.False(class1.Intersects(class2));
        }

        [Fact]
        public void Intersects_CheckedClassIntersects_ReturnsTrue()
        {
            var class1 = GetTestUClass();
            var class2 = GetTestUClass(startTime: class1.StartTime + TimeSpan.FromMinutes(20), endTime: class1.EndTime + TimeSpan.FromHours(2));

            Assert.True(class1.Intersects(class2));
        }

        [Fact]
        public void Intersects_CheckedClassIntersectsOnlyInDays_ReturnsFalse()
        {
            var class1 = GetTestUClass();
            var class2 = GetTestUClass(startTime: class1.EndTime + TimeSpan.FromHours(1), endTime: class1.EndTime + TimeSpan.FromHours(2), days: class1.Days.Take(1));

            Assert.False(class1.Intersects(class2));
        }

        [Fact]
        public void Intersects_CheckedClassIntersectsOnlyInTimes_ReturnsFalse()
        {
            var class1 = GetTestUClass();
            var class2 = GetTestUClass(startTime: class1.StartTime, endTime: class1.EndTime + TimeSpan.FromHours(2), days: Enum.GetValues(typeof(DayOfWeek)).OfType<DayOfWeek>().Except(class1.Days));

            Assert.False(class1.Intersects(class2));
        }

        [Fact]
        public void Intersects_CheckedClassIntersectsInDaysAndStartEndTimes_ReturnsFalse()
        {
            var class1 = GetTestUClass();
            var class2 = GetTestUClass(startTime: class1.EndTime, endTime: class1.EndTime + TimeSpan.FromHours(1));

            Assert.False(class1.Intersects(class2));
        }

        [Fact]
        public void Intersects_CheckedClassIsNull_ThrowsArgumentNullException()
        {
            var uClass = GetTestUClass();

            Assert.Throws<ArgumentNullException>(() => uClass.Intersects(null));
        }

        [Fact]
        public void Equals_CheckedClassIsNull_ReturnsFalse()
        {
            var uClass = GetTestUClass();

            Assert.False(uClass.Equals(null));
        }

        [Fact]
        public void Equals_CheckedObjectIsNotUClass_ReturnsFalse()
        {
            var uClass = GetTestUClass();

            Assert.False(uClass.Equals("ljaflehf"));
        }

        [Fact]
        public void Equals_CheckedClassHasDifferentData_ReturnsFalse()
        {
            var class1 = GetTestUClass();
            var class2 = GetTestUClass(startTime: class1.EndTime, endTime: class1.EndTime + TimeSpan.FromHours(1));

            Assert.False(class1.Equals(class2));
        }

        [Fact]
        public void Equals_CheckedClassHasTheSameData_ReturnsTrue()
        {
            var class1 = GetTestUClass();
            var class2 = GetTestUClass();

            Assert.True(class1.Equals(class2));
        }
    }
}