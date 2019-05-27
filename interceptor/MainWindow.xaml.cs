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
using System.Globalization;

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

        public const string CURRENT_VERSION_CLEAN = "1.e4";

        public static string CURRENT_VERSION =
            CURRENT_VERSION_CLEAN + (TEST_VERSION ? "-test" : String.Empty);

        public string updateDir = String.Empty;

        public static string PROTOCOL_PASS = "";
        public static int PROTOCOL_PORT = 80;

        public enum fieldsErrors { noError, valueError, clickError, emptySummError };

        public MainWindow()
        {
            InitializeComponent();

            Instance = this;

            if (TEST_VERSION)
                this.Title += " <--- test";

            versionLabel.Content = "версия " + CURRENT_VERSION;

            Log.Add("ПЕРЕХВАТЧИК ЗАПУЩЕН", freeLine: true);
            Log.Add("версия ---> " + CURRENT_VERSION, freeLineAfter: true);

            int MaxThreadsCount = Environment.ProcessorCount * 4;
            ThreadPool.SetMaxThreads(MaxThreadsCount, MaxThreadsCount);
            ThreadPool.SetMinThreads(2, 2);

            foreach (Button button in new List<Button> {
                closeCheck, service, service_urgent, vipsrv, concil, concil_urg_r,
                concil_n, concil_n_age, sms_status, anketasrv, printsrv, photosrv,
                xerox, dhl, insuranceRGS, insuranceKL, srv1, srv2, srv3, srv4, srv5,
                srv6, srv7, srv8, srv9
            })
                servButtonCleaningList.Add(button);

            foreach (Button button in new List<Button> { anketasrvR, printsrvR, photosrvR, xeroxR })
                receptionButtonCleaningList.Add(button);

            login.Focus();
        }

        private void WindowResize(object Sender, EventArgs e, int newHeight)
        {
            Application.Current.MainWindow.Height = newHeight;

            foreach (Canvas canvas in new List<Canvas> () { needUpdateRestart, cashboxSettingsFail, loginFail, loginPlace })
                canvas.Margin = new Thickness(0, newHeight, 0, 0);
        }

        private void HidePrevCanvas(object Sender, EventArgs e, Canvas prevCanvas)
        {
            prevCanvas.Visibility = Visibility.Hidden;
        }

        public void MoveCanvas(Canvas moveCanvas, Canvas prevCanvas, moveDirection direction = moveDirection.horizontal,
            int? newHeight = null)
        {
            double currentHeight = Application.Current.MainWindow.Height;

            moveCanvas.Visibility = Visibility.Visible;

            if ((newHeight != null) && (currentHeight > newHeight))
                 WindowResize(null, null, (int)newHeight);

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

            move.Completed += new EventHandler((sender, e) => HidePrevCanvas(sender, e, prevCanvas));

            if ((newHeight != null) && (currentHeight < newHeight))
                move.Completed += new EventHandler((sender, e) => WindowResize(sender, e, (int)newHeight));

            prevCanvas.BeginAnimation(MarginProperty, move);
        }

        private void check_Click(object sender, RoutedEventArgs e)
        {
            manDocPack.Clear();

            UpdateCenters();

            MoveCanvas(
                moveCanvas: checkPlace,
                prevCanvas: mainPlace,
                newHeight: 700
            );
        }

        private void UpdateStatuses()
        {
            string port, speed, status, version, model;

            status1.Content = CURRENT_VERSION;
            status2.Content = CRM.GetMyIP() + " ( порт " + PROTOCOL_PORT + " )";
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
                prevCanvas: checkPlace,
                newHeight: 420
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
            else if (!String.IsNullOrEmpty(updateData) && !TEST_VERSION)
            {
                updateDir = AutoUpdate.Update(updateData);

                if (!String.IsNullOrEmpty(updateDir))
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
            moneyForCheck.IsEnabled = block;
            returnDate.IsEnabled = !block;

            foreach (Button button in new List<Button>() { printCheckMoney, printCheckCard, returnSale, returnSaleCard })
                button.IsEnabled = block;

            foreach (Button serv in servButtonCleaningList)
                serv.IsEnabled = !block;

            foreach (ComboBox combobox in new List<ComboBox>() { allVisas, allCenters, allVisas, allCenters })
                combobox.IsEnabled = !block;

            foreach (TextBox textbox in new List<TextBox>() { moneyForDHL, moneyForInsuranceRGS, moneyForInsuranceKL  })
                textbox.IsEnabled = !block;
        }

        private void BlockRCheckButton(bool block)
        {
            moneyForRCheck.IsEnabled = (block ? true : false);
            appNumber.IsEnabled = (block ? false : true);
            appNumberClean.IsEnabled = (block ? false : true);

            foreach (Button button in new List<Button>() { printRCheckMoney, printRCheckCard })
                button.IsEnabled = (block ? true : false);

            foreach (Button serv in receptionButtonCleaningList)
                serv.IsEnabled = false;
        }

        private fieldsErrors CheckServiceClickOrEmptyFail(string service, TextBox field)
        {
            double currentSumm = 0;
            bool fieldClicked = false;

            CultureInfo cultureInfo = new CultureInfo("en-US", true);

            bool parse = Double.TryParse(field.Text, NumberStyles.Any, cultureInfo.NumberFormat, out currentSumm);

            if (!parse)
                return fieldsErrors.valueError;

            foreach (string serv in manDocPack)
                if (serv.StartsWith(service))
                    fieldClicked = true;

            if (fieldClicked && currentSumm == 0)
                return fieldsErrors.emptySummError;

            if (!fieldClicked && currentSumm > 0)
                return fieldsErrors.clickError;

            return fieldsErrors.noError;
        }

        private void CheckEmptyServiceFail(string service, string fieldName, TextBox field,
            string fieldEmptyOld, string fieldClickOld, string fieldParsedOld,
            out string fieldEmptyNew, out string fieldClickNew, out string fieldParsedNew)
        {
            string tmpFieldEmpty = fieldEmptyOld;
            string tmpFieldNotClick = fieldClickOld;
            string tmpFieldNotParsed = fieldParsedOld;

            switch (CheckServiceClickOrEmptyFail(service, field))
            {
                case fieldsErrors.emptySummError:
                    tmpFieldEmpty += (String.IsNullOrEmpty(tmpFieldEmpty) ? String.Empty : ", ") + fieldName;
                    break;
                case fieldsErrors.clickError:
                    tmpFieldNotClick += (String.IsNullOrEmpty(tmpFieldNotClick) ? String.Empty : ", ") + fieldName;
                    break;
                case fieldsErrors.valueError:
                    tmpFieldNotParsed += (String.IsNullOrEmpty(tmpFieldNotParsed) ? String.Empty : ", ") + fieldName;
                    break;
            }

            fieldEmptyNew = tmpFieldEmpty;
            fieldClickNew = tmpFieldNotClick;
            fieldParsedNew = tmpFieldNotParsed;
        }

        private bool CheckEmptyServicesFail()
        {
            string errorField = String.Empty;
            string errorEmpty = String.Empty;
            string errorClick = String.Empty;

            CheckEmptyServiceFail("insuranceRGS", "'страховка РГС'", moneyForInsuranceRGS,
                errorEmpty, errorClick, errorField, out errorEmpty, out errorClick, out errorField);

            CheckEmptyServiceFail("insuranceKL", "'страховка Капитал Лайф'", moneyForInsuranceKL,
                errorEmpty, errorClick, errorField, out errorEmpty, out errorClick, out errorField);

            CheckEmptyServiceFail("dhl", "'доставка'", moneyForDHL, errorEmpty,
                errorClick, errorField, out errorEmpty, out errorClick, out errorField);

            if (!String.IsNullOrEmpty(errorField))
                if (MessageBoxes.ServFieldFail(errorField) != MessageBoxResult.Yes)
                    return true;

            if (!String.IsNullOrEmpty(errorEmpty))
                if (MessageBoxes.ServSummEmpty(errorEmpty) != MessageBoxResult.Yes)
                    return true;

            if (!String.IsNullOrEmpty(errorClick))
                if (MessageBoxes.ServNoClick(errorClick) != MessageBoxResult.Yes)
                    return true;

            return false;
        }

        private void сloseCheck_Click(object sender, RoutedEventArgs e)
        {
            if (CheckEmptyServicesFail())
                return;

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

                if (MessageBoxes.NullInServices(sendingData[1]) == MessageBoxResult.Yes)
                    BlockCheckButton(block: true);
                else
                    CleanCheck();
            }
            else
            {
                Log.Add("во время формирования чека произошла ошибка: " + sendingData[1]);

                ShowError(checkPlace, sendingData[1]);
            }
        }

        private void addService_Click(object sender, RoutedEventArgs e)
        {
            Button Service = sender as Button;

            if (Service.Name == "dhl")
                manDocPack.Add(Service.Name + "=" + moneyForDHL.Text);
            else if (Service.Name == "insuranceRGS")
                manDocPack.Add(Service.Name + "=" + moneyForInsuranceRGS.Text);
            else if (Service.Name == "insuranceKL")
                manDocPack.Add(Service.Name + "=" + moneyForInsuranceKL.Text);
            else
                manDocPack.Add(Service.Name);

            Service.FontWeight = FontWeights.Bold;
            Service.FontSize = 14;

            Match ReqMatch = Regex.Match(Service.Content.ToString(), @"^([^\(]+)\s\((\d+)\)$");

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

        private void ButtonsClean(List<Button> buttons, int fontSize)
        {
            foreach (Button serv in buttons)
            {
                int bracketIndex = serv.Content.ToString().IndexOf('(');

                if (bracketIndex > 0)
                    serv.Content = serv.Content.ToString().Remove(bracketIndex - 1);

                serv.FontWeight = FontWeights.Regular;
                serv.FontSize = fontSize;
            }
        }

        private void CleanCheck()
        {
            ButtonsClean(
                buttons: servButtonCleaningList,
                fontSize: 12
            );

            BlockCheckButton(block: false);

            manDocPack.Clear();

            moneyForDHL.Text = "0.00";
            moneyForCheck.Text = "0.00";
            moneyForInsuranceRGS.Text = "0.00";
            moneyForInsuranceKL.Text = "0.00";
            total.Content = String.Empty;
            totalR.Content = String.Empty;
            returnDate.Text = String.Empty;
        }

        private void CleanRCheck()
        {
            ButtonsClean(
                buttons: receptionButtonCleaningList,
                fontSize: 18
            );

            BlockRCheckButton(block: false);

            manDocPack.Clear();

            moneyForRCheck.Text = "0.00";
            total.Content = String.Empty;
            totalR.Content = String.Empty;
            appNumber.Text = String.Empty;
        }

        private void ShowError(Canvas from, string error, string agrNumber = "")
        {
            loginFailText.Content = error;
            returnFromErrorTo = from;

            MoveCanvas(
                moveCanvas: loginFail,
                prevCanvas: from,
                direction: moveDirection.vertical
            );

            CRM.SendError(error, agrNumber);
        }

        public void CheckError(string[] result, Canvas place)
        {
            if (result[0] == "OK")
            {
                CleanCheck();
                MessageBoxes.ChangeMessage(result[1]);
            }
            else
                ShowError(place, "Ошибка кассы: " + result[1], Cashbox.manDocPackForPrinting.AgrNumber);
        }

        private void printCheckMoney_Click(object sender, RoutedEventArgs e)
        {
            decimal money = DocPack.manualParseDecimal(moneyForCheck.Text);

            if (CheckMoneyFail(money) || CheckAnotherDateFail(returnDate.Text))
                return;

            string[] result = Cashbox.PrintDocPack(
                Cashbox.manDocPackForPrinting, MoneyType: 1, MoneySumm: money
            ).Split(':');

            CheckError(result, checkPlace);
        }

        private void printCheckCard_Click(object sender, RoutedEventArgs e)
        {
            if (CheckAnotherDateFail(returnDate.Text))
                return;
                
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
            ShowError(statusPlace, Line);
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

        private void moneyImage_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            money_Click(null, null);
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

            MessageBoxes.WaitingForResetting();

            Cashbox.TablesBackup();

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

        private void money_Click(object sender, RoutedEventArgs e)
        {
            MoveCanvas(
                moveCanvas: moneyPlace,
                prevCanvas: mainPlace
            );
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
                foreach(Button button in new List<Button>() { anketasrvR, printsrvR, photosrvR, xeroxR })
                    button.IsEnabled = true;
            else
                foreach (Button button in new List<Button>() { anketasrvR, printsrvR, photosrvR, xeroxR })
                    button.IsEnabled = false;
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

                if (MessageBoxes.NullInServices(sendingData[1]) == MessageBoxResult.Yes)
                    BlockRCheckButton(block: true);
                else
                    CleanRCheck();
            }
            else
            {
                Log.Add("во время формирования чека произошла ошибка: " + sendingData[1]);

                ShowError(receptionPlace, sendingData[1]);
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

            if (MessageBoxes.NullSummCash() == MessageBoxResult.Yes)
                return false;
            else
                return true;
        }

        private bool CheckDateFail(string date)
        {
            if (!String.IsNullOrEmpty(returnDate.Text))
                return false;

            if (MessageBoxes.NullReturnDate() == MessageBoxResult.Yes)
                return false;
            else
                return true;
        }

        private bool CheckAnotherDateFail(string date)
        {
            if (String.IsNullOrEmpty(returnDate.Text))
                return false;
            else
            {
                ShowError(checkPlace, "Нельзя оплачивать даговор, указывая дату оплаты; оставьте это поле пустым");
                CleanCheck();

                return true;
            }
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
            string error = Receipt.PrintReceipt(CRM.AppNumberData(appNumber.Text, summ), Cashbox.manDocPackForPrinting);
            CleanRCheck();

            if (!String.IsNullOrEmpty(error))
                ShowError(receptionPlace, error);
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
            if (String.IsNullOrEmpty(login.Text))
                placeholderLogin.Visibility = Visibility.Visible;
            else
                placeholderLogin.Visibility = Visibility.Hidden;
        }

        private void password_KeyUp(object sender, KeyEventArgs e)
        {
            if (String.IsNullOrEmpty(password.Password))
                placeholderPass.Visibility = Visibility.Visible;
            else
                placeholderPass.Visibility = Visibility.Hidden;
        }

        private void backToMainFromMoneyPlace_Click(object sender, RoutedEventArgs e)
        {
            MoveCanvas(
                moveCanvas: mainPlace,
                prevCanvas: moneyPlace
            );
        }

        private void paymentPrepared()
        {
            if ((section.SelectedItem != null) && (stringForPrinting.Text != String.Empty))
                moneyForDirectPayment.IsEnabled = true;
            else
                moneyForDirectPayment.IsEnabled = false;
        }

        private void section_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            paymentPrepared();
        }

        private void stringForPrinting_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            paymentPrepared();
        }

        private void stringForPrinting_KeyUp(object sender, KeyEventArgs e)
        {
            paymentPrepared();
        }

        private void moneyForDirectPayment_KeyUp(object sender, KeyEventArgs e)
        {

            Match OnlyZero = Regex.Match(moneyForDirectPayment.Text, @"^0*(\.|,)0*$");

            Match OnlyNumbers = Regex.Match(moneyForDirectPayment.Text, @"^[0-9\.,]+$");

            if ((moneyForDirectPayment.Text != String.Empty) && !OnlyZero.Success && OnlyNumbers.Success)
            {
                printMoneyDirectPayment.IsEnabled = true;
                printCardDirectPayment.IsEnabled = true;
                returnSaleDirectPayment.IsEnabled = true;
                returnSaleCardPayment.IsEnabled = true;
            }
            else
            {
                printMoneyDirectPayment.IsEnabled = false;
                printCardDirectPayment.IsEnabled = false;
                returnSaleDirectPayment.IsEnabled = false;
                returnSaleCardPayment.IsEnabled = false;
            }
        }

        private void printMoneyDirectPayment_Click(object sender, RoutedEventArgs e)
        {
            ComboBoxItem selectedItem = (ComboBoxItem)section.SelectedItem;
            string department_string = selectedItem.Tag.ToString();
            int department = int.Parse(department_string);

            decimal price = DocPack.manualParseDecimal(priceForDirectPayment.Text);
            decimal summ = DocPack.manualParseDecimal(moneyForDirectPayment.Text);

            string printing = stringForPrinting.Text;
            bool vat = vatDirectPayment.IsChecked ?? true;

            string[] result = CashboxDirect.DirectPayment(
                price, summ, printing, department, 1, false, vat
            ).Split(':');

            if (result[0] == "OK")
            {
                CleanCheck();
                MessageBoxes.ChangeMessage(result[1]);
            }
            else
                ShowError(moneyPlace, "Ошибка кассы: " + result[1]);
        }
    }
}
