using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Net.Sockets;

namespace interceptor
{
    class CRM
    {
        public static Socket SocketSend = null;
        
        public static int password = 0;
        public const int adminPassword = 30;

        public static string currentLogin = String.Empty;
        public static string currentPassword = String.Empty;
        public static string cashier = String.Empty;
        public static string loginError = String.Empty;

        private static Dictionary<string, string> rCenterNames = new Dictionary<string, string>()
        {
            ["Выездная биометрия"] = "ext_biometry",
            ["Дистанционное обслуживание"] = "dist_service",
            ["Moscow (м.Киевская выдача)"] = "msk_kiev_ext"
        };


        public static bool SocketsConnect()
        {
            if (!SocketConnect(Secret.PROTOCOL_IP_SERVER, Secret.PROTOCOL_PORT_SEND, "сокет отправки", out SocketSend))
                return false;

            SocketSend.Send(Encoding.Unicode.GetBytes(MainWindow.Cashbox.serialNumber));

            if (!SocketConnect(Secret.PROTOCOL_IP_SERVER, Secret.PROTOCOL_PORT_RECEIVE, "сокета сервера", out Server.SocketReceive))
                return false;

            Server.SocketReceive.Send(Encoding.Unicode.GetBytes(MainWindow.Cashbox.serialNumber));

            return true;
        }

        private static bool SocketConnect(string ip, int port, string typeLine, out Socket socket)
        {
            socket = null;

            try
            {
                IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(ip), port);
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                socket.Connect(ipPoint);
            }
            catch(SocketException e)
            {
                Log.Add(String.Format("{0}: {1}", typeLine, e.Message));
                socket = null;
                return false;
            }

            Log.Add(String.Format("{0}: подключено", typeLine));
            return true;
        }

        public static string SockectSend(string sending)
        {
            SocketSend.Send(Encoding.Unicode.GetBytes(sending));

            byte[] data = new byte[256];
            StringBuilder builder = new StringBuilder();

            do
            {
                builder.Append(Encoding.Unicode.GetString(data, 0, SocketSend.Receive(data, data.Length, 0)));
            }
            while (SocketSend.Available > 0);

            return builder.ToString();
        }

        public static bool CashboxSerialNumberIsOk(string serialNo)
        {
            string checkResponse = String.Empty;

            string url = "/vcs/cashbox_check_cashbox.htm?s=" + serialNo;

            try
            {
                checkResponse = SockectSend(url);
            }
            catch (SocketException e)
            {
                loginError = "Ошибка доступа к серверу";
                Log.AddWeb(e.Message);

                return false;
            }

            return (checkResponse == "OK" ? true : false);
        }

        public static bool CrmAuthentication(string login, string passwordLine, string serialNo)
        {
            string authString = String.Empty;

            string url = "/vcs/cashbox_auth_socket.htm?login=" + login +
                "&p=" + passwordLine + "&ip=" + GetMyIP() + "&s=" + serialNo +
                "&v=" + MainWindow.CURRENT_VERSION;

            try
            {
                authString = SockectSend(url);
            }
            catch (SocketException e)
            {
                loginError = "Ошибка доступа к серверу";
                Log.AddWeb(e.Message);

                return false;
            }

            string[] authData = authString.Split('|');

            int result = 0;

            if (authData[0] != "OK" || !Int32.TryParse(authData[1], out result))
            {
                loginError = authData[1];
                Log.Add(loginError);

                return false;
            }

            password = Int32.Parse(authData[1]);

            MainWindow.Cashbox.currentDirectPassword = password;

            cashier = authData[2];

            Log.Add("успешный вход: " + login + "/" + authData[1] + "(" + authData[2] + ")");

            return true;
        }

