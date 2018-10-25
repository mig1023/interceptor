using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Serialization;

namespace interceptor
{
    class Log
    {
        public static void addWithCode(string line, string logType = "main",
            bool freeLine = false, bool title = false)
        {
            add(line + ": " + Cashbox.getResultLine() + " [" + Cashbox.getResultCode() + "]",
                logType, freeLine, title);
        }

        public static void addWeb(string line, string logType = "main", bool freeLine = false, bool title = false)
        {
            add("Ошибка доступа к серверу: " + line, logType, freeLine, title);
        }

        public static void add(string line, string logType = "main", bool freeLine = false, bool title = false)
        {
            string logFileName;

            switch (logType)
            {
                case "main":
                    logFileName = "shtrih-interceptor-main.log";
                    break;
                case "http":
                    logFileName = "shtrih-interceptor-http.log";
                    break;
                default:
                    logFileName = "shtrih-interceptor-etc.log";
                    break;
            } 

            using (StreamWriter sw = new StreamWriter(logFileName, true))
            {
                if (freeLine || title)
                    sw.WriteLine(DateTime.Now.ToString("yyyy-MMM-dd HH:mm:ss"));

                sw.WriteLine(DateTime.Now.ToString("yyyy-MMM-dd HH:mm:ss") + " " + line);

                if (title)
                {
                    string symbLine = new string('/', line.Length);

                    sw.WriteLine(DateTime.Now.ToString("yyyy-MMM-dd HH:mm:ss") + " " + symbLine);
                    sw.WriteLine(DateTime.Now.ToString("yyyy-MMM-dd HH:mm:ss"));
                }

                if (logType == "http") sw.WriteLine();
            }

            if (logType == "main") showCurrentStatus(line);
        } 

        public static void addDocPack(DocPack docForLog)
        {
            XmlSerializer DocPackLog = new XmlSerializer(typeof(DocPack));

            using (StreamWriter sw = new StreamWriter("shtrih-interceptor-doc.log", true))
            {
                sw.WriteLine("");
                sw.WriteLine(DateTime.Now.ToString("yyyy-MMM-dd HH:mm:ss"));
                sw.WriteLine("");
                DocPackLog.Serialize(sw, docForLog);
                sw.WriteLine("");
            }
        }

        public static void showCurrentStatus(string line)
        {
            if (Application.Current == null) return;
             
            Application.Current.Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                MainWindow main = (MainWindow)Application.Current.MainWindow;
                main.status9.Content = line;
            }));
        }
    }
}
