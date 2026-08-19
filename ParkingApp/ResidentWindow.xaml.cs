using System.Windows;

namespace ParkingApp
{
    public partial class ResidentWindow : Window
    {
        public ResidentWindow()
        {
            InitializeComponent();
        }
        private void ResidentButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Bu bo'lim hali tayyor emas.");
        }
    }
}
