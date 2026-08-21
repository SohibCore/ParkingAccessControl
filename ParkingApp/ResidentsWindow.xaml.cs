using ParkingApp.DataBase;
using System.Windows;

namespace ParkingApp
{
    public partial class ResidentsWindow : Window
    {
        private readonly DatabaseService _db;
        private List<Resident> _residents = new();
        public ResidentsWindow(DatabaseService db)
        {
            InitializeComponent();
            _db = db;
            RefreshList();
        }
        private void RefreshList()
        {
            var residents = _db.GetAll();
            ResidentsDataGrid.ItemsSource = residents;

            foreach (var r in _residents)
            {
                ResidentsDataGrid.Items.Add($"{r.CarNumber} — {r.FullName} ({r.Apartment}-xonadon)");
            }
        }
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FullNameBox.Text) || string.IsNullOrWhiteSpace(ApartmentBox.Text))
            {
                MessageBox.Show("F.I.Sh va mashina raqamini kiriting.");
                return;
            }
            try
            {
                var newResident = new Resident
                {
                    FullName = FullNameBox.Text.Trim(),
                    Apartment = ApartmentBox.Text.Trim(),
                    CarNumber = PlateBox.Text.Trim(),
                };

                _db.Add(newResident);
                FullNameBox.Clear();
                ApartmentBox.Clear();
                PlateBox.Clear();
                RefreshList();
            }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                MessageBox.Show("Bu mashina raqami allaqachon ro'yxatda bor.");
            }
        }
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (ResidentsDataGrid.SelectedItem is Resident selected)
            {
                var result = MessageBox.Show(
                    $"{selected.FullName} ({selected.CarNumber}) O'chirilsinmi ?", "Tasdiqlash :", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    _db.Delete(selected.Id);
                    RefreshList();
                }
            }
            else
            {
                MessageBox.Show("Avval ro'yxatlardan birini tanlashingiz kerak !");
            }
        }
    }
}