        public static ICashbox FindCashbox()
        {
            List<ICashbox> cashboxOrder = null;

            string currentCashbox = SaveLtlData.GetCurrentCashbox();

            if (String.IsNullOrEmpty(currentCashbox) || (currentCashbox == "Штрих"))
                cashboxOrder = new List<ICashbox>() { new ShtrihM(), new Atol() };
            else
                cashboxOrder = new List<ICashbox>() { new Atol(), new ShtrihM() };

            foreach (ICashbox cashbox in cashboxOrder)
            {
                Log.Add(String.Format("ищем кассу {0}", cashbox.Name()));

                cashbox.CheckConnection();

                if (cashbox.GetResultCode() == 0)
                {
                    SaveLtlData.SaveCurrentCashbox(cashbox.Name());
                    return cashbox;
                }
                else
                    SaveLtlData.RemoveCurrentCashbox(cashbox.Name());
            }

            return null;
        }

        public static string[] GetAllCenters(string login)
        {
            string centerString = String.Empty;

            string url = "/vcs/cashbox_centers.htm?login=" + login;

            try
            {
                centerString = SockectSend(url);
            }
            catch (SocketException e)
            {
                Log.AddWeb("(центры) " + e.Message);

                return null;
            }

            return centerString.Split('|');
        }

        public static string[] GetAllVType(string vcenterName)
        {
            string vtypeString = String.Empty;

            string url = "/vcs/cashbox_vtype.htm?center=" + vcenterName;

            try
            {
                vtypeString = SockectSend(url);
            }
            catch (SocketException e)
            {
                Log.AddWeb("(типы виз) " + e.Message);

                return null;
            }

            return vtypeString.Split('|');
        }

        public static string[] GetFDData(uint startFD, int type)
        {
            string FDData = String.Empty;

            string url = "/vcs/cashbox_fd.htm?first=" + startFD.ToString() + "&type=" + type.ToString();

            try
            {
                FDData = SockectSend(url);
            }
            catch (SocketException e)
            {
                Log.AddWeb("(запрос данных) " + e.Message);

                return null;
            }

            return FDData.Split('|');
        }

        public static void SendError(string error, string agrNumber = "")
        {
            string requestResult = String.Empty;

            string fields =
                "login=" + currentLogin + "&error=" + error + "&ip=" + GetMyIP() + "&agr=" + agrNumber;

            string url = "/vcs/cashbox_error.htm?" + fields;

            Log.Add(url, logType: "http");

            try
            {
                requestResult = SockectSend(url);
            }
            catch (SocketException e)
            {
                Log.AddWeb("(отправка информации об ошибке) " + e.Message);
            }
        }

        public static string GetMyIP()
        {
            IPAddress ip = Dns.GetHostByName(Dns.GetHostName()).AddressList[0];

            return ip.ToString();
        }

        public static string AppNumberData(string appNumber, string summ)
        {
            string requestResult = String.Empty;

            string appNumberClean = Regex.Replace(appNumber, @"[^0-9]", String.Empty);

            string fields = "app=" + appNumberClean + "&summ=" + summ;

            string request = fields + "&crc=" + CheckRequest.CreateMD5(fields, notOrd: true);

            string url = "/vcs/cashbox_appinfo.htm?" + request;

            Log.Add(url, logType: "http");

            try
            {
                requestResult = SockectSend(url);
            }
            catch (SocketException e)
            {
                Log.AddWeb("(отправка запроса на информацию о записи) " + e.Message);

                return "ERROR|Ошибка отправки запроса на информацию о записи";
            }

            return requestResult;
        }

        private static string RCenterNamesExclusion(string centerName)
        {
            foreach (KeyValuePair<string, string> entry in rCenterNames)
                if (entry.Key == centerName)
                    return entry.Value;

            return centerName;
        }

