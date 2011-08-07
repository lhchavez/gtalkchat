using System;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Security.Cryptography;

namespace gtalkchat.Voice {
    public class StunPacket {
        public delegate void SuccessHandler(StunPacket origin, object context, Socket sock, SocketAsyncEventArgs args);
        public event SuccessHandler Success;

        public delegate void ErrorHandler(StunPacket origin, object context);
        public event ErrorHandler Error;

        public const int MagicCookie = 0x2112A442;

        public enum PacketType {
            BindingRequest = 0x0001,
            BindingResponse = 0x0101,
            BindingErrorResponse = 0x0111
        }

        public enum AttributeType {
            MappedAddress = 0x0001,
            ResponseAddress = 0x0002,
            ChangeRequest = 0x0003,
            SourceAddress = 0x0004,
            ChangedAddress = 0x0005,
            Username = 0x0006,
            Password = 0x0007,
            MessageIntegrity = 0x0008,
            ErrorCode = 0x0009,
            UnknownAttributes = 0x000a,
            ReflectedFrom = 0x000b,
            Priority = 0x0024,
            UseCandidate = 0x0025,
            XorMappedAddress = 0x8020,
            XorOnly = 0x0021,
            Software = 0x8022,
            Fingerprint = 0x8028,
            IceControlled = 0x8029,
            IceControlling = 0x802a
        }

        public enum IceRole {
            None,
            IceControlling,
            IceControlled
        }

        private byte[] packet = new byte[1500];
        private int offset;
        public byte[] response = new byte[1500];
        private Random random = new Random();
        private ManualResetEvent waiter = new ManualResetEvent(false);
        private static byte[] softwareName;
        private bool success;
        private bool alive;
        private int listenPort;

