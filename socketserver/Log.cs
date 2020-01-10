using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace socketserver
{
    class Log
    {
        public static void Add(string errorLine, bool startServer)
        {
            Add(String.Empty);
            Add(errorLine);
            Add(String.Empty);
        }

        public static void Add(string errorLine)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter("socketserver.log", true))
                    sw.WriteLine(String.Format("{0} {1}", DateTime.Now.ToString("yyyy-MMM-dd HH:mm:ss"), errorLine));
            }
            catch (Exception)
            {
                // nothing to do here
            }
        }
    }
}
