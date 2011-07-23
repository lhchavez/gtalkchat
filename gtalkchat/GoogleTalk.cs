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
using System.Collections.ObjectModel;

namespace gtalkchat {
    public class GoogleTalk {
        private string Token;
        private AESUtility Aes;

        private enum ReceiveMode {
            Blob,
            SingleString,
            Lines
        };

        public delegate void WriteDataCallback(StreamWriter sw);
        public delegate void SuccessCallback(string data);
        public delegate void BinarySuccessCallback(byte[] data);
        public delegate void ContactCallback(Contact contact);
        public delegate void RosterCallback(List<Contact> roster);
        public delegate void FinishedCallback();
        public delegate void MessageCallback(Message message);
        public delegate void ErrorCallback(string error);

        public GoogleTalk(string Username, string Auth, SuccessCallback scb, ErrorCallback ecb) {
            Login(Username, Auth, scb, ecb);
        }

        public GoogleTalk(string Token) {
            this.Token = Token;
        }

        public void SetKey(string key) {
            this.Aes = new AESUtility(key);
        }

        private void Login(string Username, string Auth, SuccessCallback scb, ErrorCallback ecb) {
            Send(
                "/login",
                ReceiveMode.SingleString,
                sw => sw.Write("username=" + HttpUtility.UrlEncode(Username) + "&auth=" + HttpUtility.UrlEncode(Auth)),
                data => {
                    this.Token = data;
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
                sw => {
                    sw.Write("token=" + HttpUtility.UrlEncode(this.Token));
                },
                data => {
                    this.Aes = new AESUtility(data);
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
                sw => sw.Write("token=" + HttpUtility.UrlEncode(this.Token) + "&to=" + HttpUtility.UrlEncode(to) + "&body=" + HttpUtility.UrlEncode(body)),
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
                    string data = "token=" + HttpUtility.UrlEncode(this.Token) + "&url=" + HttpUtility.UrlEncode(url);
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
                sw => sw.Write("token=" + HttpUtility.UrlEncode(this.Token)),
                scb,
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
                sw => sw.Write("token=" + HttpUtility.UrlEncode(this.Token)),
                line => ParseContact(line, false, contact => o.Add(contact), ecb),
                null,
                ecb,
                () => rcb(o)
            );
        }

        public void GetPhoto(string jid, BinarySuccessCallback bcb, ErrorCallback ecb) {
            var o = new List<Contact>();

            Send(
                "/photo",
                ReceiveMode.Blob,
                sw => sw.Write("token=" + HttpUtility.UrlEncode(this.Token) + "&jid" + HttpUtility.UrlEncode(jid)),
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
                sw => {
                    sw.Write("token=" + HttpUtility.UrlEncode(this.Token));
                },
                cipher => {
                    ParseMessage(cipher, mcb, ecb);
                },
                null,
                ecb,
                fcb
            );
        }

        private void Send(string uri, ReceiveMode mode, WriteDataCallback wdcb, SuccessCallback scb, BinarySuccessCallback bcb, ErrorCallback ecb, FinishedCallback fcb) {
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

                        if (mode == ReceiveMode.Blob) {
                            byte[] data = new byte[response.ContentLength];

                            responseStream.BeginRead(data, 0, (int)response.ContentLength, result => {
                                if (result.IsCompleted) {
                                    bcb(data);
                                } else {
                                    ecb("Incomplete response");
                                }
                            }, null);
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

        public void ParseMessage(string cipher, MessageCallback mcb, ErrorCallback ecb) {
            bool success = true;

            var line = Aes.Decipher(cipher);
            var json = JSON.JsonDecode(line, ref success);

            if (success && json is Dictionary<string, object>) {
                var data = json as Dictionary<string, object>;

                var message = new Message();

                message.From = data["from"] as string;
                message.Time = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddMilliseconds(long.Parse(data["time"].ToString().Split(new char[] { '.' })[0])).ToLocalTime();
                if (data.ContainsKey("type")) message.Type = data["type"] as string;
                if (data.ContainsKey("body")) message.Body = data["body"] as string;
                if (data.ContainsKey("otr")) message.OTR = true.Equals(data["otr"]);

                mcb(message);
            } else {
                ecb("Invalid JSON");
            }
        }

        public void ParseContact(string cipher, bool ciphered, ContactCallback mcb, ErrorCallback ecb) {
            bool success = true;

            var json = JSON.JsonDecode(ciphered ? Aes.Decipher(cipher) : cipher, ref success);

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
