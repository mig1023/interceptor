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
        static IFptr atolDriver = new Fptr();

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

        public static void PrepareDriver(bool admin = false)
        {
            // таймаут
            atolDriver.setParam(1021, "Кассир Иванов И.");
            atolDriver.setParam(1203, "123456789047");
            atolDriver.operatorLogin();
        }

        public bool RepeatDocument()
        {
            PrepareDriver();

            // повтор чека

            Log.AddWithCode("распечатка повтора");

            return (atolDriver.printText() < 0 ? false : true);
        }

        public bool ContinueDocument()
        {
            PrepareDriver();

            // продолжение печати

            Log.AddWithCode("продолжение печати");

            return (atolDriver.printText() < 0 ? false : true);
        }

        //////////////////////////////////////////////

        public bool ReportCleaning()
        {
            // отчёт с гашением
            return true;
        }

        public bool ReportWithoutCleaning()
        {
            // отчёт без гаешния
            return true;
        }

        public bool ReportDepartment()
        {
            // отчёт по отделам
            return true;
        }

        public bool ReportTax()
        {
            // отчёт по налогам
            return true;
        }

        public bool CancelDocument()
        {
            PrepareDriver();

            atolDriver.cancelReceipt();

            Log.AddWithCode("отмена чека по кнопке");

            return (atolDriver.printText() < 0 ? false : true);
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
            // резервное копирование таблиц
            return true;
        }

        static string TableField(int tableNumber, int fieldNumber, int rowNumber, string fieldValue = "")
        {
            // резервное копирование таблиц
            return String.Empty;
        }

        public static bool FailCashboxField(int tableNumber, int fieldNumber, int rowNumber, string fieldValue)
        {
            // проверка поля
            return false;
        }

        public bool resettingCashbox()
        {
            // проверка полей
            return true;
        }

        public string[] CheckCashboxTables()
        {
            // проверка таблиц
            return new string[0];
        }

        public static void PrintLine(string text = "", bool line = false)
        {
            PrepareDriver(false); // !!!

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

            if (doc.Services.Count > 0 && doc.Services[0].ReturnShipping == 1)
                returnSale = true;

            PrepareDriver();

            //Driver.CheckType = (returnSale ? 2 : 0);

            atolDriver.setParam(Constants.LIBFPTR_PARAM_RECEIPT_TYPE, Constants.LIBFPTR_RT_SELL);
            atolDriver.openReceipt();

            string sendingCheck = String.Empty;

            if (!String.IsNullOrEmpty(sendingAddress))
                sendingCheck = sendingAddress;
            else if (!String.IsNullOrEmpty(doc.Mobile))
                sendingCheck = doc.Mobile;
            else if (!String.IsNullOrEmpty(doc.Email))
                sendingCheck = doc.Email;

            if (!String.IsNullOrEmpty(sendingCheck))
            {
                atolDriver.setParam(1008, sendingCheck);
            }

            PrintLine("Кассир: " + CRM.cashier, line: true);

            if (doc.Region)
            {
                PrintLine("Договор: " + doc.AgrNumber);
                PrintLine("BankID : " + doc.BankID, line: true);
            }


            foreach (Service service in doc.Services)
            {
                PrepareDriver();

                //отдел
                //Driver.Department = service.Department;

                atolDriver.setParam(Constants.LIBFPTR_PARAM_COMMODITY_NAME, service.Name);
                atolDriver.setParam(Constants.LIBFPTR_PARAM_PRICE, (int)service.Price);
                atolDriver.setParam(Constants.LIBFPTR_PARAM_QUANTITY, service.Quantity);
                atolDriver.setParam(Constants.LIBFPTR_PARAM_TAX_TYPE, Constants.LIBFPTR_TAX_VAT10);
                atolDriver.registration();

                //Driver.Tax1 = service.VAT;
                //Driver.Tax2 = 0;
                //Driver.Tax3 = 0;
                //Driver.Tax4 = 0;

                //if (returnSale)
                //    Driver.ReturnSale();
                //else
                //    Driver.Sale();

                PrintLine(line: true);
            }

            PrepareDriver();

            //Driver.StringForPrinting = String.Empty;

            atolDriver.setParam(Constants.LIBFPTR_PARAM_PAYMENT_SUM, (uint)(MoneySumm ?? doc.Money));

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

            atolDriver.closeReceipt();

            Log.AddWithCode("распечатка чека");

            while (atolDriver.checkDocumentClosed() < 0)
            {
                Log.AddWithCode("отмена чека:" + atolDriver.errorDescription());
                return "ERR2:Не удалось проверить:" + atolDriver.errorDescription();
            }

            if (!atolDriver.getParamBool(Constants.LIBFPTR_PARAM_DOCUMENT_CLOSED))
            {
                atolDriver.cancelReceipt();
                Log.AddWithCode("отмена чека:" + atolDriver.errorDescription());
                return "ERR2:Документ не закрылся:" + atolDriver.errorDescription();
            }

            if (!atolDriver.getParamBool(Constants.LIBFPTR_PARAM_DOCUMENT_PRINTED))
            {
                while (atolDriver.continuePrint() < 0)
                {
                    Log.AddWithCode("отмена чека:" + atolDriver.errorDescription());
                    return "ERR2:Не удалось допечатать:" + atolDriver.errorDescription();
                }
            }

            Server.ShowActivity(busy: false);

            if (!MainWindow.TEST_VERSION && !doc.Region)
            {
                // повтор
            }

            atolDriver.setParam(Constants.LIBFPTR_PARAM_DATA_TYPE, Constants.LIBFPTR_DT_RECEIPT_STATE);
            atolDriver.queryData();

            return "OK:" + atolDriver.getParamDouble(Constants.LIBFPTR_PARAM_CHANGE); ;
        }

        public static void RepeatPrint(object obj, ElapsedEventArgs e)
        {
            // повторная печать документа
        }

        public int GetResultCode()
        {
            // код ошибки
            return 0;
        }

        public string GetResultLine()
        {
            // текст ошибки
            return "error";
        }

        public int CurrentMode()
        {
            // текущий режим
            return 0;
        }

        public string CurrentModeDescription()
        {
            // описание
            return "CurrentModeDescription";
        }

        public void GetStatusData(out string port, out string speed, out string status,
            out string version, out string model)
        {
            string[] baudeRate = new string[] { "2400", "4800", "9600", "19200", "38400", "57600", "115200" };

            port = "test";
            speed = baudeRate[6];
            status = "test";
            version = "test";

            model = "test";
        }
    }
}
