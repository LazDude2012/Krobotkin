using System;
using System.Collections.Generic;
using System.Drawing;
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

            var page = _engine.Process(img);
            

            img.Save("test.png");

            textFound = page.GetText().Trim();

            return new OCRResponse() {
                Text = textFound,
                Confidence = page.GetMeanConfidence()
            };
        }
    }
}
