using System.Runtime.InteropServices;

namespace TestACSAudioCodec
{
    public class Program
    {
        [DllImport("ACSAudioCodecs.dll", CallingConvention = CallingConvention.Cdecl)] public static extern void Decode(byte[] buffer, int length);
        static async Task Main(string[] args)
        {
            byte[] byteArray = { 0x01, 0x02, 0x03, 0x04, 0x05 };

            //Console.ForegroundColor = ConsoleColor.Yellow;
            Decode(byteArray, byteArray.Length);
        }
    }
}
