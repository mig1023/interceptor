﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shtrih_interceptor
{
    class Diagnosticcs
    {
        public static bool failCashbox()
        {
            Cashbox.checkConnection();

            int testCashbox = Cashbox.getResultCode();

            Log.add("проверка связи с кассой: " + Cashbox.getResultLine() +
                " [" + testCashbox.ToString() + "]");

            if (testCashbox != 0)
                return true;
            else
                return false;
        }
    }
}
