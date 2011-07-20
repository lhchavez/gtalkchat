using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO;

namespace gtalkchat {
    public class GoogleTalk {
        private string Token;
        public delegate void WriteDataCallback(StreamWriter sw);
        public delegate void SuccessCallback(string data);
        public delegate void RosterCallback(string[] roster);
        public delegate void ErrorCallback(string error);

        public GoogleTalk(string Username, string Auth, SuccessCallback scb, ErrorCallback ecb) {
            Login(Username, Auth, scb, ecb);
        }

        public GoogleTalk(string Token) {
            this.Token = Token;
        }

        private void Login(string Username, string Auth, SuccessCallback scb, ErrorCallback ecb) {
            Send(
                "/login",
                sw => {
                    sw.Write("username=" + HttpUtility.UrlEncode(Username) + "&auth=" + HttpUtility.UrlEncode(Auth));
                },
                data => {
                    this.Token = data;
                    scb(data);
                },
                ecb
            );
        }

        public void Message(string to, string body, SuccessCallback scb, ErrorCallback ecb) {
            Send(
                "/message",
                sw => {
                    sw.Write("token=" + HttpUtility.UrlEncode(this.Token) + "&to=" + HttpUtility.UrlEncode(to) + "&body=" + HttpUtility.UrlEncode(body));
                },
                scb,
                ecb
            );
        }

        public void Register(string url, SuccessCallback scb, ErrorCallback ecb) {
            Send(
                "/register",
                sw => {
                    string data = "token=" + HttpUtility.UrlEncode(this.Token) + "&url=" + HttpUtility.UrlEncode(url);
                    sw.Write(data);
                },
                scb,
                ecb
            );
        }

        public void Logout(SuccessCallback scb, ErrorCallback ecb) {
            Send(
                "/logout",
                sw => {
                    sw.Write("token=" + HttpUtility.UrlEncode(this.Token));
                },
                scb,
                ecb
            );
        }

        public void Roster(RosterCallback rcb, ErrorCallback ecb) {
            Send(
                "/logout",
                sw => {
                    sw.Write("token=" + HttpUtility.UrlEncode(this.Token));
                },
                data => {
                },
                ecb
            );
        }

        private void Send(string uri, WriteDataCallback wdcb, SuccessCallback scb, ErrorCallback ecb) {
            var req = HttpWebRequest.CreateHttp("https://gtalkjsonproxy.lhchavez.com" + uri);

            req.ContentType = "application/x-www-form-urlencoded";
            req.Method = "POST";

            req.BeginGetRequestStream(ar => {
                using (var requestStream = req.EndGetRequestStream(ar)) {
                    using (var sr = new StreamWriter(requestStream)) {
                        wdcb(sr);
                    }
                }

                req.BeginGetResponse(a => {
                    try {
                        var response = req.EndGetResponse(a) as HttpWebResponse;

                        var responseStream = response.GetResponseStream();
                        using (var sr = new StreamReader(responseStream)) {
                            scb(sr.ReadToEnd());
                        }
                    } catch (WebException e) {
                        var response = e.Response as HttpWebResponse;

                        try {
                            using (var responseStream = response.GetResponseStream()) {
                                using (var sr = new StreamReader(responseStream)) {
                                    ecb(sr.ReadToEnd());
                                }
                            }
                        } catch (Exception ex) {
                            // What is wrong with this platform?!
                            ecb(ex.Message + "\n" + e.Message);
                        }
                    }
                }, null);
            }, null);
        }
    }
}
