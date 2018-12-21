using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace interceptor
{
    class AutoUpdate
    {
        const string URL_UPDATE = "127.0.0.1/";
        const string URL_MANIFEST = URL_UPDATE + "manifest.update";
        static string UPDATE_DIR = "update_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "\\";

        public static bool Update(string updateFiles)
        {
            Log.Add("необходимо обновление до версии " + GetLastVersion(updateFiles));

            XmlDocument updateData = new XmlDocument();

            updateData.LoadXml(updateFiles);

            WebClient webClient = new WebClient();

            Directory.CreateDirectory(UPDATE_DIR);

            foreach (XmlNode node in updateData.SelectNodes("Update/Files/File"))
            {
                string name = node["Name"].InnerText;

                webClient.DownloadFile(URL_UPDATE + name, UPDATE_DIR + name);

                Log.Add("скачан файл: " + name);

                string[] lines = System.IO.File.ReadAllLines(UPDATE_DIR + name);

                string crcCheck = CheckRequest.CreateMD5(string.Join("", lines));

                if (crcCheck != node["CRC"].InnerText)
                    return false;
            }

            return true;
        }

        public static string NeedUpdating()
        {
            string versionData = "";

            try
            {
                versionData = CRM.GetHtml(URL_MANIFEST);
            }
            catch (WebException e)
            {
                Log.AddWeb("(ошибка проверки версии обновления) " + e.Message);

                return "";
            }

            string version = GetLastVersion(versionData);

            if (version == "")
                return "";
            else if (version == MainWindow.CURRENT_VERSION)
                return "";
            else
                return versionData;
        }

        public static string GetLastVersion(string from)
        {
            if (from == "")
                return "";

            XmlDocument xmlData = new XmlDocument();

            xmlData.LoadXml(from);

            XmlNode lastVersionXml = xmlData.SelectSingleNode("Update/LastVersion");

            return lastVersionXml.InnerText;
        }
    }
}
