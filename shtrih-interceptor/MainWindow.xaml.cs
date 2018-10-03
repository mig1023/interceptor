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

namespace shtrih_interceptor
{

    public partial class MainWindow : Window
    {
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
            if (Diagnosticcs.failCashbox())
                switchOn.Background = Brushes.Red;
            else
            {
                Server.StartServer();
                switchOn.Background = Brushes.LimeGreen;
                startServButton.IsEnabled = false;
            }
            
        }

        private void settings_Click(object sender, RoutedEventArgs e)
        {
            Cashbox.settings();
        }
    }
}
