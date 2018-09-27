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

        public static void printDocPack(DocPack doc)
        {
            
            foreach (Service service in doc.Services)
            {
                Driver.Password = doc.CashierPass;

                Driver.Quantity = service.Quantity;
                Driver.Price = service.Price;
                Driver.Department = service.ServiceID;

                if (service.VAT == 1)
                    Driver.Tax1 = 1;    // НДС 18%
                else
                    Driver.Tax1 = 2;    // без НДС

                Driver.Tax2 = 0;
                Driver.Tax3 = 0;
                Driver.Tax4 = 0;

                Driver.Sale();
            }
            



            Driver.Password = doc.CashierPass;

            if (doc.MoneyType == 1)
                Driver.Summ1 = doc.Money;
            else if (doc.MoneyType == 2)
                Driver.Summ2 = doc.Money;

            //Driver.Tax1 = 1; // 1 - 18%, 0 - без НДС

            Driver.CloseCheck();

            Log.add("распечатка чека: " + getResultLine() +
                " [" + getResultCode() + "]");
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
