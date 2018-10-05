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
            if (Diagnostics.failCashbox())
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

        public void MoveCanvas(Canvas moveCanvas, Canvas prevCanvas)
        {
            ThicknessAnimation move = new ThicknessAnimation();
            move.Duration = TimeSpan.FromSeconds(0.2);
            move.From = moveCanvas.Margin;
            move.To = new Thickness(
                0,
                moveCanvas.Margin.Top,
                moveCanvas.Margin.Right,
                moveCanvas.Margin.Bottom
            );

            moveCanvas.BeginAnimation(MarginProperty, move);

            move.From = prevCanvas.Margin;
            move.To = new Thickness(
                prevCanvas.Margin.Left - moveCanvas.Margin.Left,
                prevCanvas.Margin.Top,
                prevCanvas.Margin.Right,
                prevCanvas.Margin.Bottom
            );

            prevCanvas.BeginAnimation(MarginProperty, move);
        }

        private void check_Click(object sender, RoutedEventArgs e)
        {
            MoveCanvas(
                moveCanvas: checkPlace,
                prevCanvas: mainPlace
            );
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

        private void closeSession_Click(object sender, RoutedEventArgs e)
        {
            Cashbox.closeSession();
        }
    }
}
