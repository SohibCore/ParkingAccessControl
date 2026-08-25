using System.Windows;

namespace ParkingApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var license = new LicenseService();
            if (license.IsActivated())
            {
                var login = new LoginWindow();
                login.Show();
            }
            else
            {
                var activation = new ActivationWindow();
                activation.Show();
            }
        }
    }

}
