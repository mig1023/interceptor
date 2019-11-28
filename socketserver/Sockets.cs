using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace socketserver
{
    class Sockets
    {
        public Dictionary<string, Socket> SocketsPool = new Dictionary<string, Socket>();

        public Sockets(string ipServer, int port, bool sender = false)
        {
            new Thread(() =>
            {
                Console.WriteLine("start socket " + ipServer + ":" + port.ToString());

                IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(ipServer), port);
                Socket SocketReceive = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                SocketReceive.Bind(ipPoint);
                SocketReceive.Listen(10);

                while (true)
                {
                    try
                    {
                        Socket received = SocketReceive.Accept();

                        receivedServer(received, sender);
                    }
                    catch (SocketException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }).Start();
        }

        private void receivedServer(Socket received, bool sender = false)
        {
            StringBuilder receviedHello = new StringBuilder();
            int bytes = 0;
            byte[] data = new byte[256];

            do
            {
                bytes = received.Receive(data);
                receviedHello.Append(Encoding.Unicode.GetString(data, 0, bytes));
            }
            while (received.Available > 0);

            Console.WriteLine("HELLO: " + receviedHello.ToString());

            SocketsPool[receviedHello.ToString()] = received;

            if (!sender)
                while (received.Connected)
                {
                    StringBuilder receviedLine = new StringBuilder();
                    bytes = 0;
                    data = new byte[256];

                    try
                    {
                        do
                        {
                            bytes = received.Receive(data);
                            receviedLine.Append(Encoding.Unicode.GetString(data, 0, bytes));
                        }
                        while (received.Available > 0);

                        Console.WriteLine(" ");
                        Console.WriteLine("---> " + receviedLine.ToString());

                        string message = SendToCRM(receviedLine.ToString());

                        Console.WriteLine("<--- " + message);

                        data = Encoding.Unicode.GetBytes(message);
                        received.Send(data);
                    }
                    catch (SocketException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
        }

        public static string SendToCRM(string url)
        {
            string response = String.Empty;

            try
            {
                response = GetHtml(Program.serverCRM + url);
            }
            catch (WebException e)
            {
                return String.Empty;
            }

            return response;
        }

        public static string GetHtml(string url)
        {
            WebClient client = new WebClient();
            using (Stream data = client.OpenRead(url))
            {
                using (StreamReader reader = new StreamReader(data))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
