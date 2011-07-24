using System;
using System.Net;
using System.IO;
using Procurios.Public;
using System.Collections.Generic;

namespace gtalkchat {
    public class GoogleTalk {
        private string token;
        private AesUtility aes;
        public bool LoggedIn { get; private set; }

        private enum ReceiveMode {
            Blob,
            SingleString,
            Lines
        };

        public delegate void WriteDataCallback(StreamWriter sw);

        public delegate void SuccessCallback(string data);

        public delegate void BinarySuccessCallback(String contentType, byte[] data);

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
            LoggedIn = true;
        }

        public void SetKey(string key) {
            aes = new AesUtility(key);
        }

        public void Login(string username, string auth, SuccessCallback scb, ErrorCallback ecb) {
            Send(
                "/login",
                ReceiveMode.SingleString,
                sw => sw.Write("username=" + HttpUtility.UrlEncode(username) + "&auth=" + HttpUtility.UrlEncode(auth)),
                data => {
                    token = data;
                    LoggedIn = true;
                    scb(data);
                },
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

        public void Logout(SuccessCallback scb, ErrorCallback ecb) {
            Send(
                "/logout",
                ReceiveMode.SingleString,
                sw => sw.Write("token=" + HttpUtility.UrlEncode(token)),
                data => {
                    token = null;
                    LoggedIn = false;
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

        private void Send(
            string uri, ReceiveMode mode, WriteDataCallback wdcb, SuccessCallback scb, BinarySuccessCallback bcb,
            ErrorCallback ecb, FinishedCallback fcb) {
            if (!LoggedIn && !uri.Equals("/login")) {
                throw new InvalidOperationException("Not logged in");
            }

            var req = WebRequest.CreateHttp("https://gtalkjsonproxy.lhchavez.com" + uri);

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
                        var response = (HttpWebResponse)req.EndGetResponse(a);

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
                                            if (line.Length > 0) {
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
                        var response = (HttpWebResponse)e.Response;

                        if (response == null || response.StatusCode == HttpStatusCode.Forbidden) {
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
                }, null);
            }, null);
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

                contact.JID = data["jid"] as string;
                contact.Online = !data.ContainsKey("type") || !"unavailable".Equals(data["type"] as string);
                if (data.ContainsKey("name")) contact.Name = data["name"] as string;
                if (data.ContainsKey("show")) contact.Show = data["show"] as string;
                if (data.ContainsKey("photo")) contact.Photo = data["photo"] as string;

                mcb(contact);
            } else {
                ecb("Invalid JSON");
            }
        }
    }
}