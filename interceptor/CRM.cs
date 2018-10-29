using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

namespace interceptor
{
    class CRM
    {
        public const string CRM_URL_BASE = "127.0.0.1";
        const string CRM_URL = "http://" + CRM_URL_BASE;

        public static int Password = 0;
        public const int AdminPassword = 0;

        public static string Cashier = "";
        public static string loginError = "";

        public static bool crmAuthentication(string login, string password)
        {
            string authString = "";

            string url = CRM_URL + "/vcs/cashbox_auth.htm?login=" + login +
                "&p=" + password + "&ip=" + getMyIP() + "&v=" + MainWindow.CURRENT_VERSION;

            try
            {
                authString = getHtml(url);
            }
            catch (WebException e)
            {
                loginError = "Ошибка доступа к серверу";

                Log.addWeb(e.Message);

                return false;
            }

            string[] authData = authString.Split('|');

            int result = 0;

            if (authData[0] != "OK" || !Int32.TryParse(authData[1], out result))
            {
                loginError = authData[1];

                Log.add(loginError);

                return false;
            }

            Password = Int32.Parse(authData[1]);

            Cashier = authData[2];

            Log.add("успешный вход: " + login + "/" + authData[1] + "(" + authData[2] + ")");

            return true;
        }

        public static string[] getAllCenters(string login)
        {
            string centerString = "";

            string url = CRM_URL + "/vcs/cashbox_centers.htm?login=" + login;

            try
            {
                centerString = getHtml(url);
            }
            catch (WebException e)
            {
                Log.addWeb("(центры) " + e.Message);

                return null;
            }

            return centerString.Split('|');
        }

        public static string[] getAllVType(string vcenterName)
        {
            string vtypeString = "";

            string url = CRM_URL + "/vcs/cashbox_vtype.htm?center=" + vcenterName;

            try
            {
                vtypeString = getHtml(url);
            }
            catch (WebException e)
            {
                Log.addWeb("(типы виз) " + e.Message);

                return null;
            }

            return vtypeString.Split('|');
        }

        public static string getMyIP()
        {
            IPAddress ip = Dns.GetHostByName(Dns.GetHostName()).AddressList[0];

            return ip.ToString();
        }

        public static string sendManDocPack(List<string> manDocPack, string login, int password, int moneyType,
            string money, string center, string vType, string returnDate)
        {
            string requestResult = "";

            string servicesList = String.Join("|", manDocPack.ToArray());

            Log.add("запрос на чек: " + servicesList);

            string fields =
                "login=" + login + "&pass=" + password.ToString() + "&moneytype=" + moneyType.ToString() +
                "&money=" + money + "&center=" + center + "&vtype=" + vType + "&rdate=" + returnDate +
                "&services=" + servicesList + "&callback=" + getMyIP();

            string request = fields + "&crc=" + CheckRequest.createMD5(fields);

            string url = CRM_URL + "/vcs/cashbox_mandocpack.htm?" + request;

            Log.add(url, logType: "http");

            try
            {
                requestResult = getHtml(url);
            }
            catch (WebException e)
            {
                Log.addWeb("(отправка запроса на чек) " + e.Message);

                return "ERROR";
            }

            return requestResult;
        }

        public static string generateMySQLHash(string line)
        {
            byte[] lineArray = Encoding.UTF8.GetBytes(line);

            SHA1Managed sha1 = new SHA1Managed();

            byte[] lineEncoded = sha1.ComputeHash(sha1.ComputeHash(lineArray));

            StringBuilder sha1hash = new StringBuilder(lineEncoded.Length);

            foreach (byte i in lineEncoded)
                sha1hash.Append(i.ToString("X2"));

            return "*" + sha1hash.ToString();
        }

        public static string getHtml(string url)
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
    }
}
