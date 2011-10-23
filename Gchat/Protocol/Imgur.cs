using System;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using Procurios.Public;
using System.Collections.Generic;

namespace Gchat.Protocol {
    public class ImgurFile {
        public Uri Original { get; set; }
        public Uri SmallSquare { get; set; }
        public Uri LargeThumbnail { get; set; }
    }

    public static class Imgur {
        private static readonly string Key = "93eb20147bc8173d58e9a3aa72b927b0";
        private static readonly string UploadUrl = "http://api.imgur.com/2/upload.json";

        public delegate void UploadCallback(ImgurFile);

        public static void Upload(BitmapImage bm, UploadCallback callback) {
            Upload(ConvertImageToBytes(bm), callback);
        }

        public static void Upload(byte[] image, UploadCallback callback) {

            string requestString =
                HttpUtility.UrlEncode("image") + "=" +
                HttpUtility.UrlEncode(Convert.ToBase64String(image)) + "&" +
                HttpUtility.UrlEncode("key") + "=" +
                HttpUtility.UrlEncode(Key);

            var req = (HttpWebRequest)WebRequest.Create(new Uri(UploadUrl));
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";

            req.BeginGetRequestStream(a => {
                var request = (HttpWebRequest)a.AsyncState;
                using (var stream = new StreamWriter(request.EndGetRequestStream(a))) {
                    stream.Write(requestString);
                }

                req.BeginGetResponse(ar => {
                    var comp = (HttpWebRequest)ar.AsyncState;
                    var response = (HttpWebResponse)comp.EndGetResponse(ar);
                    using (StreamReader sr = new StreamReader(response.GetResponseStream())) {
                        Dictionary<string, object> root = (Dictionary<string, object>) Json.JsonDecode(sr.ReadToEnd());
                        Dictionary<string, object> links = (Dictionary<string, object>) root["links"];
                        ImgurFile result = new ImgurFile {
                            Original = new Uri((string)links["original"], UriKind.Absolute),
                            LargeThumbnail = new Uri((string)links["large_thumbnail"], UriKind.Absolute),
                            SmallSquare = new Uri((string)links["small_square"], UriKind.Absolute)
                        };

                        callback(result);
                    }

                }, request);

            }, req);

        }

        #region Static image converter helper methods

        public static byte[] ConvertImageToBytes(BitmapImage img) {
            if (img != null) {
                WriteableBitmap bm = new WriteableBitmap(img);

                byte[] buffer = null;

                using (MemoryStream ms = new MemoryStream()) {
                    bm.SaveJpeg(ms, bm.PixelWidth, bm.PixelHeight, 0, 100);

                    buffer = ms.ToArray();
                }

                return buffer;
            } else {
                return null;
            }
        }

        public static BitmapImage ConvertBytesToImage(byte[] buffer) {
            if (buffer != null) {
                using (var ms = new MemoryStream(buffer)) {
                    BitmapImage bm = new BitmapImage();
                    bm.SetSource(ms);

                    return bm;
                }
            } else {
                return null;
            }
        }

        public static BitmapImage Resize(BitmapImage image, int height) {
            // scaled size
            double cy = height;
            double cx = image.PixelWidth * (cy / image.PixelHeight);

            Image im = new Image();
            im.Source = image;

            WriteableBitmap wb = new WriteableBitmap((int)cx, (int)cy);
            ScaleTransform transform = new ScaleTransform();
            transform.ScaleX = cx / image.PixelWidth;
            transform.ScaleY = cy / image.PixelHeight;

            wb.Render(im, transform);
            wb.Invalidate();

            using (var ms = new MemoryStream()) {
                wb.SaveJpeg(ms, wb.PixelWidth, wb.PixelHeight, 0, 100);
                image.SetSource(ms);
            }

            return image;
        }

        #endregion
    }

}
