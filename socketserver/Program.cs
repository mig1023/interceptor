using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace socketserver
{
    class Program
    {
        static string ipServer = "127.0.0.1";
        static int portReceive = 80;
        static int portSend = 80;

        static void Main(string[] args)
        {
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(ipServer), portReceive);
            Socket SocketReceive = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            SocketReceive.Bind(ipPoint);
            SocketReceive.Listen(10);

            while (true)
            {
                try
                {
                    Socket received = SocketReceive.Accept();

                    StringBuilder receviedLine = new StringBuilder();
                    int bytes = 0;
                    byte[] data = new byte[256];

                    do
                    {
                        bytes = received.Receive(data);
                        receviedLine.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (received.Available > 0);

                    Console.WriteLine(receviedLine.ToString());

                    string message = "OK|received";
                    data = Encoding.Unicode.GetBytes(message);
                    received.Send(data);

                    received.Shutdown(SocketShutdown.Both);
                    received.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }
}
