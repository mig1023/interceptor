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
            Cashbox.checkConnection();

            int testCashbox = Cashbox.getResultCode();

            Log.addWithCode("проверка связи с кассой");

            return ( Cashbox.getResultCode() != 0 ? true : false );
        }
    }
}
