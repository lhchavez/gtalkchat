using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Gchat.Data;
using Gchat.Utilities;
using Procurios.Public;

namespace Gchat.Protocol {
    public class GoogleTalk {
        private string token;
        private AesUtility aes;
        public const int MessageTimeout = 10000;
        public bool LoggedIn { get; private set; }
        public const string DefaultRootUrl = "https://gtalkjsonproxy.lhchavez.com";
        private string rootUrl = DefaultRootUrl;
        public string RootUrl { get { return rootUrl; } set { rootUrl = value; } }

        private enum ReceiveMode {
            Blob,
            SingleString,
            Lines
        };

        public delegate void WriteDataCallback(StreamWriter sw);

        public delegate void SuccessCallback(string data);

        public delegate void BinarySuccessCallback(string contentType, byte[] data);

        public delegate void ContactCallback(Contact contact);

        public delegate void RosterCallback(List<Contact> roster);

        public delegate void FinishedCallback();

        public delegate void MessageCallback(Message message);

        public delegate void ErrorCallback(string error);

        public GoogleTalk() {
            LoggedIn = false;
        }

        public void SetToken(string token) {
            this.token = token;
            LoggedIn = token != null;
        }

        public void SetKey(string key) {
            aes = new AesUtility(key);
        }

        #region Public API

        public void Login(string username, string auth, SuccessCallback scb, ErrorCallback ecb) {
            Send(
                "/login",
                ReceiveMode.SingleString,
                sw => sw.Write("username=" + HttpUtility.UrlEncode(username) + "&auth=" + HttpUtility.UrlEncode(auth)),
                data => {
                    var fragments = data.Split(new[] { '\n' });
                    token = fragments[0];
                    RootUrl = fragments[1];
                    LoggedIn = true;
                    scb(token);
                },
                null,
                ecb,
                null
            );
        }

        public void CrashReport(string exception, SuccessCallback scb, ErrorCallback ecb) {
            Send(
                "/crashreport",
                ReceiveMode.SingleString,
                sw => sw.Write("exception=" + HttpUtility.UrlEncode(exception)),
                scb,
                null,
                ecb,
                null
            );
        }

        public void GetKey(SuccessCallback scb, ErrorCallback ecb) {
            Send(
                "/key",
                ReceiveMode.SingleString,
                sw => sw.Write("token=" + HttpUtility.UrlEncode(token)),
                data => {
                    aes = new AesUtility(data);
                    scb(data);
                },
                null,
                ecb,
                null
            );
        }

        public void SendMessage(string to, string body, SuccessCallback scb, ErrorCallback ecb) {
            Send(
                "/message",
                ReceiveMode.SingleString,
                sw =>
                sw.Write(
                    "token=" + HttpUtility.UrlEncode(token) + "&to=" + HttpUtility.UrlEncode(to) + "&body=" +
                    HttpUtility.UrlEncode(body)),
                scb,
                null,
                ecb,
                null
            );
        }

        public void Register(string url, SuccessCallback scb, ErrorCallback ecb) {
            Send(
                "/register",
                ReceiveMode.SingleString,
                sw => {
                    string data = "token=" + HttpUtility.UrlEncode(token) + "&url=" + HttpUtility.UrlEncode(url);
                    sw.Write(data);
                },
                scb,
                null,
                ecb,
                null
            );
        }

        public void Register(string url, IEnumerable<string> tiles, SuccessCallback scb, ErrorCallback ecb) {
            var tileArray = new StringBuilder();
            var first = true;

            tileArray.Append('[');
            foreach(var contact in tiles) {
                if (!first) tileArray.Append(',');
                first = false;

                tileArray.AppendFormat("\"{0}\"", contact.Replace("\"", "\\\""));
            }
            tileArray.Append(']');

            Send(
                "/register",
                ReceiveMode.SingleString,
                sw => {
                    string data = "token=" + HttpUtility.UrlEncode(token) + "&url=" + HttpUtility.UrlEncode(url) + "&tiles=" + HttpUtility.UrlEncode(tileArray.ToString());
                    sw.Write(data);
                },
                scb,
                null,
                ecb,
                null
            );
        }

