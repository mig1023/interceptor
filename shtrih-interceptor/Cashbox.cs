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

        public static void checkConnection()
        {
            Driver.CheckConnection();
        }

        public static int printDocPack(DocPack doc)
        {
            
            foreach (Service service in doc.Services)
            {
                Driver.Password = doc.CashierPass;

                Driver.Quantity = service.Quantity;
                Driver.Price = service.Price;
                Driver.StringForPrinting = service.Name;


                Driver.Tax1 = service.VAT;
                Driver.Tax2 = 0;
                Driver.Tax3 = 0;
                Driver.Tax4 = 0;

                Driver.Sale();
            }
            
            Driver.Password = doc.CashierPass;
            Driver.StringForPrinting = "";

            if (doc.MoneyType == 1)
                Driver.Summ1 = doc.Money;
            else if (doc.MoneyType == 2)
                Driver.Summ2 = doc.Money;

            Driver.CloseCheck();

            Log.add("распечатка чека: " + getResultLine() +
                " [" + getResultCode() + "]");

            //for (int a = 0; a < 10; a++)
            //{
            //    System.Threading.Thread.Sleep(2000);

            //    Driver.CheckConnection();
            //    Log.add("проверка связи (" + a.ToString() + "): " + getResultLine() +
            //   " [" + getResultCode() + "]");

                Driver.Password = doc.CashierPass;
                Driver.RepeatDocument();

                Log.add("распечатка повтора: " + getResultLine() +
               " [" + getResultCode() + "]");
            //}


           

            return getResultCode();
        }

        public static int getResultCode()
        {
            return Driver.ResultCode;
        }

        public static string getResultLine()
        {
            return Driver.ResultCodeDescription;
        }

        public static decimal getChange()
        {
            return Driver.Change;
        }

        public static void settings()
        {
            Driver.ShowProperties();

        }
    }
}
