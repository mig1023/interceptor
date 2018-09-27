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
        

        private void SendResponse(TcpClient Client, string line)
        {
            Log.add("отправлен ответ: "+line);

            byte[] Buffer = Encoding.ASCII.GetBytes(line);

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

            Log.add(Request, "http");

            Match ReqMatch = Regex.Match(Request, @"message=([^;]+?);");

            string response = responsePrepare(ReqMatch.Groups[1].Value);

            SendResponse(Client, response);

            Log.add("закрываем соединение");

            Client.Close();
        }

        public static string responsePrepare(string request)
        {
            if (!CheckRequest.checkXml(request))
                return "ERROR: BROKEN DATA";

            else
            {
                DocPack docPack = new DocPack(request);

                Cashbox.printDocPack(docPack);

                return "OK";
            }
                
        }
    }
}
