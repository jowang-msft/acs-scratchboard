using System.Runtime.InteropServices;

namespace TestAeTestDLL
{
    public static class NativeMethods
    {
        [DllImport("aetest.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AESendConstruct();

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
            IntPtr aesendPtr = NativeMethods.AESendConstruct();
            int result = NativeMethods.Init(aesendPtr, 102, 36000, 20, 16, 1, 16000, 0);

            byte[] pcmData = new byte[] { 0x00, 0xFF, 0x7A, 0x42 };
            int encodedPayloadLength = 10;
            byte[] encodedPayload = new byte[encodedPayloadLength];
            int encodedRedundantPayloadLength = 10;
            byte[] encodedRedundantPayload = new byte[encodedRedundantPayloadLength];

            result = NativeMethods.ProcessFrame(aesendPtr, pcmData, pcmData.Length, 0, 1000, encodedPayload, ref encodedPayloadLength, encodedRedundantPayload, ref encodedRedundantPayloadLength, out int payloadType);

            // Free the allocated memory
            Marshal.FreeHGlobal(aesendPtr);
        }
    }
}
