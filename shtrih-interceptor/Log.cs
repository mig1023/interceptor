using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Serialization;

namespace shtrih_interceptor
{
    class Log
    {
        public static void addWithCode(string line, string logType = "main")
        {
            add(line + ": " + Cashbox.getResultLine() + " [" + Cashbox.getResultCode() + "]", logType);
        }

        public static void add(string line, string logType = "main")
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
                sw.WriteLine(DateTime.Now.ToString("yyyy-MMM-dd HH:mm:ss") + " " + line);
            }
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
    }
}
