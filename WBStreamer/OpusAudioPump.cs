using Concentus.Enums;
using Concentus.Structs;
using NAudio.Wave;
using System.Text;
using Waimea.Channel;
using Waimea.Channel.Messages.Common;
//using OpusDotNet;

namespace WBStreamer
{
    internal static class OpusAudioPump
    {
        private const int FRAME_SIZE_MS = 20;

        internal static void StartPumpingOpusFrames(this FrontendChannel channel)
        {
            // Fire and forget to pump the Opus frames from a file.
            Task.Run(() => {
                //PumpOpusFramesFromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"testaudio.opus"), channel);
                PumpPcmFramesFromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"testaudio.wav"), channel);
                //PumpPcmFramesFromFile(@"c:\t\demo_audio.wav", channel);
            });
        }

        /// This code is lifted from https://skype.visualstudio.com/SCC/_git/service-shared_framework_waimea?path=%2Fsilos%2Fnative_sdk%2Fdemos%2Fdotnet%2Faudioplayer%2FMain.cs&_a=contents&version=GBmaster
        internal static void PumpOpusFramesFromFile(string opusFile, FrontendChannel channel)
        {
            var opusFrames = ReadOpusFramesFromFile(opusFile).Result;

            ulong nextAudioTimestamp = (ulong)DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            while (true)
            {
                for (int n = 0; n < opusFrames.Count; n++)
                {
                    channel?.Send(new() {
                        OutboundMedia = new() {
                            SourceId = 1,
                            AttachmentMetadata = new() {
                                EncodedAudio = new() {
                                    Codec = AudioCodec.Opus,
                                    TimestampUs = nextAudioTimestamp * 1000,
                                }
                            }
                        }
                    },
                    new BytesAttachment(opusFrames[n]));

                    Console.WriteLine($"Sending {opusFrames[n].Length} bytes");

                    nextAudioTimestamp += FRAME_SIZE_MS;

                    ulong cur = (ulong)DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    if (nextAudioTimestamp > cur)
                    {
                        Thread.Sleep((int)(nextAudioTimestamp - cur));
                    }
                }
            }
        }

        /// <summary>
        /// This code is lifted from https://skype.visualstudio.com/SCC/_git/service-shared_framework_waimea?path=%2Fsilos%2Fnative_sdk%2Fdemos%2Fdotnet%2Faudioplayer%2FMain.cs&_a=contents&version=GBmaster
        /// </summary>
        internal static async Task<List<byte[]>> ReadOpusFramesFromFile(string filename)
        {
            var frames = new List<byte[]>();
            byte[] oggContents = await File.ReadAllBytesAsync(filename).ConfigureAwait(true);
            int oggOffset = 0;
            int segmentCnt = 0;
            bool segmentContinued = false;

            // This walks the ogg files, and extracts the Opus 20ms frames from it.
            while (oggOffset < oggContents.Length)
            {
                // Verify we're at the start of an Ogg page.
                if (Encoding.ASCII.GetString(oggContents, oggOffset, 4) != "OggS")
                {
                    throw new ArgumentException("File does not appear to be Ogg/Opus");
                }
                // Skip over a bunch of the header we don't validate.
                oggOffset += 26;

                // Number of segments per page.
                int segmentEntries = oggContents[oggOffset++];
                int segmentOffset = oggOffset + segmentEntries;
                bool appendToLastSegmentOfPreviousPage = segmentContinued;

                while (segmentEntries > 0)
                {
                    segmentContinued = true;
                    int segmentLen = 0;
                    while (segmentEntries > 0 && segmentContinued)
                    {
                        int segmentEntry = oggContents[oggOffset++];
                        segmentEntries--;
                        segmentLen += segmentEntry;
                        segmentContinued = segmentEntry == 255;
                    }

                    segmentCnt++;
                    if (segmentCnt > 2)
                    {
                        if (appendToLastSegmentOfPreviousPage)
                        {
                            Console.WriteLine($"Continuation");
                            int frameCnt = frames.Count;
                            byte[] lastSegment = frames[frameCnt - 1];
                            frames.RemoveAt(frameCnt - 1);

                            byte[] segment = new byte[lastSegment.Length + segmentLen];
                            Array.Copy(lastSegment, 0, segment, lastSegment.Length, lastSegment.Length);
                            Array.Copy(oggContents, segmentOffset, segment, lastSegment.Length, segmentLen);
                            frames.Add(segment);
                        }
                        else
                        {
                            byte[] segment = new byte[segmentLen];
                            Array.Copy(oggContents, segmentOffset, segment, 0, segmentLen);
                            frames.Add(segment);
                        }
                    }

                    appendToLastSegmentOfPreviousPage = false;
                    segmentOffset += segmentLen;
                }

                oggOffset = segmentOffset;
            }

            return frames;
        }

        internal static void PumpPcmFramesFromFile(string wavFile, FrontendChannel channel)
        {
            var pcmFrames = GetWavFrames(wavFile);

            ulong nextAudioTimestamp = (ulong)DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            while (true)
            {
                for (int n = 0; n < pcmFrames.Count; n++)
                {
                    channel?.Send(new() {
                        OutboundMedia = new() {
                            SourceId = 1,
                            AttachmentMetadata = new() {
                                EncodedAudio = new() {
                                    Codec = AudioCodec.Opus,
                                    TimestampUs = nextAudioTimestamp * 1000,
                                }
                            }
                        }
                    },
                    new BytesAttachment(pcmFrames[n]));

                    Console.WriteLine($"Sending {pcmFrames[n].Length} bytes");

                    nextAudioTimestamp += FRAME_SIZE_MS;

                    ulong cur = (ulong)DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    if (nextAudioTimestamp > cur)
                    {
                        Thread.Sleep((int)(nextAudioTimestamp - cur));
                    }
                }
            }
        }

        internal static List<byte[]> GetWavFrames(string filePath)
        {
            List<byte[]> frames = new List<byte[]>();

            using (var reader = new WaveFileReader(filePath))
            {
                var encoder = new OpusEncoder(reader.WaveFormat.SampleRate, reader.WaveFormat.Channels, OpusApplication.OPUS_APPLICATION_VOIP);

                byte[] pcmData = new byte[reader.Length];
                int bytesRead = reader.Read(pcmData, 0, pcmData.Length);

                // Encode PCM data to Opus
                int frameSize = (int)(reader.WaveFormat.SampleRate * (FRAME_SIZE_MS / 1000.0)); // 20ms frame size for 48kHz (48 samples per ms * 20 ms)
                int maxPacketSize = 4000; // A reasonable maximum packet size for Opus

                for (int i = 0; i < pcmData.Length; i += frameSize * sizeof(short) * reader.WaveFormat.Channels)
                {
                    if (i + frameSize * sizeof(short) * 2 > pcmData.Length)
                        break; // Ensure we don't go out of bounds

                    byte[] frame = new byte[frameSize * sizeof(short) * reader.WaveFormat.Channels];
                    Array.Copy(pcmData, i, frame, 0, frame.Length);

                    // Concentus encoder expects short[] input
                    short[] framesInShort = new short[frame.Length / reader.WaveFormat.Channels];
                    Buffer.BlockCopy(frame, 0, framesInShort, 0, frame.Length);

                    byte[] encodedFrame = new byte[maxPacketSize];
                    int encodedLength = encoder.Encode(framesInShort, 0, frameSize, encodedFrame, 0, encodedFrame.Length);

                    var opusBytes = new byte[encodedLength];
                    Array.Copy(encodedFrame, opusBytes, encodedLength);

                    frames.Add(opusBytes);
                }
            }

            return frames;
        }
    }
}
