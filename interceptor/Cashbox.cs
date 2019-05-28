using System;
using System.Collections.Generic;
using DrvFRLib;
using System.Timers;

namespace interceptor
{
    class Cashbox
    {
        public static DrvFR Driver;

        public static System.Timers.Timer repeatPrintingTimer = new System.Timers.Timer(5000);
        static int currentDrvPassword = 0;
        static string currentDocPack = String.Empty;
        public static int timeout = 159; // 1500ms

        public static DocPack manDocPackForPrinting;
        public static decimal manDocPackSumm;

        static Cashbox()
        {
            Driver = new DrvFR();
            Driver.FindDevice();
            repeatPrintingTimer.Elapsed += new ElapsedEventHandler(RepeatPrint);
        }

        public static void MakeBeep()
        {
            Driver.Beep();
        }

        public static void CheckConnection()
        {
            Driver.CheckConnection();
        }

        public static void PrepareDriver(bool admin = false)
        {
            Driver.Password = (admin ? CRM.adminPassword : CRM.password);
            Driver.Timeout = timeout;
        }

        public static void PrepareDriver(int pass)
        {
            Driver.Password = pass;
            Driver.Timeout = timeout;
        }

        public static bool RepeatDocument()
        {
            PrepareDriver();

            Driver.RepeatDocument();

            Log.AddWithCode("распечатка повтора");

            return (Driver.ResultCode == 0 ? true : false);
        }

        public static bool ContinueDocument()
        {
            PrepareDriver();

            Driver.ContinuePrint();

            Log.AddWithCode("продолжение печати");

            return (Driver.ResultCode == 0 ? true : false);
        }

        public static bool ReportCleaning()
        {
            PrepareDriver(admin: true);

            Driver.PrintReportWithCleaning();

            Log.AddWithCode("отчёт с гашением");

            return (Driver.ResultCode == 0 ? true : false);
        }

        public static bool ReportWithoutCleaning()
        {
            PrepareDriver(admin: true);

            Driver.PrintReportWithoutCleaning();

            Log.AddWithCode("отчёт без гашения");

            return (Driver.ResultCode == 0 ? true : false);
        }

        public static bool ReportDepartment()
        {
            PrepareDriver(admin: true);

            Driver.PrintDepartmentReport();

            Log.AddWithCode("отчёт по отделам");

            return (Driver.ResultCode == 0 ? true : false);
        }

        public static bool ReportTax()
        {
            PrepareDriver(admin: true);

            Driver.PrintTaxReport();

            Log.AddWithCode("отчёт по налогам");

            return (Driver.ResultCode == 0 ? true : false);
        }

        public static bool CancelDocument()
        {
            PrepareDriver();

            Driver.CancelCheck();

            Log.AddWithCode("отмена чека по кнопке");

            return (Driver.ResultCode == 0 ? true : false);
        }

        public static bool CashIncome(string summ)
        {
            PrepareDriver();

            Driver.Summ1 = DocPack.manualParseDecimal(summ);
            Driver.CashIncome();

            Log.AddWithCode("внесение денег (" + summ + ")");

            return (Driver.ResultCode == 0 ? true : false);
        }

        public static bool CashOutcome(string summ)
        {
            PrepareDriver();

            Driver.Summ1 = DocPack.manualParseDecimal(summ);
            Driver.CashOutcome();

            Log.AddWithCode("выплата денег (" + summ + ")");

            return (Driver.ResultCode == 0 ? true : false);
        }

        public static bool TablesBackup()
        {
            PrepareDriver(admin: true);

            for (int table = 1; table < 25; table += 1)
            {
                Driver.TableNumber = table;
                Driver.GetTableStruct();

                int rowMax = Driver.RowNumber;
                int fieldMax = Driver.FieldNumber;

                Log.Add("//////////// ТАБЛИЦА: " + Driver.TableName, "tables-backup");

                for (int row = 1; row <= rowMax; row += 1)
                    for (int field = 1; field <= fieldMax; field += 1)
                    {
                        Driver.TableNumber = table;
                        Driver.RowNumber = row;
                        Driver.FieldNumber = field;

                        Driver.GetFieldStruct();
                        Driver.ReadTable();

                        Log.Add(table + " " + row + " " + field + " " + Driver.ValueOfFieldString, "tables-backup");
                    }
            }

            return true;
        }

        static string TableField(int tableNumber, int fieldNumber, int rowNumber, string fieldValue = "")
        {
            Driver.TableNumber = tableNumber;
            Driver.FieldNumber = fieldNumber;
            Driver.RowNumber = rowNumber;

            Driver.GetFieldStruct();
            Driver.ReadTable();
            Driver.Timeout = timeout;

            if (!String.IsNullOrEmpty(fieldValue) && (fieldValue != Driver.ValueOfFieldString))
            {
                Log.AddWithCode("запись в поле кассы " + tableNumber + " " + fieldNumber + " " + rowNumber + " значения " + fieldValue);

                Driver.ValueOfFieldString = fieldValue;
                Driver.WriteTable();

                if (Driver.ResultCode != 0)
                    return String.Empty;
            }
                
            return Driver.ValueOfFieldString;
        }

