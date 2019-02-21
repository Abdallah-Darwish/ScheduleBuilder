using ScheduleBuilder.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WindowsUI
{
    class UClassClassesListModel : IEquatable<UClassClassesListModel>, INotifyPropertyChanged
    {
        private Brush _statusForegroundBrush;
        private string _status;

        public int Id { get; set; }
        public string InstructorName { get; set; }
        public string Name { get; set; }
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }
        public Brush StatusForegroundBrush
        {
            get => _statusForegroundBrush;
            set
            {
                _statusForegroundBrush = value;
                OnPropertyChanged(nameof(StatusForegroundBrush));
            }
        }
        public UClass Source { get; }
        public UClassClassesListModel(UClass cls)
        {
            Source = cls ?? throw new ArgumentNullException(nameof(cls));
            Id = cls.Id;
            InstructorName = cls.InstructorName;
            Name = cls.Course.Name;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public override bool Equals(object obj)
        {
            if (obj is UClassClassesListModel model)
            {
                return Equals(model);
            }
            return false;
        }

        public bool Equals(UClassClassesListModel other)
        {
            if (other is null) { return false; }
            return Id == other.Id;
        }
        public override int GetHashCode() => Id;
    }
}
