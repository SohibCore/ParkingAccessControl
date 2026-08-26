using System.Windows;
using ParkingApp.DataBase;
using System.Windows.Controls;

namespace ParkingApp
{
    public partial class AccessLogWindow : Window
    {
        private readonly DatabaseService _db = new();
        private List<Resident> _residents = new();
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
            LogsDataGrid.ItemsSource = _db.GetSessions();
        }
        private void LogsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (LogsDataGrid.SelectedItem is AccessLog selected)
            {
                try
                {
                    var result = MessageBox.Show(
                        $"{selected.Apartment} ({selected.CarNumber}) O'chirilsinmi?",
                        "Tasdiqlash",
                        MessageBoxButton.YesNo);

                    if (result == MessageBoxResult.Yes)
                    {
                        _db.DeleteLog(selected.Id);
                        RefreshList();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Xatolik: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Avval ro'yxatdan birini tanlashingiz kerak!");
            }
        }
        private void RefreshList()
        {
            var residents = _db.GetAll();
            LogsDataGrid.ItemsSource = residents;

            foreach (var r in _residents)
            {
                LogsDataGrid.Items.Add($"{r.CarNumber} — {r.FullName} ({r.Apartment}-xonadon)");
            }
        }
    }
}
