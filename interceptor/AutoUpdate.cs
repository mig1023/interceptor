using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace interceptor
{
    class AutoUpdate
    {
        const string URL_UPDATE = "127.0.0.1/";
        const string URL_MANIFEST = URL_UPDATE + "manifest.update";
        const string UPDATE_DIR = "update\\";

        public static bool Update(string[] updateFiles)
        {
            Log.Add("необходимо обновление до версии " + updateFiles[0]);

            WebClient webClient = new WebClient();

            Directory.CreateDirectory(UPDATE_DIR);

            for (int file = 1; file < updateFiles.Count(); file += 2)
            {
                webClient.DownloadFile(URL_UPDATE + updateFiles[file], UPDATE_DIR + updateFiles[file]);

                Log.Add("скачан файл: " + updateFiles[file]);

                string[] lines = System.IO.File.ReadAllLines(UPDATE_DIR + updateFiles[file]);

                string crcCheck = CheckRequest.CreateMD5(string.Join("", lines));

                if (crcCheck != updateFiles[file + 1])
                    return false;
            }

            return true;
        }

        public static string[] NeedUpdating()
        {
            string versionDataLine = "";

            try
            {
                versionDataLine = CRM.GetHtml(URL_MANIFEST);
            }
            catch (WebException e)
            {
                Log.AddWeb("(ошибка проверки версии обновления) " + e.Message);

                return new string[0];
            }

            string[] versionData = versionDataLine.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            return (versionData[0] == MainWindow.CURRENT_VERSION ? new string[0] : versionData);
        }
    }
}
