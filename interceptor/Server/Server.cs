using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace interceptor
{
    class Server
    {
        private static readonly BackgroundWorker asynchServ = new BackgroundWorker();

        public static Socket SocketReceive = null;

        public static void StartServer()
        {
            asynchServ.DoWork += worker_Server;
            asynchServ.RunWorkerAsync();
        }

        private static void worker_Server(object sender, DoWorkEventArgs e)
        {
            Log.Add("сервер запущен");

            while (true)
            {
                try
                {
                    StringBuilder receviedLine = new StringBuilder();
                    int bytes = 0;
                    byte[] data = new byte[256];

                    do
                    {
                        bytes = SocketReceive.Receive(data);
                        receviedLine.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (SocketReceive.Available > 0);

                    Log.Add("новый запрос " + receviedLine.ToString(), freeLine: true);

                    string responce = Client.ClientWork(receviedLine.ToString(), "1.1.1.1");

                    data = Encoding.Unicode.GetBytes(responce);
                    SocketReceive.Send(data);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
