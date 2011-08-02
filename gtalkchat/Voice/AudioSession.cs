using System.Windows.Controls;

namespace gtalkchat.Voice {
    public class AudioSession {
        public DecoderStream Decoder { get; set; }
        public MediaElement Media { get; set; }
        private bool initialized;

        public void Update(byte[] buffer, int offset, int length) {
            Decoder.Update(buffer, offset, length);
        }

        public void Start() {
            if(!initialized) {
                Media.SetSource(Decoder);
                initialized = true;
            }
            Media.Play();
        }

        public void Stop() {
            Media.Stop();
        }
    }
}
