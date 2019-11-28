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
        public static Sockets receiver;
        public static Sockets sender;

        static void Main(string[] args)
        {
            receiver = new Sockets(Secret.IP_SERVER, Secret.PORT_RECEIVE);
            sender = new Sockets(Secret.IP_SERVER, Secret.PORT_SEND, sender: true);

            Server.StartServer();
        }
    }
}