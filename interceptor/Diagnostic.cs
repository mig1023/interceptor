using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace interceptor
{
    class Diagnostics
    {
        public static bool failCashbox()
        {
            Cashbox.checkConnection();

            Log.addWithCode("проверка связи с кассой");

            return ( Cashbox.getResultCode() != 0 ? true : false );
        }

        public static string makeBeepTest()
        {
            Cashbox.makeBeep();

            return (Cashbox.getResultCode() == 0 ? "OK" : "ERR2:Касса не отвечает");
        }
    }
}
