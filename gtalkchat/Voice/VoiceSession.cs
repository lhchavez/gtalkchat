using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Xml.Linq;
using System.Diagnostics;

namespace gtalkchat.Voice {
    public class VoiceSession {
        public string SessionId { get; private set; }
        public string Initiator { get; private set; }
        public string Recipient { get; private set; }
        public string Partner { get; private set; }
        public bool IsInitiator { get; private set; }
        private static Random rand = new Random();
        private static int IdCounter;

        private List<Candidate> localCandidates = new List<Candidate>();
        private List<CandidatePair> candidatePairs = new List<CandidatePair>();
        private bool canSendCandidates;
        private bool rtpSessionStarted;

        private XNamespace phoneNs = "http://www.google.com/session/phone";
        private XNamespace sessionNs = "http://www.google.com/session";

        public VoiceSession(string recipient)
            : this(null, "s" + rand.Next(0, 1000000000)) {
            Initiator = App.Current.GtalkHelper.Jid;
            Recipient = recipient;
            Partner = Recipient;

            IsInitiator = true;
        }

        public VoiceSession(string initiator, string id) {
            SessionId = id;

            Initiator = initiator;
            Recipient = App.Current.GtalkHelper.Jid;
            Partner = Initiator;
            
            App.Current.GtalkHelper.AddSessionListener(SessionId, SessionStanzaReceived);

            for (var i = 0; i < 4; i++) {
                localCandidates.Add(
                    new Candidate {
                        Type = "stun",
                        Generation = i,
                        Priority = 0.9
                    });
            }

            localCandidates[0].Type = "local";
            //localCandidates[0].EndPoint = new IPEndPoint(IPAddress.Parse("192.168.1.103"), 12345);
            localCandidates[0].Priority = 1;

            App.Current.GtalkClient.Jingle(data => {
                var doc = XDocument.Parse(data);

                foreach (var stun in doc.Descendants("stun")) {
                    var ep = new IPEndPoint(
                        IPAddress.Parse(stun.Descendants("host").FirstOrDefault().FirstNode.ToString()),
                        int.Parse(stun.Descendants("udp").FirstOrDefault().FirstNode.ToString())
                    );

                    var endpoint = new StunPacket {
                        EndPoint = ep,
                        ClassicStun = true
                    };

                    endpoint.Success += (sp, ctx, sock, args) => {
                        if (localCandidates[1].EndPoint == null) {
                            localCandidates[1].EndPoint = sp.MappedAddress;
                            localCandidates[1].ListenPort = ep.Port;
                            localCandidates[1].Generation = 0;

                            SendCandidates();

                            sp.Serve(sock, ep.Port);
                        }
                    };

                    endpoint.Send();
                }
            }, err => {       
            });

            IsInitiator = false;
        }

        public void Close() {
            App.Current.GtalkHelper.RemoveSessionListener(SessionId);
        }

        #region XMPP Signaling

        public void Initiate() {
            Initiate(null);
        }

