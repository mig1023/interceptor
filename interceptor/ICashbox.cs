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

        void MakeBeep(); // ok
        void CheckConnection(); // ok
        bool RepeatDocument(); // tmp
        bool ContinueDocument(); // tmp
        bool ReportCleaning();
        bool ReportWithoutCleaning();
        bool ReportDepartment();
        bool ReportTax();
        bool CancelDocument();
        bool CashIncome(string summ);
        bool CashOutcome(string summ);
        bool TablesBackup();
        bool resettingCashbox();
        string[] CheckCashboxTables();
        string PrintDocPack(DocPack doc, int MoneyType = -1,
            bool returnSale = false, decimal? MoneySumm = null, string sendingAddress = "");
        string GetResultLine();
        int GetResultCode();
        int CurrentMode();
        string CurrentModeDescription();
        void GetStatusData(out string port, out string speed, out string status,
            out string version, out string model);
    }
}