        public byte[] TransactionId { get; private set; }
        public PacketType Type { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Timeout { get; set; }
        public EndPoint EndPoint { get; set; }
        public Socket Socket { get; private set; }
        public uint Priority { get; set; }
        public IceRole Role { get; set; }
        public bool ClassicStun { get; set; }
        public IPEndPoint MappedAddress { get; private set; }
        public object Context { get; set; }

        public StunPacket() {
            if(softwareName == null) {
                softwareName = Encoding.UTF8.GetBytes("gChat1.0");
            }

            Timeout = 50;
            Type = PacketType.BindingRequest;
            Role = IceRole.None;
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        public void Send() {
            Type = PacketType.BindingRequest;

            PreparePacket();

            new Thread(Run).Start();
        }

        public void Stop() {
            alive = false;
            Socket.Close();
        }

        /// <summary>
        /// Starts a STUN server on the chosen port.
        /// The port MUST be the same port as the one you sent TO, not received FROM.
        /// </summary>
        /// <param name="sock"></param>
        /// <param name="port"></param>
        public void Serve(Socket sock, int port) {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs {
                RemoteEndPoint = new IPEndPoint(IPAddress.Any, port)
            };

            listenPort = port;

            args.SetBuffer(response, 0, response.Length);
            args.Completed += ServeReceived;

            if(!sock.ReceiveFromAsync(args)) {
                ServeReceived(sock, args);
            }
        }

        private void ServeReceived(object sender, SocketAsyncEventArgs args) {
            var sock = sender as Socket;

            if (args.SocketError == SocketError.Success) {
                Parse(args.BytesTransferred);

                MappedAddress = args.RemoteEndPoint as IPEndPoint;

                if (Type == PacketType.BindingRequest) {
                    ClassicStun = true;
                    Type = PacketType.BindingResponse;

                    System.Diagnostics.Debug.WriteLine("Serving a STUN request from " + MappedAddress);

                    PreparePacket();

                    System.Diagnostics.Debug.WriteLine("Served a STUN request to " + MappedAddress + ", " + this);

                    var sendArgs = new SocketAsyncEventArgs {
                        RemoteEndPoint = args.RemoteEndPoint
                    };
                    sendArgs.SetBuffer(packet, 0, offset);
                    sock.SendToAsync(sendArgs);
                }

                args.RemoteEndPoint = new IPEndPoint(IPAddress.Any, listenPort);

                if (!sock.ReceiveFromAsync(args)) {
                    ServeReceived(sock, args);
                }
            }
        }

        public override string ToString() {
            return String.Format("StunPacket{{RemoteEndPoint = {0}, Username = {1}, MappedAddress = {2}}}", EndPoint, Username, MappedAddress);
        }

        private void PreparePacket() {
            offset = 0;
            packet.WriteUint16(0, (int)Type);

            if (TransactionId != null) {
                Array.Copy(TransactionId, 0, packet, 4, 20);
            } else {
                if (ClassicStun) {
                    for (var i = 4; i < 8; i++) {
                        packet[i] = (byte) random.Next(0, 0xFF);
                    }
                } else {
                    packet.WriteUint32(4, MagicCookie);
                }

                for (var i = 8; i < 20; i++) {
                    packet[i] = (byte) random.Next(0, 0xFF);
                }
            }

            offset = 20;

            // Software attribute
            /*
            packet.WriteUint16(offset, (int)AttributeType.Software);
            packet.WriteUint16(offset + 2, softwareName.Length);

            Array.Copy(softwareName, 0, packet, offset + 4, softwareName.Length);

            offset += 4 + softwareName.Length;

            if (offset % 4 != 0) {
                offset += 4 - offset % 4;
            }
            */

            // Priority attribute

            if (Priority != 0) {
                packet.WriteUint16(offset, (int)AttributeType.Priority);
                packet.WriteUint16(offset + 2, 4);

                packet.WriteUint32(offset + 4, Priority);

                offset += 8;
            }

            // Ice-Controlled/Ice-Controlling attribute

            if (Role != IceRole.None) {
                if (Role == IceRole.IceControlled) {
                    packet.WriteUint16(offset, (int)AttributeType.IceControlled);
                } else {
                    packet.WriteUint16(offset, (int)AttributeType.IceControlling);
                }
                packet.WriteUint16(offset + 2, 8);

                for (var i = 4; i < 12; i++) {
                    packet[offset + i] = (byte)random.Next(0, 0xFF);
                }

                offset += 12;
            }

            if (Username != null) {
                var usernameBytes = Encoding.UTF8.GetBytes(Username);

                packet.WriteUint16(offset, (int)AttributeType.Username);
                packet.WriteUint16(offset + 2, usernameBytes.Length);

                Array.Copy(usernameBytes, 0, packet, offset + 4, usernameBytes.Length);

                offset += 4 + usernameBytes.Length;

                if (offset % 4 != 0) {
                    offset += 4 - offset % 4;
                }
            }

            if (MappedAddress != null) {
                packet.WriteUint16(offset, (int)AttributeType.MappedAddress);
                packet.WriteUint16(offset + 2, 8);

                packet.WriteUint16(offset + 4, 1);
                packet.WriteUint16(offset + 6, MappedAddress.Port);

                Array.Copy(MappedAddress.Address.GetAddressBytes(), 0, packet, offset + 8, 4);

                offset += 8 + 4;
            }

            // message integrity attribute

            if (Password != null) {
                packet.WriteUint16(2, offset - 20 + 32);

                var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(Password));

                var hmacBytes = hmac.ComputeHash(packet, 0, offset);

                packet.WriteUint16(offset, (int)AttributeType.MessageIntegrity);
                packet.WriteUint16(offset + 2, 20);

                Array.Copy(hmacBytes, 0, packet, offset + 4, 20);

                offset += 24;
            } else if (ClassicStun) {
                packet.WriteUint16(2, offset - 20);
            } else {
                packet.WriteUint16(2, offset - 20 + 8);
            }

            if (!ClassicStun) {
                // fingerprint attribute

                var crc = Crc32.Compute(packet, 0, offset) ^ 0x5354554e;

                packet.WriteUint16(offset, (int)AttributeType.Fingerprint);
                packet.WriteUint16(offset + 2, 4);

                packet.WriteUint32(offset + 4, crc);

                offset += 8;
            }
        }

        private void Parse(int bytesTransferred) {
            Username = null;
            MappedAddress = null;

            switch(response.ReadUint16(0)) {
                case (int)PacketType.BindingRequest:
                    Type = PacketType.BindingRequest;
                    break;
                case (int)PacketType.BindingResponse:
                    Type = PacketType.BindingResponse;
                    break;
                default:
                    Type = PacketType.BindingErrorResponse;
                    break;
            }

            for (var pos = 20; pos < bytesTransferred; ) {
                var length = response.ReadUint16(pos + 2);
                var paddedLength = length;

                if (length % 4 != 0) {
                    paddedLength += (4 - length % 4);
                }

                switch (response.ReadUint16(pos)) {
                    case (int)AttributeType.MappedAddress:
                        var ip = response[pos + 8] | (uint)response[pos + 9] << 8 | (uint)response[pos + 10] << 16 | (uint)response[pos + 11] << 24;
                        MappedAddress = new IPEndPoint(ip, response.ReadUint16(pos + 6));
                        break;
                    case (int)AttributeType.Username:
                        Username = Encoding.UTF8.GetString(response, pos + 4, length);
                        break;
                }

                pos += paddedLength + 4;
            }
        }

        private void Run() {
            var sendEndPoint = EndPoint;
            var sendArgs = new SocketAsyncEventArgs {
                RemoteEndPoint = new IPEndPoint(IPAddress.Broadcast, (EndPoint as IPEndPoint).Port)
            };

            sendArgs.Completed += Sent;
            sendArgs.SetBuffer(packet, 0, offset);

            var recvArgs = new SocketAsyncEventArgs();

            recvArgs.Completed += Received;
            recvArgs.SetBuffer(response, 0, response.Length);
            if (EndPoint is IPEndPoint) {
                recvArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Any, (EndPoint as IPEndPoint).Port);
            } else if (EndPoint is DnsEndPoint) {
                recvArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Any, (EndPoint as DnsEndPoint).Port);
            }

