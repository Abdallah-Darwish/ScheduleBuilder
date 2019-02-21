using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScheduleBuilder.Core;
namespace ScheduleBuilder
{
    partial class MainWindow
    {
        private class UClassListViewItem
        {
            private readonly UClass _class;
            public string Days => DayOfWeekConverter.ToString(_class.Days);
            public string TeacherName => _class.InstructorName;
            public string Subject => _class.Course.ToString();
            public string StartTime => _class.StartTime.ToString("h\\:mm");
            public string EndTime => _class.EndTime.ToString("h\\:mm");

            public UClassListViewItem(UClass uClass)
            {
                if (ReferenceEquals(uClass, null))
                {
                    throw new ArgumentNullException(nameof(uClass));
                }
                _class = uClass;
            }
        }
    }
}