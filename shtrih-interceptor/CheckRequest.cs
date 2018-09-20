using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace shtrih_interceptor
{
    class CheckRequest
    {
        static string MD5_LINE_TMP;

        public static bool checkXml(string from)
        {
            string md5 = Client.getFromXml("check", from);

            Log.add("md5 отправки: " + md5);

            XmlDocument request = new XmlDocument();

            request.LoadXml(from);

            var root = request.DocumentElement;

            MD5_LINE_TMP = "";

            string md5line = createMD5(PrintItem(root)).ToLower();

            Log.add("md5 полученного: " + md5line);

            return md5line == md5;
        }

        private static string PrintItem(XmlElement item, int indent = 0)
        {
            if (item.LocalName == "check") return "";

            foreach (var child in item.ChildNodes)
            {
                if (child is XmlElement)
                    PrintItem((XmlElement)child, indent + 1);

                if (child is XmlText)
                {
                    XmlText textXml = (XmlText)child;
                    string text = textXml.InnerText;
                    MD5_LINE_TMP += text;
                }
            }

            return MD5_LINE_TMP;
        }

        public static string createMD5(string input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }
}
