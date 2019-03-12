using System;
using System.Text;
using System.Text.RegularExpressions;
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

        public static bool CheckLoginInRequest(string from, out string logins)
        {
            logins = String.Empty;

            if (String.IsNullOrEmpty(from))
                return false;

            XmlDocument request = new XmlDocument();

            request.LoadXml(from);

            XmlNode senderCashierNode = request.SelectSingleNode("toCashbox/Info/Cashier");
            string senderCashier = senderCashierNode.InnerText;

            logins = senderCashier + " <=> " + CRM.currentLogin;

            return senderCashier == CRM.currentLogin;
        }

        public static bool CheckXml(string from)
        {
            if (from == String.Empty)
                return false;

            XmlDocument request = new XmlDocument();

            request.LoadXml(from);

            XmlNode md5check = request.SelectSingleNode("toCashbox/Control/CRC");
            string md5 = md5check.InnerText;

            var root = request.DocumentElement;

            MD5_LINE_TMP = String.Empty;

            string md5lineTMP = AllXmlField(root);

            byte[] bytes = Encoding.GetEncoding(1251).GetBytes(md5lineTMP);
            string byteLine = String.Empty;

            foreach (byte b in bytes)
                byteLine += b.ToString() + " ";

            string md5line = CreateMD5(byteLine).ToLower();

            return md5line == md5;
        }

        private static string AllXmlField(XmlElement item, int indent = 0)
        {
            if (item.LocalName == "CRC")
                return String.Empty;

            foreach (var child in item.ChildNodes)
            {
                if (child is XmlElement)
                    AllXmlField((XmlElement)child, indent + 1);

                if (child is XmlText)
                {
                    XmlText textXml = (XmlText)child;
                    string text = textXml.InnerText;
                    MD5_LINE_TMP += text;
                }
            }

            return MD5_LINE_TMP;
        }

        public static string CreateMD5(string input, bool notOrd = false)
        {
            if (notOrd)
                input += MainWindow.PROTOCOL_PASS;
            else
            {
                byte[] bytes = Encoding.GetEncoding(1251).GetBytes(MainWindow.PROTOCOL_PASS);

                foreach (byte b in bytes)
                    input += b.ToString() + " ";
            }

            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < hashBytes.Length; i++)
                    sb.Append(hashBytes[i].ToString("X2"));

                return sb.ToString();
            }
        }
    }
}
