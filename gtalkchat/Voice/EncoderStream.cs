namespace gtalkchat.Voice {
    public abstract class EncoderStream {
        public abstract int Encode(
            short[] input, int inputOffset, int inputLength, byte[] output, int outputOffset, int outputLength);
    }
}
