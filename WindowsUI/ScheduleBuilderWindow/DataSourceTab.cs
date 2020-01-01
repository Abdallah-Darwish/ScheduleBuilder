using Microsoft.Win32;
using Newtonsoft.Json;
using ScheduleBuilder.Core;
using ScheduleBuilder.Core.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace WindowsUI
{
    public partial class ScheduleBuilderWindow
    {
        void InitializeDataSourceTab()
        {
            cbxSemester.Items.Add(USemester.First);
            cbxSemester.Items.Add(USemester.Second);
            cbxSemester.Items.Add(USemester.Summer);
            cbxSemester.SelectedIndex = 0;

#if DEBUG
            txtDataSource.Text = @"C:\Users\abdal\Desktop\Classes19S1.json";
            btnSourceFile.IsChecked = true;
            btnReloadData.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
#endif
        }
        private void BtnBrowseDataFile_Click(object sender, RoutedEventArgs e)
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
            var result = ofd.ShowDialog();
            if (result == false) return;
            txtDataSource.Text = ofd.FileName;
        }
        private async void BtnReloadData_Click(object sender, RoutedEventArgs e)
        {
            btnReloadData.IsEnabled = false;
            prgrsReloadData.IsIndeterminate = true;
            if (btnSourceFile.IsChecked == true)
            {
                string dataSourcePath = txtDataSource.Text;
                if (File.Exists(dataSourcePath) == false)
                {
                    MessageBox.Show($"File {dataSourcePath} doesn't exist.", "Non-Exsiting file.", MessageBoxButton.OK, MessageBoxImage.Error);
                    btnReloadData.IsEnabled = true;
                    prgrsReloadData.IsIndeterminate = false;
                    return;
                }
                _classes.Clear();
                _classes = new List<UClass>(await UClass.FromFile(dataSourcePath));
            }
            else
            {
                _classes = new List<UClass>((await ProposedCoursesParser.GetClasses((USemester)cbxSemester.SelectedItem, udYear.Value)).OrderBy(c => c.Course.Id));
            }
            lstClasses.Dispatcher.Invoke(() =>
            {
                ReloadClassesModelsAndUpdateClassesList();
                btnReloadData.IsEnabled = true;
                prgrsReloadData.IsIndeterminate = false;
                GC.Collect(3, GCCollectionMode.Forced, true, true);
            });
            
           
        }
        private async void BtnSaveData_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog
            {
                Filter = "JSON File (*.json)|*.json",
                AddExtension = true,
                CheckFileExists = false,
                DefaultExt = ".json",
                ValidateNames = true,
                Title = "Classes file.",
                DereferenceLinks = true
            };
            if (sfd.ShowDialog() == false) { return; }
            string filePath = sfd.FileName;
            await UClass.Save(_classes, filePath);
        }
    }
}
