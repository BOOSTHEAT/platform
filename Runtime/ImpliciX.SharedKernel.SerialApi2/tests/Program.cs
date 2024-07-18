// See https://aka.ms/new-console-template for more information

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using ImpliciX.SharedKernel.SerialApi2.SerialPort;

namespace ImpliciX.SharedKernel.SerialApi2.Demo
{
    public static class Program
    {
        public static void Main(params string[] args)
        {
            Console.WriteLine(Environment.GetEnvironmentVariable("LD_LIBRARY_PATH"));
            var port = new BhSerialPort("/tmp/boilerAppMotor", 115200, 8, Parity.None, StopBits.One);
            port.ReadTimeout = 50;
            port.WriteTimeout = 50;
            var req = Encoding.ASCII.GetBytes(BuildReadRequest());
            Console.WriteLine($"Is Open: {port.IsOpen}");
            port.Open();
            Console.WriteLine($"Is Open: {port.IsOpen}");
            Console.ReadLine();
            int i = 0;
            var sw = new Stopwatch();
            while (true)
            {
                
                sw.Restart();
                var rsp = new byte[255];
                port.DiscardInBuffer();
                port.DiscardOutBuffer();
                port.Write(req, 0, req.Length);    
                port.Read(rsp, 0, rsp.Length);
                sw.Stop();
                
                Print(rsp, ++i, sw.Elapsed);
                Thread.Sleep(500);
            }
        }

        private static string BuildReadRequest()
        {
            var payload = "01REA BSP";
            var checksum = Checksum(payload);
            var byteCount = payload.Length.ToString("D3");;
            return $"{b(STX)}{byteCount}{payload}{checksum}{b(ETX)}";
        }
        
        public static readonly Func<byte, string> b = e => Encoding.ASCII.GetString(new[] {e});
        private static void Print(byte[] buffer, int n, TimeSpan elapsed)
        {
            var str = string.Join(",",buffer.Select(o=>((int)o).ToString()));
            Console.WriteLine($"Trame {n}");
            Console.WriteLine(str);
            Console.WriteLine($"In {elapsed.TotalMilliseconds} ms");
        }
        
        public static string Checksum(string frameAddressAndPayload) =>
            (frameAddressAndPayload.Aggregate(0, (i, c) => i + c) % 256).ToString("X2");
        
        public static byte STX = 0x02;
        public static byte ETX = 0x03;
        public static byte ACK = 0x06;
        public static byte NACK = 0x15;
        public static byte XONERROR = 0x17;
        public static byte XON = 0x1A;
    }
}


