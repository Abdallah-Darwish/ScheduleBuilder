using ScheduleBuilder.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
namespace WindowsUI
{
    public partial class RegisteringWindow
    {
        private Timer _regestrationStatusTimer;

        private RegnewUser _user;

        private ObservableCollection<UClassClassesListModel> _lstClassesModels = new ObservableCollection<UClassClassesListModel>();
        void StopProcess()
        {
            _regestrationStatusTimer?.Dispose();
            //just to ensure that the timer has stopped
            Thread.Sleep(200);
            _user = null;

            Dispatcher.Invoke(() =>
            {
                _lstClassesModels.Clear();
                lblRegestrationStatus.Content = "Unknown";
                lblRegestrationStatus.Foreground = Brushes.Orange;
                tabInfo.IsEnabled = true;
                btnStartProcess.IsEnabled = true;
                btnStopProcess.IsEnabled = true;
                MessageBox.Show("Regestration process is aborted.", "Aborted", MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }
        void CompleteRegistration()
        {
            Dispatcher.Invoke(() =>
            {
                _regestrationStatusTimer?.Dispose();
                _user = null;
                lblRegestrationStatus.Content = "Unknown";
                lblRegestrationStatus.Foreground = Brushes.Orange;
                tabInfo.IsEnabled = true;
                btnStartProcess.IsEnabled = true;
                btnStopProcess.IsEnabled = true;
                MessageBox.Show("Registration is done.\r\nPlease open regnew and confirm your schedule.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
        void UClassRegisterationNotificationHandler(UClassRegisterationNotification notification)
        {
            Brush foregroundBrush = default;
            string status = default;
            switch (notification.Type)
            {
                case UClassRegisterationNotificationType.Processing:
                    status = "Processing";
                    foregroundBrush = Brushes.Blue;
                    break;
                case UClassRegisterationNotificationType.Succeeded:
                    status = "Registered";
                    foregroundBrush = Brushes.Green;
                    break;
                case UClassRegisterationNotificationType.SucceededAfterConfirmation:
                    status = "Registered with confirmation";
                    foregroundBrush = Brushes.Green;
                    break;
                case UClassRegisterationNotificationType.Failed:
                    status = "FAILED";
                    foregroundBrush = Brushes.Red;
                    break;
                case UClassRegisterationNotificationType.RequiredConfirmation:
                    status = "Required confirmation";
                    foregroundBrush = Brushes.Yellow;
                    break;
                case UClassRegisterationNotificationType.ClassIsFullOrDoesntExist:
                    status = "Class is full or doesn't exist";
                    foregroundBrush = Brushes.Red;
                    break;
            }
            Dispatcher.Invoke(() =>
            {
                var classModel = _lstClassesModels.First(c => c.Id == notification.Class.Id);
                classModel.Status = status;
                classModel.StatusForegroundBrush = foregroundBrush;
            });
        }
        async Task StartChecking()
        {
            Dispatcher.Invoke(() =>
            {
                lblRegestrationStatus.Content = "Unknown";
                lblRegestrationStatus.Foreground = Brushes.Orange;
            });
            _user = await RegnewUser.Login(Dispatcher.Invoke(() => txtUserName.Text), Dispatcher.Invoke(() => txtPassword.Password), false);
            if (_user is null)
            {
                Dispatcher.Invoke(() => MessageBox.Show("CANT LOGIN.", "FATAL ERROR", MessageBoxButton.OK, MessageBoxImage.Error));
                StopProcess();
            }

            await Dispatcher.Invoke(async () =>
            {
                _lstClassesModels.Clear();
                USchedule schedule = await USchedule.FromFile(txtSchedulePath.Text);
                foreach (var cls in schedule.Classes.OrderByDescending(c => c.Capacity - c.NumberOfRegisteredStudents))
                {
                    var classModel = new UClassClassesListModel(cls)
                    {
                        Status = "Waiting",
                        StatusForegroundBrush = Brushes.Black
                    };
                    _lstClassesModels.Add(classModel);
                }
            });
            _regestrationStatusTimer = new Timer(RegestrationStatusTimerCallback, null, TimeSpan.Zero, tmeDelay.Value.Value);
        }
        void RegestrationStatusTimerCallback(object __X__)
        {
            var checkingTask = _user.CanRegisterClasses();
            checkingTask.Wait();
            if (checkingTask.Result)
            {
                try
                {
                    _regestrationStatusTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                    _regestrationStatusTimer?.Dispose();
                    _regestrationStatusTimer = null;
                }
                catch { }
                Attack();
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    lblRegestrationStatus.Content = "Closed";
                    lblRegestrationStatus.Foreground = Brushes.Red;
                });
            }
        }
        void Attack()
        {
            Dispatcher.Invoke(() =>
            {
                lblRegestrationStatus.Content = "Open";
                lblRegestrationStatus.Foreground = Brushes.Green;
                btnStopProcess.IsEnabled = false;
            });
            var notificationsSubject = Observer.Create<UClassRegisterationNotification>(UClassRegisterationNotificationHandler, CompleteRegistration);
            _user.RegisterClasses(notificationsSubject, _lstClassesModels.Select(m => m.Source));
        }
        void InitializeRegisteringTab()
        {
            lstClasses.ItemsSource = _lstClassesModels;
        }

        private async void BtnStartProcess_Click(object sender, RoutedEventArgs e)
        {
            if (await VerfiyInfo() == false) { return; }

            tabInfo.IsEnabled = false;
            btnStartProcess.IsEnabled = false;
            btnStopProcess.IsEnabled = true;

            await StartChecking();
        }
        private void BtnStopProcess_Click(object sender, RoutedEventArgs e)
        {
            StopProcess();
            //MessageBox.Show("Stopped successfully.", "Stopped", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
