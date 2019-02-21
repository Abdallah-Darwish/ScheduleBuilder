using Newtonsoft.Json;
using ScheduleBuilder.Core;
using ScheduleBuilder.Core.Parsing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ScheduleBuilder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private UScheduleBuilder _builder = null;
        private readonly UIElement[] _criticalInputElemnts;

        public MainWindow()
        {
            InitializeComponent();
            _criticalInputElemnts = new UIElement[] { timeBegining, timeEnd, chkExcludeFullClasses, cboxDays, btnBrowseCSV, btnProcess, timeHours };
        }

        private void btnBrowseCSV_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableUI();
                using (var ofd = new System.Windows.Forms.OpenFileDialog { AddExtension = true, DefaultExt = ".csv", CheckFileExists = true, DereferenceLinks = true, Filter = "CSV files (*.csv)|*.csv", FilterIndex = 0, ValidateNames = true })
                {
                    if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        txtDataPath.Text = ofd.FileName;
                    }
                    if (string.IsNullOrWhiteSpace(txtDataPath.Text) || (File.Exists(txtDataPath.Text) == false))
                    {
                        MessageBox.Show("Please select a valid data file.", "Invalid input", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    _builder = new UScheduleBuilder();
                    ClearInfo();
                    lstSchedules.Items.Clear();
                    lstObligatorySubjects.ItemsSource = null;
                    lstObligatorySubjects.Items.Clear();
                    foreach (var classData in File.ReadAllLines(txtDataPath.Text).Select(a => a.Split(',')))
                    {
                        if (chkExcludeFullClasses.IsChecked ?? false)
                        {
                            if (classData[8] == classData[9])
                            {
                                continue;
                            }
                        }
                        TimeSpan[] times = classData[6].Split(' ').Select(a => TimeSpan.ParseExact(a, "h\\:mm", CultureInfo.InvariantCulture)).ToArray();
                        //UClass uClass = new UClass(course: new UCourse(int.Parse(classData[0]), int.Parse(classData[2]), classData[1]), instructorName: classData[4], days: DayOfWeekConverter.ToDays(classData[5].Split(' ').Select(a => a[0])), startTime: times[0], endTime: times[1], section: 0, capacity: 0, numberOfRegisteredStudents: 0, year: 2018, USemester.First);

                        //_builder.Classes.Add(uClass);
                    }
                    lstObligatorySubjects.ItemsSource = _builder.Classes.Select(a => a.Course).Distinct();
                    btnProcess.IsEnabled = true;
                }
            }
            finally
            {
                EnableUI();
            }
        }

        private void DisableUI()
        {
            foreach (var uiElement in _criticalInputElemnts)
            {
                uiElement.Dispatcher.Invoke(() => uiElement.IsEnabled = false);
            }
        }

        private void EnableUI()
        {
            foreach (var uiElement in _criticalInputElemnts)
            {
                uiElement.Dispatcher.Invoke(() => uiElement.IsEnabled = true);
            }
        }

        private async void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder errorMessage = new StringBuilder();
            if (timeEnd.Value <= timeBegining.Value)
            {
                errorMessage.AppendLine("End time cannot be bigger than beginnig time.");
            }
            if (cboxDays.SelectedItems.Count == 0)
            {
                errorMessage.AppendLine("Please select days.");
            }

            if (errorMessage.Length > 0)
            {
                MessageBox.Show(errorMessage.ToString(), "Invalid input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                DisableUI();
                _builder.Days.Clear();
                foreach (var item in cboxDays.SelectedItems)
                {
                    _builder.Days.Add((DayOfWeek)Enum.Parse(typeof(DayOfWeek), item.ToString(), true));
                }

                lstSchdule.ItemsSource = null;
                lstSchdule.Items.Clear();
                //TODO : Fix me
                //_builder.StartTime = timeBegining.Value.Value;
                //_builder.EndTime = timeEnd.Value.Value;
                //_builder.Hours = timeHours.Value ?? 1;
                _builder.ObligatoryCourses.Clear();
                foreach (var obligatorySubject in lstObligatorySubjects.SelectedItems.OfType<UCourse>())
                {
                    _builder.ObligatoryCourses.Add(obligatorySubject);
                }
                ClearInfo();
                lstSchedules.Items.Clear();
                var schdules = (await _builder.Build()).OrderBy(a => a.Days.Count()).ThenBy(a => a.MaximumBreaksTotal).ThenBy(a => a.LongestDayDuration);
                await lstSchedules.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var schdule in schdules)
                    {
                        lstSchedules.Items.Add(schdule);
                    }
                });
            }
            finally
            {
                EnableUI();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var day in Enum.GetNames(typeof(DayOfWeek)))
            {
                cboxDays.Items.Add(day);
            }

#if DEBUG
            txtDataPath.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "x.csv");
            timeBegining.Value = new TimeSpan(8, 0, 0);
            timeEnd.Value = new TimeSpan(17, 0, 0);
            timeHours.Value = 15;
            cboxDays.SelectedItem = cboxDays.Items[0];
            chkExcludeFullClasses.IsChecked = true;
            cboxDays.SelectedValue = string.Join(",", cboxDays.Items.OfType<string>());
#endif
        }

        private void ClearInfo()
        {
            txtInfoEndTime.Text = "End time: ";
            txtInfoStartTime.Text = "Start time: ";
            txtInfoHours.Text = "Number of hours: ";
            txtInfoMaximumBreaksTotal.Text = "Maximum breaks total: ";
            txtInfoTotalTime.Text = "Longest day period: ";
            txtInfoDays.Text = "Days: ";
        }

        private void lstSchedules_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            //TODO : fix me
            return;
            //USchedule selectedSchdule = lstScheduless.SelectedItem as USchedule;
            //if (ReferenceEquals(selectedSchdule, null))
            //{
            //    return;
            //}
            //ClearInfo();
            //txtInfoDays.Text += DayOfWeekConverter.ToString(selectedSchdule.Days);
            //txtInfoEndTime.Text += selectedSchdule.LastEndTime.ToString();
            //txtInfoStartTime.Text += selectedSchdule.FirstStartTime.ToString();
            //txtInfoTotalTime.Text += selectedSchdule.LongestDayDuration.ToString();
            //txtInfoHours.Text += selectedSchdule.NumberOfHours.ToString();
            //txtInfoMaximumBreaksTotal.Text += selectedSchdule.MaximumBreaksTotal.ToString();
            //lstSchdule.ItemsSource = null;
            //lstSchdule.Items.Clear();

            //lstSchdule.ItemsSource = selectedSchdule.Classes.OrderBy(a => a.Days.Count).ThenBy(a => a.StartTime).ThenBy(a => a.EndTime).Select(a => new UClassListViewItem(a));
        }

        private async void btnTest_Click(object sender, RoutedEventArgs e)
        {
            btnTest.IsEnabled = false;
            UClass[] cls = JsonConvert.DeserializeObject<UClass[]>(File.ReadAllText(@"C:\Users\abdal\Desktop\Courses.json"));
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var user = await RegnewUser.Create("20170112", "C#8sQlC++", false);
            var reg = await user.RegisterClass(cls);
            sw.Stop();
            GC.Collect(3, GCCollectionMode.Forced, true);
            MessageBox.Show(string.Join<UClass>("\r\n", reg), "Registered classes.", MessageBoxButton.OK, MessageBoxImage.Information);
            MessageBox.Show($"It took {sw.ElapsedMilliseconds}ms to register {reg.Length} classes.", "Performance", MessageBoxButton.OK, MessageBoxImage.Information);
            btnTest.IsEnabled = true;
        }
    }
}