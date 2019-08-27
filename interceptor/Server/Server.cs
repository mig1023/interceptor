using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;
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

            Log.Add("новое соединение c " + RemoteEndPoint, freeLine: true);

            new Client((TcpClient)state, RemoteEndPoint);
        }

        public static void StartServer()
        {
            asynchServ.DoWork += worker_DoWork;
            asynchServ.RunWorkerAsync();
        }

        private static void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Log.Add("сервер запущен");

            new Server(MainWindow.PROTOCOL_PORT);
        }

        ~Server()
        {
            if (Listener != null)
            {
                Listener.Stop();
                Log.Add("сервер остановлен");
            }
        }
    }
}
