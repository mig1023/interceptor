using System;
using System.Collections.Generic;
using DrvFRLib;
using System.Timers;

namespace interceptor
{
    class ShtrihM : ICashbox
    {
        public static DrvFR Driver;

        public static System.Timers.Timer repeatPrintingTimer = new System.Timers.Timer(5000);
        static int currentDrvPassword = 0;
        public static int currentDirectPassword = 0;
        static string currentDocPack = String.Empty;
        public static int timeout = 159; // 1500ms

        public DocPack manDocPackForPrinting { get; set; }
        public decimal manDocPackSumm { get; set; }

        static ShtrihM()
        {
            Driver = new DrvFR();
            Driver.FindDevice();
            repeatPrintingTimer.Elapsed += new ElapsedEventHandler(RepeatPrint);
        }

        public void MakeBeep()
        {
            Driver.Beep();
        }

        public void CheckConnection()
        {
            Driver.CheckConnection();
        }

        public string GetSerialNumber()
        {
            Driver.ReadSerialNumber();
            return Driver.SerialNumber;
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

        public bool RepeatDocument()
        {
            PrepareDriver();

            Driver.RepeatDocument();

            Log.AddWithCode("распечатка повтора");

            return (Driver.ResultCode == 0 ? true : false);
        }

        public bool ContinueDocument()
        {
            PrepareDriver();

            Driver.ContinuePrint();

            Log.AddWithCode("продолжение печати");

            return (Driver.ResultCode == 0 ? true : false);
        }

        public bool ReportCleaning()
        {
            PrepareDriver(admin: true);

            Driver.PrintReportWithCleaning();

            Log.AddWithCode("отчёт с гашением");

            return (Driver.ResultCode == 0 ? true : false);
        }

        public bool ReportWithoutCleaning()
        {
            PrepareDriver(admin: true);

            Driver.PrintReportWithoutCleaning();

            Log.AddWithCode("отчёт без гашения");

            return (Driver.ResultCode == 0 ? true : false);
        }

        public bool ReportDepartment()
        {
            PrepareDriver(admin: true);

            Driver.PrintDepartmentReport();

            Log.AddWithCode("отчёт по отделам");

            return (Driver.ResultCode == 0 ? true : false);
        }

        public bool ReportTax()
        {
            PrepareDriver(admin: true);

            Driver.PrintTaxReport();

            Log.AddWithCode("отчёт по налогам");

            return (Driver.ResultCode == 0 ? true : false);
        }

        public bool ReportRegion(string type)
        {
            return false;
        }

        public bool CancelDocument()
        {
            PrepareDriver();

            Driver.CancelCheck();

            Log.AddWithCode("отмена чека по кнопке");

            return (Driver.ResultCode == 0 ? true : false);
        }

        public bool CashIncome(string summ)
        {
            PrepareDriver();

            Driver.Summ1 = DocPack.manualParseDecimal(summ);
            Driver.CashIncome();

            Log.AddWithCode("внесение денег (" + summ + ")");

            return (Driver.ResultCode == 0 ? true : false);
        }

        public bool CashOutcome(string summ)
        {
            PrepareDriver();

            Driver.Summ1 = DocPack.manualParseDecimal(summ);
            Driver.CashOutcome();

            Log.AddWithCode("выплата денег (" + summ + ")");

            return (Driver.ResultCode == 0 ? true : false);
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

        public bool resettingCashbox()
        {
            foreach (Settings field in Settings.data)
                if (String.IsNullOrEmpty(TableField(field.tableNumber, field.fieldNumber, field.rowNumber, field.fieldValue)))
                    return false;

            return true;
        }

        public string[] CheckCashboxTables()
        {
            List<string> tablesCorrupted = new List<string>();

            foreach (Settings field in Settings.data)
                if (FailCashboxField(field.tableNumber, field.fieldNumber, field.rowNumber, field.fieldValue))
                    tablesCorrupted.Add(field.description);

            return tablesCorrupted.ToArray();
        }

        public static void PrintLine(string text = "", bool line = false, int password = 0)
        {
            if (password == 0)
                password = currentDrvPassword;

            PrepareDriver(password);

            if (!String.IsNullOrEmpty(text))
            {
                Driver.StringForPrinting = text;
                Driver.PrintString();
            }

            if (line)
            {
                Driver.StringForPrinting = new String('-', 36);
                Driver.PrintString();
            }
            
        } 

        public string PrintDocPack(DocPack doc, int MoneyType = -1,
            bool returnSale = false, decimal? MoneySumm = null, string sendingAddress = "")
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

            if (!String.IsNullOrEmpty(sendingAddress))
                sendingCheck = sendingAddress;
            else if (!String.IsNullOrEmpty(doc.Mobile))
                sendingCheck = doc.Mobile;
            else if (!String.IsNullOrEmpty(doc.Email))
                sendingCheck = doc.Email;

            if (!String.IsNullOrEmpty(sendingCheck))
            {
                Driver.CustomerEmail = sendingCheck;
                Driver.FNSendCustomerEmail();
            }

            PrintLine("Кассир: " + CRM.cashier, line: true);

            if (doc.Region)
            {
                PrintLine("Договор: " + doc.AgrNumber);
                PrintLine("BankID : " + doc.BankID, line: true);
            }
                

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
            }
            else if (!MainWindow.TEST_VERSION && !doc.Region)
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

            int printSuccess = GetResultCodeInner();

            Log.AddWithCode("распечатка повтора");

            if (printSuccess == 0) {
                repeatPrintingTimer.Enabled = false;
                repeatPrintingTimer.Stop();

                CRM.CashboxPaymentControl(
                    agreement: currentDocPack,
                    paymentType: MainWindow.Cashbox.manDocPackForPrinting.MoneyType.ToString()
                );
            }
        }

