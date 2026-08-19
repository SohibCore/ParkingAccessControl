using OpenCvSharp;
using System.Windows;
using OpenCvSharp.WpfExtensions;
using Point = OpenCvSharp.Point;

namespace ParkingApp
{
    public partial class MainWindow : System.Windows.Window
    {
        private VideoCapture? _capture;
        private CancellationTokenSource? _cts;
        private readonly PlateDetector _plateDetector = new();
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

                    var rects = _plateDetector.FindPlateRects(frame);

                    foreach (var rect in rects)
                    {
                        Cv2.Rectangle(frame, rect, Scalar.LimeGreen, 2);
                    }

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
            _capture = null;

            CameraView.Source = null;

            StartCameraButton.IsEnabled = true;
            StopCameraButton.IsEnabled = false;
        }
    }
    public class PlateDetector
    {
        public List<Mat> FindPlateCandidates(Mat frame)
        {
            var candidates = new List<Mat>();

            Mat gray = new Mat();
            Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

            Mat filtered = new Mat();
            Cv2.BilateralFilter(gray, filtered, 11, 17, 17);

            Mat edged = new Mat();
            Cv2.Canny(filtered, edged, 30, 200);

            Cv2.FindContours(edged, out Point[][] contours, out HierarchyIndex[] hierarchy,
                RetrievalModes.List, ContourApproximationModes.ApproxSimple);

            foreach (var contour in contours)
            {
                OpenCvSharp.Rect rect = Cv2.BoundingRect(contour);
                double aspectRatio = (double)rect.Width / rect.Height;

                if (aspectRatio > 2.0 && aspectRatio < 5.5 && rect.Width > 80)
                {
                    Mat cropped = new Mat(frame, rect); 
                    candidates.Add(cropped.Clone());
                }
            }
            return candidates;
        }
        public List<OpenCvSharp.Rect> FindPlateRects(Mat frame) //vizual tekshirish
        {
            var rects = new List<OpenCvSharp.Rect>();

            Mat gray = new Mat();
            Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);

            Mat filtered = new Mat();
            Cv2.BilateralFilter(gray, filtered, 11, 17, 17);

            Mat edged = new Mat();
            Cv2.Canny(filtered, edged, 30, 200);

            Cv2.FindContours(edged, out Point[][] contours, out HierarchyIndex[] hierarchy,
                RetrievalModes.List, ContourApproximationModes.ApproxSimple);

            foreach (var contour in contours)
            {
                OpenCvSharp.Rect rect = Cv2.BoundingRect(contour);
                double aspectRatio = (double)rect.Width / rect.Height;

                if (aspectRatio > 2.0 && aspectRatio < 5.5 && rect.Width > 80)
                {
                    rects.Add(rect);
                }
            }

            return rects;
        }
    }
}