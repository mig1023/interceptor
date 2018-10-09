using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

namespace shtrih_interceptor
{
    class CRM
    {
        const string CRM_URL = "127.0.0.1";

        public static int Password = 0;
        public static string loginError = "";

        public static bool crmAuthentication(string login, string password)
        {
            string authString = "";

            string url = CRM_URL + "/vcs/cashbox_auth.htm?login=" + login + "&pass=" + password;

            try
            {
                authString = getHtml(url);
            }
            catch
            {
                loginError = "Ошибка доступа к серверу";
                return false;
            }

            string[] authData = authString.Split('|');

            int result = 0;

            if (authData[0] != "OK" || !Int32.TryParse(authData[1], out result))
            {
                loginError = authData[1];
                return false;
            }

            Password = Int32.Parse(authData[1]);

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
            catch
            {
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
            catch
            {
                return null;
            }

            return vtypeString.Split('|');
        }

        public static void sendManDocPack(List<string> manDocPack, string login, int password, int moneyType, string money )
        {
            byte something = 1;
            something++;
            something += 2;
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
