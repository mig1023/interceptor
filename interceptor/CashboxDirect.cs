using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DrvFRLib;

namespace interceptor
{
    class CashboxDirect
    {
        static DrvFR Driver = CashboxShtrih.Driver;
        static int timeout = CashboxShtrih.timeout;

        public static int currentDirectPassword = 0;

        public static void PrepareDriver(int pass)
        {
            Driver.Password = pass;
            Driver.Timeout = timeout;
        }

        public static void PrintLine(string text = "", bool line = false)
        {
            PrepareDriver(currentDirectPassword);

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

        public static string DirectPayment(decimal? moneyPrice, decimal? moneySumm, string forPrinting,
            string sending, int department, int moneyType, bool returnSale, bool VAT)
        {
            Log.Add("прямая печать чека (" + (returnSale ? "возврат" : "оплата") + ")" );
            Log.Add("услуга '" + forPrinting + "', цена " + moneyPrice.ToString() + ", сумма " + moneySumm.ToString());

            Driver.CheckType = (returnSale ? 2 : 0);
            Driver.Password = currentDirectPassword;
            Driver.OpenCheck();

            if ((moneyType == 2) || returnSale)
                moneySumm = moneyPrice;

            if (!String.IsNullOrEmpty(sending))
            {
                Driver.Password = currentDirectPassword;
                Driver.CustomerEmail = sending;
                Driver.FNSendCustomerEmail();

                Log.Add("отправка СМС/email на адрес: " + sending);
            }

            Driver.Password = currentDirectPassword;
            PrintLine("Кассир: " + CRM.cashier, line: true);
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

            PrintLine(line: true);
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

                Server.ShowActivity(busy: false);
            }
            else if (!MainWindow.TEST_VERSION)
            {
                CashboxShtrih.repeatPrintingTimer.Enabled = true;
                CashboxShtrih.repeatPrintingTimer.Start();
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
