using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WinDivertSharp;
using WinDivertSharp.WinAPI;

namespace WinCapture
{
    class Program
    {
        private static volatile bool s_running = true;

        static void Main(string[] args)
        {

            var ip = System.Configuration.ConfigurationManager.AppSettings["ip"];
            var port = System.Configuration.ConfigurationManager.AppSettings["port"];

            Console.CancelKeyPress += ((sender, eArgs) =>
            {
                s_running = false;
            });

            string filter = $"tcp and tcp.DstPort == {port}";

            uint errorPos = 0;
            if (!WinDivert.WinDivertHelperCheckFilter(filter, WinDivertLayer.Network, out string errorMsg, ref errorPos))
            {
                Console.WriteLine($"Error in filter string at position {errorPos}");
                Console.WriteLine(errorMsg);
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }


            var handle = WinDivert.WinDivertOpen(filter, WinDivertLayer.Network, 0, WinDivertOpenFlags.None);

            if (handle == IntPtr.Zero || handle == new IntPtr(-1))
            {
                Console.WriteLine("Invalid handle. Failed to open.");
                Console.ReadKey();
                return;
            }

            // Set everything to maximum values.
            WinDivert.WinDivertSetParam(handle, WinDivertParam.QueueLen, 16384);
            WinDivert.WinDivertSetParam(handle, WinDivertParam.QueueTime, 8000);
            WinDivert.WinDivertSetParam(handle, WinDivertParam.QueueSize, 33554432);

            var threads = new List<Thread>();

            for (int i = 0; i < Environment.ProcessorCount; ++i)
            {
                threads.Add(new Thread(() =>
                {
                    RunDiversion(handle);
                }));

                threads.Last().Start();
            }

            foreach (var dt in threads)
            {
                dt.Join();
            }

            WinDivert.WinDivertClose(handle);


        }


        private static void RunDiversion(IntPtr handle)
        {
            var packet = new WinDivertBuffer();
            var addr = new WinDivertAddress();
            uint readLen = 0;

            NativeOverlapped recvOverlapped;
            IntPtr recvEvent = IntPtr.Zero;
            uint recvAsyncIoLen = 0;

            do
            {
                if (s_running)
                {
                    readLen = 0;

                    recvAsyncIoLen = 0;
                    recvOverlapped = new NativeOverlapped();
                    recvEvent = Kernel32.CreateEvent(IntPtr.Zero, false, false, IntPtr.Zero);

                    if (recvEvent == IntPtr.Zero)
                    {
                        Console.WriteLine("Failed to initialize receive IO event.");
                        continue;
                    }
                    addr.Reset();

                    recvOverlapped.EventHandle = recvEvent;

                    if (!WinDivert.WinDivertRecvEx(handle, packet, 0, ref addr, ref readLen, ref recvOverlapped))
                    {
                        var error = Marshal.GetLastWin32Error();

                        // 997 == ERROR_IO_PENDING
                        if (error != 997)
                        {
                            Console.WriteLine(string.Format("Unknown IO error ID {0} while awaiting overlapped result.", error));
                            Kernel32.CloseHandle(recvEvent);
                            continue;
                        }

                        while (Kernel32.WaitForSingleObject(recvEvent, 1000) == (uint)WaitForSingleObjectResult.WaitTimeout)
                            ;

                        if (!Kernel32.GetOverlappedResult(handle, ref recvOverlapped, ref recvAsyncIoLen, false))
                        {
                            Console.WriteLine("Failed to get overlapped result.");
                            Kernel32.CloseHandle(recvEvent);
                            continue;
                        }

                        readLen = recvAsyncIoLen;
                    }

                    Kernel32.CloseHandle(recvEvent);


                    packet = ModifyPkt(packet, readLen);


                    if (!WinDivert.WinDivertSendEx(handle, packet, readLen, 0, ref addr))
                    {
                        Console.WriteLine("Write Err: {0}", Marshal.GetLastWin32Error());
                    }
                }
            }
            while (s_running);
        }


        private static WinDivertBuffer ModifyPkt(WinDivertBuffer packet, uint readLen)
        {
            var byts = new List<byte>();
            for (var i = 40; i < readLen; i++)
            {
                byts.Add(packet[i]);
            }

            Console.WriteLine($"Data => { Encoding.ASCII.GetString(byts.ToArray()) }");
            byts.Reverse();
            Console.WriteLine($"Converted to => { Encoding.ASCII.GetString(byts.ToArray()) }");

            for (var i = 40; i < readLen; i++)
            {
                packet[i] = byts[i - 40];
            }
            return packet;
        }
    }
}
