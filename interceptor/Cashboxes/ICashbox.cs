using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace interceptor
{
    public interface ICashbox
    {
        DocPack manDocPackForPrinting { get; set; }
        decimal manDocPackSumm { get; set; }
        string serialNumber { get; set; }
        int currentDirectPassword { get; set; }

        string Name();
        void MakeBeep();
        void CheckConnection();
        string GetSerialNumber();
        bool RepeatDocument();
        bool ContinueDocument();
        bool ReportCleaning();
        bool ReportWithoutCleaning();
        bool ReportDepartment();
        bool ReportTax();
        bool ReportRegion(string type);
        bool CancelDocument();
        bool CashIncome(string summ);
        bool CashOutcome(string summ);
        bool resettingCashbox();
        string[] CheckCashboxTables();
        string PrintDocPack(DocPack doc, int MoneyType = -1,
            bool returnSale = false, decimal? MoneySumm = null, string sendingAddress = "");
        string GetResultLine();
        int GetResultCode();
        int CurrentMode();
        void GetStatusData(out string port, out string speed, out string version, out string model);
        string DirectPayment(decimal? moneyPrice, decimal? moneySumm, string forPrinting,
            string sending, int department, int moneyType, bool returnSale, bool VAT);
    }
}
