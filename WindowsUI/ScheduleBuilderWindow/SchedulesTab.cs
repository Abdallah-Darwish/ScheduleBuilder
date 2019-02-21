using Microsoft.Win32;
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
        private readonly ObservableCollection<UScheduleListModel> _lstSchedulesItems = new ObservableCollection<UScheduleListModel>();
        void InitializeSchedulesTab()
        {
            lstSchedules.ItemsSource = _lstSchedulesItems;
            lstSchedules.Items.Refresh();

        }
        private async void BtnSaveSchedule_Click(object sender, RoutedEventArgs e)
        {
            UScheduleListModel selectedSchedule = lstSchedules.SelectedItem as UScheduleListModel;
            if (selectedSchedule is null) { return; }
            var sfd = new SaveFileDialog()
            {
                Filter = "JSON File (*.json)|*.json",
                AddExtension = true,
                CheckFileExists = false,
                DefaultExt = ".json",
                ValidateNames = true,
                Title = "Schedule file.",
                DereferenceLinks = true
            };
            if (sfd.ShowDialog() == false) return;
            await selectedSchedule.Source.Save(sfd.FileName);
        }
        private void ClearSelectedSchedule()
        {
            lblDays.Content = ".";
            lblFinancialHours.Content = ".";
            lblFirstStartTime.Content = ".";
            lblLastEndTime.Content = ".";
            lblLongestDayDuration.Content = ".";
            lblMaximumBreaksTotal.Content = ".";
            lstSchedule.ItemsSource = null;
            lstSchedule.Items.Refresh();
        }
        private void LstSchedules_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UScheduleListModel selectedSchedule = lstSchedules.SelectedItem as UScheduleListModel;
            if (selectedSchedule is null)
            {
                ClearSelectedSchedule();
                return;
            }
            lstSchedule.ItemsSource = null;
            lstSchedule.ItemsSource = selectedSchedule.ClassesModels;
            lstSchedule.Items.Refresh();
            lblDays.Content = $"Days: {selectedSchedule.Days}";
            lblFinancialHours.Content = $"Financial hours: {selectedSchedule.FinancialHours}";
            lblFirstStartTime.Content = $"First start time: {selectedSchedule.FirstStartTime}";
            lblLastEndTime.Content = $"Last end time: {selectedSchedule.LastEndTime}";
            lblLongestDayDuration.Content = $"Longest day duration: {selectedSchedule.LongestDayDuration}";
            lblMaximumBreaksTotal.Content = $"Maximum breaks per day: {selectedSchedule.MaximumBreaksTotal}";
        }
        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            btnGenerate.IsEnabled = false;
            ClearSelectedSchedule();
            _lstSchedulesItems.Clear();
            if (ValidateConstraints() == false)
            {
                btnGenerate.IsEnabled = true;
                return;
            }
            UScheduleBuilder builder = new UScheduleBuilder();
            foreach (var cls in _classesModels.Values.Where(c => c.IsIncluded))
            {
                builder.Classes.Add(cls.Source);
            }
            foreach (var mandCls in lstMandatoryCourses.SelectedItems.OfType<UCourse>())
            {
                builder.ObligatoryCourses.Add(mandCls);
            }
            builder.Breaks.AddRange(_lstBreaksItems);
            foreach (var day in lstContraintDays.SelectedItems.OfType<DayOfWeek>())
            {
                builder.Days.Add(day);
            }
            builder.MinStartTime = tmeConstraintMinStartTime.Value.Value.ToTimeSpan();
            builder.MaxStartTime = tmeConstraintMaxStartTime.Value.Value.ToTimeSpan();
            builder.MinEndTime = tmeConstraintMinEndTime.Value.Value.ToTimeSpan();
            builder.MaxEndTime = tmeConstraintMaxEndTime.Value.Value.ToTimeSpan();
            builder.MinFinancialHours = udConstraintMinFinancialHours.Value.Value;
            builder.MaxFinancialHours = udConstraintMaxFinancialHours.Value.Value;
            foreach (var schedule in builder.Build())
            {
                _lstSchedulesItems.Add(new UScheduleListModel(schedule));
            }
            btnGenerate.IsEnabled = true;
        }
    }
}
