using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleBuilder.Core
{
    [Serializable]
    public class UClass : IEquatable<UClass>
    {
        public int Id { get; }
        public TimeSpan StartTime { get; }
        public TimeSpan EndTime { get; }
        public string InstructorName { get; }
        public IReadOnlyList<DayOfWeek> Days { get; }
        public UCourse Course { get; }
        public int Section { get; }
        public int Capacity { get; }
        public int NumberOfRegisteredStudents { get; }

        public bool Intersects(UBreak uBreak)
        {
            if (uBreak is null) { throw new ArgumentNullException(nameof(uBreak)); }

            return (StartTime >= uBreak.StartTime && StartTime < uBreak.EndTime) || (EndTime > uBreak.StartTime && EndTime <= uBreak.EndTime);
        }

        public bool Intersects(UClass uClass)
        {
            if (uClass is null) { throw new ArgumentNullException(nameof(uClass)); }

            return Days.Intersect(uClass.Days).Any() && ((StartTime >= uClass.StartTime && StartTime < uClass.EndTime) || (EndTime > uClass.StartTime && EndTime <= uClass.EndTime) || (uClass.StartTime >= StartTime && uClass.StartTime < EndTime) || (uClass.EndTime > StartTime && uClass.EndTime <= EndTime));
        }
        public UClass(UClass source)
        {
            if (source is null) { throw new ArgumentNullException(nameof(source)); }
            Id = source.Id;
            Course = source.Course;
            InstructorName = source.InstructorName;
            Days = source.Days.ToArray();
            StartTime = source.StartTime;
            EndTime = source.EndTime;
            Section = source.Section;
            Capacity = source.Capacity;
            NumberOfRegisteredStudents = source.NumberOfRegisteredStudents;
        }
        public UClass(UCourse course, string instructorName, IEnumerable<DayOfWeek> days, TimeSpan startTime, TimeSpan endTime, int section, int capacity = 0, int numberOfRegisteredStudents = 0)
        {
            //course id length is 5
            Id = Identify(course.Id, section);
            Course = course;
            InstructorName = instructorName;
            Days = days.Distinct().ToArray();
            StartTime = startTime;
            EndTime = endTime;
            Section = section;
            Capacity = capacity;
            NumberOfRegisteredStudents = numberOfRegisteredStudents;
        }
        [JsonConstructor]
        public UClass(int id, UCourse course, string instructorName, IEnumerable<DayOfWeek> days, TimeSpan startTime, TimeSpan endTime, int section, int capacity, int numberOfRegisteredStudents)
        {
            Id = id;
            Course = course;
            InstructorName = instructorName;
            Days = days.Distinct().ToArray();
            StartTime = startTime;
            EndTime = endTime;
            Section = section;
            Capacity = capacity;
            NumberOfRegisteredStudents = numberOfRegisteredStudents;
        }

        public override int GetHashCode() => Id;

        public override bool Equals(object obj)
        {
            if (obj is UClass cls) { return Equals(cls); }
            return false;
        }

        public bool Equals(UClass other)
        {
            if (other is null) { return false; }
            return this.GetHashCode() == other.GetHashCode();
        }
        public override string ToString() => $"{Course.Name} : {StartTime.ToString("hh\\:mm")} - {EndTime.ToString("hh\\:mm")}";

        public static async Task<UClass[]> FromFile(string filePath)
        {
            if (File.Exists(filePath) == false)
            {
                throw new FileNotFoundException($"File {Path.GetFileName(filePath)} doesn't exist.", filePath);
            }
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return await FromStream(fileStream).ConfigureAwait(false);
            }
        }
        public static async Task<UClass[]> FromStream(Stream classesStream)
        {
            if (classesStream is null) { throw new ArgumentNullException(nameof(classesStream)); }
            if (classesStream.CanRead == false) { throw new ArgumentException("Stream can't be used for reading."); }
            using (var classesStreamReader = new StreamReader(classesStream, Encoding.UTF8, true, 4096, true))
            {
                return JsonConvert.DeserializeObject<UClass[]>(await classesStreamReader.ReadToEndAsync().ConfigureAwait(false));
            }
        }
        public static async Task Save(IEnumerable<UClass> classes, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException($"Parameter {nameof(filePath)} can't be null or whitespace.");
            }
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await Save(classes, fileStream).ConfigureAwait(false);
            }
        }
        public static async Task Save(IEnumerable<UClass> classes, Stream classesStream)
        {
            if (classesStream is null) { throw new ArgumentNullException(nameof(classesStream)); }
            if (classesStream.CanWrite == false) { throw new ArgumentException("Stream can't be used for writing."); }
            using (var classesStreamWriter = new StreamWriter(classesStream, Encoding.UTF8, 4096, true))
            {
                await classesStreamWriter.WriteAsync(JsonConvert.SerializeObject(classes)).ConfigureAwait(false);
            }
        }
        /// <summary>
        /// Merges the <see cref="Id"/> and <see cref="Section"/> to create a unique id for the class.
        /// </summary>
        public static int Identify(int courseId, int classSectionNumber) => int.Parse(courseId.ToString() + classSectionNumber.ToString().PadLeft(2, '0'));
    }
}