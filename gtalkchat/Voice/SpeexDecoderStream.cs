using System;
using NSpeex;

namespace gtalkchat.Voice {
    public class SpeexDecoderStream : DecoderStream {
        private readonly SpeexDecoder decoder;
        private readonly SpeexJitterBuffer speex;

        public SpeexDecoderStream(BandMode mode) {
            decoder = new SpeexDecoder(mode);
            speex = new SpeexJitterBuffer(decoder);

            Initialize(decoder.SampleRate, decoder.FrameSize * 32);
        }

        public override void Update(byte[] data, int offset, int count) {
            var packet = new byte[count];

            Array.Copy(data, offset, packet, 0, count);

            speex.Put(packet);
        }

        protected override void FillBuffer(short[] data) {
            speex.Get(data);
        }
    }
}
