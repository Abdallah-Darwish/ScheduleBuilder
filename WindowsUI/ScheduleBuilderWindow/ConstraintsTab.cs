using ScheduleBuilder.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WindowsUI
{
    public partial class ScheduleBuilderWindow
    {
        Dictionary<int, int> _mandatoryCoursesReferencingCount = new Dictionary<int, int>();
        ObservableCollection<UCourse> _lstMandatoryCoursesItems = new ObservableCollection<UCourse>();
        ObservableCollection<UBreak> _lstBreaksItems = new ObservableCollection<UBreak>();
        void InitializeConstraintsTab()
        {
            lstMandatoryCourses.ItemsSource = _lstMandatoryCoursesItems;
            lstBreaks.ItemsSource = _lstBreaksItems;
            lstContraintDays.Items.Add(DayOfWeek.Saturday);
            lstContraintDays.Items.Add(DayOfWeek.Sunday);
            lstContraintDays.Items.Add(DayOfWeek.Monday);
            lstContraintDays.Items.Add(DayOfWeek.Tuesday);
            lstContraintDays.Items.Add(DayOfWeek.Wednesday);
            lstContraintDays.Items.Add(DayOfWeek.Thursday);

            tmeConstraintMinStartTime.Value = new DateTime(1, 1, 1, 8, 0, 0);
            tmeConstraintMinEndTime.Value = new DateTime(1, 1, 1, 8, 0, 0);

            tmeConstraintMaxStartTime.Value = new DateTime(1, 1, 1, 20, 0, 0);
            tmeConstraintMaxEndTime.Value = new DateTime(1, 1, 1, 20, 0, 0);

            udConstraintMinFinancialHours.Value = 12;
            udConstraintMaxFinancialHours.Value = 16;

            lstContraintDays.SelectAll();

        }
        void AddMandatoryCourse(UCourse course)
        {
            if (_mandatoryCoursesReferencingCount.ContainsKey(course.Id))
            {
                _mandatoryCoursesReferencingCount[course.Id]++;
            }
            else
            {
                _mandatoryCoursesReferencingCount.Add(course.Id, 1);
                _lstMandatoryCoursesItems.Add(course);
            }
        }
        void RemoveMandatoryCourse(UCourse course)
        {
            int referencesCount = _mandatoryCoursesReferencingCount[course.Id];
            if (referencesCount == 1)
            {
                _lstMandatoryCoursesItems.Remove(course);
                _mandatoryCoursesReferencingCount.Remove(course.Id);
            }
            else
            {
                _mandatoryCoursesReferencingCount[course.Id]--;
            }
        }
        void ClearMandatoryCourses()
        {
            lstMandatoryCourses.UnSelectAll();
            _lstMandatoryCoursesItems.Clear();
            _mandatoryCoursesReferencingCount.Clear();
        }
        private void BtnAddBreak_Click(object sender, RoutedEventArgs e)
        {
            if (tmeBreakStartTime.Value.HasValue == false)
            {
                MessageBox.Show("Invalid break start time.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (tmeBreakEndTime.Value.HasValue == false)
            {
                MessageBox.Show("Invalid break end time.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            TimeSpan startTime = tmeBreakStartTime.Value.Value.ToTimeSpan(), endTime = tmeBreakEndTime.Value.Value.ToTimeSpan();

            if (startTime >= endTime)
            {
                MessageBox.Show("Break start time can't be after or equal to its end time.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            _lstBreaksItems.Add(new UBreak(startTime, endTime));
        }
        private void BtnRemoveBreak_Click(object sender, RoutedEventArgs e)
        {
            var selectedBreakIndex = lstBreaks.SelectedIndex;
            if (selectedBreakIndex == -1) { return; }
            _lstBreaksItems.RemoveAt(selectedBreakIndex);
        }

        bool ValidateConstraints()
        {

            //check for fully missing info
            if (tmeConstraintMinStartTime.Value.HasValue == false && tmeConstraintMaxStartTime.Value.HasValue == false)
            {
                MessageBox.Show("Please enter minimum and maximum start time.", "Missing Data.", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (tmeConstraintMinEndTime.Value.HasValue == false && tmeConstraintMaxEndTime.Value.HasValue == false)
            {
                MessageBox.Show("Please enter minimum and maximum end time.", "Missing Data.", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (udConstraintMinFinancialHours.Value.HasValue == false && udConstraintMaxFinancialHours.Value.HasValue == false)
            {
                MessageBox.Show("Please enter minimum and maximum financial hours.", "Missing Data.", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (lstContraintDays.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select one day at least.", "Missing Information.", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            //Check for invalid values
            if (tmeConstraintMinStartTime.Value.HasValue && tmeConstraintMaxStartTime.Value.HasValue
                && tmeConstraintMinStartTime.Value > tmeConstraintMaxStartTime.Value)
            {
                MessageBox.Show("Minimum start time can't be after Maximum start time.", "Invalid Data.", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (tmeConstraintMinEndTime.Value.HasValue && tmeConstraintMaxEndTime.Value.HasValue
                && tmeConstraintMinEndTime.Value > tmeConstraintMaxEndTime.Value)
            {
                MessageBox.Show("Minimum end time can't be after maximum end time.", "Invalid Data.", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (udConstraintMinFinancialHours.Value.HasValue && udConstraintMaxFinancialHours.Value.HasValue
                && udConstraintMinFinancialHours.Value > udConstraintMaxFinancialHours.Value)
            {
                MessageBox.Show("Minimum financial hours can't be bigger than maximum financial hours.", "Invalid Data.", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            //In case one field is entered only
            if (tmeConstraintMinStartTime.Value.HasValue ^ tmeConstraintMaxStartTime.Value.HasValue)
            {
                tmeConstraintMinStartTime.Value = tmeConstraintMaxStartTime.Value = tmeConstraintMinStartTime.Value ?? tmeConstraintMaxStartTime.Value;
            }
            if (tmeConstraintMinEndTime.Value.HasValue ^ tmeConstraintMaxEndTime.Value.HasValue)
            {
                tmeConstraintMinEndTime.Value = tmeConstraintMaxEndTime.Value = tmeConstraintMinEndTime.Value ?? tmeConstraintMaxEndTime.Value;
            }
            if (udConstraintMinFinancialHours.Value.HasValue ^ udConstraintMaxFinancialHours.Value.HasValue)
            {
                udConstraintMinFinancialHours.Value = udConstraintMaxFinancialHours.Value = udConstraintMinFinancialHours.Value ?? udConstraintMaxFinancialHours.Value;
            }
            return true;
        }
    }
}
