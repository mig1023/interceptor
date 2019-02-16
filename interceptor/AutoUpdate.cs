using System;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Xml;

namespace interceptor
{
    class AutoUpdate
    {
        const string URL_UPDATE = "127.0.0.1/";
        const string URL_MANIFEST = URL_UPDATE + "manifest.update";
        static string UPDATE_DIR = "update_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "\\";

        public static string Update(string updateFiles)
        {
            Log.Add("необходимо обновление до версии " + GetLastVersion(updateFiles), "update", freeLine: true);

            XmlDocument updateData = new XmlDocument();

            updateData.LoadXml(updateFiles);

            WebClient webClient = new WebClient();

            Directory.CreateDirectory(UPDATE_DIR);

            foreach (XmlNode node in updateData.SelectNodes("Update/Files/File"))
            {
                string name = node["Name"].InnerText;

                webClient.DownloadFile(URL_UPDATE + name, UPDATE_DIR + name);

                Log.Add("скачан файл: " + name, "update");

                string[] lines = System.IO.File.ReadAllLines(UPDATE_DIR + name);

                string crcCheck = CheckRequest.CreateMD5(string.Join(String.Empty, lines));

                if (crcCheck != node["CRC"].InnerText)
                    return String.Empty;
            }

            return UPDATE_DIR;
        }

        public static void StartUpdater()
        {
            Log.Add("запущен процесс обновления и перезапуска", "update");

            Process.Start("autoupdate.exe", "interceptor.exe " + UPDATE_DIR);
            Process.GetCurrentProcess().Kill();
        }

        public static string NeedUpdating()
        {
            string versionData;

            try
            {
                versionData = CRM.GetHtml(URL_MANIFEST);
            }
            catch (WebException e)
            {
                Log.AddWeb(e.Message, "update");

                return String.Empty;
            }

            string version = GetLastVersion(versionData);

            if ((version == String.Empty) || (version == MainWindow.CURRENT_VERSION_CLEAN))
                return String.Empty;
            else
                return versionData;
        }

        public static string GetLastVersion(string from)
        {
            if (from == String.Empty)
                return from;

            XmlDocument xmlData = new XmlDocument();

            xmlData.LoadXml(from);

            XmlNode lastVersionXml = xmlData.SelectSingleNode("Update/LastVersion");

            return lastVersionXml.InnerText;
        }
    }
}
