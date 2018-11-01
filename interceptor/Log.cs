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
            bool freeLine = false, bool freeLineAfter = false, bool title = false)
        {
            add(line + ": " + Cashbox.getResultLine() + " [" + Cashbox.getResultCode() + "]",
                logType, freeLine, freeLineAfter, title);
        }

        public static void addWeb(string line, string logType = "main", bool freeLine = false,
            bool freeLineAfter = false, bool title = false)
        {
            add("Ошибка доступа к серверу: " + line, logType, freeLine, freeLineAfter, title);
        }

        public static void add(string line, string logType = "main",
            bool freeLine = false, bool freeLineAfter = false, bool title = false)
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
                if (freeLine || title) writeLine(sw, date: true);

                writeLine(sw, line: line, date: true);

                if (title) writeLine(sw, line: new string('/', line.Length), date: true);

                if (logType == "http") writeLine(sw);

                if (freeLineAfter) writeLine(sw, date: true);
            }

            if (logType == "main") showCurrentStatus(line);
        } 

        public static void addDocPack(DocPack docForLog)
        {
            XmlSerializer DocPackLog = new XmlSerializer(typeof(DocPack));

            using (StreamWriter sw = new StreamWriter("shtrih-interceptor-doc.log", true))
            {
                writeLine(sw);
                writeLine(sw, date: true);
                writeLine(sw);
                DocPackLog.Serialize(sw, docForLog);
                writeLine(sw);
            }
        }

        public static void writeLine(StreamWriter sw, string line = "", bool date = false)
        {
            string dateLine = (date ? DateTime.Now.ToString("yyyy-MMM-dd HH:mm:ss") : "");
            sw.WriteLine(dateLine + (line != "" ? " " : "") + line);
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
