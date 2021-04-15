using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpClient
{
    class Program
    {
        static void Main(string[] args)
        {
            //var ip = args[0];
            //var port = Convert.ToInt32(args[1]);

            var ip = System.Configuration.ConfigurationManager.AppSettings["ip"];
            var port = System.Configuration.ConfigurationManager.AppSettings["port"];


            Console.WriteLine($"{ip} : {port}");
            ExecuteClient(ip, Convert.ToInt32(port));
        }

        // ExecuteClient() Method
        static void ExecuteClient(string ip, int port)
        {

            try
            {
                IPAddress ipAddr = IPAddress.Parse(ip);
                IPEndPoint localEndPoint = new IPEndPoint(ipAddr, port);
                Socket sender = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    sender.Connect(localEndPoint);
                    Console.WriteLine("Socket connected to -> {0} ", sender.RemoteEndPoint.ToString());

                    while (true)
                    {
                        Console.WriteLine("Input:");
                        var txt = Console.ReadLine();
                        byte[] messageSent = Encoding.ASCII.GetBytes(txt);
                        int byteSent = sender.Send(messageSent);
                        byte[] messageReceived = new byte[1024];
                        int byteRecv = sender.Receive(messageReceived);
                        Console.WriteLine("Message from Server -> {0}", Encoding.ASCII.GetString(messageReceived, 0, byteRecv));

                        if (messageSent.Contains((byte)3))
                            break;
                    }

                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                }

                // Manage of Socket's Exceptions
                catch (ArgumentNullException ane)
                {

                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }

                catch (SocketException se)
                {

                    Console.WriteLine("SocketException : {0}", se.ToString());
                }

                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }
            }

            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
            }
        }
    }
}
