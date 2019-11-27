﻿using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace socketserver
{
    class Program
    {
        public static Socket SocketSend = null;
        public static Socket Socket2Send = null;

        public static string ipServer = "127.0.0.1";
        public static int portReceive = 80;
        public static int portSend = 80;
        public static int portServerCRM = 80;

        static string serverCRM = "http://" + "127.0.0.1";

        static void Main(string[] args)
        {
            IPAddress ip = Dns.GetHostByName(Dns.GetHostName()).AddressList[0];
            Console.WriteLine(ip.ToString());
            Console.WriteLine(portReceive.ToString());
            Console.WriteLine(portSend.ToString());


            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(ipServer), portReceive);
            Socket SocketReceive = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            SocketReceive.Bind(ipPoint);
            SocketReceive.Listen(10);

            new Thread(() =>
            {
                ipPoint = new IPEndPoint(IPAddress.Parse(ipServer), portSend);
                SocketSend = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                SocketSend.Bind(ipPoint);
                SocketSend.Listen(10);
                Socket2Send = SocketSend.Accept();
                Console.WriteLine("Socket2Send connected");
            }).Start();

            Server.StartServer();

            while (true)
            {
                try
                {
                    Socket received = SocketReceive.Accept();

                    new Thread(() =>
                    {
                        while (true)
                        {
                            Console.WriteLine("SocketReceive connected");

                            StringBuilder receviedLine = new StringBuilder();
                            int bytes = 0;
                            byte[] data = new byte[256];

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
                    }).Start();
                }
                catch (Exception e)
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
                //Log.AddWeb("(запрос данных) " + e.Message);

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
