using System;

namespace gtalkchat.Voice {
    public class SinewaveGeneratorStream : DecoderStream {
        private int t;

        public SinewaveGeneratorStream() {
            Initialize(16000, 1024);
        }

        public override void Update(byte[] data, int offset, int count) {
            // yeah
        }

        protected override void FillBuffer(short[] data) {
            for(var i = 0; i < data.Length; i++) {
                data[i] = (short) (short.MaxValue * Math.Sin(t++));
            }
        }
    }
}
