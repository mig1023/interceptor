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
        private bool cancelOpenReceipt = false;

        public int currentDirectPassword { get; set; }
        public DocPack manDocPackForPrinting { get; set; }
        public decimal manDocPackSumm { get; set; }
        public string serialNumber { get; set; }

        static Atol()
        {
            String version = atolDriver.version();
            atolDriver.open();
        }

        public string Name()
        {
            return "Атол";
        }

        public void MakeBeep()
        {
            atolDriver.beep();
        }

        public void CheckConnection()
        {
            atolDriver.isOpened();
        }

        public string GetSerialNumber()
        {
            atolDriver.setParam(Constants.LIBFPTR_PARAM_DATA_TYPE, Constants.LIBFPTR_DT_SERIAL_NUMBER);
            atolDriver.queryData();

            return atolDriver.getParamString(Constants.LIBFPTR_PARAM_SERIAL_NUMBER);
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

            atolDriver.setParam(Constants.LIBFPTR_PARAM_REPORT_TYPE, Constants.LIBFPTR_RT_DEPARTMENTS);
            int resultCode = atolDriver.report();

            return (resultCode < 0 ? false : true);
        }

        public bool ReportRegion(string reportType)
        {
            Dictionary<uint, string> agreements = new Dictionary<uint, string>();

            atolDriver.setParam(Constants.LIBFPTR_PARAM_FN_DATA_TYPE, Constants.LIBFPTR_FNDT_LAST_DOCUMENT);
            atolDriver.fnQueryData();
            uint lastDoc = atolDriver.getParamInt(Constants.LIBFPTR_PARAM_DOCUMENT_NUMBER);

            atolDriver.setParam(Constants.LIBFPTR_PARAM_FN_DATA_TYPE, Constants.LIBFPTR_FNDT_SHIFT);
            atolDriver.fnQueryData();
            uint docsInLine = atolDriver.getParamInt(Constants.LIBFPTR_PARAM_RECEIPT_NUMBER);

            uint firstDoc = lastDoc - docsInLine + 1;

            string getDataVar = reportType;

            if (reportType == "3")
                getDataVar = "1";
            else if (reportType == "4")
                getDataVar = "2";

            string[] FDData = CRM.GetFDData(firstDoc, getDataVar);

            if (FDData.Length <= 0 || FDData[0] == "ERR")
                return false;
            else
            {
                for (int i = 1; i < FDData.Length; i++)
                {
                    if (String.IsNullOrEmpty(FDData[i]))
                        continue;

                    string[] fd = FDData[i].Split(':');
                    agreements.Add(uint.Parse(fd[0]), fd[1]);
                }
            }

            PrintLine(line: true);
            PrintLine("ОТЧЁТ до ЗАКРЫТИЯ СМЕНЫ");
            PrintLine(line: true);

            for (uint i = firstDoc; i <= lastDoc; i++)
            {
                atolDriver.setParam(Constants.LIBFPTR_PARAM_FN_DATA_TYPE, Constants.LIBFPTR_FNDT_DOCUMENT_BY_NUMBER);
                atolDriver.setParam(Constants.LIBFPTR_PARAM_DOCUMENT_NUMBER, i);
                atolDriver.fnQueryData();

                uint documentType = atolDriver.getParamInt(Constants.LIBFPTR_PARAM_FN_DOCUMENT_TYPE);
                uint documentNumber = atolDriver.getParamInt(Constants.LIBFPTR_PARAM_DOCUMENT_NUMBER);
                DateTime dateTime = atolDriver.getParamDateTime(Constants.LIBFPTR_PARAM_DATE_TIME);
                String fiscalSign = atolDriver.getParamString(Constants.LIBFPTR_PARAM_FISCAL_SIGN);

                double sum = atolDriver.getParamDouble(1020);
                uint type = atolDriver.getParamInt(1054);
                string doc = (agreements.ContainsKey(documentNumber) ? agreements[documentNumber] : "не найден");

                if (reportType == "3" || reportType == "4")
                {
                    PrintLine(
                    String.Format(
                        "{0}.{1} {2}:{3} {4} {5} {6}",
                        dateTime.Day, dateTime.Month, dateTime.Hour, dateTime.Minute,
                        documentNumber, doc, (type == 2 ? "-" : "") + sum.ToString()
                        )
                    );
                }
                else
                {
                    PrintLine(line: true);
                    PrintLine("документ ФД: " + documentNumber.ToString());
                    PrintLine("договор: " + (agreements.ContainsKey(documentNumber) ? agreements[documentNumber] : "не найден"));
                    PrintLine("тип: " + (type == 1 ? "приход" : (type == 2 ? "возврат" : "ИНОЕ")));
                    PrintLine("дата: " + dateTime.ToString());
                    PrintLine("ФП: " + fiscalSign.ToString());
                    PrintLine("сумма чека: " + (type == 2 ? "-" : "") + sum.ToString());
                }
            }

            PrintLine(line: true);

            for (int i = 0; i < 10; i++)
                PrintLine(" ");

            atolDriver.cut();

            return true;
        }

        public bool ReportTax()
        {
            atolDriver.printCliche();

            atolDriver.setParam(Constants.LIBFPTR_PARAM_DATA_TYPE, Constants.LIBFPTR_DT_STATUS);
            atolDriver.queryData();

            uint operatorID = atolDriver.getParamInt(Constants.LIBFPTR_PARAM_OPERATOR_ID);
            DateTime dateTime = atolDriver.getParamDateTime(Constants.LIBFPTR_PARAM_DATE_TIME);
            String serialNumber = atolDriver.getParamString(Constants.LIBFPTR_PARAM_SERIAL_NUMBER);

            atolDriver.setParam(Constants.LIBFPTR_PARAM_FN_DATA_TYPE, Constants.LIBFPTR_FNDT_REG_INFO);
            atolDriver.fnQueryData();

            String organizationVATIN = atolDriver.getParamString(1018);
            String fnsUrl = atolDriver.getParamString(1060);
            String registrationNumber = atolDriver.getParamString(1037);

            atolDriver.setParam(Constants.LIBFPTR_PARAM_FN_DATA_TYPE, Constants.LIBFPTR_FNDT_FN_INFO);
            atolDriver.fnQueryData();

            String fnSerial = atolDriver.getParamString(Constants.LIBFPTR_PARAM_SERIAL_NUMBER);

            PrintLine(dateTime.ToShortDateString() + " " + dateTime.ToShortTimeString());
            PrintLine("ЗН ККТ: " + serialNumber);
            PrintLine("ИНН: " + organizationVATIN);
            PrintLine("КАССИР: " + CRM.cashier);
            PrintLine("РН ККТ: " + registrationNumber);
            PrintLine("ФН: " + fnSerial);

            PrintLine(line: true);
            PrintLine("ОТЧЁТ по НАЛОГАМ ЗА СМЕНУ");

            Dictionary<int, string> pTypes = new Dictionary<int, string> { [1] = "ЧЕК ПРИХОДА", [2] = "ЧЕК ВОЗВРАТА", [7] = "ЧЕК КОРРЕКЦИИ" };
            Dictionary<int, string> vatTypes = new Dictionary<int, string> { [7] = "Сумма НДС 20%", [6] = "Сумма без НДС" };

            foreach (int pType in new List<int> { Constants.LIBFPTR_RT_SELL, Constants.LIBFPTR_RT_SELL_RETURN, Constants.LIBFPTR_RT_SELL_CORRECTION })
            {
                PrintLine(line: true);
                PrintLine(pTypes[pType]);

                foreach (int vatType in new List<int> { Constants.LIBFPTR_TAX_VAT20, Constants.LIBFPTR_TAX_NO })
                {
                    atolDriver.setParam(Constants.LIBFPTR_PARAM_DATA_TYPE, Constants.LIBFPTR_DT_SHIFT_TAX_SUM);
                    atolDriver.setParam(Constants.LIBFPTR_PARAM_RECEIPT_TYPE, pType);
                    atolDriver.setParam(Constants.LIBFPTR_PARAM_TAX_TYPE, vatType);
                    atolDriver.queryData();

                    double sum = atolDriver.getParamDouble(Constants.LIBFPTR_PARAM_SUM);

                    PrintLine(vatTypes[vatType].PadRight(16) + sum.ToString());
                }
            }

            PrintLine(line: true);

            for (int i = 0; i < 10; i++)
                PrintLine(" ");

            atolDriver.cut();

            return true;
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
            PrepareDriver();

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

            if (cancelOpenReceipt)
            {
                cancelOpenReceipt = false;

                if (!atolDriver.getParamBool(Constants.LIBFPTR_PARAM_DOCUMENT_CLOSED))
                {
                    atolDriver.cancelReceipt();
                    Log.AddWithCode("поздняя отмена документа");
                }
            }

            if (MoneyType != -1)
                doc.MoneyType = MoneyType;

            PrepareDriver();

            double SELL_OR_RETURN = (returnSale ? Constants.LIBFPTR_RT_SELL_RETURN : Constants.LIBFPTR_RT_SELL);
            atolDriver.setParam(Constants.LIBFPTR_PARAM_RECEIPT_TYPE, SELL_OR_RETURN);

            atolDriver.openReceipt();

            Log.AddWithCode("открытие чека");

            string sendingCheck = String.Empty;

            if (!String.IsNullOrEmpty(sendingAddress))
                sendingCheck = sendingAddress;
            else if (!String.IsNullOrEmpty(doc.Mobile))
                sendingCheck = doc.Mobile;
            else if (!String.IsNullOrEmpty(doc.Email))
                sendingCheck = doc.Email;

            if (!String.IsNullOrEmpty(sendingCheck))
            {
                Log.Add("отправка СМС/email на адрес: " + sendingCheck);
                atolDriver.setParam(1008, sendingCheck);
            }

            PrintLine("Кассир: " + CRM.cashier, line: true);

            if (doc.Region)
            {
                PrintLine("Договор:" + doc.AgrNumber);
                PrintLine("BankID:" + doc.BankID, line: true);
            }

            foreach (Service service in doc.Services)
            {
                PrepareDriver();

                double VAT = (service.VAT == 1 ? Constants.LIBFPTR_TAX_VAT20 : Constants.LIBFPTR_TAX_NO);
                atolDriver.setParam(Constants.LIBFPTR_PARAM_TAX_TYPE, VAT);

                atolDriver.setParam(Constants.LIBFPTR_PARAM_DEPARTMENT, service.Department);
                atolDriver.setParam(Constants.LIBFPTR_PARAM_COMMODITY_NAME, service.Name);
                atolDriver.setParam(Constants.LIBFPTR_PARAM_PRICE, (double)service.Price);
                atolDriver.setParam(Constants.LIBFPTR_PARAM_QUANTITY, service.Quantity);
                atolDriver.setParam(Constants.LIBFPTR_PARAM_COMMODITY_PIECE, 1);
                atolDriver.registration();

                PrintLine(line: true);
            }

            PrepareDriver();

            atolDriver.setParam(Constants.LIBFPTR_PARAM_PAYMENT_SUM, (double)(MoneySumm ?? doc.Money));

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
                Log.AddWithCode("проверка статуса чека");
                return "ERR2:Не удалось проверить - " + atolDriver.errorDescription();
            }

            if (!atolDriver.getParamBool(Constants.LIBFPTR_PARAM_DOCUMENT_CLOSED))
            {
                Log.Add("документ не закрылся");
                atolDriver.cancelReceipt();
                Log.AddWithCode("отмена чека");

                if (atolDriver.errorCode() != 0)
                    cancelOpenReceipt = true;

                return "ERR2:Документ не закрылся - " + atolDriver.errorDescription();
            }

            string fdForRegion = String.Empty;

            if (doc.Region)
            {
                atolDriver.setParam(Constants.LIBFPTR_PARAM_FN_DATA_TYPE, Constants.LIBFPTR_FNDT_LAST_RECEIPT);
                atolDriver.fnQueryData();
                fdForRegion = ":" + atolDriver.getParamInt(Constants.LIBFPTR_PARAM_DOCUMENT_NUMBER).ToString();
            }

            if (!atolDriver.getParamBool(Constants.LIBFPTR_PARAM_DOCUMENT_PRINTED))
            {
                while (atolDriver.continuePrint() < 0)
                {
                    Log.AddWithCode("чек не удалось допечатать");

                    int errCode = (doc.Region ? 5 : 2);

                    return "ERR" + errCode.ToString() + ":Не удалось допечатать - " + atolDriver.errorDescription() + fdForRegion;
                }
            }

            if (!MainWindow.TEST_VERSION && !doc.Region)
                RepeatPrint(null, null);

            return "OK:" + change.ToString() + fdForRegion;
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
            return 0;
        }

        public void GetStatusData(out string port, out string speed, out string version, out string model)
        {
            atolDriver.setParam(Constants.LIBFPTR_PARAM_DATA_TYPE, Constants.LIBFPTR_DT_UNIT_VERSION);
            atolDriver.setParam(Constants.LIBFPTR_PARAM_UNIT_TYPE, Constants.LIBFPTR_UT_FIRMWARE);
            atolDriver.setParam(Constants.LIBFPTR_PARAM_DATA_TYPE, Constants.LIBFPTR_DT_STATUS);

            atolDriver.queryData();

            version = atolDriver.getParamString(Constants.LIBFPTR_PARAM_UNIT_VERSION);
            model = atolDriver.getParamString(Constants.LIBFPTR_PARAM_MODEL_NAME);
            speed = atolDriver.getSingleSetting(Constants.LIBFPTR_SETTING_BAUDRATE);
            port = "com" + atolDriver.getSingleSetting(Constants.LIBFPTR_SETTING_COM_FILE);
        }

        public string DirectPayment(decimal? moneyPrice, decimal? moneySumm, string forPrinting,
            string sending, int department, int moneyType, bool returnSale, bool VAT)
        {
            Log.Add("прямая печать чека (" + (returnSale ? "возврат" : "оплата") + ")");
            Log.Add("услуга '" + forPrinting + "', цена " + moneyPrice.ToString() + ", сумма " + moneySumm.ToString());

            currentDrvPassword = currentDirectPassword;

            if ((moneyType == 2) || returnSale)
                moneySumm = moneyPrice;

            PrepareDriver();

            int SELL_OR_RETURN = (returnSale ? Constants.LIBFPTR_RT_SELL_RETURN : Constants.LIBFPTR_RT_SELL);
            atolDriver.setParam(Constants.LIBFPTR_PARAM_RECEIPT_TYPE, SELL_OR_RETURN);

            atolDriver.openReceipt();

            string sendingCheck = String.Empty;

            if (!String.IsNullOrEmpty(sending))
            {
                Log.Add("отправка СМС/email на адрес: " + sending);
                atolDriver.setParam(1008, sending);
            }

            PrintLine("Кассир: " + CRM.cashier, line: true);

            PrepareDriver();

            atolDriver.setParam(Constants.LIBFPTR_PARAM_QUANTITY, 1);
            atolDriver.setParam(Constants.LIBFPTR_PARAM_PRICE, (float)(moneyPrice ?? 0));
            atolDriver.setParam(Constants.LIBFPTR_PARAM_COMMODITY_NAME, forPrinting);
            atolDriver.setParam(Constants.LIBFPTR_PARAM_DEPARTMENT, department);

            int directVAT = (VAT ? Constants.LIBFPTR_TAX_VAT20 : Constants.LIBFPTR_TAX_NO);
            atolDriver.setParam(Constants.LIBFPTR_PARAM_TAX_TYPE, directVAT);

            atolDriver.setParam(Constants.LIBFPTR_PARAM_COMMODITY_PIECE, 1);
            atolDriver.registration();

            PrepareDriver();

            atolDriver.setParam(Constants.LIBFPTR_PARAM_PAYMENT_SUM, (float)moneySumm);

            if (moneyType == 1)
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

            if (!MainWindow.TEST_VERSION)
                RepeatPrint(null, null);

            return "OK:" + change.ToString();
        }
    }
}
