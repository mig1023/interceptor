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

namespace shtrih_interceptor
{
    public enum moveDirection { horizontal, vertical };

    public partial class MainWindow : Window
    {

        List<string> manDocPack = new List<string>();

        public MainWindow()
        {
            InitializeComponent();

            Log.add("перехватчик запущен", freeLine: true);

            int MaxThreadsCount = Environment.ProcessorCount * 4;
            ThreadPool.SetMaxThreads(MaxThreadsCount, MaxThreadsCount);
            ThreadPool.SetMinThreads(2, 2);
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
            updateVTypes();

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

        private void сloseCheck_Click(object sender, RoutedEventArgs e)
        {
            CRM.sendManDocPack(manDocPack, login.Text, CRM.Password, 1, moneyForCheck.Text);
        }

        private void addService_Click(object sender, RoutedEventArgs e)
        {
            manDocPack.Add(this.Name);
        }

        private void allCenters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updateVTypes();
        }
    }
}
