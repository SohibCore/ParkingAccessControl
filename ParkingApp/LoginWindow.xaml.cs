using System.Windows;

namespace ParkingApp
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void AdminButton_Click(object sender, RoutedEventArgs e)
        {
            // Role 
            RolePanel.Visibility = Visibility.Collapsed;

            // Parol 
            PasswordPanel.Visibility = Visibility.Visible;
        }

        private void ConfirmAdminLogin_Click(object sender, RoutedEventArgs e)
        {
            string password = AdminPasswordBox.Password;

            if (password == "admin1234")
            {
                var adminWindow = new AdminWindow();
                adminWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Parol noto'g'ri!");
            }
        }

        private void ResidentButton_Click(object sender, RoutedEventArgs e)
        {
            var residentWindow = new ResidentWindow();
            residentWindow.Show();
            this.Close();
        }
    }
}