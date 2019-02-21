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
        private Timer _untilLoginTimer, _untilAttackTimer;
        private TimeSpan _untilLoginTime, _untilAttackTime;

        private readonly TimeSpan DelayBetweenLoginAndAttack = new TimeSpan(0, 1, 0), UntilLoginTimerPeriod = new TimeSpan(0, 0, 1), UntilAttackTimerPeriod = new TimeSpan(0, 0, 0, 0, 500);
        private RegnewUser _user;

        private ObservableCollection<UClassClassesListModel> _lstClassesModels = new ObservableCollection<UClassClassesListModel>();
        void StopProcess()
        {
            _untilLoginTimer?.Dispose();
            _untilAttackTimer?.Dispose();
            _untilLoginTime = _untilAttackTime = TimeSpan.Zero;
            _user = null;

            Dispatcher.Invoke(() =>
            {
                _lstClassesModels.Clear();
                lblLoginCountdown.Content = "Until login:";
                lblRegisterCountdown.Content = "Until attack:";
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
                _untilLoginTimer?.Dispose();
                _untilAttackTimer?.Dispose();
                _untilLoginTime = _untilAttackTime = TimeSpan.Zero;
                _user = null;
                lblLoginCountdown.Content = "Until login:";
                lblRegisterCountdown.Content = "Until attack:";
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
        async Task LoginAndSetup()
        {
            Dispatcher.Invoke(() => lblLoginCountdown.Content = "Until login: 00:00");
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
                foreach (var cls in schedule.Classes)
                {
                    var classModel = new UClassClassesListModel(cls)
                    {
                        Status = "Waiting",
                        StatusForegroundBrush = Brushes.Black
                    };
                    _lstClassesModels.Add(classModel);
                }
            });
        }
        void UntilLoginTimerCallback(object __X__)
        {
            _untilLoginTime -= UntilLoginTimerPeriod;
            if (_untilLoginTime > TimeSpan.Zero)
            {
                Dispatcher.Invoke(() => lblLoginCountdown.Content = $"Until login: {_untilLoginTime:mm\\:ss}");
            }
            else
            {
                _untilLoginTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                _untilLoginTimer.Dispose();

                LoginAndSetup();
            }
        }
        void Attack()
        {
            Dispatcher.Invoke(() =>
            {
                lblLoginCountdown.Content = "Until login: 00:00";
                lblRegisterCountdown.Content = "Until attack: 00:00";
                btnStopProcess.IsEnabled = false;
            });
            var notificationsSubject = Observer.Create<UClassRegisterationNotification>(UClassRegisterationNotificationHandler, CompleteRegistration);
            _user.RegisterClasses(notificationsSubject, _lstClassesModels.Select(m => m.Source));
        }
        void UntilAttackTimerCallback(object __X__)
        {
            _untilAttackTime -= UntilAttackTimerPeriod;
            if (_untilAttackTime > TimeSpan.Zero)
            {
                Dispatcher.Invoke(() => lblRegisterCountdown.Content = $"Until attack: {_untilAttackTime:mm\\:ss}");
            }
            else
            {
                _untilAttackTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _untilLoginTimer?.Dispose();
                _untilAttackTimer.Dispose();

                Dispatcher.Invoke(() => lblRegisterCountdown.Content = "Until attack: 00:00");
                Attack();
            }
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

            TimeSpan attackTime = tmeStartTime.Value.Value.ToTimeSpan();
            attackTime = new TimeSpan(attackTime.Hours, attackTime.Minutes, 0);
            if (DateTime.Now.ToTimeSpan() >= attackTime || (attackTime - DateTime.Now.ToTimeSpan()) <= DelayBetweenLoginAndAttack)
            {
                if (attackTime > DateTime.Now.ToTimeSpan())
                {
                    var waitTime = attackTime - DateTime.Now.ToTimeSpan();
                    if (waitTime > TimeSpan.Zero) { await Task.Delay(waitTime); }
                }
                await LoginAndSetup();
                Attack();
            }
            else
            {
                _untilAttackTime = attackTime - DateTime.Now.ToTimeSpan() + TimeSpan.FromSeconds(2);
                _untilLoginTime = _untilAttackTime - DelayBetweenLoginAndAttack;
                _untilLoginTimer = new Timer(UntilLoginTimerCallback, null, TimeSpan.Zero, UntilLoginTimerPeriod);
                _untilAttackTimer = new Timer(UntilAttackTimerCallback, null, TimeSpan.Zero, UntilAttackTimerPeriod);
            }
        }
        private void BtnStopProcess_Click(object sender, RoutedEventArgs e)
        {
            StopProcess();
            //MessageBox.Show("Stopped successfully.", "Stopped", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
