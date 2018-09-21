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

        public static int testBeep()
        {
            Driver.Beep();

            Log.add("проверка связи с кассой: " + Driver.ResultCodeDescription + 
                " [" + Driver.ResultCode.ToString() + "]");

            return Driver.ResultCode;
        }

        public static void settings()
        {
            Driver.ShowProperties();
        }
    }
}
