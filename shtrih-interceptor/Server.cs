using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace shtrih_interceptor
{
    class Server
    {
        TcpListener Listener;

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

            Log.add("новое соединение c " + RemoteEndPoint);

            new Client((TcpClient)state);
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
