using NAudio.Wave;
using System.Collections.Concurrent;

// OpusDotNet doesn't do will with PCM -> Opus, so we'll use Concentus instead.
// Concentus doesn't do well with Open -> PCM, so we'll use NAudio instead.

namespace WBStreamee
{
    internal static class MediaReceiver
    {
        private static OpusDotNet.OpusDecoder decoder = new OpusDotNet.OpusDecoder(48000, 2);
        private static WaveFormat waveFormat = new WaveFormat(48000, 16, 2);
        private static BufferedWaveProvider bufferedWaveProvider;
        private static WaveOutEvent waveOut = new WaveOutEvent();

        private static ConcurrentQueue<byte[]> _audioQueue = new ConcurrentQueue<byte[]>();

        internal static void Start()
        {
            bufferedWaveProvider = new BufferedWaveProvider(waveFormat)
            {
                BufferDuration = TimeSpan.FromSeconds(60),
            };

            waveOut.Init(bufferedWaveProvider);
            waveOut.Play();

            Task.Run(() =>
            {
                while (true)
                {
                    if (_audioQueue.TryDequeue(out byte[] media))
                    {
                        ProcessMedia(media);
                    }
                }
            });
        }

        public static void Collect(byte[] media)
        {
            _audioQueue.Enqueue(media);
        }

        public static byte[] DecodeOpusToPcm(byte[] opusData, int sampleRate, int channels)
        {
            var opus = new ReadOnlySpan<byte>(opusData);

            int frames = Concentus.Structs.OpusPacketInfo.GetNumFrames(opus);
            int samplesPerFrame = Concentus.Structs.OpusPacketInfo.GetNumSamplesPerFrame(opus, 48000);
            int packetDuration = frames * samplesPerFrame;

            short[] pcmBuffer = new short[samplesPerFrame * channels];
            byte[] pcmBytes = decoder.Decode(opusData, opusData.Length, out int decodedLength);

            short[] pcmShorts = new short[decodedLength / 2];
            Buffer.BlockCopy(pcmBytes, 0, pcmShorts, 0, decodedLength);

            byte[] waveBuffer = new byte[pcmShorts.Length * sizeof(short)];
            Buffer.BlockCopy(pcmShorts, 0, waveBuffer, 0, waveBuffer.Length);
            
            return waveBuffer;
        }

        internal static void ProcessMedia(byte[] media)
        {
            //var data = new ReadOnlySpan<byte>(media);

            // Decode Opus data to PCM
            byte[] pcmData = DecodeOpusToPcm(media, 48000, 2);
            bufferedWaveProvider.AddSamples(pcmData, 0, pcmData.Length);

            Console.WriteLine($"Processed media of size {pcmData.Length}");
        }
    }
}
