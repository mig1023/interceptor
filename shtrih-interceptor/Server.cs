using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace interceptor
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

            Log.add("новое соединение c " + RemoteEndPoint, freeLine: true);

            ShowActivity(busy: true);

            new Client((TcpClient)state);
        }

        public static void ShowActivity(bool busy)
        {
            Application.Current.Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                MainWindow main = (MainWindow)Application.Current.MainWindow;
                if (busy)
                    main.activity.Background = Brushes.Orange;
                else
                    main.activity.Background = Brushes.Gray;
            }));
        }

        public static void StartServer()
        {
            asynchServ.DoWork += worker_DoWork;
            asynchServ.RunWorkerAsync();
        }

        private static void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            new Server(80);

            Log.add("сервер запущен");
        }

        ~Server()
        {
            if (Listener != null)
            {
                Listener.Stop();
                Log.add("сервер остановлен");
            }
        }
    }
}
