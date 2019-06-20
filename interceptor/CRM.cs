using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace interceptor
{
    class CRM
    {
        const string CRM_URL_TEST = "127.0.0.1";

        const string CRM_URL_FIGHT = "127.0.0.1";
        public const string CRM_URL_BASE = (MainWindow.TEST_VERSION ? CRM_URL_TEST : CRM_URL_FIGHT);
        public const string CRM_URL = "http://" + CRM_URL_BASE;

        const string CRM_URL_FIGHT_RUSERV = "127.0.0.1";
        public const string CRM_URL_BASE_RUSERV = (MainWindow.TEST_VERSION ? CRM_URL_TEST : CRM_URL_FIGHT_RUSERV);
        public const string CRM_URL_RUSERV = "http://" + CRM_URL_BASE_RUSERV;

        public static int password = 0;
        public const int adminPassword = 0;

        public static string currentLogin = String.Empty;
        public static string currentPassword = String.Empty;
        public static string cashier = String.Empty;
        public static string loginError = String.Empty;

        public static bool CrmAuthentication(string login, string passwordLine)
        {
            string authString = String.Empty;

            string url = CRM_URL + "/vcs/cashbox_auth.htm?login=" + login +
                "&p=" + passwordLine + "&ip=" + GetMyIP() + "&v=" + MainWindow.CURRENT_VERSION;

            try
            {
                authString = GetHtml(url);
            }
            catch (WebException e)
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

            ShtrihM.currentDirectPassword = password;

            cashier = authData[2];

            Log.Add("успешный вход: " + login + "/" + authData[1] + "(" + authData[2] + ")");

            return true;
        }

        public static string[] GetAllCenters(string login)
        {
            string centerString = String.Empty;

            string url = CRM_URL + "/vcs/cashbox_centers.htm?login=" + login;

            try
            {
                centerString = GetHtml(url);
            }
            catch (WebException e)
            {
                Log.AddWeb("(центры) " + e.Message);

                return null;
            }

            return centerString.Split('|');
        }

        public static string[] GetAllVType(string vcenterName)
        {
            string vtypeString = String.Empty;

            string url = CRM_URL + "/vcs/cashbox_vtype.htm?center=" + vcenterName;

            try
            {
                vtypeString = GetHtml(url);
            }
            catch (WebException e)
            {
                Log.AddWeb("(типы виз) " + e.Message);

                return null;
            }

            return vtypeString.Split('|');
        }

        public static void SendError(string error, string agrNumber = "")
        {
            string requestResult = String.Empty;

            string fields =
                "login=" + currentLogin + "&error=" + error + "&ip=" + GetMyIP() + "&agr=" + agrNumber;

            string url = CRM_URL + "/vcs/cashbox_error.htm?" + fields;

            Log.Add(url, logType: "http");

            try
            {
                requestResult = GetHtml(url);
            }
            catch (WebException e)
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

            string url = CRM_URL + "/vcs/cashbox_appinfo.htm?" + request;

            Log.Add(url, logType: "http");

            try
            {
                requestResult = GetHtml(url);
            }
            catch (WebException e)
            {
                Log.AddWeb("(отправка запроса на информацию о записи) " + e.Message);

                return "ERROR|Ошибка отправки запроса на информацию о записи";
            }

            return requestResult;
        }

        public static string SendManDocPack(List<string> manDocPack, string login, int password, int moneyType,
            string money, string center, string vType, string returnDate, bool reception = false)
        {
            string requestResult = String.Empty;

            string servicesList = String.Join("|", manDocPack.ToArray());

            Log.Add("запрос на чек: " + servicesList);

            string fields =
                "login=" + login + "&pass=" + password.ToString() + "&moneytype=" + moneyType.ToString() +
                "&money=" + money + "&center=" + center + "&vtype=" + vType + "&rdate=" + returnDate +
                "&services=" + servicesList + "&callback=" + GetMyIP() + "&r=" + (reception ? "1" : "0");

            string request = fields + "&crc=" + CheckRequest.CreateMD5(fields, notOrd: true);

            string url = CRM_URL + "/vcs/cashbox_mandocpack.htm?" + request;

            Log.Add(url, logType: "http");

            try
            {
                requestResult = GetHtml(url);
            }
            catch (WebException e)
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

        public static string GetHtml(string url)
        {
            WebClient client = new WebClient();
            using (Stream data = client.OpenRead(url))
            {
                using (StreamReader reader = new StreamReader(data))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static void SendFile(string pathToFile, string appID, string actNum,
            string xerox, string form, string print, string photo)
        {
            string url = CRM_URL + "/vcs/cashbox_upload.htm";

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

        public static void CashboxPaymentControl(string agreement)
        {
            string controlString = String.Empty;

            string url = CRM_URL_RUSERV + "/individuals/cashbox_payment_control.htm?" +
                "docnum=" + agreement + "&login="+ currentLogin + "&p=" + currentPassword;

            try
            {
                controlString = GetHtml(url);
            }
            catch (WebException e)
            {
                Log.AddWeb("(ошибка контроля оплаты) " + e.Message);
            }

            string[] control = controlString.Split('|');

            Log.Add("контроль оплаты: " + control[1]);
        }
    }
}
