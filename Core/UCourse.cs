using System;

namespace ScheduleBuilder.Core
{
    [Serializable]
    public class UCourse : IEquatable<UCourse>
    {
        public string Name { get; }
        public int Id { get; }
        public int FinancialHours { get; }

        public override int GetHashCode() => Id;

        public override bool Equals(object obj)
        {
            if (obj is UCourse course)
            {
                return Equals(course);
            }
            return false;
        }
        
        /// <param name="name">Arabic name of the course.</param>
        public UCourse(int id, int financialHours, string name)
        {
            Id = id;
            FinancialHours = financialHours;
            Name = name;
        }

        public bool Equals(UCourse other)
        {
            if (other is null)
            {
                return false;
            }
            return Id == other.Id;
        }

        public override string ToString() => $"{Id} : {Name}";
    }
}