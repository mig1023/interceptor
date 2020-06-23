using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace interceptor
{
    class Cached
    {
        static string CachedFileName = "intreceptor.cached";

        public static void CashboxSave(string name)
        {
            using (StreamWriter writetext = new StreamWriter(CachedFileName))
            {
                writetext.Write(name);
            }
        }

        public static string GetCashbox()
        {
            if (!File.Exists(CachedFileName))
                return String.Empty;

            return File.ReadAllText(CachedFileName);
        }
    }
}
