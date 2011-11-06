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

        public delegate void UploadCallback(ImgurFile i, string error);

        public static void Upload(BitmapImage bm, UploadCallback callback) {
            if (bm.PixelWidth > 640 || bm.PixelHeight > 480) {
                bm = Resize(bm, 480);
            }
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
                try {
                    using (var stream = new StreamWriter(req.EndGetRequestStream(a))) {
                        stream.Write(requestString);
                    }

                    req.BeginGetResponse(ar => {
                        try {
                            var response = (HttpWebResponse)req.EndGetResponse(ar);
                            using (StreamReader sr = new StreamReader(response.GetResponseStream())) {
                                string text = sr.ReadToEnd();

                                callback(ParseResponse(text), null);
                            }
                        } catch (WebException e) {
                            if (e.Status == WebExceptionStatus.RequestCanceled || e.Status == WebExceptionStatus.SendFailure) {
                                callback(null, AppResources.Chat_ErrorUploadingPhotoTimeout);
                                return;
                            }

                            var response = (HttpWebResponse)e.Response;

                            if (response.StatusCode == HttpStatusCode.Forbidden) {
                                callback(null, AppResources.Chat_ErrorUploadingPhotoApiLimitExceeded);
                            } else {
                                callback(null, AppResources.Chat_ErrorUploadingPhoto);
                            }
                        }
                    }, req);
                } catch (WebException) {
                    callback(null, AppResources.Chat_ErrorUploadingPhoto);
                }
            }, req);
        }

        private static ImgurFile ParseResponse(string response) {
            bool success = false;

            var json = Json.JsonDecode(response, ref success);

            if (success && json is Dictionary<string, object>) {
                var data = json as Dictionary<string, object>;
                var upload = data["upload"] as Dictionary<string, object>;
                if (upload != null) {
                    var links = upload["links"] as Dictionary<string, object>;
                    if (links != null) {
                        var original = links["original"] as string;
                        var largethumb = links["large_thumbnail"] as string;
                        var smallthumb = links["small_square"] as string;

                        ImgurFile result = new ImgurFile {
                            Original = new Uri(original, UriKind.Absolute),
                            LargeThumbnail = new Uri(largethumb, UriKind.Absolute),
                            SmallSquare = new Uri(smallthumb, UriKind.Absolute)
                        };

                        return result;
                    }
                }
            }

            return null;
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