        public void Logout(SuccessCallback scb, ErrorCallback ecb) {
            Send(
                "/logout",
                ReceiveMode.SingleString,
                sw => sw.Write("token=" + HttpUtility.UrlEncode(token)),
                data => {
                    aes = null;
                    token = null;
                    LoggedIn = false;
                    RootUrl = DefaultRootUrl;
                    scb(data);
                },
                null,
                ecb,
                null
            );
        }

        public void GetRoster(RosterCallback rcb, ErrorCallback ecb) {
            var o = new List<Contact>();

            Send(
                "/roster",
                ReceiveMode.Lines,
                sw => sw.Write("token=" + HttpUtility.UrlEncode(token)),
                line => ParseContact(line, false, o.Add, ecb),
                null,
                ecb,
                () => rcb(o)
            );
        }

        public void GetPhoto(string jid, BinarySuccessCallback bcb, ErrorCallback ecb) {
            Send(
                "/photo",
                ReceiveMode.Blob,
                sw => sw.Write("token=" + HttpUtility.UrlEncode(token) + "&jid" + HttpUtility.UrlEncode(jid)),
                null,
                bcb,
                ecb,
                null
            );
        }

        public void MessageQueue(MessageCallback mcb, ErrorCallback ecb, FinishedCallback fcb) {
            Send(
                "/messagequeue",
                ReceiveMode.Lines,
                sw => sw.Write("token=" + HttpUtility.UrlEncode(token)),
                cipher => ParseMessage(cipher, mcb, ecb),
                null,
                ecb,
                fcb
            );
        }

        public void OTR(string jid, bool enabled, SuccessCallback mcb, ErrorCallback ecb) {
            Send(
                "/otr",
                ReceiveMode.SingleString,
                sw => sw.Write("token=" + HttpUtility.UrlEncode(token) + "&jid=" + HttpUtility.UrlEncode(jid) + "&enabled=" + enabled.ToString()),
                mcb,
                null,
                ecb,
                null
            );
        }

        public void Notifications(bool toast, bool tile, bool secondaryTile, SuccessCallback mcb, ErrorCallback ecb) {
            Send(
                "/notifications",
                ReceiveMode.SingleString,
                sw => sw.Write("token=" + HttpUtility.UrlEncode(token) + "&toast=" + toast.ToString() + "&tile=" + tile.ToString() + "&secondarytile=" + secondaryTile.ToString()),
                mcb,
                null,
                ecb,
                null
            );
        }

        #endregion

        #region Helper Methods

