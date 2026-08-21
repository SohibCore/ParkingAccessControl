using ParkingApp.DataBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ParkingApp
{
    /// <summary>
    /// Interaction logic for AccessLogWindow.xaml
    /// </summary>
    public partial class AccessLogWindow : Window
    {
        private readonly DatabaseService _db = new();
        public AccessLogWindow()
        {
            InitializeComponent();
            LoadLogs();
        }
        private void LoadLogs()
        {
            var logs = _db.GetAllLogs();
            MessageBox.Show($"Bazadan topilgan loglar soni: {logs.Count}");
            LogsDataGrid.ItemsSource = logs;
        }
    }
}
