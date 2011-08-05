using System;
using Microsoft.Xna.Framework.Audio;
using System.Windows.Threading;
using Microsoft.Xna.Framework;

namespace gtalkchat.Voice {
    public class RecordingSession {
        private Microphone microphone;
        private DispatcherTimer timer;

        private byte[] buffer;
        private short[] samples;

        public delegate void AudioReadyHandler(byte[] data, int offset, int count);
        public event AudioReadyHandler AudioReady;

        public EncoderStream Encoder { get; set; }
        public long Timestamp { get; private set; }

        public RecordingSession() {
            App.Current.RootFrame.Dispatcher.BeginInvoke(
                () => {
                    timer = new DispatcherTimer {
                        Interval = TimeSpan.FromMilliseconds(20)
                    };

                    timer.Tick += delegate { try { FrameworkDispatcher.Update(); } catch { } };

                    microphone = Microphone.Default;

                    microphone.BufferReady += microphone_BufferReady;
                    microphone.BufferDuration = TimeSpan.FromMilliseconds(100);

                    samples = new short[microphone.GetSampleSizeInBytes(microphone.BufferDuration)];
                    buffer = new byte[samples.Length * 2];
                });
        }

        public void Start() {
            App.Current.RootFrame.Dispatcher.BeginInvoke(
                () => {
                    timer.Start();
                    microphone.Start();
                });
        }

        public void Stop() {
            App.Current.RootFrame.Dispatcher.BeginInvoke(
                () => {
                    timer.Stop();
                    microphone.Stop();
                });
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

            Timestamp += sampleIndex;
        }
    }
}
