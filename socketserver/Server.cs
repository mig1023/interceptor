using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;

namespace socketserver
{
    class Server
    {


        TcpListener Listener;

        private static readonly BackgroundWorker asynchServ = new BackgroundWorker();

        public Server(int port)
        {
            Listener = new TcpListener(IPAddress.Any, port);
            Listener.Start();

            while (true)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(ClientThread), Listener.AcceptTcpClient());
            }
        }

        private void ClientThread(object state)
        {
            string RemoteEndPoint = (state as TcpClient).Client.RemoteEndPoint.ToString();

            new Client((TcpClient)state, RemoteEndPoint);
        }

        public static void StartServer()
        {
            asynchServ.DoWork += worker_DoWork;
            asynchServ.RunWorkerAsync();
        }

        private static void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            new Server(Program.portServerCRM);
        }

        ~Server()
        {
            if (Listener != null)
            {
                Listener.Stop();
            }
        }
    }
}
