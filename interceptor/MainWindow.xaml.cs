﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Windows.Media.Animation;
using System.Text.RegularExpressions;
using System.Windows.Controls.Primitives;
using System.Timers;
using System.Reflection;

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

        public const string CURRENT_VERSION = "1.с2";
            
        public MainWindow()
        {
            InitializeComponent();

            Instance = this;

            Log.add("ПЕРЕХВАТЧИК ЗАПУЩЕН", freeLine: true);
            Log.add("версия ---> " + CURRENT_VERSION, freeLineAfter: true);

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
                "transum",
                "dhl",
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

            updateCenters();

            MoveCanvas(
                moveCanvas: checkPlace,
                prevCanvas: mainPlace
            );
        }

        private void updateStatuses()
        {
            string port, speed, status, version, model;

            status1.Content = CURRENT_VERSION;
            status2.Content = CRM.getMyIP();
            status3.Content = CRM.CRM_URL_BASE;

            Cashbox.getStatusData(out port, out speed, out status, out version, out model);

            status4.Content = port;
            status5.Content = speed;
            status6.Content = model;
            status7.Content = version;
            status8.Content = status.ToLower();
        }

        private void updateCenters()
        {
            allCenters.Items.Clear();

            foreach (string center_name in CRM.getAllCenters(login.Text))
                allCenters.Items.Add(center_name);

            allCenters.SelectedIndex = 0;
        }

        private void updateVTypes()
        {
            if (allCenters.SelectedItem == null) return;

            allVisas.Items.Clear();

            foreach (string visa_name in CRM.getAllVType(allCenters.SelectedItem.ToString()))
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

            cleanCheck();
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

            if (!CRM.crmAuthentication(login.Text, CRM.generateMySQLHash(password.Password)))
            {
                loginFailText.Content = CRM.loginError;
                returnFromErrorTo = loginPlace;
                canvasToGo = loginFail;

                Log.add("ошибка входа с логином " + login.Text);
            }
            else if (Diagnostics.failCashbox())
            {
                loginFailText.Content = "Ошибка подключения к кассе. Проверьте подключение и перезапустите приложение";
                returnFromErrorTo = loginPlace;
                canvasToGo = loginFail;

                Log.add("ошибка подключения к кассе");
            }
            else if (Cashbox.checkCashboxTables() != "")
            {
                settingText2.Content = Cashbox.checkCashboxTables();
                returnFromErrorTo = loginPlace;
                canvasToGo = cashboxSettingsFail;

                Log.add("ошибка настроек кассы");

                if (Cashbox.currentMode() != 4)
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

                status10.Content = login.Text.Replace("_", "__");
            }

            MoveCanvas(
                moveCanvas: canvasToGo,
                prevCanvas: loginPlace,
                direction: moveDirection.vertical
            );

            updateStatuses();
        }

        private void returnFromError_Click(object sender, RoutedEventArgs e)
        {
            password.Password = "";

            MoveCanvas(
                moveCanvas: returnFromErrorTo,
                prevCanvas: loginFail,
                direction: moveDirection.vertical
            );
        }

        private void reportCleaning_Click(object sender, RoutedEventArgs e)
        {
            if (!Cashbox.reportCleaning())
                moveToErrorFromReports(Cashbox.getResultLine());
        }

        private void reportWithoutCleaning_Click(object sender, RoutedEventArgs e)
        {
            if (!Cashbox.reportWithoutCleaning())
                moveToErrorFromReports(Cashbox.getResultLine());
        }

        private void reportDepartment_Click(object sender, RoutedEventArgs e)
        {
            if (!Cashbox.reportDepartment())
                moveToErrorFromReports(Cashbox.getResultLine());
        }

        private void repeatDocument_Click(object sender, RoutedEventArgs e)
        {
            if (!Cashbox.repeatDocument())
                moveToErrorFromReports(Cashbox.getResultLine());
        }

        private void reportTax_Click(object sender, RoutedEventArgs e)
        {
            if (!Cashbox.reportTax())
                moveToErrorFromReports(Cashbox.getResultLine());
        }

        private void blockCheckButton(bool block)
        {
            printCheckMoney.IsEnabled = (block ? true : false);
            printCheckCard.IsEnabled = (block ? true : false);
            returnSale.IsEnabled = (block ? true : false);

            foreach(Button serv in servButtonCleaningList)
                serv.IsEnabled = (block ? false : true);

            moneyForDHL.IsEnabled = (block ? false : true);
            allCenters.IsEnabled = (block ? false : true);
            allVisas.IsEnabled = (block ? false : true);
            returnDate.IsEnabled = (block ? false : true);
        }

        private void сloseCheck_Click(object sender, RoutedEventArgs e)
        {
            string sendingSuccess = CRM.sendManDocPack(
                manDocPack, login.Text, CRM.Password, 1, moneyForCheck.Text,
                allCenters.Text, allVisas.Text, returnDate.Text
            );

            string[] sendingData = sendingSuccess.Split('|');

            if (sendingData[0] == "OK")
            {
                Log.add("успешно закрыт чек");

                blockCheckButton(block: true);
            }
            else if (sendingData[0] == "WARNING")
            {
                Log.add("некоторые услуги из чека не имеют цены: " + sendingData[1]);

                MessageBoxResult result = MessageBox.Show(
                    "Услуги имеют цену по прайслисту выбранного центра: " +
                    sendingData[1] + "." +
                    "Такие услуги не будут отображены в чеке. Продолжить?",
                    "Внимание!",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                    blockCheckButton(block: true);
                else
                    cleanCheck();
            }
            else
            {
                Log.add("во время формирования чека произошла ошибка");

                loginFailText.Content = "Во время отправки запроса произошла ошибка";
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
            else
                manDocPack.Add(Service.Name);

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
            updateVTypes();
        }

        private void cleanCheck()
        {
            foreach (Button serv in servButtonCleaningList)
            {
                int bracketIndex = serv.Content.ToString().IndexOf('(');

                if (bracketIndex > 0) serv.Content = serv.Content.ToString().Remove(bracketIndex);
            }

            blockCheckButton(block: false);

            manDocPack.Clear();

            moneyForDHL.Text = "0.00";
            moneyForCheck.Text = "0.00";
            total.Content = "";
        }

        private void showError(Canvas from, string error)
        {
            loginFailText.Content = error;
            returnFromErrorTo = from;

            MoveCanvas(
                moveCanvas: loginFail,
                prevCanvas: from,
                direction: moveDirection.vertical
            );
        }

        public void checkError(string[] result, Canvas place, string error)
        {
            if (result[0] == "OK")
                cleanCheck();
            else
                showError(place, error + ": " + result[1]);
        }

        private void printCheckMoney_Click(object sender, RoutedEventArgs e)
        {
            decimal money = DocPack.manualParseDecimal(moneyForCheck.Text);

            string[] result = Cashbox.printDocPack(
                Cashbox.manDocPackForPrinting, MoneyType: 1, MoneySumm: money
            ).Split(':');

            checkError(result, checkPlace, "Ошибка кассы");
        }

        private void printCheckCard_Click(object sender, RoutedEventArgs e)
        {
            string[] result = Cashbox.printDocPack(
                Cashbox.manDocPackForPrinting, MoneyType: 2, MoneySumm: Cashbox.manDocPackSumm
            ).Split(':');

            checkError(result, checkPlace, "Ошибка кассы");
        }

        private void returnSale_Click(object sender, RoutedEventArgs e)
        {
            string[] result = Cashbox.printDocPack(
                Cashbox.manDocPackForPrinting, returnSale: true, MoneySumm: Cashbox.manDocPackSumm
            ).Split(':');

            checkError(result, checkPlace, "Ошибка кассы");
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
            if (!Cashbox.continueDocument())
                moveToErrorFromReports(Cashbox.getResultLine());
        }

        private void cashIncome_Click(object sender, RoutedEventArgs e)
        {
            if (!Cashbox.cashIncome(moneyForIncome.Text))
                moveToErrorFromReports(Cashbox.getResultLine());
        }

        private void cashOutcome_Click(object sender, RoutedEventArgs e)
        {
            if (!Cashbox.cashOutcome(moneyForOutcome.Text))
                moveToErrorFromReports(Cashbox.getResultLine());
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
            if (!Cashbox.cancelDocument())
                moveToErrorFromReports(Cashbox.getResultLine());
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
            if (Cashbox.currentMode() != 4)
                Cashbox.reportCleaning();

            restoringSettingsCashbox.Elapsed += new ElapsedEventHandler(restoreSetting);
            restoringSettingsCashbox.Enabled = true;
            restoringSettingsCashbox.Start();

            Cashbox.resettingCashbox();

            MoveCanvas(
                moveCanvas: returnFromErrorTo,
                prevCanvas: cashboxSettingsFail,
                direction: moveDirection.vertical
            );
        }

        public static void restoreSetting(object obj, ElapsedEventArgs e)
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
            return; // <------- пока не готово

            // manDocPack.Clear();

            // updateCenters();

            MoveCanvas(
                moveCanvas: receptionPlace,
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

            appNumber.Focus();

            // cleanCheck();
        }

        private void appNumber_KeyUp(object sender, KeyEventArgs e)
        {
            appNumber.Text = Regex.Replace(appNumber.Text, @"[^0-9/]", "");
            string appNumberClean = Regex.Replace(appNumber.Text, @"[^0-9]", "");

            if (appNumberClean.Length == 15)
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

            //if (e.Key == Key.Enter)
            //    sendLogin_Click(null, null);
        }
    }
}
