using System;

namespace gtalkchat.Voice {
    public class PcmDecoderStream : DecoderStream {
        private readonly short[] sampleBuffer = new short[4096 * 16];
        private int samplePosition;

        public PcmDecoderStream() {
            Initialize(16000, 1024);
        }

        public override void Update(byte[] data, int offset, int count) {
            lock (sampleBuffer) {
                for (int index = 0; samplePosition < sampleBuffer.Length && index < count; index += 2, samplePosition++) {
                    sampleBuffer[samplePosition] = BitConverter.ToInt16(data, index);
                }
            }
        }

        protected override double BufferStatus {
            get { return samplePosition / (double) SampleBuffer.Length; }
        }

        protected override void FillBuffer(short[] data) {
            lock (sampleBuffer) {
                Array.Copy(sampleBuffer, 0, data, 0, data.Length);

                for(var i = 0; i < samplePosition - data.Length; i++) {
                    sampleBuffer[i] = sampleBuffer[i + data.Length];
                }

                samplePosition -= data.Length;
            }
        }
    }
}
