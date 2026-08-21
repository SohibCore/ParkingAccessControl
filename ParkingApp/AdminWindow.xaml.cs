using OpenCvSharp;
using System.Windows;
using ParkingApp.DataBase;
using OpenCvSharp.WpfExtensions;
using Point = OpenCvSharp.Point;

namespace ParkingApp
{
    public partial class AdminWindow : System.Windows.Window
    {
        private VideoCapture? _entryCapture;
        private VideoCapture? _exitCapture;
        private string? _lastDetectedPlate;
        private CancellationTokenSource? _entryCts;
        private CancellationTokenSource? _exitCts;
        private readonly DatabaseService _db = new();
        private readonly OcrService _ocrService = new();
        private readonly PlateDetector _plateDetector = new();
        private readonly object _ocrLock = new object();
        private DateTime _lastDetectedTime = DateTime.MinValue;

        public AdminWindow()
        {
            InitializeComponent();
        }
        private void AdminButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new ResidentsWindow(_db);
            window.Owner = this;
            window.ShowDialog();
        }
        private void ShowAccessLogButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new AccessLogWindow();
            window.Owner = this;
            window.ShowDialog();
        }
        private void StartEntryCameraButton_Click(object sender, RoutedEventArgs e)
        {
            _entryCapture = new VideoCapture(0);

            if (!_entryCapture.IsOpened())
            {
                MessageBox.Show("Kirish kamerasi ochilmadi");
                return;
            }

            _entryCts = new CancellationTokenSource();
            var token = _entryCts.Token;

            Task.Run(() =>
            {
                var frame = new Mat();
                while (!token.IsCancellationRequested)
                {
                    _entryCapture.Read(frame);
                    if (frame.Empty()) continue;

                    var rects = _plateDetector.FindPlateRects(frame);

                    foreach (var rect in rects)
                    {
                        using var cropped = new Mat(frame, rect);

                        string? text;
                        lock (_ocrLock)
                        {
                            text = _ocrService.ReadText(cropped);
                        }

                        if (!string.IsNullOrWhiteSpace(text) && text.Length >= 5 && text.Length <= 9)
                        {
                            _lastDetectedPlate = text;
                            _lastDetectedTime = DateTime.Now;

                            var resident = _db.GetByCarNumber(text);

                            var log = new AccessLog
                            {
                                CarNumber = text,
                                Timestamp = DateTime.Now,
                                Granted = resident != null,
                                Apartment = resident?.FullName!,
                                EventType = "IN"
                            };
                            try
                            {
                                _db.LogAccess(log);
                            }
                            catch (Exception ex)
                            {
                                Dispatcher.Invoke(() => MessageBox.Show($"LogAccess xatosi: {ex.Message}"));
                            }
                            Scalar color = resident != null ? Scalar.LimeGreen : Scalar.Red;
                            Cv2.Rectangle(frame, rect, Scalar.LimeGreen, 2);
                            Cv2.PutText(frame, text, new Point(rect.X, rect.Y - 10),
                                HersheyFonts.HersheySimplex, 0.8, Scalar.LimeGreen, 2);

                            Dispatcher.Invoke(() =>
                            {
                                if (resident != null)
                                {
                                    StatusTextBlock.Text = $"✅ RUXSAT: {resident.FullName} ({resident.Apartment}-xonadon)";
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
                        EntryCameraView.Source = bitmap;

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
        private void StartExitCameraButton_Click(object sender, RoutedEventArgs e)
        {
            _exitCapture = new VideoCapture(1);

            if (!_exitCapture.IsOpened())
            {
                MessageBox.Show("Chiqish kamerasi ochilmadi");
                return;
            }

            _exitCts = new CancellationTokenSource();
            var token = _exitCts.Token;

            Task.Run(() =>
            {
                var frame = new Mat();
                while (!token.IsCancellationRequested)
                {
                    _exitCapture.Read(frame);
                    if (frame.Empty()) continue;

                    var rects = _plateDetector.FindPlateRects(frame);

                    foreach (var rect in rects)
                    {
                        using var cropped = new Mat(frame, rect);
                        string? text;
                        lock (_ocrLock)
                        {
                            text = _ocrService.ReadText(cropped);
                        }
                        if (!string.IsNullOrWhiteSpace(text) && text.Length >= 5 && text.Length <= 9)
                        {
                            _lastDetectedPlate = text;
                            _lastDetectedTime = DateTime.Now;

                            var resident = _db.GetByCarNumber(text);

                            var log = new AccessLog
                            {
                                CarNumber = text,
                                Timestamp = DateTime.Now,
                                Granted = resident != null,
                                Apartment = resident?.Apartment,
                                EventType = "OUT"
                            };
                            _db.LogAccess(log);
                            Scalar color = resident != null ? Scalar.LimeGreen : Scalar.Red;
                            Cv2.Rectangle(frame, rect, Scalar.LimeGreen, 2);
                            Cv2.PutText(frame, text, new Point(rect.X, rect.Y - 10),
                                HersheyFonts.HersheySimplex, 0.8, Scalar.LimeGreen, 2);

                            Dispatcher.Invoke(() =>
                            {
                                if (resident != null)
                                {
                                    StatusTextBlock.Text = $"✅ RUXSAT: {resident.FullName} ({resident.Apartment} - xonadon)";
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
                        ExitCameraView.Source = bitmap;

                        if (_lastDetectedPlate != null && (DateTime.Now - _lastDetectedTime).TotalSeconds < 3)
                        {
                            PlateTextBlock.Text = $"Aniqlangan: {_lastDetectedPlate}";
                        }
                    });
                }
            }, token);
        }
        private void StopCameraButton_Click(object sender, RoutedEventArgs e)
        {
            _entryCts?.Cancel();
            _entryCapture?.Release();
            _entryCapture?.Dispose();
            _ocrService.Dispose();
            base.OnClosed(e);
            _entryCapture = null;

            EntryCameraView.Source = null;
            ExitCameraView.Source = null;

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