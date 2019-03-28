using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace interceptor
{
    class Log
    {
        const string UPDATE_LOGS_DIR = "logs";

        public static void AddWithCode(string line, string logType = "main",
            bool freeLine = false, bool freeLineAfter = false)
        {
            Add(line + ": " + Cashbox.GetResultLine() + " [" + Cashbox.GetResultCode() + "]",
                logType, freeLine, freeLineAfter);
        }

        public static void AddWeb(string line, string logType = "main", bool freeLine = false,
            bool freeLineAfter = false)
        {
            Add("ошибка доступа к серверу: " + line, logType, freeLine, freeLineAfter);
        }

        public static void Add(string line, string logType = "main",
            bool freeLine = false, bool freeLineAfter = false)
        {
            try
            {
                LogDirectory();

                string logFileName = UPDATE_LOGS_DIR + "\\interceptor-" + logType + ".log";

                using (StreamWriter sw = new StreamWriter(logFileName, true))
                {
                    if (freeLine)
                        WriteLine(sw, date: true);

                    WriteLine(sw, line: line, date: true, logtype: logType);

                    if (logType == "http")
                        WriteLine(sw);

                    if (freeLineAfter)
                        WriteLine(sw, date: true);
                }
            }
            finally
            {
                // nothing to do here
            }

            if (logType == "main") ShowCurrentStatus(line);
        } 

        public static void AddDocPack(DocPack docForLog)
        {
            XmlSerializer DocPackLog = new XmlSerializer(typeof(DocPack));

            try
            {
                LogDirectory();

                string logFileName = UPDATE_LOGS_DIR + "\\interceptor-doc.log";

                using (StreamWriter sw = new StreamWriter(logFileName, true))
                {
                    WriteLine(sw);
                    WriteLine(sw, date: true);
                    WriteLine(sw);
                    DocPackLog.Serialize(sw, docForLog);
                    WriteLine(sw);
                }
            }
            finally
            {
                // nothing to do here
            }
        }

        public static void WriteLine(StreamWriter sw, string line = "", bool date = false, string logtype = "")
        {
            string dateLine = (date ? DateTime.Now.ToString("yyyy-MMM-dd HH:mm:ss") : String.Empty);
            sw.WriteLine(dateLine + (!String.IsNullOrEmpty(line) ? " " : String.Empty) + line);

            if ((Application.Current == null) || (String.IsNullOrWhiteSpace(line)) || logtype != "main")
                return;

            Application.Current.Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                MainWindow main = (MainWindow)Application.Current.MainWindow;

                string[] logLines = new string[main.logBox.Items.Count];

                for (int a = 0; a < main.logBox.Items.Count; a += 1)
                    logLines[a] = main.logBox.Items[a].ToString();

                main.logBox.Items.Clear();

                main.logBox.Items.Add(dateLine + " " + line);

                for (int b = 0; b < logLines.Count(); b += 1)
                    main.logBox.Items.Add(logLines[b]);
            }));
        }

        public static void LogDirectory()
        {
            if (!Directory.Exists(UPDATE_LOGS_DIR))
                Directory.CreateDirectory(UPDATE_LOGS_DIR);
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
