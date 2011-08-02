using System;
using Microsoft.Xna.Framework.Audio;
using System.Windows.Threading;
using Microsoft.Xna.Framework;

namespace gtalkchat.Voice {
    public class RecordingSession {
        private readonly Microphone microphone = Microphone.Default;
        private readonly DispatcherTimer timer;

        private readonly byte[] buffer;
        private readonly short[] samples;

        public delegate void AudioReadyHandler(byte[] data, int offset, int count);
        public event AudioReadyHandler AudioReady;

        public EncoderStream Encoder { get; set; }

        public RecordingSession() {
            timer = new DispatcherTimer {
                Interval = TimeSpan.FromMilliseconds(50)
            };

            timer.Tick += delegate { try { FrameworkDispatcher.Update(); } catch { } };

            microphone.BufferReady += microphone_BufferReady;
            microphone.BufferDuration = TimeSpan.FromMilliseconds(100);

            samples = new short[microphone.GetSampleSizeInBytes(microphone.BufferDuration)];
            buffer = new byte[samples.Length * 2];
        }

        public void Start() {
            timer.Start();
            microphone.Start();
        }

        public void Stop() {
            timer.Stop();
            microphone.Stop();
        }

        private void microphone_BufferReady(object sender, EventArgs e) {
            var length = microphone.GetData(buffer, 0, buffer.Length);

            // convert to short
            int sampleIndex = 0;
            for (int index = 0; index < length; index += 2, sampleIndex++) {
                samples[sampleIndex] = BitConverter.ToInt16(buffer, index);
            }

            var encodedBytes = Encoder.Encode(samples, 0, sampleIndex, buffer, 0, buffer.Length);
            if (encodedBytes != 0 && AudioReady != null) {
                AudioReady(buffer, 0, encodedBytes);
            }
        }
    }
}
