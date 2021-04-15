using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var ip = System.Configuration.ConfigurationManager.AppSettings["ip"];
            var port = System.Configuration.ConfigurationManager.AppSettings["port"];

            ExecuteServer(ip, port);
        }



        public static void ExecuteServer(string ip, string port)
        {
            // Establish the local endpoint
            // for the socket. Dns.GetHostName
            // returns the name of the host
            // running the application.

            IPAddress ipAddr = IPAddress.Parse("0.0.0.0");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddr, Convert.ToInt32(port));

            // Creation TCP/IP Socket using
            // Socket Class Costructor
            Socket listener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {

                // Using Bind() method we associate a
                // network address to the Server Socket
                // All client that will connect to this
                // Server Socket must know this network
                // Address
                listener.Bind(localEndPoint);

                // Using Listen() method we create
                // the Client list that will want
                // to connect to Server
                listener.Listen(10);

                while (true)
                {

                    Console.WriteLine("Waiting connection ... ");

                    // Suspend while waiting for
                    // incoming connection Using
                    // Accept() method the server
                    // will accept connection of client
                    Socket clientSocket = listener.Accept();

                    while (true)
                    {
                        // Data buffer
                        byte[] bytes = new Byte[1024];
                        string data = null;


                        int numByte = clientSocket.Receive(bytes);
                        if (numByte == 0)
                            break;
                        data = Encoding.ASCII.GetString(bytes, 0, numByte);
                        //if (data.IndexOf("<EOF>") > -1 || data.IndexOf("\n") > -1)
                        //    break;
                        //if (bytes.Contains((byte)3))
                        //    break;



                        Console.WriteLine($"Text received {DateTime.Now} ->  {data} ");
                        byte[] message = Encoding.ASCII.GetBytes(data);


                        // Send a message to Client
                        // using Send() method
                        clientSocket.Send(message);

                        if (message.Contains((byte)3))
                            break;
                    }


                    // Close client Socket using the
                    // Close() method. After closing,
                    // we can use the closed Socket
                    // for a new Client Connection
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                }
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
