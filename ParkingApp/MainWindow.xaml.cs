using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Windows;

namespace ParkingApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private VideoCapture? _capture;
        private CancellationTokenSource? _cts;
        public MainWindow()
        {
            InitializeComponent();
        }
        private void StartCameraButton_Click(object sender, RoutedEventArgs e)
        {
            _capture = new VideoCapture(0);

            if (!_capture.IsOpened())
            {
                MessageBox.Show("Kamera ochilmadi");
                return;
            }

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            Task.Run(() =>
            {
                var frame = new Mat();
                while (!token.IsCancellationRequested)
                {
                    _capture.Read(frame);
                    if (frame.Empty()) continue;

                    var bitmap = frame.ToBitmapSource();
                    bitmap.Freeze();
                        
                    Dispatcher.Invoke(() =>
                    {
                        CameraView.Source = bitmap;
                    });
                }
            }, token);

            StartCameraButton.IsEnabled = false;
            StopCameraButton.IsEnabled = true;
        }
        private void StopCameraButton_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            _capture?.Release();
            _capture?.Dispose();
            base.OnClosed(e);
            _capture = null;

            CameraView.Source = null;

            StartCameraButton.IsEnabled = true;
            StopCameraButton.IsEnabled = false;
        }
    }
}