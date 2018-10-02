using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DrvFRLib;
using System.Timers;

namespace shtrih_interceptor
{
    class Cashbox
    {
        static DrvFR Driver;

        public static System.Timers.Timer repeatPrintingTimer = new System.Timers.Timer(5000);
        static int currentDrvPassword = 0;

        static Cashbox()
        {
            Driver = new DrvFR();
            repeatPrintingTimer.Elapsed += new ElapsedEventHandler(repeatPrint);
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
            currentDrvPassword = doc.CashierPass;

            foreach (Service service in doc.Services)
            {
                Driver.Password = currentDrvPassword;

                Driver.Quantity = service.Quantity;
                Driver.Price = service.Price;
                Driver.StringForPrinting = service.Name;


                Driver.Tax1 = service.VAT;
                Driver.Tax2 = 0;
                Driver.Tax3 = 0;
                Driver.Tax4 = 0;

                Driver.Sale();
            }

            Driver.Password = currentDrvPassword;
            Driver.StringForPrinting = "";

            if (doc.MoneyType == 1)
                Driver.Summ1 = doc.Money;
            else if (doc.MoneyType == 2)
                Driver.Summ2 = doc.Money;

            Driver.CloseCheck();

            Log.add("распечатка чека: " + getResultLine() +
                " [" + getResultCode() + "]");

            repeatPrintingTimer.Enabled = true;
            repeatPrintingTimer.Start();

            return getResultCode();
        }

        public static void repeatPrint(object obj, ElapsedEventArgs e)
        {
            Driver.Password = currentDrvPassword;
            Driver.RepeatDocument();

            int printSuccess = getResultCode();

            Log.add("распечатка повтора: " + getResultLine() +
                " [" + printSuccess.ToString() + "]");

            if (printSuccess == 0) {
                repeatPrintingTimer.Enabled = false;
                repeatPrintingTimer.Stop();
            }
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
