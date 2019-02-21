using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleBuilder.Core
{
    public class USchedule : ICloneable, IEquatable<USchedule>
    {
        public IEnumerable<DayOfWeek> Days => _classes.SelectMany(a => a.Days).Distinct().OrderBy(a => a);

        private List<UClass> _classes = new List<UClass>();

        public IReadOnlyList<UClass> Classes => _classes;

        public TimeSpan LastEndTime => _classes.OrderBy(a => a.EndTime).LastOrDefault()?.EndTime ?? default;

        public TimeSpan FirstStartTime => _classes.OrderBy(a => a.StartTime).FirstOrDefault()?.StartTime ?? default;

        public TimeSpan MaximumBreaksTotal
        {
            get
            {
                if (_classes.Count == 0) { return default; }
                Dictionary<DayOfWeek, List<UClass>> classes = new Dictionary<DayOfWeek, List<UClass>>();
                foreach (var uClass in _classes)
                {
                    foreach (var day in uClass.Days)
                    {
                        if (classes.ContainsKey(day) == false)
                        {
                            classes.Add(day, new List<UClass> { uClass });
                        }
                        else
                        {
                            classes[day].Add(uClass);
                        }
                    }
                }
                foreach (var lst in classes)
                {
                    lst.Value.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
                }
                return classes.Select(a =>
                {
                    TimeSpan breaks = new TimeSpan();
                    for (int i = 1; i < a.Value.Count; i++)
                    {
                        breaks += a.Value[i].StartTime - a.Value[i - 1].EndTime;
                    }
                    return breaks;
                }).DefaultIfEmpty().Max();
            }
        }

        public TimeSpan LongestDayDuration
        {
            get
            {
                if (_classes.Count == 0) { return default; }
                Dictionary<DayOfWeek, List<UClass>> classes = new Dictionary<DayOfWeek, List<UClass>>();
                foreach (var uClass in _classes)
                {
                    foreach (var day in uClass.Days)
                    {
                        if (classes.ContainsKey(day) == false)
                        {
                            classes.Add(day, new List<UClass> { uClass });
                        }
                        else
                        {
                            classes[day].Add(uClass);
                        }
                    }
                }
                return classes.Select(a => a.Value.Max(c => c.EndTime) - a.Value.Min(c => c.StartTime)).Max();
            }
        }

        public int FinancialHours => (_classes.Count == 0) ? default : _classes.Sum(a => a.Course.FinancialHours);

        public bool Add(UClass newClass)
        {
            if (newClass is null)
            {
                throw new ArgumentNullException(nameof(newClass));
            }
            if (_classes.Any(a => a.Intersects(newClass)) || this.HasCourse(newClass.Course))
            {
                return false;
            }
            _classes.Add(newClass);
            return true;
        }

        public void Remove(UClass uClass)
        {
            if (uClass is null)
            {
                throw new ArgumentNullException(nameof(uClass));
            }
            _classes.RemoveAll(a => a == uClass);
        }

        public bool HasCourse(UCourse course)
        {
            if (course is null)
            {
                throw new ArgumentNullException(nameof(course));
            }
            return _classes.Any(c => c.Course.Equals(course));
        }

        public bool HasCourses(IEnumerable<UCourse> courses)
        {
            if (courses is null)
            {
                throw new ArgumentNullException(nameof(courses));
            }
            return courses.Except(_classes.Select(c => c.Course)).Any() == false;
        }

        public override string ToString()
        {
            return $"{FirstStartTime}\t{LastEndTime}";
        }

        public object Clone()
        {
            return new USchedule() { _classes = new List<UClass>(this._classes) };
        }

        public override int GetHashCode() => _classes.OrderBy(a => a.StartTime).ThenBy(a => a.InstructorName).Aggregate(0, (a, b) => HashCodeHelper.CombineHashCodes(a, b.GetHashCode()));

        public override bool Equals(object obj)
        {
            if (obj is USchedule schedule)
            {
                return Equals(schedule);
            }

            return false;
        }

        public bool Equals(USchedule other)
        {
            if (other is null || other._classes.Count != _classes.Count)
            {
                return false;
            }
            IEnumerable<UClass> OrderClassesForComparsion(IEnumerable<UClass> classes) =>
               classes.OrderBy(c => c.StartTime).ThenBy(c => c.InstructorName);

            return OrderClassesForComparsion(_classes).SequenceEqual(OrderClassesForComparsion(other.Classes));
        }

        public static async Task<USchedule> FromFile(string filePath)
        {
            if (File.Exists(filePath) == false)
            {
                throw new FileNotFoundException($"File {Path.GetFileName(filePath)} doesn't exist.", filePath);
            }
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return await FromStream(fileStream);
            }
        }
        public static async Task<USchedule> FromStream(Stream scheduleStream)
        {
            if (scheduleStream is null) { throw new ArgumentNullException(nameof(scheduleStream)); }
            if (scheduleStream.CanRead == false) { throw new ArgumentException("Stream can't be used for reading."); }
          
            var classes = new List<UClass>(await UClass.FromStream(scheduleStream));
            USchedule result = new USchedule();
            foreach (var cls in classes)
            {
                if (result.Add(cls) == false)
                {
                    throw new Exception("Invalid schedule.");
                }
            }
            return result;
        }
        public async Task Save(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException($"Parameter {nameof(filePath)} can't be null or whitespace.");
            }
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await Save(fileStream);
            }
        }
        public async Task Save(Stream scheduleStream)
        {
            if (scheduleStream is null) { throw new ArgumentNullException(nameof(scheduleStream)); }
            if (scheduleStream.CanWrite == false) { throw new ArgumentException("Stream can't be used for writing."); }
            await UClass.Save(Classes, scheduleStream);
        }
    }
}