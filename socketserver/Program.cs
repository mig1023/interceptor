using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;

namespace socketserver
{
    class Program
    {
        public static Sockets receiver;
        public static Sockets sender;

        public static string autorestartProcess = "socketserver-autorestart";

        static void Main(string[] args)
        {

            Log.Add("СОКЕТ-СЕРВЕР ЗАПУЩЕН", startServer: true);
            Console.WriteLine("\n\n\n\tСОКЕТ-СЕРВЕР ЗАПУЩЕН");

            Process proc = Process.GetCurrentProcess();

            Process[] procAutorestart = Process.GetProcessesByName(autorestartProcess);
            if (procAutorestart.Length == 0)
                Process.Start(autorestartProcess + ".exe", proc.ProcessName);

            receiver = new Sockets(Secret.IP_SERVER, Secret.PORT_RECEIVE);
            sender = new Sockets(Secret.IP_SERVER, Secret.PORT_SEND, sender: true);

            Server.StartServer();
        }
    }
}