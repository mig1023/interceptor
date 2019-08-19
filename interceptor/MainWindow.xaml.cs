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

        public static ICashbox Cashbox = null;

        List<Button> servButtonCleaningList = new List<Button>();
        List<Button> receptionButtonCleaningList = new List<Button>();
        public static System.Timers.Timer restoringSettingsCashbox = new System.Timers.Timer(5000);
        public Canvas returnFromErrorTo;

        public const bool TEST_VERSION = true;

        public const string CURRENT_VERSION_CLEAN = "2.5";

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

            string updateData = AutoUpdate.NeedUpdating();

            if (!String.IsNullOrEmpty(updateData) && !TEST_VERSION)
            {
                updateDir = AutoUpdate.Update(updateData);

                if (!String.IsNullOrEmpty(updateDir))
                    AutoUpdate.StartUpdater();
                else
                {
                    updateText.Content = "В процессе обновления программы произошла ошибка загрузки необходимых данных!\nПожалуйста, обратитесь к системным администраторам";
                    updateButton.Visibility = Visibility.Hidden;
                    needUpdateRestart.Background = (Brush)new BrushConverter().ConvertFromString("#FFFF4E4E");

                    Log.Add("ошибка обновления: контрольные суммы файлов не совпали", "update");

                    returnFromErrorTo = loginPlace;

                    MoveCanvas(
                        moveCanvas: needUpdateRestart,
                        prevCanvas: loginPlace,
                        direction: moveDirection.vertical
                    );
                }
            }

            int MaxThreadsCount = Environment.ProcessorCount * 4;
            ThreadPool.SetMaxThreads(MaxThreadsCount, MaxThreadsCount);
            ThreadPool.SetMinThreads(2, 2);

            foreach (Button button in new List<Button> {
                closeCheck, service, service_urgent, vipsrv,
                concil, concil_urg_r, concil_n, concil_n_age,
                sms_status, anketasrv, printsrv, photosrv, xerox,
                srv1, srv2, srv3, srv4, srv5, srv6, srv7, srv8, srv9, srv11
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
            ManualDocPack.CleanServices();

            UpdateCenters();

            MoveCanvas(
                moveCanvas: checkPlace,
                prevCanvas: mainPlace,
                newHeight: 670
            );
        }

        private void UpdateStatuses()
        {
            string port, speed, version, model;

            status1.Content = CURRENT_VERSION;
            status2.Content = CRM.GetMyIP() + " ( порт " + PROTOCOL_PORT + " )";
            status3.Content = CRM.CRM_URL_BASE;

            Cashbox.GetStatusData(out port, out speed, out version, out model);

            status4.Content = port;
            status5.Content = speed;
            status6.Content = model;
            status7.Content = version;
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

            string passwordHash = CRM.GenerateMySQLHash(password.Password);

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
            else if (Cashbox.CheckCashboxTables().Count() != 0)
            {
                int index = 0;

                foreach (string field in Cashbox.CheckCashboxTables())
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

            if (Cashbox != null)
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

        private void SupplBlock(Button button, bool block)
        {
            Button removeService = mainGrid.FindName(button.Name + "_remove") as Button;
            if (removeService != null)
                removeService.IsEnabled = block;
        }

        private void BlockCheckButton(bool block)
        {
            moneyForCheck.IsEnabled = block;
            printSending.IsEnabled = !block;
            returnDate.IsEnabled = !block;

            foreach (Button button in new List<Button>() { printCheckMoney, printCheckCard, returnSale, returnSaleCard })
                button.IsEnabled = block;

            foreach (Button serv in servButtonCleaningList)
            {
                serv.IsEnabled = !block;
                SupplBlock(serv, !block);
            }

            foreach (ComboBox combobox in new List<ComboBox>() { allVisas, allCenters, allVisas, allCenters })
                combobox.IsEnabled = !block;

            foreach (TextBox textbox in new List<TextBox>() { moneyForDHL, moneyForInsuranceRGS, moneyForInsuranceKL  })
                textbox.IsEnabled = !block;

            foreach (Button button in new List<Button>() {
                printMoneyDirectPayment, printCardDirectPayment, returnSaleDirectPayment, returnSaleCardPayment,
            })
                button.IsEnabled = block;
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

        private void AddedNonPricedService(TextBox field, string service)
        {
            decimal summ = DocPack.manualParseDecimal(field.Text);

            if (summ > 0)
                ManualDocPack.AddService(service + "=" + summ.ToString());
        }

        private void AddNonPricedServices()
        {
            AddedNonPricedService(moneyForDHL, "dhl");
            AddedNonPricedService(moneyForInsuranceRGS, "insuranceRGS");
            AddedNonPricedService(moneyForInsuranceKL, "insuranceKL");
        }

        private void сloseCheck_Click(object sender, RoutedEventArgs e)
        {
            AddNonPricedServices();

            string sendingSuccess = CRM.SendManDocPack(
                login.Text, CRM.password, 1, moneyForCheck.Text,
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

        private void removeService_Click(object sender, RoutedEventArgs e)
        {
            Button Service = sender as Button;

            string serviceName = Service.Name.Replace("_remove", "");

            ManualDocPack.SubService(serviceName.TrimEnd('R'));

            Button serviceButton = mainGrid.FindName(serviceName) as Button;

            modifyService_Click(serviceButton, e, remove: true);
        }

        private void addService_Click(object sender, RoutedEventArgs e)
        {
            Button Service = sender as Button;

            ManualDocPack.AddService(Service.Name.TrimEnd('R'));

            modifyService_Click(sender, e);
        }

        private void modifyService_Click(object sender, RoutedEventArgs e, bool remove = false)
        {
            Button Service = sender as Button;

            bool rService = (Service.Name.EndsWith("R") ? true : false);

            int serviceNum = ManualDocPack.GetService(Service.Name.TrimEnd('R'));

            Canvas currentCanvas = (rService ? receptionPlace : checkPlace);

            if (serviceNum > 0)
            {
                if ((serviceNum == 1) && !remove)
                {
                    Service.Width -= 40;

                    Button removeService = new Button();
                    removeService.Click += removeService_Click;
                    removeService.Name = Service.Name + "_remove";
                    removeService.Width = 40;
                    removeService.Height = Service.Height;
                    removeService.Content = "X";
                    removeService.FontSize = 20;
                    removeService.Tag = Service.Tag;
                    removeService.Background = Service.Background;
                    removeService.Margin = new Thickness(Canvas.GetLeft(Service) + Service.Width, Canvas.GetTop(Service), 0, 0);

                    currentCanvas.Children.Add(removeService);
                    currentCanvas.RegisterName(removeService.Name, removeService);
                }

                Service.FontWeight = FontWeights.Bold;

                Label labelService = Service.FindName(Service.Name + "_num") as Label;
                double topPositionBig = (rService ? -14 : 6);
                double topPositionLtl = (rService ? 24 : 3);

                if (labelService == null)
                {
                    Label newLabel = new Label();
                    newLabel.Name = Service.Name + "_num";
                    newLabel.Content = serviceNum.ToString();
                    newLabel.FontSize = 30;

                    Service.Width -= 30;
                    Canvas.SetLeft(Service, Canvas.GetLeft(Service) + 30);

                    Canvas.SetLeft(newLabel, Canvas.GetLeft(Service) - 32);
                    Canvas.SetTop(newLabel, Canvas.GetTop(Service) - topPositionBig);

                    currentCanvas.Children.Add(newLabel);
                    currentCanvas.RegisterName(newLabel.Name, newLabel);
                }
                else
                {
                    if (serviceNum >= 10)
                    {
                        labelService.FontSize = 18;
                        Canvas.SetLeft(labelService, Canvas.GetLeft(Service) - 32);
                        Canvas.SetTop(labelService, Canvas.GetTop(Service) + topPositionLtl);
                    }
                    else
                    {
                        labelService.FontSize = 30;
                        Canvas.SetLeft(labelService, Canvas.GetLeft(Service) - 32);
                        Canvas.SetTop(labelService, Canvas.GetTop(Service) - topPositionBig);
                    }

                    labelService.Content = serviceNum.ToString();
                }
            }
            else
                CleanButton(Service, currentCanvas);
        }

        private void CleanButton(Button service, Canvas currentCanvas)
        {
            Button removeService = mainGrid.FindName(service.Name + "_remove") as Button;
            currentCanvas.Children.Remove(removeService);
            currentCanvas.UnregisterName(removeService.Name);

            Label labelService = service.FindName(service.Name + "_num") as Label;
            currentCanvas.Children.Remove(labelService);
            currentCanvas.UnregisterName(labelService.Name);

            service.Width += 70;
            Canvas.SetLeft(service, Canvas.GetLeft(service) - 30);

            service.FontWeight = FontWeights.Normal;
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

                bool rService = (serv.Name.EndsWith("R") ? true : false);
                Canvas currentCanvas = (rService ? receptionPlace : checkPlace);
                Button removeService = mainGrid.FindName(serv.Name + "_remove") as Button;

                if (removeService != null)
                    CleanButton(serv, currentCanvas);
            }

            printSending.Text = String.Empty;
        }

        private void CleanCheck()
        {
            ButtonsClean(
                buttons: servButtonCleaningList,
                fontSize: 12
            );

            BlockCheckButton(block: false);

            foreach (TextBox text in new List<TextBox> { moneyForDHL, moneyForCheck, moneyForInsuranceRGS,
                moneyForInsuranceKL, directPaymentSending, priceForDirectPayment
            })
                text.Text = String.Empty;

            foreach (Label label in new List<Label> {
                    placeholderDHL, placeholderMoneyForCheck, placeholderRGS, placeholderKL, placeholderPrintSending,
                    placeholderSection, placeholderStringForPrinting, placeholderForDirectPayment, placeholderDirectMoney
            })
                label.Visibility = Visibility.Visible;

            foreach(Label label in new List<Label> { total, totalR })
                label.Content = String.Empty;

            totalContent.Visibility = Visibility.Hidden;

            returnDate.Text = String.Empty;

            // CleanRCheck

            ButtonsClean(
                buttons: receptionButtonCleaningList,
                fontSize: 18
            );

            BlockRCheckButton(block: false);

            ManualDocPack.CleanServices();

            moneyForRCheck.Text = String.Empty;
            appNumber.Text = String.Empty;

            // Direct

            stringForPrinting.Text = String.Empty;
            section.Text = String.Empty;
            vatDirectPayment.IsChecked = true;
            moneyForDirectPayment.Text = String.Empty;
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

            string sending = printSending.Text;

            if (CheckMoneyFail(money) || CheckAnotherDateFail(returnDate.Text))
                return;

            string[] result = Cashbox.PrintDocPack(
                Cashbox.manDocPackForPrinting, MoneyType: 1, MoneySumm: money, sendingAddress: sending
            ).Split(':');

            CheckError(result, checkPlace);
        }

        private void printCheckCard_Click(object sender, RoutedEventArgs e)
        {
            if (CheckAnotherDateFail(returnDate.Text))
                return;

            string sending = printSending.Text;

            string[] result = Cashbox.PrintDocPack(
                Cashbox.manDocPackForPrinting, MoneyType: 2, MoneySumm: Cashbox.manDocPackSumm, sendingAddress: sending
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

            string sending = printSending.Text;

            string[] result = Cashbox.PrintDocPack(
                Cashbox.manDocPackForPrinting, returnSale: true, MoneySumm: Cashbox.manDocPackSumm, sendingAddress: sending
            ).Split(':');

            CheckError(result, checkPlace);
        }

        private void returnSaleCard_Click(object sender, RoutedEventArgs e)
        {
            if (CheckDateFail(returnDate.Text))
            {
                CleanCheck();
                return;
            }

            string sending = printSending.Text;

            string[] result = Cashbox.PrintDocPack(
                Cashbox.manDocPackForPrinting, returnSale: true, MoneyType: 2, MoneySumm: Cashbox.manDocPackSumm, sendingAddress: sending
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
            else
            {
                moneyForIncome.Text = String.Empty;
                placeholderMoneyForIncome.Visibility = Visibility.Visible;
            }
        }

        private void cashOutcome_Click(object sender, RoutedEventArgs e)
        {
            if (!Cashbox.CashOutcome(moneyForOutcome.Text))
                moveToErrorFromReports(Cashbox.GetResultLine());
            else
            {
                moneyForOutcome.Text = String.Empty;
                placeholderMoneyForOutcome.Visibility = Visibility.Visible;
            }
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
            if (Cashbox.CancelDocument())
                MessageBoxes.CanceledDocument();
            else
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

            // MessageBoxes.WaitingForResetting();
            // Cashbox.TablesBackup();

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
            ManualDocPack.CleanServices();

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

            CleanCheck();
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

            if (String.IsNullOrEmpty(appNumber.Text))
                placeholderAppNum.Visibility = Visibility.Visible;
            else
                placeholderAppNum.Visibility = Visibility.Hidden;
        }

        private void closeRCheck_Click(object sender, RoutedEventArgs e)
        {
            string appNumberClean = Regex.Replace(appNumber.Text, @"[^0-9]", String.Empty);

            string sendingSuccess = CRM.SendManDocPack(
                login.Text, CRM.password, 1, moneyForRCheck.Text,
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
                    CleanCheck();
            }
            else
            {
                Log.Add("во время формирования чека произошла ошибка: " + sendingData[1]);

                ShowError(receptionPlace, sendingData[1]);
            }
        }

        private void printRCheckMoney_Click(object sender, RoutedEventArgs e)
        {
            Receipt.currentAppNumber = appNumber.Text;

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
                ShowError(checkPlace, "Нельзя оплачивать договор, указывая дату оплаты; оставьте это поле пустым");
                CleanCheck();

                return true;
            }
        }

        private void printRCheckCard_Click(object sender, RoutedEventArgs e)
        {
            Receipt.currentAppNumber = appNumber.Text;

            string[] result = Cashbox.PrintDocPack(
                Cashbox.manDocPackForPrinting, MoneyType: 2, MoneySumm: Cashbox.manDocPackSumm
            ).Split(':');

            CheckError(result, receptionPlace);

            if (result[0] == "OK")
                getAppInfoAndPrintRecepeit(Cashbox.manDocPackSumm.ToString());
        }

        private void getAppInfoAndPrintRecepeit(string summ)
        {
            string error = Receipt.PrintReceipt(CRM.AppNumberData(Receipt.currentAppNumber, summ), Cashbox.manDocPackForPrinting);

            CleanCheck();
            appNumber_KeyUp(null, null);
            appNumber.Focus();

            if (!String.IsNullOrEmpty(error))
                ShowError(receptionPlace, error);
        }

        private void appNumberClean_Click(object sender, RoutedEventArgs e)
        {
            appNumber.Text = String.Empty;

            CleanCheck();
            appNumber_KeyUp(null, null);
            appNumber.Focus();
        }

        private void updateButton_Click(object sender, RoutedEventArgs e)
        {
            AutoUpdate.StartUpdater();
        }

        private void placeholder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Label senderLabel = (Label)sender;

            string ralatedTextboxName = senderLabel.Tag.ToString();

            Control ralatedTextbox = (Control)FindName(ralatedTextboxName);
            ralatedTextbox.Focus();
        }

        private void withPlaceholder_KeyUp(object sender, KeyEventArgs e)
        {
            Control senderbox = (Control)sender;

            string ralatedPlaceholderName = senderbox.Tag.ToString();

            Label ralatedPlaceholder = (Label)FindName(ralatedPlaceholderName);

            string senderText;

            if (senderbox is PasswordBox)
            {
                PasswordBox pass = (PasswordBox)senderbox;
                senderText = pass.Password;
            }
            else if (senderbox is ComboBox)
            {
                ComboBox box = (ComboBox)senderbox;
                senderText = box.Text;

                if (box.SelectedIndex > -1)
                {
                    ralatedPlaceholder.Visibility = Visibility.Hidden;
                    return;
                }
            }
            else
            {
                TextBox text = (TextBox)senderbox;
                senderText = text.Text;
            }

            if (String.IsNullOrEmpty(senderText))
                ralatedPlaceholder.Visibility = Visibility.Visible;
            else
                ralatedPlaceholder.Visibility = Visibility.Hidden;

            if (senderbox.Name == "appNumber")
                appNumber_KeyUp(null, null);
        }

        private void backToMainFromMoneyPlace_Click(object sender, RoutedEventArgs e)
        {
            MoveCanvas(
                moveCanvas: mainPlace,
                prevCanvas: moneyPlace
            );

            Server.ShowActivity(busy: false);

            CleanCheck();
            appNumber_KeyUp(null, null);
        }

        private void paymentPrepared()
        {
            Match OnlyZero = Regex.Match(priceForDirectPayment.Text, @"^0*(\.|,)?0*$");

            Match OnlyNumbers = Regex.Match(priceForDirectPayment.Text, @"^[0-9\.,]+$");

            bool price = !(String.IsNullOrEmpty(priceForDirectPayment.Text)) && !OnlyZero.Success && OnlyNumbers.Success;

            if ((section.SelectedItem != null) && (stringForPrinting.Text != String.Empty) && price)
                foreach (Control direct in new List<Control> {
                    moneyForDirectPayment, printCardDirectPayment, returnSaleDirectPayment, returnSaleCardPayment
                })
                    direct.IsEnabled = true;
            else
                foreach (Control direct in new List<Control> {
                    moneyForDirectPayment, printCardDirectPayment, returnSaleDirectPayment, returnSaleCardPayment
                })
                    direct.IsEnabled = false;
        }

        private void directSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            paymentPrepared();

            withPlaceholder_KeyUp(sender, null);
        }

        private void stringForPrinting_KeyUp(object sender, KeyEventArgs e)
        {
            paymentPrepared();

            withPlaceholder_KeyUp(sender, null);
        }

        private void moneyForDirectPayment_KeyUp(object sender, KeyEventArgs e)
        {
            withPlaceholder_KeyUp(sender, null);

            Match OnlyZero = Regex.Match(moneyForDirectPayment.Text, @"^0*(\.|,)0*$");

            Match OnlyNumbers = Regex.Match(moneyForDirectPayment.Text, @"^[0-9\.,]+$");

            if ((moneyForDirectPayment.Text != String.Empty) && !OnlyZero.Success && OnlyNumbers.Success)
                printMoneyDirectPayment.IsEnabled = true;
            else
                printMoneyDirectPayment.IsEnabled = false;
        }

        private void directPayment(int moneyType, bool returnSale)
        {
            ComboBoxItem selectedItem = (ComboBoxItem)section.SelectedItem;
            string department_string = selectedItem.Tag.ToString();
            int department = int.Parse(department_string);

            decimal price = DocPack.manualParseDecimal(priceForDirectPayment.Text);
            decimal summ = DocPack.manualParseDecimal(moneyForDirectPayment.Text);

            string sendingSMSorEMAIL = directPaymentSending.Text;

            string printing = stringForPrinting.Text;
            bool vat = vatDirectPayment.IsChecked ?? true;

            string[] result = Cashbox.DirectPayment(
                moneyPrice: price, moneySumm: summ, forPrinting: printing, sending: sendingSMSorEMAIL,
                department: department, moneyType: moneyType, returnSale: returnSale, VAT: vat
            ).Split(':');

            if (result[0] == "OK")
            {
                CleanCheck();
                MessageBoxes.ChangeMessage(result[1]);
            }
            else
                ShowError(moneyPlace, "Ошибка кассы: " + result[1]);
        }

        private void printMoneyDirectPayment_Click(object sender, RoutedEventArgs e)
        {
            decimal summ = DocPack.manualParseDecimal(moneyForDirectPayment.Text);

            if (CheckMoneyFail(summ))
                return;

            directPayment(moneyType: 1, returnSale: false);
        }

        private void printCardDirectPayment_Click(object sender, RoutedEventArgs e)
        {
            directPayment(moneyType: 2, returnSale: false);
        }

        private void returnSaleDirectPayment_Click(object sender, RoutedEventArgs e)
        {
            directPayment(moneyType: 1, returnSale: true);
        }

        private void returnCardDirectPayment_Click(object sender, RoutedEventArgs e)
        {
            directPayment(moneyType: 2, returnSale: true);
        }

        private void regionReport_Click(object sender, RoutedEventArgs e)
        {
            Button reportButton = sender as Button;

            if (!Cashbox.ReportRegion(reportButton.Tag.ToString()))
                moveToErrorFromReports(Cashbox.GetResultLine());
        }

        private void totalContent_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MessageBoxes.ShowReceiptContent(Cashbox.manDocPackForPrinting);
        }
    }
}
