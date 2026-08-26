using System.IO;
using OpenCvSharp;
using System.Windows;
using System.IO.Ports;
using ParkingApp.DataBase;
using OpenCvSharp.WpfExtensions;
using Point = OpenCvSharp.Point;
using ParkingApp.Services;

namespace ParkingApp
{
    public partial class AdminWindow : System.Windows.Window
    {
        private SerialPort? _arduinoPort;
        private VideoCapture? _exitCapture;
        private string? _lastDetectedPlate;
        private VideoCapture? _entryCapture;
        private CancellationTokenSource? _exitCts;
        private CancellationTokenSource? _entryCts;
        private readonly DatabaseService _db = new();
        private readonly OcrService _ocrService = new();
        private readonly object _ocrLock = new object();
        private DateTime _lastDeniedTime = DateTime.MinValue;
        private readonly PlateDetector _plateDetector = new();
        private DateTime _lastGrantedTime = DateTime.MinValue;
        private DateTime _lastDetectedTime = DateTime.MinValue;
        private DateTime _lastExitGrantedTime = DateTime.MinValue;

        public AdminWindow()
        {
            InitializeComponent();
            UpdateStatistics();
            //ConnectArduino();
        }

        //Shlagbaunni ko'tarish
        private void OpenBarrier()
        {
            if (_arduinoPort != null && _arduinoPort.IsOpen)
            {
                _arduinoPort.Write("O");
            }
        }
        private void ConnectArduino()
        {
            string portName = File.ReadAllText("settings.txt").Trim();
            _arduinoPort = new SerialPort(portName, 9600);
            _arduinoPort.Open();
        }

        //Admin pagega o'tish
        private void AdminButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new ResidentsWindow(_db);
            window.Owner = this;
            window.ShowDialog();
        }

