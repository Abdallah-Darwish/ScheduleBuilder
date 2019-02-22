using Microsoft.Win32;
using ScheduleBuilder.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
namespace WindowsUI
{
    public partial class RegisteringWindow
    {
        void InitializeInfoTab()
        {
#if DEBUG
            txtUserName.Text = "20170112";
            txtPassword.Password = "C#8sQlC++";
#endif
        }
        private async void BtnTestAccount_Click(object sender, RoutedEventArgs e)
        {
            txtUserName.IsEnabled = false;
            txtPassword.IsEnabled = false;
            try
            {
                if (string.IsNullOrWhiteSpace(txtUserName.Text))
                {
                    MessageBox.Show("Please enter your username.", "Missing Data", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtPassword.Password))
                {
                    MessageBox.Show("Please enter your password.", "Missing Data", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var testUser = await RegnewUser.Login(txtUserName.Text, txtPassword.Password, false);
                if (testUser is null)
                {
                    MessageBox.Show("Invalid username or password.\r\nIn case you are sure about your username and password please login from the browser then try again.", "Can't login", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show($"Logged in successfully as {testUser.Name}.", "Logged in successfully", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            finally
            {
                txtUserName.IsEnabled = true;
                txtPassword.IsEnabled = true;
            }
        }
        private void BtnBrowseSchedule_Click(object sender, RoutedEventArgs e)
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
            txtSchedulePath.Text = ofd.FileName;
        }
        async Task<bool> VerfiyInfo()
        {
            //verify that all fields are entered
            if (string.IsNullOrWhiteSpace(txtUserName.Text))
            {
                MessageBox.Show("Please enter your username.", "Missing Data", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MessageBox.Show("Please enter your password.", "Missing Data", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtSchedulePath.Text))
            {
                MessageBox.Show("Please enter your schedule path.", "Missing Data", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if(tmeDelay.Value == null)
            {
                MessageBox.Show("Please the delay between checks.", "Missing Data", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            //login
            var testUser = await RegnewUser.Login(txtUserName.Text, txtPassword.Password, false);
            if (testUser is null)
            {
                MessageBox.Show("Invalid username or password.\r\nIn case you are sure about your username and password please login from the browser then try again.", "Can't login", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            //verify the existinse and validity of the schedule
            try
            {
                USchedule testSchedule = await USchedule.FromFile(txtSchedulePath.Text);
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("Schedule file doesn't exist.", "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception)
            {
                MessageBox.Show("Corrupt Schedule file.", "Corrupt File", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            //verify that the delay is not more than 5000ms
            if(tmeDelay.Value.Value.TotalMilliseconds > 5000 &&
                MessageBox.Show("The delay between the checks is too large.\r\nDo you want to proceed ?",
                "Propably Invalid Data", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            {
                return false;
            }
            return true;
        }
    }
}
