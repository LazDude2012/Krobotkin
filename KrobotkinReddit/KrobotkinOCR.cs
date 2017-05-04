using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Tesseract;

namespace KrobotkinReddit {
    class OCRResponse {
        public string Text;
        public float Confidence;
    }
    class KrobotkinOCR {
        private TesseractEngine _engine = new TesseractEngine("./tessdata", "eng", EngineMode.Default);

        public OCRResponse GetTextFromImage(string url) {
            var textFound = "";

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

            var img = (Bitmap)Image.FromStream(resp.GetResponseStream());

            Bitmap upscaled = new Bitmap(img.Width * 4, img.Height * 4);
            using (Graphics gr = Graphics.FromImage(upscaled)) {
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(img, new Rectangle(0, 0, img.Width * 4, img.Height * 4));

            }

            var page = _engine.Process(upscaled);

            try {
                textFound = page.GetText().Trim();
            } catch(AccessViolationException) {
                Console.WriteLine("Found weird bug");
            }
            

            upscaled.Dispose();
            img.Dispose();

            return new OCRResponse() {
                Text = textFound,
                Confidence = page.GetMeanConfidence()
            };
        }
    }
}
