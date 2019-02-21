using Microsoft.Win32;
using Newtonsoft.Json;
using ScheduleBuilder.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WindowsUI
{
    public partial class ScheduleBuilderWindow
    {
        private static readonly DaysEqualityComparer s_daysEqualityComparer = new DaysEqualityComparer();
        void InitializeClassesTab()
        {
            lstClasses.ItemsSource = _searchListItems;
            tmeSearchStartTime.Value = null;
            tmeSearchEndTime.Value = null;
            cbxSearchDays.Items.Add(DayOfWeek.Monday);
            cbxSearchDays.Items.Add(DayOfWeek.Saturday);
            cbxSearchDays.Items.Add(DayOfWeek.Sunday);
            cbxSearchDays.Items.Add(DayOfWeek.Thursday);
            cbxSearchDays.Items.Add(DayOfWeek.Tuesday);
            cbxSearchDays.Items.Add(DayOfWeek.Wednesday);
        }
        private void BtnClearSeacrhFields_Click(object sender, RoutedEventArgs e)
        {
            cbxSearchDays.UnSelectAll();
            cbxSearchClassName.SelectedItem = null;
            cbxSearchInstructorName.SelectedItem = null;
            chkSearchHasAvilablePlaces.IsChecked = false;
            tmeSearchStartTime.Value = null;
            tmeSearchEndTime.Value = null;

            UpdateClassesListView();
        }
        void UpdateClassesListView()
        {
            var includedClassesIds = new HashSet<int>(
                _classesModels.AsParallel().AsUnordered()
                .WithMergeOptions(ParallelMergeOptions.NotBuffered)
                .Where(c => c.Value.IsIncluded).Select(a => a.Value.Id));

            _searchListItems.Clear();
            foreach (var selectedClassId in includedClassesIds)
            {
                _searchListItems.Add(_classesModels[selectedClassId]);
            }
            foreach (var cls in _classesModels.Values)
            {
                if (includedClassesIds.Contains(cls.Id)) { continue; }
                _searchListItems.Add(cls);
            }
            lstClasses.Items.Refresh();
            lstClasses.UpdateLayout();

        }
        void ReloadClassesModelsAndUpdateClassesList()
        {
            var includedClassesIds = new HashSet<int>(_classesModels.AsParallel().AsUnordered().WithMergeOptions(ParallelMergeOptions.NotBuffered)
            .Where(c => c.Value.IsIncluded).Select(a => a.Value.Id));

            _classesModels.Clear();
            _classesModels = _classes.Select(c => new UClassListModel(c)).ToDictionary(c => c.Id);

            foreach (var selectedClassId in includedClassesIds)
            {
                if (_classesModels.TryGetValue(selectedClassId, out var selectedClass))
                {
                    selectedClass.IsIncluded = true;
                }
            }

            UpdateClassesListView();

            cbxSearchClassName.Items.Clear();
            var classesNames = _classes.Select(c => c.Course).Distinct().Select(c => c.Name).OrderBy(n => n);
            var instructorsNames = _classes.Select(c => c.InstructorName).Distinct().OrderBy(n => n);

            foreach (var className in classesNames)
            {
                cbxSearchClassName.Items.Add(className);
            }
            foreach (var instructorName in instructorsNames)
            {
                cbxSearchInstructorName.Items.Add(instructorName);
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            bool UselessFilter(UClassListModel _____x) { return true; }
            string className = cbxSearchClassName.SelectedItem as string, instructorName = cbxSearchInstructorName.SelectedItem as string;
            DayOfWeek[] selectedDays = cbxSearchDays.SelectedItems.OfType<DayOfWeek>().ToArray();
            TimeSpan startTime = tmeSearchStartTime.Value?.ToTimeSpan() ?? default;
            TimeSpan endTime = tmeSearchEndTime.Value?.ToTimeSpan() ?? default;
            Func<UClassListModel, bool> classNameFilter = string.IsNullOrWhiteSpace(className) == false ? new Func<UClassListModel, bool>((UClassListModel m) => m.Name == className) : UselessFilter;
            Func<UClassListModel, bool> instructorNameFilter = string.IsNullOrWhiteSpace(instructorName) == false ? new Func<UClassListModel, bool>((UClassListModel m) => m.InstructorName == instructorName) : UselessFilter;
            Func<UClassListModel, bool> daysFilter = selectedDays.Length != 0 ? new Func<UClassListModel, bool>((UClassListModel m) => s_daysEqualityComparer.Equals(selectedDays, m.Source.Days)) : UselessFilter;
            Func<UClassListModel, bool> startTimeFilter = tmeSearchStartTime.Value.HasValue ? new Func<UClassListModel, bool>((UClassListModel m) => m.Source.StartTime == startTime) : UselessFilter;
            Func<UClassListModel, bool> endTimeFilter = tmeSearchEndTime.Value.HasValue ? new Func<UClassListModel, bool>((UClassListModel m) => m.Source.EndTime == endTime) : UselessFilter;
            Func<UClassListModel, bool> emptySpotsFilter = chkSearchHasAvilablePlaces.IsChecked == true ? new Func<UClassListModel, bool>((UClassListModel m) => m.NumberOfPlaces > 0) : UselessFilter;

            var passedClasses = _classesModels.Values.AsParallel().WithMergeOptions(ParallelMergeOptions.NotBuffered)
                .Where(classNameFilter)
                .Where(instructorNameFilter)
                .Where(daysFilter)
                .Where(startTimeFilter)
                .Where(endTimeFilter)
                .Where(emptySpotsFilter).ToArray();

            _searchListItems.Clear();
            foreach (var cls in passedClasses)
            {
                _searchListItems.Add(cls);
            }

        }

        private void ChkIncluded_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox includedCheckBox = e.Source as CheckBox;
            var selectedClass = _classesModels[(int)includedCheckBox.Tag];
            AddMandatoryCourse(selectedClass.Source.Course);
            int newIndex = 0;
            while (newIndex < _searchListItems.Count && _searchListItems[newIndex].Id != selectedClass.Id && _searchListItems[newIndex].IsIncluded)
            {
                newIndex++;
            }
            selectedClass.IsIncluded = true;
            _searchListItems.Move(_searchListItems.IndexOf(selectedClass), newIndex);
        }
        private void ChkIncluded_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox includedCheckBox = e.Source as CheckBox;
            var selectedClass = _classesModels[(int)includedCheckBox.Tag];
            RemoveMandatoryCourse(selectedClass.Source.Course);

            var newIndex = _searchListItems.IndexOf(selectedClass) + 1;
            while (newIndex < _searchListItems.Count && _searchListItems[newIndex].IsIncluded)
            {
                newIndex++;
            }
            if (newIndex == _searchListItems.Count) newIndex--;
            _searchListItems.Move(_searchListItems.IndexOf(selectedClass), newIndex);

        }
        private void ClearSelection()
        {
            foreach (var item in _classesModels.Values)
            {
                item.IsIncluded = false;
            }
            ClearMandatoryCourses();
            UpdateClassesListView();
        }
        private void BtnClearSelection_Click(object sender, RoutedEventArgs e)
        {
            ClearSelection();
        }

        private async void BtnSaveSelected_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog()
            {
                Filter = "JSON File (*.json)|*.json",
                AddExtension = true,
                CheckFileExists = false,
                DefaultExt = ".json",
                ValidateNames = true,
                Title = "Classes file",
                DereferenceLinks = true
            };
            if (sfd.ShowDialog() == false) return;
            await UClass.Save(_classesModels.Values.Where(c => c.IsIncluded).Select(c => c.Source), sfd.FileName);
        }
        private async void BtnLoadSelection_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                Filter = "JSON File (*.json)|*.json",
                AddExtension = true,
                CheckFileExists = false,
                DefaultExt = ".json",
                Multiselect = false,
                ValidateNames = true,
                Title = "Classes file",
                DereferenceLinks = true
            };

            if (ofd.ShowDialog() == false) return;
            UClass[] newSelection = await UClass.FromFile(ofd.FileName);
            ClearSelection();
            foreach (var selectedClass in newSelection)
            {
                _classesModels[selectedClass.Id].IsIncluded = true;
                AddMandatoryCourse(selectedClass.Course);
            }

            UpdateClassesListView();
        }

    }
}
