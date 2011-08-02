using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Threading;

namespace gtalkchat.Voice {
    public abstract class DecoderStream : MediaStreamSource {
        public int SamplesPerSecond = 16000;
        protected int Channels = 1;
        protected int BitsPerSample = 16;

        private long currentTimeStamp;
        private int byteRate;
        private short blockAlign;

        private MediaStreamDescription mediaStreamDescription;
        private readonly Dictionary<MediaSampleAttributeKeys, string> emptySampleDict =
            new Dictionary<MediaSampleAttributeKeys, string>();

        protected short[] SampleBuffer;

        protected void Initialize(int sampleRate, int frameSize) {
            SamplesPerSecond = sampleRate;

            byteRate = SamplesPerSecond * Channels * BitsPerSample / 8;
            blockAlign = (short)(Channels * (BitsPerSample / 8));

            SampleBuffer = new short[frameSize];
        }

        protected override void OpenMediaAsync() {
            currentTimeStamp = 0;

            var streamAttributes = new Dictionary<MediaStreamAttributeKeys, string>();
            var sourceAttributes = new Dictionary<MediaSourceAttributesKeys, string>();
            var availableStreams = new List<MediaStreamDescription>();

            string format = "";
            format += ToLittleEndianString(string.Format("{0:X4}", 1));  //PCM
            format += ToLittleEndianString(string.Format("{0:X4}", Channels));
            format += ToLittleEndianString(string.Format("{0:X8}", SamplesPerSecond));
            format += ToLittleEndianString(string.Format("{0:X8}", byteRate));
            format += ToLittleEndianString(string.Format("{0:X4}", blockAlign));
            format += ToLittleEndianString(string.Format("{0:X4}", BitsPerSample));
            format += ToLittleEndianString(string.Format("{0:X4}", 0));

            streamAttributes[MediaStreamAttributeKeys.CodecPrivateData] = format;
            mediaStreamDescription = new MediaStreamDescription(MediaStreamType.Audio, streamAttributes);
            availableStreams.Add(mediaStreamDescription);
            sourceAttributes[MediaSourceAttributesKeys.Duration] = "0";
            sourceAttributes[MediaSourceAttributesKeys.CanSeek] = "false";

            ReportOpenMediaCompleted(sourceAttributes, availableStreams);
        }

        public abstract void Update(byte[] data, int offset, int count);

        protected abstract void FillBuffer(short[] data);

        protected virtual double BufferStatus {
            get { return 1.0; }
        }

        protected override void GetSampleAsync(MediaStreamType mediaStreamType) {
            var memoryStream = new MemoryStream();

            if(BufferStatus < 1.0) {
                do {
                    ReportGetSampleProgress(BufferStatus / 2.0);
                    Thread.Sleep(20);
                } while (BufferStatus < 2.0);
                ReportGetSampleProgress(1.0);
            }

            FillBuffer(SampleBuffer);

            foreach (short sample in SampleBuffer) {
                memoryStream.WriteByte((byte) (sample & 0xFF));
                memoryStream.WriteByte((byte) (sample >> 8));
            }

            var duration = memoryStream.Position * 10000000L / byteRate;

            var mediaStreamSample =
                new MediaStreamSample(mediaStreamDescription, memoryStream, 0,
                                      memoryStream.Position, currentTimeStamp, duration, emptySampleDict);

            currentTimeStamp += duration;

            ReportGetSampleCompleted(mediaStreamSample);
        }

        protected override void SeekAsync(long seekToTime) {
            ReportSeekCompleted(seekToTime);
        }

        protected override void CloseMedia() {
            currentTimeStamp = 0;
            mediaStreamDescription = null;
        }

        protected override void GetDiagnosticAsync(MediaStreamSourceDiagnosticKind diagnosticKind) {
            throw new NotImplementedException();
        }

        protected override void SwitchMediaStreamAsync(MediaStreamDescription description) {
            throw new NotImplementedException();
        }

        protected string ToLittleEndianString(string bigEndianString) {
            var builder = new StringBuilder();

            for (var i = 0; i < bigEndianString.Length; i += 2)
                builder.Insert(0, bigEndianString.Substring(i, 2));

            return builder.ToString();
        }
    }
}
