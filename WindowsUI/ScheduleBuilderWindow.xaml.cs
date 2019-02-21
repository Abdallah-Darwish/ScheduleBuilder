using ScheduleBuilder.Core;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace WindowsUI
{
    /// <summary>
    /// Interaction logic for ScheduleBuilder.xaml
    /// </summary>
    public partial class ScheduleBuilderWindow : Window
    {
        List<UClass> _classes = new List<UClass>();
        Dictionary<int, UClassListModel> _classesModels = new Dictionary<int, UClassListModel>();
        ObservableCollection<UClassListModel> _searchListItems = new ObservableCollection<UClassListModel>();
        public ScheduleBuilderWindow()
        {
            InitializeComponent();
            InitializeDataSourceTab();
            InitializeClassesTab();
            InitializeConstraintsTab();
            InitializeSchedulesTab();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
         
        }

       
    }
}
