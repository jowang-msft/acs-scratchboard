using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using NAudio.Wave;

// Media format can be found at:
// https://skype.visualstudio.com/SCC/_search?action=contents&text=OPUS%20%3D%20102%2C&type=code&lp=code-Project&filters=ProjectFilters%7BSCC%7D&pageSize=25&result=DefaultCollection/SCC/media_stack_all/GBmaster//src/audio/inc/AudioPayloadEnum.h

namespace TestAeTestDLL
{
    public static class NativeMethods
    {
        // RtmPal.dll
        [DllImport("RtmPal.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern ulong RtcPalStartup();

        [DllImport("RtmPal.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern void RtcPalCleanup();

        [DllImport("RtmPal.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern ulong RtcPalGetTimeLongIn100ns();

        // aetest.dll
        [DllImport("aetest.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AESendConstruct();

        [DllImport("aetest.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr decoder_construct();

        [DllImport("aetest.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr encoder_construct();

        [DllImport("aetest.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int encoder_select(
            IntPtr encoder,
            int adspPayloadType,
            int bitrate,
            int ptime,
            int channels);

        [DllImport("aetest.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int encoder_get_sampling_rate(IntPtr encoder);

        [DllImport("aetest.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int encoder_encode(
            IntPtr encoder,
            byte[] pcm,
            int pcmLength,
            byte[] encodedData,
            ref int encodedDataLength,
            byte[] redEncodedData,
            ref int redEncodedDataLength);


        [DllImport("aetest.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int decoder_select(
            IntPtr decoder,
            int adspPayloadType);

        [DllImport("aetest.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int decoder_get_sampling_rate(IntPtr decoder);

        [DllImport("aetest.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int decoder_decode(
            IntPtr decoder,
            byte[] encodedData,
            int encodedDataLength,
            byte[] pcmData,
            ref int pcmDataLength);

        [DllImport("aetest.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Init(
            IntPtr aeSend,
            int payloadType,
            int bitRate,
            int ptime,
            int bitsPerSample,
            int channels,
            int samplesPerSec,
            ulong timestamp);

        [DllImport("aetest.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ProcessFrame(
            IntPtr aeSend,
            byte[] pcmInput,
            int pcmInputLength,
            int silentFrame,
            ulong currentTimeStamp100ns,
            byte[] encodedPayload,
            ref int encodedPayloadLength,
            byte[] encodedRedundantPayload,
            ref int encodedRedundantPayloadLength,
            out int payloadType);
    }

    public class Program
    {
        static async Task Main(string[] args)
        {
            NativeMethods.RtcPalStartup(); // This is needed for using debug build of MSRTC dlls

            int result = 0;
            int sampleRate = 0;

            string path = @"testaudio.wav";
            using (var reader = new WaveFileReader(path))
            {
                IntPtr encoder = NativeMethods.encoder_construct();
                
                if (reader.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
                {
                    result = NativeMethods.encoder_select(
                        encoder,
                        102, // Opus
                        64000, // https://skype.visualstudio.com/SCC/_search?action=contents&text=510000&type=code&lp=code-Project&filters=ProjectFilters%7BSCC%7D&pageSize=25&result=DefaultCollection/SCC/media_stack_webrtc-src/GBmaster//api/audio_codecs/opus/audio_encoder_opus_config.h
                        1000,
                        reader.WaveFormat.Channels);

                    sampleRate = NativeMethods.encoder_get_sampling_rate(encoder);

                    int bytesPerFrame = (int)(sampleRate * 0.02 * reader.WaveFormat.BitsPerSample/8 * reader.WaveFormat.Channels); // 20ms per frame
                    byte[] frameBuffer = new byte[bytesPerFrame];

                    while(reader.Read(frameBuffer, 0, bytesPerFrame) > 0)
                    {
                        byte[] encoded = new byte[1000];
                        int encodedLength = encoded.Length;
                        byte[] redudantEncoded = new byte[1000];
                        int redudantEncodedLength = redudantEncoded.Length;

                        result = NativeMethods.encoder_encode(encoder, frameBuffer, frameBuffer.Length, encoded, ref encodedLength, redudantEncoded, ref redudantEncodedLength);
                    }
                }
            }

            IntPtr decoder = NativeMethods.decoder_construct();
            result = NativeMethods.decoder_select(decoder, 102);
            sampleRate = NativeMethods.decoder_get_sampling_rate(decoder);

            var frames = new List<byte[]>();
            byte[] oggContents = await File.ReadAllBytesAsync(@"testaudio.opus");
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
                //int version = BitConverter.ToInt32(oggContents, 0);
                //int channels = oggContents[4];
                //ushort preSkip = BitConverter.ToUInt16(oggContents, 4);
                //uint inputSampleRate = BitConverter.ToUInt32(oggContents, 9);
                //ushort outputGain = BitConverter.ToUInt16(oggContents, 16);
                //byte channelMapping = oggContents[18];

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
                        byte[] segment;

                        if (appendToLastSegmentOfPreviousPage)
                        {
                            Console.WriteLine($"Continuation");
                            int frameCnt = frames.Count;
                            byte[] lastSegment = frames[frameCnt - 1];
                            frames.RemoveAt(frameCnt - 1);

                            segment = new byte[lastSegment.Length + segmentLen];
                            Array.Copy(lastSegment, 0, segment, lastSegment.Length, lastSegment.Length);
                            Array.Copy(oggContents, segmentOffset, segment, lastSegment.Length, segmentLen);
                            //frames.Add(segment);
                        }
                        else
                        {
                            segment = new byte[segmentLen];
                            Array.Copy(oggContents, segmentOffset, segment, 0, segmentLen);
                            //frames.Add(segment);
                        }

                        byte[] pcmBytes = new byte[15000];
                        int pcmLen = 15000;
                        result = NativeMethods.decoder_decode(decoder, segment, segment.Length, pcmBytes, ref pcmLen);
                        frames.Add(pcmBytes);
                    }

                    appendToLastSegmentOfPreviousPage = false;
                    segmentOffset += segmentLen;
                }

                oggOffset = segmentOffset;
            }


            //IntPtr aesendPtr = NativeMethods.AESendConstruct();
            //int result = NativeMethods.Init(aesendPtr, 102, 36000, 20, 16, 1, 16000, 0);

            //byte[] pcmData = new byte[] { 0x00, 0xFF, 0x7A, 0x42 };
            //int encodedPayloadLength = 10;
            //byte[] encodedPayload = new byte[encodedPayloadLength];
            //int encodedRedundantPayloadLength = 10;
            //byte[] encodedRedundantPayload = new byte[encodedRedundantPayloadLength];

            //result = NativeMethods.ProcessFrame(aesendPtr, pcmData, pcmData.Length, 0, 1000, encodedPayload, ref encodedPayloadLength, encodedRedundantPayload, ref encodedRedundantPayloadLength, out int payloadType);

            //// Free the allocated memory
            //Marshal.FreeHGlobal(aesendPtr);

            NativeMethods.RtcPalCleanup();
        }
    }
}