        public static bool FailCashboxField(int tableNumber, int fieldNumber, int rowNumber, string fieldValue)
        {
            return (TableField(tableNumber, fieldNumber, rowNumber) != fieldValue);
        }

        public static bool resettingCashbox()
        {
            foreach (CashboxData field in CashboxData.data)
                if (String.IsNullOrEmpty(TableField(field.tableNumber, field.fieldNumber, field.rowNumber, field.fieldValue)))
                    return false;

            return true;
        }

        public static string[] CheckCashboxTables()
        {
            List<string> tablesCorrupted = new List<string>();

            foreach (CashboxData field in CashboxData.data)
                if (FailCashboxField(field.tableNumber, field.fieldNumber, field.rowNumber, field.fieldValue))
                    tablesCorrupted.Add(field.description);

            return tablesCorrupted.ToArray();
        }

        public static void PrintLine(string text = "", bool line = false)
        {
            PrepareDriver(currentDrvPassword);

            if (!String.IsNullOrEmpty(text))
            {
                Driver.StringForPrinting = text;
                Driver.PrintString();
            }

            if (line)
            {
                Driver.StringForPrinting = "------------------------------------";
                Driver.PrintString();
            }
            
        } 

        public static string PrintDocPack(DocPack doc, int MoneyType = -1,
            bool returnSale = false, decimal? MoneySumm = null)
        {
            currentDrvPassword = doc.CashierPass;
            currentDocPack = doc.AgrNumber;

            if (MoneyType != -1)
                doc.MoneyType = MoneyType;

            if (doc.Services.Count > 0 && doc.Services[0].ReturnShipping == 1)
                returnSale = true;

            PrepareDriver(currentDrvPassword);
            Driver.CheckType = (returnSale ? 2 : 0);

            Driver.OpenCheck();

            string sendingCheck = String.Empty;

            if (!String.IsNullOrEmpty(doc.Mobile))
                sendingCheck = doc.Mobile;
            else if (!String.IsNullOrEmpty(doc.Email))
                sendingCheck = doc.Email;

            if (!String.IsNullOrEmpty(sendingCheck))
            {
                Driver.CustomerEmail = sendingCheck;
                Driver.FNSendCustomerEmail();
            }

            PrintLine("Кассир: " + CRM.cashier, line: true);

            foreach (Service service in doc.Services)
            {
                Driver.Password = currentDrvPassword;
                Driver.Timeout = timeout;

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

                PrintLine(line: true);
            }

            PrepareDriver(currentDrvPassword);

            Driver.StringForPrinting = String.Empty;

            if (doc.MoneyType == 1)
            {
                Log.Add("тип оплаты: наличными");

                Driver.Summ1 = MoneySumm ?? doc.Money;
                Driver.Summ2 = 0;
            }
            else
            {
                Log.Add("тип оплаты: безнал (реальный: " + doc.MoneyType.ToString() + ")");

                Driver.Summ2 = MoneySumm ?? doc.Money;
                Driver.Summ1 = 0;
            }           

            Driver.CloseCheck();

            int checkClosingResult = Driver.ResultCode;
            string checkClosingErrorText = Driver.ResultCodeDescription;

            Log.AddWithCode("распечатка чека");

            if (checkClosingResult != 0)
            {
                PrepareDriver(currentDrvPassword);

                Driver.CancelCheck();

                Log.AddWithCode("отмена чека");

                Server.ShowActivity(busy: false);
            }
            else if (!MainWindow.TEST_VERSION)
            {
                repeatPrintingTimer.Enabled = true;
                repeatPrintingTimer.Start();
            }

            if (checkClosingResult == 0)
                return "OK:" + Driver.Change;
            else
            {
                CRM.SendError(checkClosingErrorText, doc.AgrNumber);

                return "ERR2:" + checkClosingErrorText;
            }
                
        }

        public static void RepeatPrint(object obj, ElapsedEventArgs e)
        {
            PrepareDriver(currentDrvPassword);

            Driver.RepeatDocument();

            int printSuccess = GetResultCode();

            Log.AddWithCode("распечатка повтора");

            if (printSuccess == 0) {
                repeatPrintingTimer.Enabled = false;
                repeatPrintingTimer.Stop();

                CRM.CashboxPaymentControl(agreement: currentDocPack);

                Server.ShowActivity(busy: false);
            }
        }

        public static int GetResultCode()
        {
            return Driver.ResultCode;
        }

        public static string GetResultLine()
        {
            return Driver.ResultCodeDescription;
        }

        public static int CurrentMode()
        {
            Driver.Timeout = timeout;
            Driver.GetECRStatus();

            return Driver.ECRMode;
        }

        public static string CurrentModeDescription()
        {
            Driver.Timeout = timeout;
            Driver.GetECRStatus();

            return Driver.ECRModeDescription;
        }

        public static void GetStatusData(out string port, out string speed, out string status,
            out string version, out string model)
        {
            string[] baudeRate = new string[] { "2400", "4800", "9600", "19200", "38400", "57600", "115200" };

            Driver.Timeout = timeout;
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
