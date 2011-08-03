using System;
using System.Linq;
using System.Xml.Linq;
using System.Windows;

namespace gtalkchat.Voice {
    public class VoiceSession {
        public string SessionId { get; private set; }
        public string Initiator { get; private set; }
        public string Recipient { get; private set; }
        private static Random rand = new Random();
        private static int IdCounter = 0;

        private XNamespace phoneNs = "http://www.google.com/session/phone";
        private XNamespace sessionNs = "http://www.google.com/session";

        public VoiceSession(string recipient)
            : this(null, "s" + rand.Next(0, 1000000000)) {
            Initiator = App.Current.GtalkHelper.Jid;
            Recipient = recipient;
        }

        public VoiceSession(string initiator, string id) {
            SessionId = id;

            Initiator = initiator;
            Recipient = App.Current.GtalkHelper.Jid;
            
            App.Current.GtalkHelper.AddSessionListener(SessionId, SessionStanzaReceived);
        }

        public void Close() {
            App.Current.GtalkHelper.RemoveSessionListener(SessionId);
        }

        public void Initiate() {
            string id = "phone" + IdCounter++;
            string msg = "<iq from=\"" + Initiator + "\" id=\"" + id + "\" to=\"" + Recipient + "\" type=\"set\">" +
                "<session xmlns=\"http://www.google.com/session\" type=\"initiate\" id=\"" + SessionId + "\" initiator=\"" + Initiator + "\">" +
                    "<description xmlns=\"http://www.google.com/session/phone\">" +
                        //"<payload-type id=\"103\" name=\"ISAC\" clockrate=\"16000\"/>" +
                        //"<payload-type id=\"97\" name=\"IPCMWB\" bitrate=\"80000\" clockrate=\"16000\"/>" +
                        "<payload-type id=\"99\" name=\"speex\" bitrate=\"22000\" clockrate=\"16000\"/>" +
                        //"<payload-type id=\"102\" name=\"iLBC\" bitrate=\"13300\" clockrate=\"8000\"/>" +
                        "<payload-type id=\"98\" name=\"speex\" bitrate=\"11000\" clockrate=\"8000\"/>" +
                        //"<payload-type id=\"100\" name=\"EG711U\" bitrate=\"64000\" clockrate=\"8000\"/>" +
                        //"<payload-type id=\"101\" name=\"EG711A\" bitrate=\"64000\" clockrate=\"8000\"/>" +
                        "<payload-type id=\"0\" name=\"PCMU\" bitrate=\"64000\" clockrate=\"8000\"/>" +
                        "<payload-type id=\"8\" name=\"PCMA\" bitrate=\"64000\" clockrate=\"8000\"/>" +
                        //"<payload-type id=\"106\" name=\"telephone-event\" clockrate=\"8000\"/>" +
                    "</description>" +
                "</session>" +
            "</iq>";

            App.Current.GtalkClient.RawIQ(
                "",
                msg,
                response => { },
                error => { }
            );
        }

        private void SessionStanzaReceived(string xml, XElement iq) {
            var session = iq.Descendants("session").FirstOrDefault();
            var id = iq.Attribute("id").Value;

            if(iq.Attribute("type").Value == "set" && session.Attribute("type") != null) {
                switch(session.Attribute("type").Value) {
                    case "reject":
                        GoogleTalkHelper.ShowToast("Other party declined the connection");
                        Close();

                        break;
                    case "initiate":
                        App.Current.RootFrame.Dispatcher.BeginInvoke(
                            () => {
                                if (MessageBox.Show(
                                    iq.Attribute("from").Value + "wants to call you. Do you accept the call?",
                                    "Voice call",
                                    MessageBoxButton.OKCancel
                                ) == MessageBoxResult.OK) {
                                    App.Current.GtalkClient.RawIQ(
                                        "",
                                        new XElement(
                                            "iq",
                                            new XAttribute("from", iq.Attribute("to").Value),
                                            new XAttribute("to", iq.Attribute("from").Value),
                                            new XAttribute("id", id),
                                            new XAttribute("type", "result")
                                        ).ToString(SaveOptions.DisableFormatting),
                                        data => { },
                                        error => { }
                                    );
                                } else {
                                    App.Current.GtalkClient.RawIQ(
                                        "",
                                        new XElement(
                                            "iq",
                                            new XAttribute("from", iq.Attribute("to").Value),
                                            new XAttribute("to", iq.Attribute("from").Value),
                                            new XAttribute("id", id),
                                            new XAttribute("type", "set"),
                                            new XElement(
                                                sessionNs.GetName("session"),
                                                new XAttribute(XNamespace.Xmlns + "ses", sessionNs),
                                                new XAttribute("type", "reject"),
                                                new XAttribute("id", SessionId),
                                                new XAttribute("initiator", session.Attribute("initiator").Value)
                                            )
                                        ).ToString(SaveOptions.DisableFormatting),
                                        data => { },
                                        error => { }
                                    );
                                    Close();
                                }
                            });
                        break;
                }
            }
        }
    }
}
