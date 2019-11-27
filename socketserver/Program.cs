using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace socketserver
{
    class Program
    {
        // public static Socket Socket2Send = null;

        public static Dictionary<string, Socket> Sockets2Send = new Dictionary<string, Socket>();

        public static string ipServer = "127.0.0.1";
        public static int portReceive = 80;
        public static int portSend = 80;
        public static int portServerCRM = 80;

        static string serverCRM = "http://" + "127.0.0.1";

        static void Main(string[] args)
        {
            IPAddress ip = Dns.GetHostByName(Dns.GetHostName()).AddressList[0];
            Console.WriteLine(ip.ToString());

            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(ipServer), portReceive);
            Socket SocketReceive = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            SocketReceive.Bind(ipPoint);
            SocketReceive.Listen(10);

            new Thread(() =>
            {

                try
                {
                    ipPoint = new IPEndPoint(IPAddress.Parse(ipServer), portSend);
                    Socket SocketSend = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    SocketSend.Bind(ipPoint);
                    SocketSend.Listen(10);
                    Socket Socket2Send = SocketSend.Accept();

                    while (true)
                    {

                        StringBuilder receviedLine = new StringBuilder();
                        int bytes = 0;
                        byte[] data = new byte[256];

                        do
                        {
                            bytes = Socket2Send.Receive(data);
                            receviedLine.Append(Encoding.Unicode.GetString(data, 0, bytes));
                        }
                        while (Socket2Send.Available > 0);

                        Sockets2Send[receviedLine.ToString()] = Socket2Send;
                    };
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e.Message);
                }
            }).Start();

            Server.StartServer();

            while (true)
            {
                try
                {
                    Socket received = SocketReceive.Accept();

                    new Thread(() => receivedServer(received)).Start();
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private static void receivedServer(Socket received)
        {
            while (received.Connected)
            {
                StringBuilder receviedLine = new StringBuilder();
                int bytes = 0;
                byte[] data = new byte[256];

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
                response = GetHtml(serverCRM + url);
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
