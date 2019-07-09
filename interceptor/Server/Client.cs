using System;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Threading;

namespace interceptor
{
    class Client
    {
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

        public Client(TcpClient Client)
        {
            string Request = String.Empty;
            byte[] Buffer = new byte[1024];
            int Count;

            while ((Count = Client.GetStream().Read(Buffer, 0, Buffer.Length)) > 0)
            {
                Request += Encoding.ASCII.GetString(Buffer, 0, Count);

                if (Request.IndexOf("\r\n\r\n") >= 0)
                    break;
            }

            Request = Uri.UnescapeDataString(Request);

            Log.Add(Request, logType: "http");

            Match ReqMatch = Regex.Match(Request, @"message=([^;]+?);");

            string response = ResponsePrepare(ReqMatch.Groups[1].Value);

            SendResponse(Client, response);

            Log.Add("соединение закрыто");

            Client.Close();
        }

        public static void ShowTotal(string summ)
        {
            Application.Current.Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                MainWindow main = (MainWindow)Application.Current.MainWindow;
                main.total.Content = "сумма: " + summ;
                main.totalR.Content = main.total.Content;
            }));
        }

        public static string ResponsePrepare(string request)
        {
            string err = String.Empty;

            if (!CheckRequest.CheckLoginInRequest(request, out err))
            {
                Log.Add("конфликт логинов: " + err);

                CRM.SendError("Конфликт логинов: " + err);

                Server.ShowActivity(busy: false);

                return "ERR3:Кассовая программа запущена пользователем " + CRM.currentLogin;
            }

            if (CheckRequest.CheckConnection(request))
            {
                Log.Add("бип-тест подключения к кассе");

                string testResult = Diagnostics.MakeBeepTest();

                Server.ShowActivity(busy: false);

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

                if (docPack.RequestOnly == 1)
                {
                    MainWindow.Cashbox.manDocPackForPrinting = docPack;
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
