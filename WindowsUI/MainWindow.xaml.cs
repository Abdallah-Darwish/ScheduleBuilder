using Newtonsoft.Json;
using ScheduleBuilder.Core;
using ScheduleBuilder.Core.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
namespace WindowsUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) { }

        private void BtnOpenBuilder_Click(object sender, RoutedEventArgs e)
        {
            btnOpenBuilder.IsEnabled = false;
            var bWindow = new ScheduleBuilderWindow();
            bWindow.Closed += (_, __) => btnOpenBuilder.IsEnabled = true;
            bWindow.Show();
        }

        private void BtnOpenRegisterer_Click(object sender, RoutedEventArgs e)
        {
            btnOpenRegisterer.IsEnabled = false;
            var rWindow = new RegisteringWindow();
            rWindow.Closed += (_, __) => btnOpenRegisterer.IsEnabled = true;
            rWindow.Show();
        }

        private void BtnShowAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Regnew client alpha.\r\nBuilt by Abdullah Darweish.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
