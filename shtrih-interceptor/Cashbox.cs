using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DrvFRLib;

namespace shtrih_interceptor
{
    class Cashbox
    {
        static DrvFR Driver;

        static Cashbox()
        {
            Driver = new DrvFR();
        }

        public static void makeBeep()
        {
            Driver.Beep();
        }

        public static int getResultCode()
        {
            return Driver.ResultCode;
        }

        public static string getResultLine()
        {
            return Driver.ResultCodeDescription;
        }

        public static void settings()
        {
            Driver.ShowProperties();

        }
    }
}
