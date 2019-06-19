namespace interceptor
{
    class Diagnostics
    {
        public static bool FailCashbox()
        {
            MainWindow.Cashbox.CheckConnection();

            Log.AddWithCode("проверка связи с кассой");

            return (MainWindow.Cashbox.GetResultCode() != 0 ? true : false );
        }

        public static string MakeBeepTest()
        {
            MainWindow.Cashbox.MakeBeep();

            return (MainWindow.Cashbox.GetResultCode() == 0 ? "OK" : "ERR2:Касса не отвечает");
        }
    }
}
