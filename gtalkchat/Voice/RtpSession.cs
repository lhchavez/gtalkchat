using System;
using System.Net;
using System.Net.Sockets;

namespace gtalkchat.Voice {
    public class RtpSession {
        private byte[] packet = new byte[1500];
        private byte[] incoming = new byte[1500];
        private Random rand = new Random();
        private int sequenceId;
        private SocketAsyncEventArgs readAsync;

        private long baseTimestamp;

        public Socket Socket { get; set; }
        public EndPoint EndPoint { get; set; }
        public AudioSession AudioSession { get; set; }
        private RecordingSession recordingSession;
        public RecordingSession RecordingSession {
            get { return recordingSession; }
            set {
                recordingSession = value;
                recordingSession.AudioReady += SendPacket;
            }
        }

        public RtpSession(byte payload) {
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
        }

        public void Start() {
            if(Socket.ProtocolType == ProtocolType.Udp) {
                if (RecordingSession != null) {
                    RecordingSession.Start();
                }
                if (AudioSession != null) {
                    AudioSession.Start();
                }

                readAsync = new SocketAsyncEventArgs {
                    RemoteEndPoint = new IPEndPoint(IPAddress.Any, (readAsync.RemoteEndPoint as IPEndPoint).Port)
                };

                readAsync.Completed += Receive;
                readAsync.SetBuffer(incoming, 0, incoming.Length);
                if (!Socket.ReceiveFromAsync(readAsync)) {
                    Receive(null, readAsync);
                }
            } else {
                var connectAsync = new SocketAsyncEventArgs {
                    RemoteEndPoint = EndPoint
                };
                connectAsync.Completed += Connect;
                if(!Socket.ConnectAsync(connectAsync)) {
                    Connect(Socket, connectAsync);
                }
            }
        }

        public void SendPacket(byte[] data, int offset, int length) {
            var timestamp = baseTimestamp + RecordingSession.Timestamp;

            packet[2] = (byte)(sequenceId >> 8);
            packet[3] = (byte)(sequenceId & 0xFF);

            packet[4] = (byte)((timestamp >> 24) & 0xFF);
            packet[5] = (byte)((timestamp >> 16) & 0xFF);
            packet[6] = (byte)((timestamp >>  8) & 0xFF);
            packet[7] = (byte)((timestamp      ) & 0xFF);

            Array.Copy(data, offset, packet, 12, Math.Min(packet.Length - 12, length));

            var async = new SocketAsyncEventArgs();
            async.SetBuffer(packet, 0, Math.Min(packet.Length, length + 12));
            async.RemoteEndPoint = EndPoint;

            Socket.SendToAsync(async);

            sequenceId++;
        }

        public void Connect(object token, SocketAsyncEventArgs args) {
            if(args.SocketError != SocketError.Success) {
                System.Diagnostics.Debug.WriteLine("RTP session died X_X");
                return;
            }

            if (RecordingSession != null) {
                RecordingSession.Start();
            }
            if(AudioSession != null) {
                AudioSession.Start();
            }

            readAsync = new SocketAsyncEventArgs {
                RemoteEndPoint = args.RemoteEndPoint
            };

            readAsync.Completed += Receive;
            readAsync.SetBuffer(incoming, 0, incoming.Length);
            if(Socket.ProtocolType == ProtocolType.Udp) {
                if (!Socket.ReceiveFromAsync(readAsync)) {
                    Receive(null, readAsync);
                }
            } else {
                if (!Socket.ReceiveAsync(readAsync)) {
                    Receive(null, readAsync);
                }
            }
        }

        public void Receive(object token, SocketAsyncEventArgs args) {
            if (args.BytesTransferred > 0) {
                System.Diagnostics.Debug.WriteLine("{0} bytes received", args.BytesTransferred);

                if (AudioSession != null) {
                    AudioSession.Update(args.Buffer, 12, args.BytesTransferred);
                }
            }

            if(Socket.ProtocolType == ProtocolType.Udp) {
                (readAsync.RemoteEndPoint as IPEndPoint).Address = IPAddress.Any;

                if (!Socket.ReceiveFromAsync(readAsync)) {
                    Receive(null, readAsync);
                }
            } else {
                if (!Socket.ReceiveAsync(readAsync)) {
                    Receive(null, readAsync);
                }
            }
        }
    }
}
