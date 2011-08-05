namespace gtalkchat {
    public static class ByteArrayExtensions {
        public static void WriteUint16(this byte[] buffer, int offset, int data) {
            buffer[offset] = (byte)((data >> 8) & 0xFF);
            buffer[offset + 1] = (byte)((data) & 0xFF);
        }

        public static void WriteUint32(this byte[] buffer, int offset, uint data) {
            buffer[offset] = (byte)((data >> 24) & 0xFF);
            buffer[offset + 1] = (byte)((data >> 16) & 0xFF);
            buffer[offset + 2] = (byte)((data >> 8) & 0xFF);
            buffer[offset + 3] = (byte)((data) & 0xFF);
        }

        public static int ReadUint16(this byte[] buffer, int offset) {
            return buffer[offset] << 8 | buffer[offset + 1];
        }

        public static uint ReadUint32(this byte[] buffer, int offset) {
            return (uint)(buffer[offset] << 24 | buffer[offset + 1] << 16 | buffer[offset + 2] << 8 | buffer[offset + 3]);
        }
    }
}
