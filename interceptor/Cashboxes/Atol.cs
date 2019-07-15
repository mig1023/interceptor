using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atol.Drivers10.Fptr;
using System.Timers;

namespace interceptor
{
    class Atol : ICashbox
    {
        public static IFptr atolDriver = new Fptr();

        static int currentDrvPassword = 0;
        static string currentDocPack = String.Empty;

        public DocPack manDocPackForPrinting { get; set; }
        public decimal manDocPackSumm { get; set; }

        static Atol()
        {
            String version = atolDriver.version();
            atolDriver.open();
        }

        public void MakeBeep()
        {
            atolDriver.beep();
        }

        public void CheckConnection()
        {
            atolDriver.isOpened();
        }

        public static void PrepareDriver()
        {
            atolDriver.setParam(1021, "Кассир " + currentDrvPassword);
            atolDriver.operatorLogin();
        }

        public bool RepeatDocument()
        {
            PrepareDriver();

            atolDriver.setParam(Constants.LIBFPTR_PARAM_REPORT_TYPE, Constants.LIBFPTR_RT_LAST_DOCUMENT);

            int resultCode = atolDriver.report();

            Log.AddWithCode("распечатка повтора");

            return (resultCode < 0 ? false : true);
        }

        public bool ContinueDocument()
        {
            PrepareDriver();

            int resultCode = atolDriver.continuePrint();

            Log.AddWithCode("продолжение печати");

            return (resultCode < 0 ? false : true);
        }

        public bool ReportCleaning()
        {
            PrepareDriver();

            atolDriver.setParam(Constants.LIBFPTR_PARAM_REPORT_TYPE, Constants.LIBFPTR_RT_CLOSE_SHIFT);
            int resultCode = atolDriver.report();

            return (resultCode < 0 ? false : true);
        }

        public bool ReportWithoutCleaning()
        {
            PrepareDriver();

            atolDriver.setParam(Constants.LIBFPTR_PARAM_REPORT_TYPE, Constants.LIBFPTR_RT_X);
            int resultCode = atolDriver.report();

            return (resultCode < 0 ? false : true);
        }

        public bool ReportDepartment()
        {
            PrepareDriver();

            atolDriver.setParam(Constants.LIBFPTR_PARAM_REPORT_TYPE, Constants.LIBFPTR_RT_COMMODITIES_BY_DEPARTMENTS);
            int resultCode = atolDriver.report();

            return (resultCode < 0 ? false : true);
        }

        public bool ReportTax()
        {
            return false;
        }

        public bool CancelDocument()
        {
            PrepareDriver();

            Log.AddWithCode("отмена чека по кнопке");

            return (atolDriver.cancelReceipt() < 0 ? false : true);
        }

        public bool CashIncome(string summ)
        {
            PrepareDriver();

            atolDriver.setParam(Constants.LIBFPTR_PARAM_SUM, (uint)DocPack.manualParseDecimal(summ));
            atolDriver.cashIncome();

            Log.AddWithCode("внесение денег (" + summ + ")");

            return (atolDriver.printText() < 0 ? false : true);
        }

        public bool CashOutcome(string summ)
        {
            PrepareDriver();

            atolDriver.setParam(Constants.LIBFPTR_PARAM_SUM, (uint)DocPack.manualParseDecimal(summ));
            atolDriver.cashOutcome();

            Log.AddWithCode("выплата денег (" + summ + ")");

            return (atolDriver.printText() < 0 ? false : true);
        }

        public bool TablesBackup()
        {
            return true;
        }

        static string TableField(int tableNumber, int fieldNumber, int rowNumber, string fieldValue = "")
        {
            return String.Empty;
        }

        public static bool FailCashboxField(int tableNumber, int fieldNumber, int rowNumber, string fieldValue)
        {
            return false;
        }

        public bool resettingCashbox()
        {
            return true;
        }

        public string[] CheckCashboxTables()
        {
            return new string[0];
        }

        public static void PrintLine(string text = "", bool line = false)
        {

            if (!String.IsNullOrEmpty(text))
            {
                atolDriver.setParam(Constants.LIBFPTR_PARAM_TEXT, text);
                atolDriver.printText();

            }

            if (line)
            {
                atolDriver.setParam(Constants.LIBFPTR_PARAM_TEXT, new String('-', 36));
                atolDriver.printText();
            }
        }

