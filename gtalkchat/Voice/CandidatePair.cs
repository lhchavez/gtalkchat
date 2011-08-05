using System;
using System.Net;

namespace gtalkchat.Voice {
    public class Candidate {
        private static Random random = new Random();

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
            var Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-/";

            var ans = "";

            for(var i = 0; i < 16; i++) {
                ans += Alphabet[random.Next(0, Alphabet.Length)];
            }

            return ans;
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