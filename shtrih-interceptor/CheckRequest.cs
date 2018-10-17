using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace interceptor
{
    class CheckRequest
    {
        static string MD5_LINE_TMP;

        public static bool CheckConnection(string from)
        {
            Match ReqMatch = Regex.Match(from, @"<CheckConnection>MakeBeep</CheckConnection>");

            return ReqMatch.Success;
        }

        public static bool CheckXml(string from)
        {
            if (from == "") return false;

            XmlDocument request = new XmlDocument();

            request.LoadXml(from);

            XmlNode md5check = request.SelectSingleNode("toCashbox/Control/CRC");
            string md5 = md5check.InnerText;

            var root = request.DocumentElement;

            MD5_LINE_TMP = "";

            string md5lineTMP = allXmlField(root);

            byte[] bytes = Encoding.GetEncoding(1251).GetBytes(md5lineTMP);
            string byteLine = "";

            foreach (byte b in bytes)
                byteLine += b.ToString() + " ";

            string md5line = createMD5(byteLine).ToLower();

            return md5line == md5;
        }

        private static string allXmlField(XmlElement item, int indent = 0)
        {
            if (item.LocalName == "CRC") return "";

            foreach (var child in item.ChildNodes)
            {
                if (child is XmlElement)
                    allXmlField((XmlElement)child, indent + 1);

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