            alive = true;

            for (var round = 0; alive && round < 9; round++, Timeout *= 2) {
                if(round > 0) {
                    sendArgs.RemoteEndPoint = sendEndPoint;
                }
                if (!Socket.SendToAsync(sendArgs)) {
                    Sent(Socket, sendArgs);
                }

                waiter.WaitOne();
                waiter.Reset();

                if (!success) break;

                if (round == 0) {
                    if (!Socket.ReceiveFromAsync(recvArgs)) {
                        Received(Socket, recvArgs);
                    }
                }

                Thread.Sleep(Timeout);
            }

            if(alive && Error != null) {
                Error(this, Context);
                Socket.Close();
                Socket = null;
            }
        }

        private void Sent(object sender, SocketAsyncEventArgs args) {
            success = args.SocketError == SocketError.Success;
            System.Diagnostics.Debug.WriteLine("Sent a packet " + this);
            waiter.Set();
        }

        private void Received(object sender, SocketAsyncEventArgs args) {
            if (args.SocketError != SocketError.Success) return;

            bool valid = true;

            for (var i = 8; i < 20; i++) {
                if (packet[i] != response[i]) {
                    valid = false;
                    break;
                }
            }

            if (!valid) {
                if (!Socket.ReceiveFromAsync(args)) {
                    Received(Socket, args);
                }
            }

            Parse(args.BytesTransferred);

            System.Diagnostics.Debug.WriteLine("Received a packet " + this);

            args.Completed -= Received;

            alive = false;

            if (Success != null) {
                Success(this, Context, Socket, args);
            }
        }
    }
}
