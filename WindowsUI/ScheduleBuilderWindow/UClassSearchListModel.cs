using ScheduleBuilder.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsUI
{
    class UClassListModel : IEquatable<UClassListModel>
    {
        public int Id { get; set; }
        public bool IsIncluded { get; set; } = false;
        public string Time { get; set; }
        public string InstructorName { get; set; }
        public string Days { get; set; }
        public string Name { get; set; }
        public int NumberOfPlaces { get; set; }
        public UClass Source { get; }
        public UClassListModel(UClass cls)
        {
            Source = cls ?? throw new ArgumentNullException(nameof(cls));
            Id = cls.Id;
            Time = $"{cls.StartTime:hh\\:mm}-{cls.EndTime:hh\\:mm}";
            InstructorName = cls.InstructorName;
            Days = DayOfWeekConverter.ToString(cls.Days);
            Name = cls.Course.Name;
            NumberOfPlaces = Math.Max(0, cls.Capacity - cls.NumberOfRegisteredStudents);
        }
        public override bool Equals(object obj)
        {
            if(obj is UClassListModel model)
            {
                return Equals(model);
            }
            return false;
        }

        public bool Equals(UClassListModel other)
        {
            if(other is null) { return false; }
            return Id == other.Id;
        }
        public override int GetHashCode() => (int)Id;
    }
}
