using System;
using System.Net;
using System.Net.Sockets;

namespace gtalkchat.Voice {
    public class RtpSession {
        private byte[] packet = new byte[1500];
        private byte[] incoming = new byte[1500];
        private Random rand = new Random();
        private int sequenceId;
        private Socket sock;
        private EndPoint endpoint;
        private SocketAsyncEventArgs readAsync;

        private long baseTimestamp;

        public AudioSession AudioSession { get; set; }
        private RecordingSession recordingSession;
        public RecordingSession RecordingSession {
            get { return recordingSession; }
            set {
                recordingSession = value;
                recordingSession.AudioReady += SendPacket;
            }
        }

        public RtpSession(string host, int port, byte payload) {
            sequenceId = rand.Next(0, 0xFFFF);
            baseTimestamp = rand.Next(0, 0x7FF);

            packet[0] = (byte) (
                2 << 6 | // version = 2
                0 << 5 | // padding = no
                0 << 4 | // extension = no
                0 // CSRC count = 0
            );

            packet[1] = (byte)(
                0 << 7 | // Marker = 0
                payload
            );

            for (var i = 8; i < 12; i++) {
                packet[i] = (byte)rand.Next(0, 255);
            }

            endpoint = new DnsEndPoint(host, port);
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            /*
            readAsync = new SocketAsyncEventArgs();
            readAsync.RemoteEndPoint = new IPEndPoint(IPAddress.Any, port);
            readAsync.Completed += Receive;
            readAsync.SetBuffer(incoming, 0, incoming.Length);
            if(!sock.ReceiveFromAsync(readAsync)) {
                Receive(null, readAsync);
            }
            */
        }

        public void SendPacket(byte[] data, int offset, int length) {
            var timestamp = baseTimestamp + RecordingSession.Timestamp;

            packet[2] = (byte)(sequenceId >> 8);
            packet[3] = (byte)(sequenceId & 0xFF);

            packet[4] = (byte)((timestamp >> 24) & 0xFF);
            packet[5] = (byte)((timestamp >> 16) & 0xFF);
            packet[6] = (byte)((timestamp >>  8) & 0xFF);
            packet[7] = (byte)((timestamp      ) & 0xFF);

            Array.Copy(data, offset, packet, 12, length);

            var async = new SocketAsyncEventArgs();
            async.SetBuffer(packet, 0, length + 12);
            async.RemoteEndPoint = endpoint;

            sock.SendToAsync(async);

            sequenceId++;
        }

        public void Receive(object token, SocketAsyncEventArgs args) {
            if (AudioSession != null) {
                AudioSession.Update(args.Buffer, 12, args.BytesTransferred);
            }

            if (!sock.ReceiveFromAsync(readAsync)) {
                Receive(null, readAsync);
            }
        }
    }
}
