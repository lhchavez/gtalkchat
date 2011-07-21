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
using Procurios.Public;
using System.Collections.Generic;

namespace gtalkchat {
    public class GoogleTalk {
        private string Token;
        public delegate void WriteDataCallback(StreamWriter sw);
        public delegate void SuccessCallback(string data);
        public delegate void RosterCallback(Roster roster);
        public delegate void MessageCallback(Message message);
        public delegate void ErrorCallback(string error);

        public struct Message {
            public string From;
            public DateTime Time;
            public string Type;
            public string Body;
            public bool OTR;
        };

        public struct Roster {
            public string JID;
            public bool Online;
            public string Name;
            public string Show;
            public string Status;
            public string Photo;
        };

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

        public void SendMessage(string to, string body, SuccessCallback scb, ErrorCallback ecb) {
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

        public void GetRoster(RosterCallback rcb, ErrorCallback ecb) {
            Send(
                "/roster",
                sw => {
                    sw.Write("token=" + HttpUtility.UrlEncode(this.Token));
                },
                line => {
                    bool success = true;

                    var json = JSON.JsonDecode(line, ref success);

                    if (success && json is Dictionary<string, object>) {
                        var data = json as Dictionary<string, object>;

                        var roster = new Roster();

                        roster.JID = data["jid"] as string;
                        roster.Online = !data.ContainsKey("type") || !"unavailable".Equals(data["type"] as string);
                        if (data.ContainsKey("name")) roster.Name = data["name"] as string;
                        if (data.ContainsKey("show")) roster.Show = data["show"] as string;
                        if (data.ContainsKey("photo")) roster.Photo = data["photo"] as string;

                        rcb(roster);
                    } else {
                        ecb("Invalid JSON");
                    }
                },
                ecb,
                true
            );
        }

        public void MessageQueue(MessageCallback mcb, ErrorCallback ecb) {
            Send(
                "/messagequeue",
                sw => {
                    sw.Write("token=" + HttpUtility.UrlEncode(this.Token));
                },
                line => {
                    bool success = true;

                    var json = JSON.JsonDecode(line, ref success);

                    if (success && json is Dictionary<string, object>) {
                        var data = json as Dictionary<string, object>;

                        var message = new Message();

                        message.From = data["from"] as string;
                        message.Time = new DateTime(1970,1,1,0,0,0,0).AddMilliseconds(long.Parse(data["time"].ToString().Split(new char[] { '.' })[0])).ToLocalTime();
                        if (data.ContainsKey("type")) message.Type = data["type"] as string;
                        if (data.ContainsKey("body")) message.Body = data["body"] as string;
                        if (data.ContainsKey("otr")) message.OTR = true.Equals(data["otr"]);

                        mcb(message);
                    } else {
                        ecb("Invalid JSON");
                    }
                },
                ecb
            );
        }

        private void Send(string uri, WriteDataCallback wdcb, SuccessCallback scb, ErrorCallback ecb) {
            Send(uri, wdcb, scb, ecb, false);
        }

        private void Send(string uri, WriteDataCallback wdcb, SuccessCallback scb, ErrorCallback ecb, bool lines) {
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
                            if (lines) {
                                string line;

                                while ((line = sr.ReadLine()) != null) {
                                    if (line.Length > 0) {
                                        scb(line);
                                    }
                                }
                            } else {
                                scb(sr.ReadToEnd());
                            }
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
