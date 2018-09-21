using System;
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
            int testCashbox = Cashbox.testBeep();

            if (testCashbox == -2)
                return true;
            else
                return false;
        }
    }
}
