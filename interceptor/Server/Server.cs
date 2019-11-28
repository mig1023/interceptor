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

                    string responce = ClientWork(receviedLine.ToString(), "1.1.1.1");

                    data = Encoding.Unicode.GetBytes(responce);
                    SocketReceive.Send(data);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private byte[] EncodeToUTF8(string line)
        {
            Encoding utf8 = Encoding.GetEncoding("UTF-8");
            Encoding win1251 = Encoding.GetEncoding("Windows-1251");

            byte[] win1251Bytes = win1251.GetBytes(line);

            return Encoding.Convert(win1251, utf8, win1251Bytes);
        }

        private void SendResponse(TcpClient Client, string line)
        {
            Log.Add("отправлен ответ: " + line);

            byte[] Buffer = EncodeToUTF8(line);

            Client.GetStream().Write(Buffer, 0, Buffer.Length);

            Client.Close();
        }

        public static string ClientWork(string request, string clientIP)
        {
            request = Uri.UnescapeDataString(request);

            Log.Add(request, logType: "http");

            bool emplyRequest = (String.IsNullOrEmpty(request) ? true : false);

            return ResponsePrepare(request, emplyRequest, clientIP);
        }

        public static void ShowTotal(string summ)
        {
            Application.Current.Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                MainWindow main = (MainWindow)Application.Current.MainWindow;
                main.total.Content = "сумма: " + summ;
                main.totalR.Content = main.total.Content;
                main.totalContent.Visibility = Visibility.Visible;
                main.totalRContent.Visibility = Visibility.Visible;
            }));
        }

        public static string ResponsePrepare(string request, bool empty, string clientIP)
        {
            string err = String.Empty;

            if (empty)
            {
                Log.Add("пустой запрос с " + clientIP);
                CRM.SendError("Пустой запрос с " + clientIP);

                return "403";
            }
            else if (!CheckRequest.CheckValidRequest(request))
            {
                Log.Add("невалидный запрос " + clientIP);
                CRM.SendError("Невалидный запрос с " + clientIP);

                return "404";
            }

            if (!CheckRequest.CheckLoginInRequest(request, out err))
            {
                Log.Add("конфликт логинов: " + err);
                CRM.SendError("Конфликт логинов: " + err);

                return "ERR3:Кассовая программа запущена пользователем " + CRM.currentLogin;
            }

            if (CheckRequest.CheckConnection(request))
            {
                Log.Add("бип-тест подключения к кассе");

                string testResult = Diagnostics.MakeBeepTest();

                return testResult;
            }

            if (!CheckRequest.CheckXml(request))
            {
                Log.Add("CRC ошибочна, возвращаем ошибку данных");
                CRM.SendError("CRC ошибка");

                return "ERR1:Ошибка переданных данных";
            }
            else
            {
                Log.Add("CRC запроса корректна, логин соответствует");

                DocPack docPack = new DocPack();

                docPack.DocPackFromXML(request);

                if (!String.IsNullOrWhiteSpace(docPack.AgrNumber))
                    Log.Add("номер договора: " + docPack.AgrNumber);

                MainWindow.Cashbox.manDocPackForPrinting = docPack;

                if (docPack.RequestOnly == 1)
                {
                    MainWindow.Cashbox.manDocPackSumm = docPack.Total;

                    ShowTotal(docPack.Total.ToString());

                    return "OK:Callback запрос получен";
                }
                else
                    return MainWindow.Cashbox.PrintDocPack(docPack);
            }
        }
    }
}
