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
        public static void AddWithCode(string line, string logType = "main",
            bool freeLine = false, bool freeLineAfter = false)
        {
            Add(line + ": " + Cashbox.GetResultLine() + " [" + Cashbox.GetResultCode() + "]",
                logType, freeLine, freeLineAfter);
        }

        public static void AddWeb(string line, string logType = "main", bool freeLine = false,
            bool freeLineAfter = false)
        {
            Add("Ошибка доступа к серверу: " + line, logType, freeLine, freeLineAfter);
        }

        public static void Add(string line, string logType = "main",
            bool freeLine = false, bool freeLineAfter = false)
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
                if (freeLine)
                    WriteLine(sw, date: true);

                WriteLine(sw, line: line, date: true);

                if (logType == "http")
                    WriteLine(sw);

                if (freeLineAfter)
                    WriteLine(sw, date: true);
            }

            if (logType == "main") ShowCurrentStatus(line);
        } 

        public static void AddDocPack(DocPack docForLog)
        {
            XmlSerializer DocPackLog = new XmlSerializer(typeof(DocPack));

            using (StreamWriter sw = new StreamWriter("shtrih-interceptor-doc.log", true))
            {
                WriteLine(sw);
                WriteLine(sw, date: true);
                WriteLine(sw);
                DocPackLog.Serialize(sw, docForLog);
                WriteLine(sw);
            }
        }

        public static void WriteLine(StreamWriter sw, string line = "", bool date = false)
        {
            string dateLine = (date ? DateTime.Now.ToString("yyyy-MMM-dd HH:mm:ss") : "");
            sw.WriteLine(dateLine + (line != "" ? " " : "") + line);
        }

        public static void ShowCurrentStatus(string line)
        {
            if (Application.Current == null)
                return;
             
            Application.Current.Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                MainWindow main = (MainWindow)Application.Current.MainWindow;
                main.status9.Content = line;
                main.status11.Content = Cashbox.CurrentModeDescription().ToLower();
            }));
        }
    }
}
