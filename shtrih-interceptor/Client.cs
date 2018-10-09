using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace shtrih_interceptor
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
            Log.add("отправлен ответ: " + line);

            byte[] Buffer = EncodeToUTF8(line);

            Client.GetStream().Write(Buffer, 0, Buffer.Length);

            Client.Close();
        }

        public Client(TcpClient Client)
        {
            string Request = "";
            byte[] Buffer = new byte[1024];
            int Count;

            while ((Count = Client.GetStream().Read(Buffer, 0, Buffer.Length)) > 0)
            {
                Request += Encoding.ASCII.GetString(Buffer, 0, Count);

                if (Request.IndexOf("\r\n\r\n") >= 0) break;
            }

            Request = Uri.UnescapeDataString(Request);

            Log.add(Request, logType: "http");

            Match ReqMatch = Regex.Match(Request, @"message=([^;]+?);");

            string response = responsePrepare(ReqMatch.Groups[1].Value);

            SendResponse(Client, response);

            Log.add("закрываем соединение");

            Client.Close();
        }

        public static string responsePrepare(string request)
        {
            if (CheckRequest.CheckConnection(request))
            {
                Log.add("бип-тест подключения к кассе");

                string testResult = Diagnostics.makeBeepTest();

                Server.ShowActivity(false);

                return testResult;
            }

            if (!CheckRequest.CheckXml(request))
            {
                Log.add("md5 ошибочен, возвращаем ошибку данных");

                return "ERR1:Ошибка переданных данных";
            }
            else
            {
                Log.add("md5 запроса корректен");

                DocPack docPack = new DocPack();

                docPack.DocPackFromXML(request);

                return Cashbox.printDocPack(docPack);
            }
                
        }
    }
}
