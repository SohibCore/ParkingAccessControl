using ParkingApp.Services;
using System.Windows;

namespace ParkingApp
{
    public partial class ActivationWindow : System.Windows.Window
    {
        private readonly LicenseService _license = new();

        public ActivationWindow()
        {
            InitializeComponent();
        }

        private void ActivateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_license.Activate(KeyTextBox.Password))
            {
                MessageBox.Show("Muvaffaqiyatli faollashtirildi!");
                var login = new LoginWindow();
                login.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Noto'g'ri kod. Qayta urinib ko'ring.");
            }
        }
    }
}