        private void Send(
            string uri, ReceiveMode mode, WriteDataCallback wdcb, SuccessCallback scb, BinarySuccessCallback bcb,
            ErrorCallback ecb, FinishedCallback fcb) {
            if (!LoggedIn && uri != "/login" && uri != "/crashreport") {
                throw new InvalidOperationException("Not logged in");
            }

            var req = WebRequest.CreateHttp(((uri == "/login" || uri == "/crashreport") ? DefaultRootUrl : RootUrl) + uri);

            req.ContentType = "application/x-www-form-urlencoded";
            req.Method = "POST";

            var waitHandle = new ManualResetEvent(false);

            req.BeginGetRequestStream(ar => {
                try {
                    using (var requestStream = req.EndGetRequestStream(ar)) {
                        using (var sr = new StreamWriter(requestStream)) {
                            wdcb(sr);
                        }
                    }

                    req.BeginGetResponse(
                        a => {
                            waitHandle.Set();

                            try {
                                var response = (HttpWebResponse) req.EndGetResponse(a);

                                var responseStream = response.GetResponseStream();

                                if (mode == ReceiveMode.Blob) {
                                    var data = new byte[response.ContentLength];

                                    responseStream.BeginRead(
                                        data,
                                        0,
                                        (int) response.ContentLength,
                                        result => {
                                            if (result.IsCompleted) {
                                                bcb(response.ContentType, data);
                                            } else {
                                                ecb("Incomplete response");
                                            }
                                        },
                                        null
                                        );
                                } else {
                                    using (var sr = new StreamReader(responseStream)) {
                                        switch (mode) {
                                            case ReceiveMode.Lines:
                                                string line;

                                                while ((line = sr.ReadLine()) != null) {
                                                    if (line != string.Empty) {
                                                        scb(line);
                                                    }
                                                }

                                                break;
                                            case ReceiveMode.SingleString:
                                                scb(sr.ReadToEnd());

                                                break;
                                        }

                                        if (fcb != null) {
                                            fcb();
                                        }
                                    }
                                }
                            } catch (WebException e) {
                                if(e.Status == WebExceptionStatus.RequestCanceled) {
                                    ecb("");
                                    return;
                                }

                                var response = (HttpWebResponse) e.Response;

                                if (response.StatusCode == HttpStatusCode.Forbidden) {
                                    LoggedIn = false;
                                }

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
                        }, null
                    );
                } catch(WebException e) {
                    // The request was aborted
                    ecb("");
                }
            }, null);

            ThreadPool.QueueUserWorkItem(
                state => {
                    if (!waitHandle.WaitOne(MessageTimeout)) {
                        (state as HttpWebRequest).Abort();
                    }
                },
                req
            );
        }

        public void ParseMessage(string cipher, MessageCallback mcb, ErrorCallback ecb) {
            bool success = true;

            var line = aes.Decipher(cipher);
            var json = Json.JsonDecode(line, ref success);

            if (success && json is Dictionary<string, object>) {
                var data = json as Dictionary<string, object>;

                var message = new Message();

                message.From = data["from"] as string;
                message.Time =
                    new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(
                        long.Parse(data["time"].ToString().Split(new[] {'.'})[0])
                    ).ToLocalTime();
                if (data.ContainsKey("type")) message.Type = data["type"] as string;
                if (data.ContainsKey("body")) message.Body = data["body"] as string;
                if (data.ContainsKey("otr")) message.OTR = true.Equals(data["otr"]);
                if (data.ContainsKey("typing")) message.Typing = true.Equals(data["typing"]);
                message.Outbound = false;

                mcb(message);
            } else {
                ecb("Invalid JSON");
            }
        }

        public void ParseContact(string cipher, bool ciphered, ContactCallback mcb, ErrorCallback ecb) {
            bool success = true;

            var json = Json.JsonDecode(ciphered ? aes.Decipher(cipher) : cipher, ref success);

            if (success && json is Dictionary<string, object>) {
                var data = json as Dictionary<string, object>;

                var contact = new Contact();

                contact.Email = data["jid"] as string;
                if (data.ContainsKey("name")) contact.Name = data["name"] as string;
                if (data.ContainsKey("photo")) contact.Photo = data["photo"] as string;

                var sessions = data["sessions"] as Dictionary<string, object>;

                if (sessions != null) {
                    foreach (var element in sessions) {
                        var session = new ContactSession {
                            JID = element.Key
                        };

                        var s = element.Value as Dictionary<string, object>;

                        if (s != null) {
                            if (s.ContainsKey("show")) session.Show = s["show"] as string;
                            if (s.ContainsKey("status")) session.Status = s["status"] as string;
                            if (s.ContainsKey("caps")) {
                                var caps = s["caps"] as List<object>;
                                if(caps != null) {
                                    foreach(var o in caps) {
                                        session.Capabilities.Add(o.ToString());
                                    }
                                }
                            }

                            contact.AddSession(session);
                        }
                    }
                }

                mcb(contact);
            } else {
                ecb("Invalid JSON");
            }
        }

        #endregion
    }
}