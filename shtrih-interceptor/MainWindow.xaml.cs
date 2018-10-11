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

namespace shtrih_interceptor
{
    public enum moveDirection { horizontal, vertical };

    public partial class MainWindow : Window
    {
        List<string> manDocPack = new List<string>();
        List<Button> servButtonCleaningList = new List<Button>();

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

        private void startServButton_Click(object sender, RoutedEventArgs e)
        {
            if (Diagnostics.failCashbox())
                switchOn.Background = Brushes.Red;
            else
            {
                Server.StartServer();
                switchOn.Background = Brushes.LimeGreen;
                startServButton.IsEnabled = false;
                check.IsEnabled = true;
            }
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
            }
            else
            {
                loginFailText.Content = CRM.loginError;

                MoveCanvas(
                    moveCanvas: loginFail,
                    prevCanvas: loginPlace,
                    direction: moveDirection.vertical
                );
            }
        }

        private void backToLoginFromFail_Click(object sender, RoutedEventArgs e)
        {
            password.Password = "";

            MoveCanvas(
                moveCanvas: loginPlace,
                prevCanvas: loginFail,
                direction: moveDirection.vertical
            );
        }
        private void settings_Click(object sender, RoutedEventArgs e)
        {
            Cashbox.settings();
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
            CRM.sendManDocPack(manDocPack, login.Text, CRM.Password, 1,
                moneyForCheck.Text, allCenters.Text, allVisas.Text);

            blockCheckButton(block: true);
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

        private void printCheckMoney_Click(object sender, RoutedEventArgs e)
        {
            decimal money = DocPack.manualParseDecimal(moneyForCheck.Text);

            Cashbox.printDocPack(Cashbox.manDocPackForPrinting, MoneyType: 1, MoneySumm: money);

            cleanCheck();
        }

        private void printCheckCard_Click(object sender, RoutedEventArgs e)
        {
            Cashbox.printDocPack(Cashbox.manDocPackForPrinting, MoneyType: 2, MoneySumm: Cashbox.manDocPackSumm);

            cleanCheck();
        }

        private void returnSale_Click(object sender, RoutedEventArgs e)
        {
            decimal money = DocPack.manualParseDecimal(moneyForCheck.Text);

            Cashbox.printDocPack(Cashbox.manDocPackForPrinting, returnSale: true, MoneySumm: money);

            cleanCheck();
        }
    }
}
