using OpenCvSharp;
using System.Windows;
using OpenCvSharp.WpfExtensions;
using Point = OpenCvSharp.Point;

namespace ParkingApp
{
    public partial class MainWindow : System.Windows.Window
    {
        private VideoCapture? _capture;
        private string? _lastDetectedPlate;
        private CancellationTokenSource? _cts;
        private readonly DatabaseService _db = new();
        private readonly OcrService _ocrService = new();
        private readonly PlateDetector _plateDetector = new();
        private DateTime _lastDetectedTime = DateTime.MinValue;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                _db.Add("Sardor Aliyev", "24", "01A123BC");
            }
            catch { }
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
                        using var cropped = new Mat(frame, rect);
                        var text = _ocrService.ReadText(cropped);

                        if (!string.IsNullOrWhiteSpace(text) && text.Length >= 5 && text.Length <= 9)
                        {
                            _lastDetectedPlate = text;
                            _lastDetectedTime = DateTime.Now;

                            var resident = _db.GetByCarNumber(text);

                            Scalar color = resident != null ? Scalar.LimeGreen : Scalar.Red;
                            Cv2.Rectangle(frame, rect, Scalar.LimeGreen, 2);
                            Cv2.PutText(frame, text, new Point(rect.X, rect.Y - 10),
                                HersheyFonts.HersheySimplex, 0.8, Scalar.LimeGreen, 2);

                            Dispatcher.Invoke(() =>
                            {
                                if (resident != null)
                                {
                                    StatusTextBlock.Text = $"✅ RUXSAT: {resident.Value.FullName} ({resident.Value.Apartment}-xonadon)";
                                }
                                else
                                {
                                    StatusTextBlock.Text = $"⛔ RAD ETILDI: {text} ro'yxatda yo'q";
                                }
                            });
                        }
                    }

                    var bitmap = frame.ToBitmapSource();
                    bitmap.Freeze();

                    Dispatcher.Invoke(() =>
                    {
                        CameraView.Source = bitmap;

                        if (_lastDetectedPlate != null && (DateTime.Now - _lastDetectedTime).TotalSeconds < 3)
                        {
                            PlateTextBlock.Text = $"Aniqlangan: {_lastDetectedPlate}";
                        }
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
            _ocrService.Dispose();
            base.OnClosed(e);
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