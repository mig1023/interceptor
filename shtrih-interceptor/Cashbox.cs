using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DrvFRLib;
using System.Timers;
using System.Windows;

namespace interceptor
{
    class Cashbox
    {
        static DrvFR Driver;

        public static System.Timers.Timer repeatPrintingTimer = new System.Timers.Timer(5000);
        static int currentDrvPassword = 0;

        public static DocPack manDocPackForPrinting;
        public static decimal manDocPackSumm;



        static Cashbox()
        {
            Driver = new DrvFR();
            Driver.FindDevice();
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

        public static void continueDocument()
        {
            Driver.Password = CRM.Password;
            Driver.ContinuePrint();

            Log.addWithCode("продолжение печати");
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

        public static void cancelDocument()
        {
            Driver.Password = CRM.Password;
            Driver.CancelCheck();

            Log.addWithCode("отмена чека по кнопке");
        }

        public static void cashIncome(string summ)
        {
            Driver.Password = CRM.Password;
            Driver.Summ1 = DocPack.manualParseDecimal(summ);
            Driver.CashIncome();

            Log.addWithCode("внесение денег (" + summ + ")");
        }

        public static void cashOutcome(string summ)
        {
            Driver.Password = CRM.Password;
            Driver.Summ1 = DocPack.manualParseDecimal(summ);
            Driver.CashOutcome();

            Log.addWithCode("выплата денег (" + summ + ")");
        }

        static string tableField(int tableNumber, int fieldNumber, int rowNumber, string fieldValue = "")
        {
            Driver.TableNumber = tableNumber;
            Driver.FieldNumber = fieldNumber;
            Driver.RowNumber = rowNumber;

            Driver.GetFieldStruct();

            Driver.ReadTable();

            if (fieldValue != "")
            {
                if (fieldValue == Driver.ValueOfFieldString)
                    Log.addWithCode("поле " + tableNumber + "/" + fieldNumber + "/" + rowNumber + "/" + fieldValue + " идентично");
                else
                {
                    Log.addWithCode("запись поля " + tableNumber + "/" + fieldNumber + "/" + rowNumber + "/" + fieldValue);

                    Driver.ValueOfFieldString = fieldValue;
                    Driver.WriteTable();

                    if (Driver.ResultCode != 0) return "";
                }
            }
                
            return Driver.ValueOfFieldString;
        }

        public static bool failCashboxField(int tableNumber, int fieldNumber, int rowNumber, string fieldValue)
        {
            return (tableField(tableNumber, fieldNumber, rowNumber) != fieldValue);
        }

        public static bool resettingCashbox()
        {
            foreach (CashboxData field in CashboxData.data)
                if (tableField(field.tableNumber, field.fieldNumber, field.rowNumber, field.fieldValue) == "")
                    return false;

            return true;
        }

        public static string checkCashboxTables()
        {
            List<string> tablesCorrupted = new List<string>();

            foreach (CashboxData field in CashboxData.data)
                if (failCashboxField(field.tableNumber, field.fieldNumber, field.rowNumber, field.fieldValue))
                    tablesCorrupted.Add(field.description);

            return string.Join(", ", tablesCorrupted.ToArray());
        }

        public static void printLine()
        {
            Driver.Password = currentDrvPassword;
            Driver.StringForPrinting = "------------------------------------";
            Driver.PrintString();
        } 

        public static string printDocPack(DocPack doc, int MoneyType = -1,
            bool returnSale = false, decimal? MoneySumm = null)
        {
            currentDrvPassword = doc.CashierPass;

            if (MoneyType != -1) doc.MoneyType = MoneyType;

            if (returnSale)
            {
                Driver.CheckType = 2;
                Driver.OpenCheck();
            }

            foreach (Service service in doc.Services)
            {
                Driver.Password = currentDrvPassword;

                Driver.Quantity = service.Quantity;
                Driver.Price = service.Price;
                Driver.StringForPrinting = service.Name;

                Driver.Department = service.Department;

                Driver.Tax1 = service.VAT;
                Driver.Tax2 = 0;
                Driver.Tax3 = 0;
                Driver.Tax4 = 0;

                if (returnSale)
                    Driver.ReturnSale();
                else
                    Driver.Sale();

                printLine();
            }

            Driver.Password = currentDrvPassword;
            Driver.StringForPrinting = "";

            if (doc.MoneyType == 1)
            {
                Driver.Summ1 = MoneySumm ?? doc.Money;
                Driver.Summ2 = 0;
            }
            else if (doc.MoneyType == 2)
            {
                Driver.Summ2 = MoneySumm ?? doc.Money;
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

                Server.ShowActivity(busy: false);
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

                Server.ShowActivity(busy: false);
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

        public static void getStatusData(out string port, out string speed, out string status,
            out string version, out string model)
        {
            string[] baudeRate = new string[] { "2400", "4800", "9600", "19200", "38400", "57600", "115200" };

            Driver.GetECRStatus();

            int portIndex = Driver.PortNumber + 1;

            port = "com" + portIndex.ToString();
            speed = baudeRate[Driver.BaudRate];
            status = Driver.ResultCodeDescription;
            version = Driver.ECRSoftVersion;

            Driver.GetDeviceMetrics();
            model = Driver.UDescription;
        }
    }
}
