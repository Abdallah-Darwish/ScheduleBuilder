using Xunit;

namespace ScheduleBuilder.Tests
{
    public class UCourseTests
    {
        internal UCourse GetTestUCourse() => new UCourse(1, 1, "TestsCourse");

        [Fact]
        public void Equals_ComparedObjectIsNull_ReturnsFalse()
        {
            var course = GetTestUCourse();
            Assert.False(course.Equals(null));
        }

        [Fact]
        public void Equals_ComparedUCourseIsNull_ReturnsFalse()
        {
            var course = GetTestUCourse();
            UCourse comapredSubject = null;
            Assert.False(course.Equals(comapredSubject));
        }

        [Fact]
        public void Equals_ComparedObjectIsNotUSubject_ReturnsFalse()
        {
            var course = GetTestUCourse();
            string comapredObj = "TestSuject";
            Assert.False(course.Equals(comapredObj));
        }

        [Fact]
        public void Equals_ComparedSubjectHasTheSameData_ReturnsTrue()
        {
            var course1 = GetTestUCourse();
            var course2 = GetTestUCourse();
            Assert.True(course1.Equals(course2));
        }

        [Fact]
        public void GetHashCode_TwoEqualSubjects_ReturnsTheSameHash()
        {
            var course1 = GetTestUCourse();
            var course2 = GetTestUCourse();
            Assert.StrictEqual(course1.GetHashCode(), course2.GetHashCode());
        }

        [Fact]
        public void Equals_ComparedSubjectHasDifferentData_ReturnsFalse()
        {
            var course1 = GetTestUCourse();
            var course2 = new UCourse(course1.ID + 1, course1.NumberOfHours, "abc");
            Assert.False(course1.Equals(course2));
        }

        [Fact]
        public void GetHashCode_TwoEqualSubjects_ReturnsDifferenHash()
        {
            var course1 = GetTestUCourse();
            var course2 = new UCourse(course1.ID + 1, course1.NumberOfHours, "abc");
            Assert.NotStrictEqual(course1.GetHashCode(), course2.GetHashCode());
        }
    }
}