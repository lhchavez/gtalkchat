using NSpeex;
using System;

namespace gtalkchat.Voice {
    public class SpeexEncoderStream : EncoderStream {
        private readonly SpeexEncoder encoder;
        private readonly short[] sampleBuffer;
        private int sampleOffset;
        private bool downsampling;

        public SpeexEncoderStream(BandMode mode) {
            encoder = new SpeexEncoder(mode);

            if(mode == BandMode.Narrow) {
                downsampling = true;
            }

            sampleBuffer = new short[encoder.FrameSize];
        }

        public override int Encode(short[] input, int inputOffset, int inputLength, byte[] output, int outputOffset, int outputLength) {
            int count = 0;

            if (downsampling) {
                inputLength /= 2;
                for(var i = 0; i < inputLength; i++) {
                    input[i] = (short)(input[2 * i] + input[2 * i + 1] / 2);
                }
            }

            if (sampleOffset > 0) {
                var len = Math.Min(inputLength, sampleBuffer.Length - sampleOffset);

                Array.Copy(input, inputOffset, sampleBuffer, sampleOffset, len);
                sampleOffset += len;
                inputOffset += len;
                inputLength -= len;

                if (sampleOffset < sampleBuffer.Length) return 0;

                var bytesEncoded = encoder.Encode(sampleBuffer, 0, sampleBuffer.Length, output, outputOffset, outputLength);

                sampleOffset = 0;

                outputOffset += bytesEncoded;
                outputLength -= bytesEncoded;

                count += bytesEncoded;
            }

            var process = inputLength - inputLength % sampleBuffer.Length;

            if (process > 0) {
                var bytesEncoded = encoder.Encode(input, inputOffset, process, output, outputOffset, outputLength);

                inputOffset += process;
                inputLength -= process;

                count += bytesEncoded;
            }

            if (inputLength > 0) {
                Array.Copy(input, inputOffset, sampleBuffer, 0, inputLength);

                sampleOffset = inputLength;
            }

            return count;
        }
    }
}
