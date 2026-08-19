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
            _residents = _db.GetAll();
            ResidentsListBox.Items.Clear();

            foreach (var r in _residents)
            {
                ResidentsListBox.Items.Add($"{r.CarNumber} — {r.FullName} ({r.Apartment}-xonadon)");
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
    }
}
