using System;
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

namespace shtrih_interceptor
{
    public enum moveDirection { horizontal, vertical };

    public partial class MainWindow : Window
    {
        List<string> manDocPack = new List<string>();
        List<Button> servButtonCleaningList = new List<Button>();

        public Canvas returnFromErrorTo;

        public const string CURRENT_VERSION = "1.0a1";

        public MainWindow()
        {
            InitializeComponent();

            Log.add("перехватчик запущен", freeLine: true);

            int MaxThreadsCount = Environment.ProcessorCount * 4;
            ThreadPool.SetMaxThreads(MaxThreadsCount, MaxThreadsCount);
            ThreadPool.SetMinThreads(2, 2);

            foreach (string buttonName in new[] {
                "service", "service_urgent", "vip_comfort", "vipsrv", "concil", "concil_urg_r",
                "concil_n", "concil_n_age", "sms_status", "vip_standart", "anketasrv", "printsrv",
                "photosrv", "xerox", "transum", "dhl"
            }) servButtonCleaningList.Add((Button)mainGrid.FindName(buttonName));
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
            if (CRM.crmAuthentication(login.Text, CRM.generateMySQLHash(password.Password)))
            {
                MoveCanvas(
                    moveCanvas: mainPlace,
                    prevCanvas: loginPlace,
                    direction: moveDirection.vertical
                );

                updateStatuses();

                if (Diagnostics.failCashbox())
                    switchOn.Background = Brushes.Red;
                else
                {
                    Server.StartServer();
                    switchOn.Background = Brushes.LimeGreen;
                }
            }
            else
            {
                loginFailText.Content = CRM.loginError;
                returnFromErrorTo = loginPlace;

                MoveCanvas(
                    moveCanvas: loginFail,
                    prevCanvas: loginPlace,
                    direction: moveDirection.vertical
                );
            }
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
            Cashbox.reportCleaning();
        }

        private void reportWithoutCleaning_Click(object sender, RoutedEventArgs e)
        {
            Cashbox.reportWithoutCleaning();
        }

        private void reportDepartment_Click(object sender, RoutedEventArgs e)
        {
            Cashbox.reportDepartment();
        }

        private void repeatDocument_Click(object sender, RoutedEventArgs e)
        {
            Cashbox.repeatDocument();
        }

        private void reportTax_Click(object sender, RoutedEventArgs e)
        {
            Cashbox.reportTax();
        }

        private void blockCheckButton(bool block)
        {
            printCheckMoney.IsEnabled = (block ? true : false);
            printCheckCard.IsEnabled = (block ? true : false);
            returnSale.IsEnabled = (block ? true : false);

            foreach(Button serv in servButtonCleaningList)
                serv.IsEnabled = (block ? false : true);

            moneyForDHL.IsEnabled = (block ? false : true);
        }

        private void сloseCheck_Click(object sender, RoutedEventArgs e)
        {
            bool sendingSuccess = CRM.sendManDocPack(manDocPack, login.Text, CRM.Password, 1,
                moneyForCheck.Text, allCenters.Text, allVisas.Text, returnDate.Text);

            if (sendingSuccess)
            {
                blockCheckButton(block: true);
            }
            else
            {
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
            {
                manDocPack.Add(Service.Name + "=" + moneyForDHL.Text);
            }
            else
                manDocPack.Add(Service.Name);

            Match ReqMatch = Regex.Match(Service.Content.ToString(), @"^([^\d]+)\s\((\d+)\)");

            if (ReqMatch.Success)
            {
                int servCount = Int32.Parse(ReqMatch.Groups[2].Value);

                servCount++;

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

            string[] result = Cashbox.printDocPack(Cashbox.manDocPackForPrinting, MoneyType: 1, MoneySumm: money).Split(':');

            checkError(result, checkPlace, "Ошибка кассы");
        }

        private void printCheckCard_Click(object sender, RoutedEventArgs e)
        {
            string[] result = Cashbox.printDocPack(Cashbox.manDocPackForPrinting, MoneyType: 2, MoneySumm: Cashbox.manDocPackSumm).Split(':');

            checkError(result, checkPlace, "Ошибка кассы");
        }

        private void returnSale_Click(object sender, RoutedEventArgs e)
        {
            decimal money = DocPack.manualParseDecimal(moneyForCheck.Text);

            string[] result = Cashbox.printDocPack(Cashbox.manDocPackForPrinting, returnSale: true, MoneySumm: money).Split(':');

            checkError(result, checkPlace, "Ошибка кассы");
        }

        private void password_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) sendLogin_Click(null, null);
        }

        private void continueDocument_Click(object sender, RoutedEventArgs e)
        {
            Cashbox.continueDocument();
        }
    }
}
