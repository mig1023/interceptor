using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Text.RegularExpressions;
using System.Timers;

namespace interceptor
{
    public enum moveDirection { horizontal, vertical };

    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }

        List<string> manDocPack = new List<string>();
        List<Button> servButtonCleaningList = new List<Button>();
        List<Button> receptionButtonCleaningList = new List<Button>();
        public static System.Timers.Timer restoringSettingsCashbox = new System.Timers.Timer(5000);
        public Canvas returnFromErrorTo;

        public const bool TEST_VERSION = true;

        public const string CURRENT_VERSION_CLEAN = "1.e3";

        public static string CURRENT_VERSION =
            CURRENT_VERSION_CLEAN + (TEST_VERSION ? "-test" : String.Empty);

        public string updateDir = String.Empty;


        public MainWindow()
        {
            InitializeComponent();

            Instance = this;

            if (TEST_VERSION)
                this.Title += " <--- test";

            Log.Add("ПЕРЕХВАТЧИК ЗАПУЩЕН", freeLine: true);
            Log.Add("версия ---> " + CURRENT_VERSION, freeLineAfter: true);

            int MaxThreadsCount = Environment.ProcessorCount * 4;
            ThreadPool.SetMaxThreads(MaxThreadsCount, MaxThreadsCount);
            ThreadPool.SetMinThreads(2, 2);

            foreach (string buttonName in new[] {
                "closeCheck",
                "service", "service_urgent",
                "vipsrv",
                "concil", "concil_urg_r", "concil_n", "concil_n_age",
                "sms_status",
                "anketasrv",
                "printsrv",
                "photosrv",
                "xerox",
                "dhl", "insurance",
                "srv1", "srv2", "srv3", "srv4", "srv5", "srv6", "srv7", "srv8", "srv9"
            }) servButtonCleaningList.Add((Button)mainGrid.FindName(buttonName));

            foreach (string buttonName in new[] {
                "anketasrvR", "printsrvR", "photosrvR", "xeroxR"
            }) receptionButtonCleaningList.Add((Button)mainGrid.FindName(buttonName));

            login.Focus();
        }

        public void MoveCanvas(Canvas moveCanvas, Canvas prevCanvas, moveDirection direction = moveDirection.horizontal)
        {
            double left = (direction == moveDirection.horizontal ? 0 : moveCanvas.Margin.Left);
            double top = (direction == moveDirection.vertical ? 0 : moveCanvas.Margin.Top);

            ThicknessAnimation move = new ThicknessAnimation();
            move.Duration = TimeSpan.FromSeconds(0.2);
            move.From = moveCanvas.Margin;

            move.To = new Thickness(left, top, moveCanvas.Margin.Right, moveCanvas.Margin.Bottom);

            moveCanvas.BeginAnimation(MarginProperty, move);

            left = (direction == moveDirection.horizontal ?
                prevCanvas.Margin.Left - moveCanvas.Margin.Left : prevCanvas.Margin.Left);
            top = (direction == moveDirection.vertical ?
                prevCanvas.Margin.Top - moveCanvas.Margin.Top : prevCanvas.Margin.Top);

            move.From = prevCanvas.Margin;

            move.To = new Thickness(left, top, prevCanvas.Margin.Right, prevCanvas.Margin.Bottom );

            prevCanvas.BeginAnimation(MarginProperty, move);
        }

        private void check_Click(object sender, RoutedEventArgs e)
        {
            manDocPack.Clear();

            UpdateCenters();

            MoveCanvas(
                moveCanvas: checkPlace,
                prevCanvas: mainPlace
            );
        }

        private void UpdateStatuses()
        {
            string port, speed, status, version, model;

            status1.Content = CURRENT_VERSION;
            status2.Content = CRM.GetMyIP();
            status3.Content = CRM.CRM_URL_BASE;

            Cashbox.GetStatusData(out port, out speed, out status, out version, out model);

            status4.Content = port;
            status5.Content = speed;
            status6.Content = model;
            status7.Content = version;
            status8.Content = status.ToLower();
        }

        private void UpdateCenters()
        {
            allCenters.Items.Clear();

            foreach (string center_name in CRM.GetAllCenters(login.Text))
                allCenters.Items.Add(center_name);

            allCenters.SelectedIndex = 0;
        }

        private void UpdateVTypes()
        {
            if (allCenters.SelectedItem == null) return;

            allVisas.Items.Clear();

            foreach (string visa_name in CRM.GetAllVType(allCenters.SelectedItem.ToString()))
                allVisas.Items.Add(visa_name);

            allVisas.SelectedIndex = 0;
        }

        private void backToMainFromCheck_Click(object sender, RoutedEventArgs e)
        {
            MoveCanvas(
                moveCanvas: mainPlace,
                prevCanvas: checkPlace
            );
            
            Server.ShowActivity(busy: false);
            Cashbox.manDocPackForPrinting = null;

            CleanCheck();
        }

        private void status_Click(object sender, RoutedEventArgs e)
        {
            MoveCanvas(
                moveCanvas: statusPlace,
                prevCanvas: mainPlace
            );
        }

        private void backToMainFromStatus_Click(object sender, RoutedEventArgs e)
        {
            MoveCanvas(
                moveCanvas: mainPlace,
                prevCanvas: statusPlace
            );
        }

        private void sendLogin_Click(object sender, RoutedEventArgs e)
        {
            Canvas canvasToGo = mainPlace;

            string updateData = AutoUpdate.NeedUpdating();

            string passwordHash = CRM.GenerateMySQLHash(password.Password);

            string[] tablesChecked = Cashbox.CheckCashboxTables();

            if (!CRM.CrmAuthentication(login.Text, passwordHash))
            {
                loginFailText.Content = CRM.loginError;
                returnFromErrorTo = loginPlace;
                canvasToGo = loginFail;

                Log.Add("ошибка входа с логином " + login.Text);
            }
            else if (Diagnostics.FailCashbox())
            {
                loginFailText.Content = "Ошибка подключения к кассе. Проверьте подключение и перезапустите приложение";
                returnFromErrorTo = loginPlace;
                canvasToGo = loginFail;

                Log.Add("ошибка подключения к кассе");
            }
            else if (tablesChecked.Count() != 0)
            {
                int index = 0;

                foreach (string field in tablesChecked)
                {
                    index += 1;
                    settingText2.Items.Add(index.ToString() + ". " + field);
                }
                    
                returnFromErrorTo = loginPlace;
                canvasToGo = cashboxSettingsFail;

                Log.Add("ошибка настроек кассы");

                if (Cashbox.CurrentMode() != 4)
                {
                    settingText5.Visibility = Visibility.Visible;
                    reportAndRessetting.Content = "закрыть смену, распечатать отчёт и перенастроить";
                }
                else
                {
                    settingText5.Visibility = Visibility.Hidden;
                    reportAndRessetting.Content = "перенастроить таблицы настроек";
                }
            }
            else if (updateData != String.Empty)
            {
                updateDir = AutoUpdate.Update(updateData);

                if (updateDir != String.Empty)
                {
                    updateText.Content = "Програме необходимо обновиться. В процессе обновления программа будет перезапущена";
                    needUpdateRestart.Background = (Brush)new BrushConverter().ConvertFromString("#28CC51");

                    Log.Add("необходимо обновление с перезапуском", "update");
                }
                else
                {
                    updateText.Content = "В процессе обновления программы произошла ошибка загрузки необходимых данных!\nПожалуйста, обратитесь к системным администраторам";
                    updateButton.Visibility = Visibility.Hidden;
                    needUpdateRestart.Background = (Brush)new BrushConverter().ConvertFromString("#FFFF4E4E");

                    Log.Add("ошибка обновления: контрольные суммы файлов не совпали", "update");
                }

                returnFromErrorTo = loginPlace;
                canvasToGo = needUpdateRestart;
            }
            else
            {
                Server.StartServer();
                switchOn.Background = Brushes.LimeGreen;
                CRM.currentLogin = login.Text;
                CRM.currentPassword = passwordHash;

                status10.Content = login.Text.Replace("_", "__");
            }

            MoveCanvas(
                moveCanvas: canvasToGo,
                prevCanvas: loginPlace,
                direction: moveDirection.vertical
            );

            UpdateStatuses();
        }

        private void returnFromError_Click(object sender, RoutedEventArgs e)
        {
            password.Password = String.Empty;
            placeholderPass.Visibility = Visibility.Visible;

            MoveCanvas(
                moveCanvas: returnFromErrorTo,
                prevCanvas: loginFail,
                direction: moveDirection.vertical
            );
        }

        private void reportCleaning_Click(object sender, RoutedEventArgs e)
        {
            if (!Cashbox.ReportCleaning())
                moveToErrorFromReports(Cashbox.GetResultLine());
        }

        private void reportWithoutCleaning_Click(object sender, RoutedEventArgs e)
        {
            if (!Cashbox.ReportWithoutCleaning())
                moveToErrorFromReports(Cashbox.GetResultLine());
        }

        private void reportDepartment_Click(object sender, RoutedEventArgs e)
        {
            if (!Cashbox.ReportDepartment())
                moveToErrorFromReports(Cashbox.GetResultLine());
        }

        private void repeatDocument_Click(object sender, RoutedEventArgs e)
        {
            if (!Cashbox.RepeatDocument())
                moveToErrorFromReports(Cashbox.GetResultLine());
        }

        private void reportTax_Click(object sender, RoutedEventArgs e)
        {
            if (!Cashbox.ReportTax())
                moveToErrorFromReports(Cashbox.GetResultLine());
        }

        private void BlockCheckButton(bool block)
        {
            moneyForCheck.IsEnabled = (block ? true : false);
            printCheckMoney.IsEnabled = (block ? true : false);
            printCheckCard.IsEnabled = (block ? true : false);
            returnSale.IsEnabled = (block ? true : false);
            returnSaleCard.IsEnabled = (block ? true : false);

            foreach (Button serv in servButtonCleaningList)
                serv.IsEnabled = (block ? false : true);

            allCenters.IsEnabled = (block ? false : true);
            allVisas.IsEnabled = (block ? false : true);
            returnDate.IsEnabled = (block ? false : true);
            moneyForDHL.IsEnabled = (block ? false : true);
            allCenters.IsEnabled = (block ? false : true);
            allVisas.IsEnabled = (block ? false : true);
        }

        private void BlockRCheckButton(bool block)
        {
            moneyForRCheck.IsEnabled = (block ? true : false);
            printRCheckMoney.IsEnabled = (block ? true : false);
            printRCheckCard.IsEnabled = (block ? true : false);

            foreach (Button serv in receptionButtonCleaningList)
                serv.IsEnabled = false;

            appNumber.IsEnabled = (block ? false : true);
        }

        private void сloseCheck_Click(object sender, RoutedEventArgs e)
        {
            string sendingSuccess = CRM.SendManDocPack(
                manDocPack, login.Text, CRM.password, 1, moneyForCheck.Text,
                allCenters.Text, allVisas.Text, returnDate.Text
            );

            string[] sendingData = sendingSuccess.Split('|');

            if (sendingData[0] == "OK")
            {
                Log.Add("успешно закрыт чек");

                BlockCheckButton(block: true);
            }
            else if (sendingData[0] == "WARNING")
            {
                Log.Add("некоторые услуги из чека не имеют цены: " + sendingData[1]);

                MessageBoxResult result = MessageBox.Show(
                    "Услуги не имеют цену по прайслисту выбранного центра:\n" +
                    sendingData[1] + "." +
                    "\nТакие услуги не будут отображены в чеке. Продолжить?",
                    "Внимание!",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                    BlockCheckButton(block: true);
                else
                    CleanCheck();
            }
            else
            {
                Log.Add("во время формирования чека произошла ошибка: " + sendingData[1]);

                loginFailText.Content = sendingData[1];
                returnFromErrorTo = checkPlace;

                MoveCanvas(
                    moveCanvas: loginFail,
                    prevCanvas: checkPlace,
                    direction: moveDirection.vertical
                );
            }
        }

        private void addService_Click(object sender, RoutedEventArgs e)
        {
            Button Service = sender as Button;

            if (Service.Name == "dhl")
                manDocPack.Add(Service.Name + "=" + moneyForDHL.Text);
            else if (Service.Name == "insurance")
                manDocPack.Add(Service.Name + "=" + moneyForInsurance.Text);
            else
                manDocPack.Add(Service.Name);

            Service.FontWeight = FontWeights.Bold;
            Service.FontSize = 14;

            Match ReqMatch = Regex.Match(Service.Content.ToString(), @"^([^\d]+)\s\((\d+)\)");

            if (ReqMatch.Success)
            {
                int servCount = Int32.Parse(ReqMatch.Groups[2].Value);

                servCount += 1;

                Service.Content = ReqMatch.Groups[1].Value + " (" + servCount.ToString() + ")";
            }
            else
                Service.Content = Service.Content + " (1)";
        }

        private void addRService_Click(object sender, RoutedEventArgs e)
        {
            Button Service = sender as Button;

            manDocPack.Add(Service.Name.TrimEnd('R'));

            Service.FontWeight = FontWeights.Bold;
            Service.FontSize = 20;

            Match ReqMatch = Regex.Match(Service.Content.ToString(), @"^([^\d]+)\s\((\d+)\)");

            if (ReqMatch.Success)
            {
                int servCount = Int32.Parse(ReqMatch.Groups[2].Value);

                servCount += 1;

                Service.Content = ReqMatch.Groups[1].Value + " (" + servCount.ToString() + ")";
            }
            else
                Service.Content = Service.Content + " (1)";
        }

        private void allCenters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateVTypes();
        }

        private void CleanCheck()
        {
            foreach (Button serv in servButtonCleaningList)
            {
                int bracketIndex = serv.Content.ToString().IndexOf('(');

                if (bracketIndex > 0)
                    serv.Content = serv.Content.ToString().Remove(bracketIndex - 1);

                serv.FontWeight = FontWeights.Regular;
                serv.FontSize = 12;
            }

            BlockCheckButton(block: false);

            manDocPack.Clear();

            moneyForDHL.Text = "0.00";
            moneyForCheck.Text = "0.00";
            moneyForInsurance.Text = "0.00";
            total.Content = String.Empty;
            totalR.Content = String.Empty;
        }

        private void CleanRCheck()
        {
            foreach (Button serv in receptionButtonCleaningList)
            {
                int bracketIndex = serv.Content.ToString().IndexOf('(');

                if (bracketIndex > 0)
                    serv.Content = serv.Content.ToString().Remove(bracketIndex - 1);

                serv.FontWeight = FontWeights.Regular;
                serv.FontSize = 18;
            }

            BlockRCheckButton(block: false);

            manDocPack.Clear();

            moneyForRCheck.Text = "0.00";
            total.Content = String.Empty;
            totalR.Content = String.Empty;
            appNumber.Text = String.Empty;
        }

        private void ShowError(Canvas from, string error)
        {
            loginFailText.Content = error;
            returnFromErrorTo = from;

            MoveCanvas(
                moveCanvas: loginFail,
                prevCanvas: from,
                direction: moveDirection.vertical
            );
        }

        public void CheckError(string[] result, Canvas place)
        {
            if (result[0] == "OK")
                CleanCheck();
            else
                ShowError(place, "Ошибка кассы: " + result[1]);
        }

        private void printCheckMoney_Click(object sender, RoutedEventArgs e)
        {
            decimal money = DocPack.manualParseDecimal(moneyForCheck.Text);

            if (CheckMoneyFail(money))
                return;

            string[] result = Cashbox.PrintDocPack(
                Cashbox.manDocPackForPrinting, MoneyType: 1, MoneySumm: money
            ).Split(':');

            CheckError(result, checkPlace);
        }

        private void printCheckCard_Click(object sender, RoutedEventArgs e)
        {
            string[] result = Cashbox.PrintDocPack(
                Cashbox.manDocPackForPrinting, MoneyType: 2, MoneySumm: Cashbox.manDocPackSumm
            ).Split(':');

            CheckError(result, checkPlace);
        }

        private void returnSale_Click(object sender, RoutedEventArgs e)
        {
            if (CheckDateFail(returnDate.Text))
            {
                CleanCheck();
                return;
            }

            string[] result = Cashbox.PrintDocPack(
                Cashbox.manDocPackForPrinting, returnSale: true, MoneySumm: Cashbox.manDocPackSumm
            ).Split(':');

            CheckError(result, checkPlace);
        }

        private void password_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                sendLogin_Click(null, null);
        }

        private void moveToErrorFromReports(string Line)
        {
            loginFailText.Content = Line;
            returnFromErrorTo = statusPlace;

            MoveCanvas(
                moveCanvas: loginFail,
                prevCanvas: statusPlace,
                direction: moveDirection.vertical
            );
        }

        private void continueDocument_Click(object sender, RoutedEventArgs e)
        {
            if (!Cashbox.ContinueDocument())
                moveToErrorFromReports(Cashbox.GetResultLine());
        }

        private void cashIncome_Click(object sender, RoutedEventArgs e)
        {
            if (!Cashbox.CashIncome(moneyForIncome.Text))
                moveToErrorFromReports(Cashbox.GetResultLine());
        }

        private void cashOutcome_Click(object sender, RoutedEventArgs e)
        {
            if (!Cashbox.CashOutcome(moneyForOutcome.Text))
                moveToErrorFromReports(Cashbox.GetResultLine());
        }

        private void backToMainFromInfo_Click(object sender, RoutedEventArgs e)
        {
            MoveCanvas(
                moveCanvas: mainPlace,
                prevCanvas: systemInfoPlace,
                direction: moveDirection.vertical
            );
        }

        private void statusImage_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            status_Click(null, null);
        }

        private void checkImage_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            check_Click(null, null);
        }

        private void receptionImage_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            reception_Click(null, null);
        }

        private void cancelDocument_Click(object sender, RoutedEventArgs e)
        {
            if (!Cashbox.CancelDocument())
                moveToErrorFromReports(Cashbox.GetResultLine());
        }

        private void backToLoginFromSettingsFial_Click(object sender, RoutedEventArgs e)
        {
            MoveCanvas(
                moveCanvas: returnFromErrorTo,
                prevCanvas: cashboxSettingsFail,
                direction: moveDirection.vertical
            );
        }

        private void reportAndRessetting_Click(object sender, RoutedEventArgs e)
        {
            if (Cashbox.CurrentMode() != 4)
                Cashbox.ReportCleaning();

            restoringSettingsCashbox.Elapsed += new ElapsedEventHandler(RestoreSetting);
            restoringSettingsCashbox.Enabled = true;
            restoringSettingsCashbox.Start();

            Cashbox.resettingCashbox();

            MoveCanvas(
                moveCanvas: returnFromErrorTo,
                prevCanvas: cashboxSettingsFail,
                direction: moveDirection.vertical
            );
        }

        public static void RestoreSetting(object obj, ElapsedEventArgs e)
        {
            if (Cashbox.resettingCashbox())
            {
                restoringSettingsCashbox.Enabled = false;
                restoringSettingsCashbox.Stop();
            }
        }

        private void switchOn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MoveCanvas(
                moveCanvas: systemInfoPlace,
                prevCanvas: mainPlace,
                direction: moveDirection.vertical
            );
        }

        private void reception_Click(object sender, RoutedEventArgs e)
        {
            manDocPack.Clear();

            UpdateCenters();

            MoveCanvas(
                moveCanvas: receptionPlace,
                prevCanvas: mainPlace
            );

            appNumber.Focus();
        }

        private void backToMainFromReception_Click(object sender, RoutedEventArgs e)
        {
            MoveCanvas(
                moveCanvas: mainPlace,
                prevCanvas: receptionPlace
            );

            Server.ShowActivity(busy: false);
            Cashbox.manDocPackForPrinting = null;

            CleanRCheck();
        }

        private void appNumber_KeyUp(object sender, KeyEventArgs e)
        {
            appNumber.Text = Regex.Replace(appNumber.Text, @"[^0-9/]", String.Empty);
            string appNumberClean = Regex.Replace(appNumber.Text, @"[^0-9]", String.Empty);

            if ((appNumberClean.Length == 15) || (appNumberClean.Length == 9))
            {
                anketasrvR.IsEnabled = true;
                printsrvR.IsEnabled = true;
                photosrvR.IsEnabled = true;
                xeroxR.IsEnabled = true;
            }
            else
            {
                anketasrvR.IsEnabled = false;
                printsrvR.IsEnabled = false;
                photosrvR.IsEnabled = false;
                xeroxR.IsEnabled = false;
            }
        }

        private void closeRCheck_Click(object sender, RoutedEventArgs e)
        {
            string appNumberClean = Regex.Replace(appNumber.Text, @"[^0-9]", String.Empty);

            string sendingSuccess = CRM.SendManDocPack(
                manDocPack, login.Text, CRM.password, 1, moneyForRCheck.Text,
                appNumberClean, allVisas.Text, returnDate.Text, reception: true
            );

            string[] sendingData = sendingSuccess.Split('|');

            if (sendingData[0] == "OK")
            {
                Log.Add("успешно закрыт чек ресепшена");

                BlockRCheckButton(block: true);
            }
            else if (sendingData[0] == "WARNING")
            {
                Log.Add("некоторые услуги из чека не имеют цены: " + sendingData[1]);

                MessageBoxResult result = MessageBox.Show(
                    "Услуги не имеют цену по прайслисту выбранного центра: " +
                    sendingData[1] + "." +
                    "Такие услуги не будут отображены в чеке. Продолжить?",
                    "Внимание!",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                    BlockRCheckButton(block: true);
                else
                    CleanRCheck();
            }
            else
            {
                Log.Add("во время формирования чека произошла ошибка: " + sendingData[1]);

                loginFailText.Content = sendingData[1];
                returnFromErrorTo = receptionPlace;

                MoveCanvas(
                    moveCanvas: loginFail,
                    prevCanvas: receptionPlace,
                    direction: moveDirection.vertical
                );
            }
        }

        private void printRCheckMoney_Click(object sender, RoutedEventArgs e)
        {
            decimal money = DocPack.manualParseDecimal(moneyForRCheck.Text);

            if (CheckMoneyFail(money))
                return;

            string[] result = Cashbox.PrintDocPack(
                Cashbox.manDocPackForPrinting, MoneyType: 1, MoneySumm: money
            ).Split(':');

            CheckError(result, receptionPlace);

            if (result[0] == "OK")
                getAppInfoAndPrintRecepeit(Cashbox.manDocPackSumm.ToString());
        }

        private bool CheckMoneyFail(decimal money)
        {
            if (money > 0)
                return false;

            MessageBoxResult resultMsg = MessageBox.Show(
                "Введена нулевая сумма оплаты чека. Продолжить?", "Внимание!",
                MessageBoxButton.YesNo, MessageBoxImage.Question
            );

            if (resultMsg == MessageBoxResult.Yes)
                return false;
            else
                return true;
        }

        private bool CheckDateFail(string date)
        {
            if (returnDate.Text != String.Empty)
                return false;

            MessageBoxResult resultMsg = MessageBox.Show(
                "Дата договора не введена, будет использована текущая. Продолжить?", "Внимание!",
                MessageBoxButton.YesNo, MessageBoxImage.Question
            );

            if (resultMsg == MessageBoxResult.Yes)
                return false;
            else
                return true;
        }

    private void printRCheckCard_Click(object sender, RoutedEventArgs e)
        {
            string[] result = Cashbox.PrintDocPack(
                Cashbox.manDocPackForPrinting, MoneyType: 2, MoneySumm: Cashbox.manDocPackSumm
            ).Split(':');

            CheckError(result, receptionPlace);

            if (result[0] == "OK")
                getAppInfoAndPrintRecepeit(Cashbox.manDocPackSumm.ToString());
        }

        private void getAppInfoAndPrintRecepeit(string summ)
        {
            Receipt.PrintReceipt(CRM.AppNumberData(appNumber.Text, summ), Cashbox.manDocPackForPrinting);
            CleanRCheck();
        }

        private void appNumberClean_Click(object sender, RoutedEventArgs e)
        {
            appNumber.Text = String.Empty;
            CleanRCheck();
            appNumber_KeyUp(null, null);
            appNumber.Focus();
        }

        private void returnSaleCard_Click(object sender, RoutedEventArgs e)
        {
            if (CheckDateFail(returnDate.Text))
            {
                CleanCheck();
                return;
            }

            string[] result = Cashbox.PrintDocPack(
                Cashbox.manDocPackForPrinting, returnSale: true, MoneyType: 2, MoneySumm: Cashbox.manDocPackSumm
            ).Split(':');

            CheckError(result, checkPlace);
        }

        private void updateButton_Click(object sender, RoutedEventArgs e)
        {
            AutoUpdate.StartUpdater();
        }

        private void placeholderLogin_MouseDown(object sender, MouseButtonEventArgs e)
        {
            login.Focus();
        }

        private void placeholderPass_MouseDown(object sender, MouseButtonEventArgs e)
        {
            password.Focus();
        }

        private void login_KeyUp(object sender, KeyEventArgs e)
        {
            if (login.Text == String.Empty)
                placeholderLogin.Visibility = Visibility.Visible;
            else
                placeholderLogin.Visibility = Visibility.Hidden;
        }

        private void password_KeyUp(object sender, KeyEventArgs e)
        {
            if (password.Password == String.Empty)
                placeholderPass.Visibility = Visibility.Visible;
            else
                placeholderPass.Visibility = Visibility.Hidden;
        }
    }
}
