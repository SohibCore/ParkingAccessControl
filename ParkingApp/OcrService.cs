using OpenCvSharp;
using Tesseract;

namespace ParkingApp
{
    public class OcrService : IDisposable
    {
        private readonly TesseractEngine _engine;
        public OcrService()
        {
            _engine = new TesseractEngine("tessdata", "eng", EngineMode.Default);
            _engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");
        }

        public string? ReadText(Mat plateImage)
        {
            using var ms = plateImage.ToMemoryStream();
            using var pix = Pix.LoadFromMemory(ms.ToArray());
            using var page = _engine.Process(pix);

            var text = page.GetText()?.Trim();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        public void Dispose() => _engine.Dispose();
    }
}
