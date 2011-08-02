namespace gtalkchat.Voice {
    public class PcmEncoderStream : EncoderStream {
        public override int Encode(short[] input, int inputOffset, int inputLength, byte[] output, int outputOffset, int outputLength) {
            int count = 0;

            for (var i = 0; i < inputLength && count < outputLength; i++) {
                output[outputOffset + count++] = (byte)(input[inputOffset + i] & 0xFF);
                output[outputOffset + count++] = (byte)(input[inputOffset + i] >> 8);
            }

            return count;
        }
    }
}
