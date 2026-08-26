using ParkingApp.DataBase;
using System.Windows;
using System.Windows.Controls;

namespace ParkingApp
{
    public partial class ResidentsWindow : Window
    {
        private readonly DatabaseService _db;
        private List<Resident> _residents = new();
        private int? _editingResidentId = null;
        private List<Resident> _allResidents = new();
        public ResidentsWindow(DatabaseService db)
        {
            InitializeComponent();
            _db = db;
            RefreshList();
            _allResidents = _db.GetAll();
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
                if (_editingResidentId.HasValue)
                {
                    _db.Update(_editingResidentId.Value, FullNameBox.Text.Trim(), ApartmentBox.Text.Trim(), PlateBox.Text.Trim());
                    _editingResidentId = null;
                    AddButton.Content = "➕ Qo'shish";
                }
                else
                {
                    _db.Add(new Resident
                    {
                        FullName = FullNameBox.Text.Trim(),
                        Apartment = ApartmentBox.Text.Trim(),
                        CarNumber = PlateBox.Text.Trim()
                    });
                }

                FullNameBox.Clear();
                ApartmentBox.Clear();
                PlateBox.Clear();
                RefreshList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Xatolik: {ex.Message}");
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
        private void ResidentsDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }
        private void FullNameBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }
        private void UpdateResidents_Click(object sender, RoutedEventArgs e)
        {
            if (ResidentsDataGrid.SelectedItem is Resident selected)
            {
                FullNameBox.Text = selected.FullName;
                ApartmentBox.Text = selected.Apartment;
                PlateBox.Text = selected.CarNumber;

                _editingResidentId = selected.Id;
                AddButton.Content = "💾 Saqlash";
            }
            else
            {
                MessageBox.Show("Avval ro'yxatlardan birini tanlashingiz kerak !");
            }
        }
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchBox.Text.Trim().ToUpper();

            if (string.IsNullOrWhiteSpace(query))
            {
                ResidentsDataGrid.ItemsSource = _allResidents;
            }
            else
            {
                var filtered = _allResidents.Where(r => r.CarNumber.ToUpper().Contains(query) || r.Apartment.ToUpper().Contains(query)).ToList();

                ResidentsDataGrid.ItemsSource = filtered;
            }
        }
    }
}
