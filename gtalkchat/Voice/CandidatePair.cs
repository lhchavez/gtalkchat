using System;
using System.Net;
using System.Net.Sockets;

namespace gtalkchat.Voice {
    public class Candidate {
        private static Random random = new Random();
        private static string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        private string username;
        public string Username {
            get {
                if(username == null) {
                    username = GenerateRandomString();
                }
                return username;    
            }
            set { username = value; }
        }

        private string password;
        public string Password {
            get {
                if (password == null) {
                    password = GenerateRandomString();
                }
                return password;
            }
            set { password = value; }
        }
        public EndPoint EndPoint { get; set; }
        public double Priority { get; set; }
        public string Type { get; set; }
        public int Generation { get; set; }
        public int ListenPort { get; set; }
        public bool Sent { get; set; }
        public string Protocol { get; set; }

        public string Address {
            get {
                if (EndPoint is IPEndPoint) {
                    return (EndPoint as IPEndPoint).Address.ToString();
                } else if (EndPoint is DnsEndPoint) {
                    return (EndPoint as DnsEndPoint).Host;
                } else {
                    return "0.0.0.0";
                }
            }
        }

        public int Port {
            get {
                if (EndPoint is IPEndPoint) {
                    return (EndPoint as IPEndPoint).Port;
                } else if (EndPoint is DnsEndPoint) {
                    return (EndPoint as DnsEndPoint).Port;
                } else {
                    return 0;
                }
            }
        }

        public static string GenerateRandomString() {
            var ans = "";

            for(var i = 0; i < 16; i++) {
                ans += Alphabet[random.Next(0, Alphabet.Length)];
            }

            return ans;
        }

        public void Connect() {
            System.Diagnostics.Debug.WriteLine("Connecting tcpily to {0}", EndPoint);

            var rtp = new RtpSession(0) {
                EndPoint = EndPoint,
                Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp),
                RecordingSession = new RecordingSession {
                    Encoder = new PcmEncoderStream()
                }
            };

            rtp.Start();
        }
    }

    public class CandidatePair : IComparable<CandidatePair> {
        public Candidate Local { get; set; }
        public Candidate Remote { get; set; }

        public StunPacket Negotiation { get; set; }

        public double Priority { get { return Local.Priority * Remote.Priority; } }

        public int CompareTo(CandidatePair other) {
            return Math.Sign(Priority - other.Priority);
        }
    }
}