        public static int GetResultCodeInner()
        {
            return Driver.ResultCode;
        }

        public int GetResultCode()
        {
            return Driver.ResultCode;
        }

        public string GetResultLine()
        {
            return Driver.ResultCodeDescription;
        }

        public int CurrentMode()
        {
            Driver.Timeout = timeout;
            Driver.GetECRStatus();

            return Driver.ECRMode;
        }

        public void GetStatusData(out string port, out string speed, out string version, out string model)
        {
            string[] baudeRate = new string[] { "2400", "4800", "9600", "19200", "38400", "57600", "115200" };

            Driver.Timeout = timeout;
            Driver.GetECRStatus();

            int portIndex = Driver.PortNumber + 1;

            port = "com" + portIndex.ToString();
            speed = baudeRate[Driver.BaudRate];
            version = Driver.ECRSoftVersion;

            Driver.GetDeviceMetrics();
            model = Driver.UDescription;
        }

        public string DirectPayment(decimal? moneyPrice, decimal? moneySumm, string forPrinting,
            string sending, int department, int moneyType, bool returnSale, bool VAT)
        {
            Log.Add("прямая печать чека (" + (returnSale ? "возврат" : "оплата") + ")");
            Log.Add("услуга '" + forPrinting + "', цена " + moneyPrice.ToString() + ", сумма " + moneySumm.ToString());

            Driver.CheckType = (returnSale ? 2 : 0);
            Driver.Password = currentDirectPassword;
            Driver.OpenCheck();

            if ((moneyType == 2) || returnSale)
                moneySumm = moneyPrice;

            if (!String.IsNullOrEmpty(sending))
            {
                PrepareDriver(currentDirectPassword);
                Driver.CustomerEmail = sending;
                Driver.FNSendCustomerEmail();

                Log.Add("отправка СМС/email на адрес: " + sending);
            }

            PrepareDriver(currentDirectPassword);
            PrintLine("Кассир: " + CRM.cashier, line: true, password: currentDirectPassword);
            Driver.Timeout = timeout;

            Driver.Quantity = 1;
            Driver.Price = moneyPrice ?? 0;
            Driver.StringForPrinting = forPrinting;

            Driver.Department = department;

            Driver.Tax1 = (VAT ? 1 : 0);
            Driver.Tax2 = 0;
            Driver.Tax3 = 0;
            Driver.Tax4 = 0;

            if (returnSale)
                Driver.ReturnSale();
            else
                Driver.Sale();

            PrintLine(line: true, password: currentDirectPassword);
            PrepareDriver(currentDirectPassword);

            if (moneyType == 1)
            {
                Log.Add("тип оплаты: наличными");

                Driver.Summ1 = moneySumm ?? 0;
                Driver.Summ2 = 0;
            }
            else
            {
                Log.Add("тип оплаты: безнал");

                Driver.Summ2 = moneySumm ?? 0;
                Driver.Summ1 = 0;
            }

            PrepareDriver(currentDirectPassword);
            Driver.StringForPrinting = String.Empty;
            Driver.CloseCheck();

            int checkClosingResult = Driver.ResultCode;
            string checkClosingErrorText = Driver.ResultCodeDescription;

            Log.AddWithCode("распечатка чека");

            if (checkClosingResult != 0)
            {
                PrepareDriver(currentDirectPassword);

                Driver.CancelCheck();

                Log.AddWithCode("отмена чека");
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
                CRM.SendError(checkClosingErrorText);

                return "ERR2:" + checkClosingErrorText;
            }
        }
    }
}