        public void Initiate(string id) {
            if (id == null) {
                id = "phone" + IdCounter++;
            }

            string msg;
            
            if(IsInitiator)
                msg = "<iq id=\"" + id + "\" to=\"" + Partner + "\" type=\"set\">";
            else
                msg = "<iq id=\"" + id + "\" to=\"" + Partner + "\" type=\"result\">";

            msg +=
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

        private void SendCandidates() {
            if (!canSendCandidates) return;

            foreach(var candidate in localCandidates) {
                if (candidate.EndPoint == null || candidate.Sent) continue;
                candidate.Sent = true;

                Debug.WriteLine("sent a candidate " + candidate.EndPoint + ", " + candidate.Username);

                App.Current.GtalkClient.RawIQ(
                    "",
                    new XElement(
                        "iq",
                        new XAttribute("to", Partner),
                        new XAttribute("id", "phone" + IdCounter++),
                        new XAttribute("type", "set"),
                        new XElement(
                            sessionNs.GetName("session"),
                            new XAttribute(XNamespace.Xmlns + "ses", sessionNs),
                            new XAttribute("type", "candidates"),
                            new XAttribute("id", SessionId),
                            new XAttribute("initiator", Initiator),
                            new XElement(
                                sessionNs.GetName("candidate"),
                                new XAttribute("address", candidate.Address),
                                new XAttribute("port", candidate.Port),
                                new XAttribute("username", candidate.Username),
                                new XAttribute("password", candidate.Password),
                                new XAttribute("name", "rtp"),
                                new XAttribute("preference", candidate.Priority),
                                new XAttribute("protocol", "udp"),
                                new XAttribute("generation", candidate.Generation),
                                new XAttribute("network", "1"),
                                new XAttribute("type", candidate.Type)
                            )
                        )
                    ).ToString(SaveOptions.DisableFormatting),
                    data => { },
                    error => { }
                );
            }
        }

        private void SessionStanzaReceived(string xml, XElement iq) {
            var session = iq.Descendants("session").FirstOrDefault();
            var iqId = iq.Attribute("id").Value;

            if(iq.Attribute("type").Value == "result" && session.Attribute("type").Value == "initiate") {
                canSendCandidates = true;
                SendCandidates();
            }

            if(iq.Attribute("type").Value == "set" && session.Attribute("type") != null) {
                switch (session.Attribute("type").Value) {
                    case "reject":
                        GoogleTalkHelper.ShowToast("Other party declined the connection");
                        Close();

                        break;
                    case "terminate":
                        GoogleTalkHelper.ShowToast("Call ended");
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
                                    Initiate(iqId);

                                    canSendCandidates = true;
                                    SendCandidates();
                                } else {
                                    App.Current.GtalkClient.RawIQ(
                                        "",
                                        new XElement(
                                            "iq",
                                            new XAttribute("to", Partner),
                                            new XAttribute("id", iqId),
                                            new XAttribute("type", "set"),
                                            new XElement(
                                                sessionNs.GetName("session"),
                                                new XAttribute(XNamespace.Xmlns + "ses", sessionNs),
                                                new XAttribute("type", "reject"),
                                                new XAttribute("id", SessionId),
                                                new XAttribute("initiator", Initiator)
                                                )
                                            ).ToString(SaveOptions.DisableFormatting),
                                        data => { },
                                        error => { }
                                        );
                                    Close();
                                }
                            });
                        break;
                    case "candidates":
                        foreach (var candidate in session.Descendants("candidate")) {
                            App.Current.GtalkClient.RawIQ(
                                "",
                                new XElement(
                                    "iq",
                                    new XAttribute("to", Partner),
                                    new XAttribute("id", iqId),
                                    new XAttribute("type", "result")
                                    ).ToString(SaveOptions.DisableFormatting),
                                data => { },
                                error => { }
                                );

                            var remoteCandidate = new Candidate {
                                EndPoint = new IPEndPoint(
                                    IPAddress.Parse(candidate.Attribute("address").Value),
                                    int.Parse(candidate.Attribute("port").Value)
                                    ),
                                Username = candidate.Attribute("username").Value,
                                Priority = double.Parse(candidate.Attribute("preference").Value),
                                Password = candidate.Attribute("password").Value,
                                Type = candidate.Attribute("type").Value,
                                Protocol = candidate.Attribute("protocol").Value
                            };
                            Candidate localCandidate;

                            Debug.WriteLine("received a candidate " + remoteCandidate.EndPoint + ", " + remoteCandidate.Username);

                            CandidatePair candidatePair;

                            switch (remoteCandidate.Type) {
                                case "local":
                                    localCandidate = localCandidates[0];
                                    break;
                                case "stun":
                                    localCandidate = localCandidates[1];
                                    break;
                                default:
                                    return;
                            }

                            candidatePair = new CandidatePair {
                                Local = localCandidate,
                                Remote = remoteCandidate
                            };

                            /*
                            candidatePair.Negotiation = new StunPacket {
                                EndPoint = candidatePair.Remote.EndPoint,
                                Username = candidatePair.Remote.Username + ":" + candidatePair.Local.Username,
                                Password = candidatePair.Remote.Password,
                                Priority = (uint)(0x100000000L * candidatePair.Priority),
                                Role = IsInitiator ? StunPacket.IceRole.IceControlling : StunPacket.IceRole.IceControlled,
                                Context = candidatePair
                            };
                            */

                            candidatePair.Negotiation = new StunPacket {
                                EndPoint = candidatePair.Remote.EndPoint,
                                Username = candidatePair.Remote.Username + candidatePair.Local.Username,
                                ClassicStun = true
                            };

                            candidatePairs.Add(candidatePair);

                            candidatePair.Negotiation.Success += (a, b, c, d) => {
                                Debug.WriteLine("negotiations succeeded for " + a);

                                if (remoteCandidate.Type == "local" &&
                                    localCandidates[0].ListenPort == 0) {
                                    localCandidates[0].EndPoint = a.MappedAddress;
                                    localCandidates[0].Generation = 1;
                                    localCandidates[0].ListenPort = 12345;
                                    localCandidates[0].Sent = false;

                                    localCandidates[1].Generation = 1;
                                    localCandidates[1].Sent = false;

                                    SendCandidates();
                                }
                            };

                            candidatePair.Negotiation.Error += (a, b) => {
                                Debug.WriteLine("negotiations failed for " + a);
                            };

                            Debug.WriteLine("starting negotiations for " + candidatePair.Negotiation);
                            candidatePair.Negotiation.Send();
                        }
                        break;
                }
            }
        }
        #endregion
    }
}
