﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace interceptor
{
    class Diagnostics
    {
        public static bool FailCashbox()
        {
            Cashbox.CheckConnection();

            Log.AddWithCode("проверка связи с кассой");

            return ( Cashbox.GetResultCode() != 0 ? true : false );
        }

        public static string MakeBeepTest()
        {
            Cashbox.MakeBeep();

            return (Cashbox.GetResultCode() == 0 ? "OK" : "ERR2:Касса не отвечает");
        }
    }
}
