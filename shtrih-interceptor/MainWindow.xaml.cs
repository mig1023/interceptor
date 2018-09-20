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

            Log.add("перехватчик запущен");

            int MaxThreadsCount = Environment.ProcessorCount * 4;
            ThreadPool.SetMaxThreads(MaxThreadsCount, MaxThreadsCount);
            ThreadPool.SetMinThreads(2, 2);
        }

        private readonly BackgroundWorker asynchServ = new BackgroundWorker();

        private void button_Click(object sender, RoutedEventArgs e)
        {
            asynchServ.DoWork += worker_DoWork;
            asynchServ.RunWorkerAsync();
            switchon.Background = Brushes.LimeGreen;
            button.IsEnabled = false;
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            new Server(80);

            Log.add("сервер запущен");
        }
    }
}
