using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace interceptor
{
    class SaveLtlData
    {
        public static void SaveCurrentCashbox(string name)
        {
            FileInfo cashbox = new FileInfo(String.Format("{0}.current", name));
            if (!cashbox.Exists)
                cashbox.Create();
        }

        public static void RemoveCurrentCashbox(string name)
        {
            FileInfo cashbox = new FileInfo(String.Format("{0}.current", name));
            if (cashbox.Exists)
                cashbox.Delete();
        }

        public static string GetCurrentCashbox()
        {
            List<String> tmpCashboxes = new List<string> { "Штрих", "Атол" };

            foreach(string s in tmpCashboxes)
            {
                FileInfo cashbox = new FileInfo(String.Format("{0}.current", s));
                if (cashbox.Exists)
                    return s;
            }

            return String.Empty;
        }
    }
}
