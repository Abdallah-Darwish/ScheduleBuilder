using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsUI
{
    internal static class Extensions
    {
        public static TimeSpan ToTimeSpan(this in DateTime date)
        {
            return new TimeSpan(0, date.Hour, date.Minute, date.Second);
        }
    }
}
