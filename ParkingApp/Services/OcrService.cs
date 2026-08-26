using Tesseract;
using OpenCvSharp;

namespace ParkingApp.Services
{
    public class OcrService : IDisposable
    {
        private readonly TesseractEngine _engine;
        public OcrService()
        {
            _engine = new TesseractEngine("tessdata", "eng", EngineMode.Default);
            _engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");
            _engine.DefaultPageSegMode = PageSegMode.SingleLine;
        } 

        public string? ReadText(Mat plateImage)
        {
            using var gray = new Mat();
            Cv2.CvtColor(plateImage, gray, ColorConversionCodes.BGR2GRAY);//kulrang 

            using var resized = new Mat();
            Cv2.Resize(gray, resized, new Size(gray.Width * 3, gray.Height * 3), interpolation: InterpolationFlags.Cubic);//kattalashtiramiz

            using var thresh = new Mat();
            Cv2.Threshold(resized, thresh, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);//Oq/qora

            using var ms = plateImage.ToMemoryStream();
            using var pix = Pix.LoadFromMemory(ms.ToArray());
            using var page = _engine.Process(pix);

            var text = page.GetText()?.Trim();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        public void Dispose() => _engine.Dispose();
    }
}