        public string PrintDocPack(DocPack doc, int MoneyType = -1,
            bool returnSale = false, decimal? MoneySumm = null, string sendingAddress = "")
        {
            currentDrvPassword = doc.CashierPass;
            currentDocPack = doc.AgrNumber;

            if (MoneyType != -1)
                doc.MoneyType = MoneyType;

            PrepareDriver();

            int SELL_OR_RETURN = (returnSale ? Constants.LIBFPTR_RT_SELL_RETURN : Constants.LIBFPTR_RT_SELL);
            atolDriver.setParam(Constants.LIBFPTR_PARAM_RECEIPT_TYPE, SELL_OR_RETURN);

            atolDriver.openReceipt();

            string sendingCheck = String.Empty;

            if (!String.IsNullOrEmpty(sendingAddress))
                sendingCheck = sendingAddress;
            else if (!String.IsNullOrEmpty(doc.Mobile))
                sendingCheck = doc.Mobile;
            else if (!String.IsNullOrEmpty(doc.Email))
                sendingCheck = doc.Email;

            if (!String.IsNullOrEmpty(sendingCheck))
                atolDriver.setParam(1008, sendingCheck);

            PrintLine("Кассир: " + CRM.cashier, line: true);

            if (doc.Region)
            {
                PrintLine("Договор:" + doc.AgrNumber);
                PrintLine("BankID:" + doc.BankID, line: true);
            }

            foreach (Service service in doc.Services)
            {
                PrepareDriver();

                int VAT = (service.VAT == 1 ? Constants.LIBFPTR_TAX_VAT20 : Constants.LIBFPTR_TAX_NO);
                atolDriver.setParam(Constants.LIBFPTR_PARAM_TAX_TYPE, VAT);

                atolDriver.setParam(Constants.LIBFPTR_PARAM_DEPARTMENT, service.Department);
                atolDriver.setParam(Constants.LIBFPTR_PARAM_COMMODITY_NAME, service.Name);
                atolDriver.setParam(Constants.LIBFPTR_PARAM_PRICE, (float)service.Price);
                atolDriver.setParam(Constants.LIBFPTR_PARAM_QUANTITY, service.Quantity);
                atolDriver.setParam(Constants.LIBFPTR_PARAM_COMMODITY_PIECE, 1);
                atolDriver.registration();

                PrintLine(line: true);
            }

            PrepareDriver();

            atolDriver.setParam(Constants.LIBFPTR_PARAM_PAYMENT_SUM, (float)(MoneySumm ?? doc.Money));

            if (doc.MoneyType == 1)
            {
                Log.Add("тип оплаты: наличными");

                atolDriver.setParam(Constants.LIBFPTR_PARAM_PAYMENT_TYPE, Constants.LIBFPTR_PT_CASH);
            }
            else
            {
                Log.Add("тип оплаты: безнал");

                atolDriver.setParam(Constants.LIBFPTR_PARAM_PAYMENT_TYPE, Constants.LIBFPTR_PT_ELECTRONICALLY);
            }
            atolDriver.payment();

            atolDriver.setParam(Constants.LIBFPTR_PARAM_DATA_TYPE, Constants.LIBFPTR_DT_RECEIPT_STATE);
            atolDriver.queryData();
            double change = atolDriver.getParamDouble(Constants.LIBFPTR_PARAM_CHANGE);

            atolDriver.closeReceipt();

            Log.AddWithCode("распечатка чека");

            while (atolDriver.checkDocumentClosed() < 0)
            {
                Log.AddWithCode("отмена чека");
                return "ERR2:Не удалось проверить:" + atolDriver.errorDescription();
            }

            if (!atolDriver.getParamBool(Constants.LIBFPTR_PARAM_DOCUMENT_CLOSED))
            {
                atolDriver.cancelReceipt();
                Log.AddWithCode("отмена чека");
                return "ERR2:Документ не закрылся:" + atolDriver.errorDescription();
            }

            if (!atolDriver.getParamBool(Constants.LIBFPTR_PARAM_DOCUMENT_PRINTED))
            {
                while (atolDriver.continuePrint() < 0)
                {
                    Log.AddWithCode("отмена чека");
                    return "ERR2:Не удалось допечатать:" + atolDriver.errorDescription();
                }
            }

            Server.ShowActivity(busy: false);

            if (!MainWindow.TEST_VERSION && !doc.Region)
                RepeatPrint(null, null);

            return "OK:" + change.ToString();
        }

        public static void RepeatPrint(object obj, ElapsedEventArgs e)
        {
            atolDriver.setParam(Constants.LIBFPTR_PARAM_REPORT_TYPE, Constants.LIBFPTR_RT_LAST_DOCUMENT);
            atolDriver.report();
        }

        public int GetResultCode()
        {
            return atolDriver.errorCode();
        }

        public string GetResultLine()
        {
            return atolDriver.errorDescription();
        }

        public int CurrentMode()
        {
            atolDriver.setParam(Constants.LIBFPTR_PARAM_DATA_TYPE, Constants.LIBFPTR_DT_STATUS);
            atolDriver.queryData();
            return (int)atolDriver.getParamInt(Constants.LIBFPTR_PARAM_MODE);
        }

        public string CurrentModeDescription()
        {
            atolDriver.setParam(Constants.LIBFPTR_PARAM_DATA_TYPE, Constants.LIBFPTR_DT_STATUS);
            atolDriver.queryData();
            return atolDriver.getParamInt(Constants.LIBFPTR_PARAM_MODE).ToString();
        }

        public void GetStatusData(out string port, out string speed, out string status,
            out string version, out string model)
        {
            atolDriver.setParam(Constants.LIBFPTR_PARAM_DATA_TYPE, Constants.LIBFPTR_DT_UNIT_VERSION);
            atolDriver.setParam(Constants.LIBFPTR_PARAM_UNIT_TYPE, Constants.LIBFPTR_UT_FIRMWARE);
            atolDriver.setParam(Constants.LIBFPTR_PARAM_DATA_TYPE, Constants.LIBFPTR_DT_STATUS);

            atolDriver.queryData();

            version = atolDriver.getParamString(Constants.LIBFPTR_PARAM_UNIT_VERSION);
            model = atolDriver.getParamString(Constants.LIBFPTR_PARAM_MODEL_NAME);
            speed = atolDriver.getSingleSetting(Constants.LIBFPTR_SETTING_BAUDRATE);
            port = "com" + atolDriver.getSingleSetting(Constants.LIBFPTR_SETTING_COM_FILE);
            status = atolDriver.getParamInt(Constants.LIBFPTR_PARAM_MODE).ToString();
        }
    }
}