        public static string SendManDocPack(string login, int password, int moneyType, string money,
            string center, string vType, string returnDate, bool reception = false, bool withoutApp = false)
        {
            string requestResult = String.Empty;

            string servicesList = ManualDocPack.AllServices();

            Log.Add("запрос на чек: " + servicesList, freeLine: true);

            string fields =
                "login=" + login + "&pass=" + password.ToString() + "&moneytype=" + moneyType.ToString() +
                "&money=" + money + "&center=" + RCenterNamesExclusion(center) + "&vtype=" + vType +
                "&rdate=" + returnDate + "&services=" + servicesList + "&callback=" + MainWindow.Cashbox.serialNumber +
                "&r=" + (reception ? "1" : "0") + "&n=" + (withoutApp ? "1" : "0");

            string request = fields + "&crc=" + CheckRequest.CreateMD5(fields, notOrd: true);

            string url = "/vcs/cashbox_mandocpack.htm?" + request;

            Log.Add(url, logType: "http");

            try
            {
                requestResult = SockectSend(url);
            }
            catch (SocketException e)
            {
                Log.AddWeb("(отправка запроса на чек) " + e.Message);

                return "ERROR|Ошибка отправки запроса на чек";
            }

            return requestResult;
        }

        public static string GenerateMySQLHash(string line)
        {
            byte[] lineArray = Encoding.UTF8.GetBytes(line);

            SHA1Managed sha1 = new SHA1Managed();

            byte[] lineEncoded = sha1.ComputeHash(sha1.ComputeHash(lineArray));

            StringBuilder sha1hash = new StringBuilder(lineEncoded.Length);

            foreach (byte i in lineEncoded)
                sha1hash.Append(i.ToString("X2"));

            return "*" + sha1hash.ToString();
        }

        public static void SendFile(string pathToFile, string appID, string actNum,
            string xerox, string form, string print, string photo)
        {
            string url = "/vcs/cashbox_upload.htm";

            Log.Add("отправлена информация в БД об акте для AppID " + appID);
            Log.Add("копирование: " + xerox + " Анкета: " + form + " Распечатка: " + print + " Фото: " + photo);

            var md5 = System.Security.Cryptography.MD5.Create();
            string md5sum = BitConverter.ToString(
                md5.ComputeHash(
                    File.ReadAllBytes(pathToFile)
                )
            ).Replace("-", String.Empty).ToLower();

            WebClient client = new WebClient();
            client.Proxy = WebRequest.DefaultWebProxy;
            client.Proxy.Credentials = CredentialCache.DefaultCredentials;
            client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

            NameValueCollection parameters = new NameValueCollection();
            parameters.Add("md5", md5sum);
            parameters.Add("login", currentLogin);
            parameters.Add("appID", appID);

            parameters.Add("xerox", xerox);
            parameters.Add("form", form);
            parameters.Add("photo", photo);
            parameters.Add("print", print);
            parameters.Add("actnum", actNum);

            client.QueryString = parameters;

            Uri fServer = new Uri(url);
            client.UploadFileAsync(fServer, pathToFile);
            client.UploadFileCompleted += SendFileComplete;
        }

        private static void SendFileComplete(object sender, UploadFileCompletedEventArgs e)
        {
            string[] serverResult = System.Text.Encoding.UTF8.GetString(e.Result).Split('|');

            if (e.Error == null && serverResult[0] == "OK")
                Log.Add("акт успешно загружен на сервер");
            else
                Log.Add($"ошибка загрузки акта на сервер: {e.Error}");
        }

        public static void CashboxPaymentControl(string agreement, string paymentType)
        {
            string controlString = String.Empty;

            string url = "/individuals/cashbox_payment_control.htm?" +
                "docnum=" + agreement + "&login="+ currentLogin +
                "&p=" + currentPassword + "&t=" + paymentType + "&ip=" + GetMyIP();

            try
            {
                controlString = SockectSend(url);
            }
            catch (SocketException e)
            {
                Log.AddWeb("(ошибка контроля оплаты) " + e.Message);
            }

            string[] control = controlString.Split('|');

            Log.Add("контроль оплаты: " + control[1]);
        }
    }
}