        //Kimligini tasdiqlash
        private void ShowAccessLogButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new AccessLogWindow();
            window.Owner = this;
            window.ShowDialog();
        }

        // Kameralarni boshqarish nechta bo'lishidan qatiy nazar
        private void RunCameraLoop(VideoCapture capture, CancellationToken token, System.Windows.Controls.Image cameraView, string eventType, Func<DateTime> getLastGrantedTime, Action<DateTime> setLastGrantedTime)
        {
            Task.Run(() =>
            {
                var frame = new Mat();
                while (!token.IsCancellationRequested)
                {
                    capture.Read(frame);
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

                        if (string.IsNullOrWhiteSpace(text)) continue;

                        bool hasDigit = text.Any(char.IsDigit);
                        bool hasLetter = text.Any(char.IsLetter);

                        if (text.Length >= 5 && text.Length <= 9 && hasDigit && hasLetter)
                        {
                            var resident = _db.GetByCarNumber(text);

                            var log = new AccessLog
                            {
                                CarNumber = text,
                                Timestamp = DateTime.Now,
                                Granted = resident != null,
                                Apartment = resident?.FullName!,
                                EventType = eventType
                            };

                            if (resident != null)
                            {
                                bool tooSoon = (DateTime.Now - getLastGrantedTime()).TotalSeconds < 5;
                                if (!tooSoon)
                                {
                                    try
                                    {
                                        _db.LogAccess(log);
                                    }
                                    catch (Exception ex)
                                    {
                                        Dispatcher.Invoke(() => MessageBox.Show($"LogAccess xatosi: {ex.Message}"));
                                    }
                                    setLastGrantedTime(DateTime.Now);

                                    if (eventType == "IN")
                                    {
                                        OpenBarrier();
                                    }
                                }
                            }

                            _lastDetectedPlate = text;
                            _lastDetectedTime = DateTime.Now;

                            Scalar color = resident != null ? Scalar.LimeGreen : Scalar.Red;
                            Cv2.Rectangle(frame, rect, color, 2);
                            Cv2.PutText(frame, text, new Point(rect.X, rect.Y - 10),
                                HersheyFonts.HersheySimplex, 0.8, color, 2);

                            Dispatcher.Invoke(() =>
                            {
                                UpdateStatistics();
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
                        cameraView.Source = bitmap;

                        if (_lastDetectedPlate != null && (DateTime.Now - _lastDetectedTime).TotalSeconds < 3)
                        {
                            PlateTextBlock.Text = $"Aniqlangan: {_lastDetectedPlate}";
                        }
                    });
                }
            }, token);
        }

        //Kirish kamera
        private void StartEntryCameraButton_Click(object sender, RoutedEventArgs e)
        {
            _entryCapture = new VideoCapture(1);

            if (!_entryCapture.IsOpened())
            {
                MessageBox.Show("Kirish kamerasi ochilmadi");
                return;
            }

            _entryCts = new CancellationTokenSource();

            RunCameraLoop(
                _entryCapture,
                _entryCts.Token,
                EntryCameraView,
                "IN",
                () => _lastGrantedTime,
                (value) => _lastGrantedTime = value
            );

            StartCameraButton.IsEnabled = false;
            StopCameraButton.IsEnabled = true;
        }

        //Chiqish kamera
        private void StartExitCameraButton_Click(object sender, RoutedEventArgs e)
        {
            _exitCapture = new VideoCapture(0);

            if (!_exitCapture.IsOpened())
            {
                MessageBox.Show("Chiqish kamerasi ochilmadi");
                return;
            }

            _exitCts = new CancellationTokenSource();

            RunCameraLoop(
                _exitCapture,
                _exitCts.Token,
                ExitCameraView,
                "OUT",
                () => _lastExitGrantedTime,
                (value) => _lastExitGrantedTime = value
            );
        }

        //Kirish kamerani tuxtatish
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

        //Statistikaga yangilanish berish 
        private void UpdateStatistics()
        {
            TodayEntryText.Text = _db.GetTodayEntryCount().ToString();
            TodayExitText.Text = _db.GetTodayExitCount().ToString();
            TotalResidentsText.Text = _db.GetTotalResidentsCount().ToString();
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
        public List<OpenCvSharp.Rect> FindPlateRects(Mat frame) //Raqamga o'xshash hududlarni topish
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

/* private void StartEntryCameraButton_Click(object sender, RoutedEventArgs e)
 {
     _entryCapture = new VideoCapture(1);

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

                 if (string.IsNullOrWhiteSpace(text)) continue;

                 bool hasDigit = text.Any(char.IsDigit);
                 bool hasLetter = text.Any(char.IsLetter);

                 if (text.Length >= 5 && text.Length <= 9 && hasDigit && hasLetter)
                 {
                     var resident = _db.GetByCarNumber(text);

                     var log = new AccessLog
                     {
                         CarNumber = text,
                         Timestamp = DateTime.Now,
                         Granted = resident != null,
                         Apartment = resident?.FullName!,
                         EventType = "IN"
                     };

                     if (resident != null)
                     {
                         bool tooSoon = (DateTime.Now - _lastGrantedTime).TotalSeconds < 5;
                         if (!tooSoon)
                         {
                             try
                             {
                                 _db.LogAccess(log);
                             }
                             catch (Exception ex)
                             {
                                 Dispatcher.Invoke(() => MessageBox.Show($"LogAccess xatosi: {ex.Message}"));
                             }
                             _lastGrantedTime = DateTime.Now;
                             //OpenBarrier();
                         }
                     }

                     _lastDetectedPlate = text;
                     _lastDetectedTime = DateTime.Now;

                     Scalar color = resident != null ? Scalar.LimeGreen : Scalar.Red;
                     Cv2.Rectangle(frame, rect, color, 2);
                     Cv2.PutText(frame, text, new Point(rect.X, rect.Y - 10),
                         HersheyFonts.HersheySimplex, 0.8, color, 2);

                     Dispatcher.Invoke(() =>
                     {
                         UpdateStatistics();
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
 }*/
/* private void StartExitCameraButton_Click(object sender, RoutedEventArgs e)
 {
     _exitCapture = new VideoCapture(0);

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

                 if (string.IsNullOrWhiteSpace(text)) continue;

                 bool hasDigit = text.Any(char.IsDigit);
                 bool hasLetter = text.Any(char.IsLetter);

                 if (text.Length >= 5 && text.Length <= 9 && hasDigit && hasLetter)
                 {
                     var resident = _db.GetByCarNumber(text);

                     var log = new AccessLog
                     {
                         CarNumber = text,
                         Timestamp = DateTime.Now,
                         Granted = resident != null,
                         Apartment = resident?.FullName!,
                         EventType = "OUT"
                     };

                     // Faqat RUXSAT berilgan holatlar loglanadi
                     if (resident != null)
                     {
                         bool tooSoon = (DateTime.Now - _lastExitGrantedTime).TotalSeconds < 5;
                         if (!tooSoon)
                         {
                             try
                             {
                                 _db.LogAccess(log);
                             }
                             catch (Exception ex)
                             {
                                 Dispatcher.Invoke(() => MessageBox.Show($"LogAccess xatosi: {ex.Message}"));
                             }
                             _lastExitGrantedTime = DateTime.Now;
                         }
                     }

                     _lastDetectedPlate = text;
                     _lastDetectedTime = DateTime.Now;

                     Scalar color = resident != null ? Scalar.LimeGreen : Scalar.Red;
                     Cv2.Rectangle(frame, rect, color, 2);
                     Cv2.PutText(frame, text, new Point(rect.X, rect.Y - 10),
                         HersheyFonts.HersheySimplex, 0.8, color, 2);

                     Dispatcher.Invoke(() =>
                     {
                         UpdateStatistics();
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
                 ExitCameraView.Source = bitmap;

                 if (_lastDetectedPlate != null && (DateTime.Now - _lastDetectedTime).TotalSeconds < 3)
                 {
                     PlateTextBlock.Text = $"Aniqlangan: {_lastDetectedPlate}";
                 }
             });
         }
     }, token);
 }*/