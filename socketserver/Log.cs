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
        public static void Add(string errorLine)
        {
            string errorDateLine = DateTime.Now.ToString("yyyy-MMM-dd HH:mm:ss") + " " + errorLine;

            try
            {
                using (StreamWriter sw = new StreamWriter("socketserver.log", true))
                    sw.WriteLine(errorDateLine);
            }
            catch (Exception)
            {
                // nothing to do here
            }
        }
    }
}
