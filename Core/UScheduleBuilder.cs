using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleBuilder.Core
{
    /// <remarks>
    /// NOT THREAD SAFE
    /// </remarks>
    public class UScheduleBuilder
    {
        public HashSet<DayOfWeek> Days { get; } = new HashSet<DayOfWeek>();
        public HashSet<UCourse> ObligatoryCourses { get; } = new HashSet<UCourse>();
        public HashSet<UClass> Classes { get; } = new HashSet<UClass>();
        public List<UBreak> Breaks { get; } = new List<UBreak>();
        public TimeSpan MinStartTime { get; set; }
        public TimeSpan MaxStartTime { get; set; }
        public TimeSpan MinEndTime { get; set; }
        public TimeSpan MaxEndTime { get; set; }

        public int MinFinancialHours { get; set; }
        public int MaxFinancialHours { get; set; }

        /// <summary>
        /// The classes that are used to build the schedules
        /// </summary>
        private UClass[] _buildClasses;
        /// <summary>
        /// The built schedules from <see cref="_buildClasses"/>
        /// </summary>
        private List<USchedule> _builtSchedules;

        /// <summary>
        /// The temporary schedule thats used to build the schedules
        /// </summary>
        USchedule _buildingSchedule;
        //A fucked up dynamic
        //more like a brute-force
        private void BuildFrom(int index)
        {
            if (index >= _buildClasses.Length) { return; }

            //With me
            if (_buildingSchedule.Add(_buildClasses[index]))
            {
                if (_buildingSchedule.FinancialHours > MaxFinancialHours)
                {
                    //Just don't try adding other classes
                    _buildingSchedule.Remove(_buildClasses[index]);
                }
                else if (_buildingSchedule.FinancialHours == MaxFinancialHours)
                {
                    //Just don't try adding other classes
                    _builtSchedules.Add(_buildingSchedule.Clone() as USchedule);
                }
                else
                {
                    //If we reached here then FinancialHours < MaxFinancialHours
                    if (_buildingSchedule.FinancialHours >= MinFinancialHours)
                    {
                        _builtSchedules.Add(_buildingSchedule.Clone() as USchedule);
                    }
                    BuildFrom(index + 1);
                }
                _buildingSchedule.Remove(_buildClasses[index]);
            }

            //without me
            BuildFrom(index + 1);
        }

        public USchedule[] Build()
        {
            if (MinFinancialHours > MaxFinancialHours) { throw new Exception($"{nameof(MinFinancialHours)} can't be >= {nameof(MaxFinancialHours)}."); }
            if (MinStartTime > MaxStartTime) { throw new Exception($"{nameof(MinStartTime)} can't be >= {nameof(MaxStartTime)}."); }
            if (MinEndTime > MaxEndTime) { throw new Exception($"{nameof(MinEndTime)} can't be >= {nameof(MaxEndTime)}."); }
            if (Days.Count == 0) { throw new Exception($"There must be at least one day in collection {nameof(Days)}"); }
            if (Classes.Count == 0) { throw new Exception($"There must be at least one class in collection {nameof(Classes)}"); }


            _buildClasses = Classes
            .Where(c => c.Days.Except(Days.AsEnumerable()).Any() == false)
            .Where(c => c.StartTime >= MinStartTime && c.EndTime <= MaxEndTime)
            .Where(c => Breaks.Where(b => c.Intersects(b)).Any() == false)
            .OrderBy(a => a.StartTime).ToArray();
            _builtSchedules = new List<USchedule>();
            _buildingSchedule = new USchedule();
            BuildFrom(0);

            var result = _builtSchedules
             //Filter the schedules and take the ones that fit our conditions
            .Where(s => s.HasCourses(ObligatoryCourses))
            .Where(s => s.FirstStartTime >= MinStartTime && s.FirstStartTime <= MaxStartTime)
            .Where(s => s.LastEndTime >= MinEndTime && s.LastEndTime <= MaxEndTime)
            
            .OrderBy(c => c.Days.Count())
            .ThenBy(c => c.MaximumBreaksTotal)
            .ThenByDescending(c => c.FinancialHours)
            .ThenByDescending(c => c.FirstStartTime)
            .ThenBy(c => c.LastEndTime)
            .ToArray();

            _builtSchedules.Clear();
            _buildClasses = null;
            _buildingSchedule = null;
            return result;
        }
    }
}