using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleBuilder.Core
{
    /// <summary>
    /// Converts days from there string or char form to <see cref="DayOfWeek"/> represntation and vice-versa.
    /// </summary>
    public static class DayOfWeekConverter
    {
        private static readonly Dictionary<string, DayOfWeek[]> _daysOfWeek = new Dictionary<string, DayOfWeek[]>
        {
            ["ح"] = new DayOfWeek[] { DayOfWeek.Sunday },
            ["ن"] = new DayOfWeek[] { DayOfWeek.Monday },
            ["ث"] = new DayOfWeek[] { DayOfWeek.Tuesday },
            ["ر"] = new DayOfWeek[] { DayOfWeek.Wednesday },
            ["خ"] = new DayOfWeek[] { DayOfWeek.Thursday },
            ["ي"] = new DayOfWeek[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday },
            ["الاحد"] = new DayOfWeek[] { DayOfWeek.Sunday },
            ["الاثنين"] = new DayOfWeek[] { DayOfWeek.Monday },
            ["الثلاثاء"] = new DayOfWeek[] { DayOfWeek.Tuesday },
            ["الاربعاء"] = new DayOfWeek[] { DayOfWeek.Wednesday },
            ["الخميس"] = new DayOfWeek[] { DayOfWeek.Thursday },
            ["السبت"] = new DayOfWeek[] { DayOfWeek.Saturday },
            ["الجمعة"] = new DayOfWeek[] { DayOfWeek.Friday },
            ["يومياً"] = new DayOfWeek[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday }
        };

        private static readonly Dictionary<DayOfWeek, char> _charDaysNames = new Dictionary<DayOfWeek, char>
        {
            [DayOfWeek.Sunday] = 'ح',
            [DayOfWeek.Monday] = 'ن',
            [DayOfWeek.Tuesday] = 'ث',
            [DayOfWeek.Wednesday] = 'ر',
            [DayOfWeek.Thursday] = 'خ',
            [DayOfWeek.Saturday] = 'س',
            [DayOfWeek.Thursday] = 'خ',
            [DayOfWeek.Friday] = 'ج',
        };
        private static readonly Dictionary<DayOfWeek, string> _fullDaysNames = new Dictionary<DayOfWeek, string>
        {
            [DayOfWeek.Sunday] = "الاحد",
            [DayOfWeek.Monday] = "الاثنين",
            [DayOfWeek.Tuesday] = "الثلاثاء",
            [DayOfWeek.Wednesday] = "الاربعاء",
            [DayOfWeek.Thursday] = "الخميس",
            [DayOfWeek.Saturday] = "السبت",
            [DayOfWeek.Friday] = "الجمعة"
        };
        public const string DaysNamesSepertaor = " ";

        /// <summary>
        /// Converts a <see cref="DayOfWeek"/> to its one character name.
        /// </summary>
        /// <param name="day">The <see cref="DayOfWeek"/> to get its name.</param>
        public static char ToChar(DayOfWeek day) => _charDaysNames[day];

        /// <summary>
        /// Converts a <see cref="DayOfWeek"/> to its full name.
        /// </summary>
        /// <param name="day">The <see cref="DayOfWeek"/> to get its name.</param>
        public static string ToString(DayOfWeek day) => _fullDaysNames[day];

        /// <summary>
        /// Converts a <see cref="DayOfWeek"/> list to its appropriate text represntation.
        /// </summary>
        /// <param name="days">The days to convert to text.</param>
        public static string ToString(IEnumerable<DayOfWeek> days)
        {
            if (days is null)
            {
                throw new ArgumentNullException(nameof(days));
            }
            days = days.Distinct();

            if (_daysOfWeek["يومياً"].Except(days).Any() == false)
            {
                return "يومياً";
            }
            else if (days.Count() == 1)
            {
                return ToString(days.First());
            }
            else
            {
                return string.Join(DaysNamesSepertaor, days.Select(a => ToChar(a)));
            }
        }
        /// <summary>
        /// Converts a day name to its <see cref="DayOfWeek"/> represntation.
        /// </summary>
        /// <param name="day">The day to convert.</param>
        public static DayOfWeek ToDayOfWeek(char day)
        {
            if (_daysOfWeek.TryGetValue(day.ToString(), out DayOfWeek[] res))
            {
                return res.First();
            }
            throw new ArgumentException($"The value {day} is invalid day name.", nameof(day));
        }

        /// <summary>
        /// Converts a day string(a one or multiple days seperated by <see cref="DaysNamesSepertaor"/>) to its <see cref="DayOfWeek"/> represntation.
        /// </summary>
        /// <param name="daysString">The day string to convert.</param>
        public static IEnumerable<DayOfWeek> ToDays(string daysString)
        {
            //this line is needed because "ProposedCoursesPage" sometimes will call this method with empty string.
            if (string.IsNullOrWhiteSpace(daysString)) return Enumerable.Empty<DayOfWeek>();

            var days = daysString.Split(new string[] { DaysNamesSepertaor }, StringSplitOptions.RemoveEmptyEntries);
            if (days.Count() == 1)
            {
                string dayName = days.First();
                if (_daysOfWeek.TryGetValue(dayName, out var res) == false)
                {
                    throw new ArgumentException($"Cant parse {dayName} as a {nameof(DayOfWeek)}");

                }
                return res;
            }
            string errorCauser = days.FirstOrDefault(a => a.Length > 1);
            if (errorCauser != null)
            {
                throw new ArgumentException($"Cant parse {errorCauser} as a {nameof(DayOfWeek)}");
            }
            return ToDays(days.Select(a => a[0]));
        }
        /// <summary>
        /// Converts a list of days char names to a <see cref="DayOfWeek"/> list.
        /// </summary>
        /// <param name="daysChars">The list of days char names to convert</param>
        public static IEnumerable<DayOfWeek> ToDays(IEnumerable<char> daysChars)
        {
            if (daysChars is null)
            {
                throw new ArgumentNullException(nameof(daysChars));
            }
            return daysChars.Select(a => ToDayOfWeek(a));
        }
    }
}