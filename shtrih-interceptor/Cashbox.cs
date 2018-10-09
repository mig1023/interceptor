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

        public static void repeatDocument()
        {
            Driver.Password = CRM.Password;
            Driver.RepeatDocument();

            Log.addWithCode("распечатка повтора");
        }

        public static void reportCleaning()
        {
            Driver.Password = CRM.Password;
            Driver.PrintReportWithCleaning();

            Log.addWithCode("отчёт с гашением");
        }

        public static void reportWithoutCleaning()
        {
            Driver.Password = CRM.Password;
            Driver.PrintReportWithoutCleaning();

            Log.addWithCode("отчёт без гашения");
        }

        public static void reportDepartment()
        {
            Driver.Password = CRM.Password;
            Driver.PrintDepartmentReport();

            Log.addWithCode("отчёт по отделам");
        }

        public static void reportTax()
        {
            Driver.Password = CRM.Password;
            Driver.PrintTaxReport();

            Log.addWithCode("отчёт по налогам");
        }

        public static string printDocPack(DocPack doc)
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
            {
                Driver.Summ1 = doc.Money;
                Driver.Summ2 = 0;
            }
            else if (doc.MoneyType == 2)
            {
                Driver.Summ2 = doc.Money;
                Driver.Summ1 = 0;
            }
                

            Driver.CloseCheck();

            int checkClosingResult = Driver.ResultCode;
            string checkClosingErrorText = Driver.ResultCodeDescription;

            Log.addWithCode("распечатка чека");

            if (checkClosingResult != 0)
            {
                Driver.Password = currentDrvPassword;
                Driver.CancelCheck();

                Log.addWithCode("отмена чека");

                Server.ShowActivity(false);
            }
            else
            {
                repeatPrintingTimer.Enabled = true;
                repeatPrintingTimer.Start();
            }

            if (checkClosingResult == 0)
                return "OK:" + Driver.Change;
            else
                return "ERR2:" + checkClosingErrorText;
        }

        public static void repeatPrint(object obj, ElapsedEventArgs e)
        {
            Driver.Password = currentDrvPassword;
            Driver.RepeatDocument();

            int printSuccess = getResultCode();

            Log.addWithCode("распечатка повтора");

            if (printSuccess == 0) {
                repeatPrintingTimer.Enabled = false;
                repeatPrintingTimer.Stop();

                Server.ShowActivity(false);
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
