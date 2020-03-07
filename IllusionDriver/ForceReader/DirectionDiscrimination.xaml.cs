using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Windows.Shapes;

namespace ForceReader
{
    public partial class DirectionDiscrimination : Window
    {
        int trialNumber = 1;
        int currentDirectionIdx = 0;
        BackgroundWorker worker;
        bool recordingInProgress = false;
        bool experimentInProgress = false;
        bool lowValueHit = false;
        bool highValueHit = false;
        static string[] directionsToTest = { "n", "e", "s", "w", "ne", "se", "sw", "nw" };
        static string[] imageFiles = { "north", "east", "south", "west", "northeast", "southeast", "southwest", "northwest" };

        public DirectionDiscrimination()
        {
            InitializeComponent();
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_doWork);
            worker.ProgressChanged += new ProgressChangedEventHandler
                    (worker_ProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler
                    (worker_RunWorkerCompleted);
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
        }

        void worker_doWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                Thread.Sleep(1);
                worker.ReportProgress(1);
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    worker.ReportProgress(0);
                    return;
                }
            }
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled) ;

            else if (e.Error != null)
            {
                Debug.WriteLine("Error while performing background operation.");
            }
            else
            {
                Debug.WriteLine("Everything complete");
            }
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!experimentInProgress) return;
            if (highValueHit && lowValueHit)
            {
                experimentInProgress = false;
                trialNumber += 1;
                TrialBreak();
                if (trialNumber > 10 && currentDirectionIdx < 7)
                {
                    currentDirectionIdx += 1;
                    trialNumber = 1;
                    northImg.Source = new BitmapImage(new Uri(@"C:\Users\anastasialalamentik\Downloads\PseudoBend-master\PseudoBend-master\IllusionDriver\ForceReader\" + imageFiles[currentDirectionIdx] + ".png"));
                }
                highValueHit = false;
                lowValueHit = false;
                experimentInProgress = true;
            }
            else {
                detectRecordingStart();
                if (recordingInProgress && (!highValueHit && !lowValueHit))
                {
                    String fileName = @"C: \Users\anastasialalamentik\Downloads\DataCollection\DirectionDiscrimination_Block1_Trial" + trialNumber + "-direction_" + directionsToTest[currentDirectionIdx] + string.Format("-{0:yyyy-MM-dd_hh-mm-ss-tt}.csv", DateTime.Now);
                    MainWindow.setRecordValuesMode(true, fileName);
                    recordingInProgress = true;
                }
            }
        }

        void detectRecordingStart() {
            String currentDirection = directionsToTest[currentDirectionIdx];
            if (currentDirection == "s" || currentDirection == "n")
            {
                manageStateChanges(MainWindow.force.fy);
            }
            else if (currentDirection == "w" || currentDirection == "e") {
                manageStateChanges(MainWindow.force.fx);
            }
        }

        void manageStateChanges(double force) {
            if (highValueHit && Math.Abs(force) < 0.08)
            {
                lowValueHit = true;
                recordingInProgress = false;
                MainWindow.setRecordValuesMode(false, null);
                Console.WriteLine("done... " + force);
            }
            else if (Math.Abs(force) > 0.4 && recordingInProgress && !highValueHit)
            {
                highValueHit = true;
                highPointHit.Visibility = Visibility.Visible;
                Console.WriteLine("top hit... " + force);
            }
            else if (force > 0.08 && !recordingInProgress && !lowValueHit && !highValueHit)
            {
                recordingInProgress = true;
                Console.WriteLine("recording..." + force);
            }
        }

        private async void StartExperiment(object sender, RoutedEventArgs e)
        {
            startExperiment.Visibility = Visibility.Hidden;
            readyTimer.Visibility = Visibility.Visible;
            readyTimer.Content = "3...";
            await Task.Delay(1000);
            readyTimer.Content = "3...2...";
            await Task.Delay(1000);
            readyTimer.Content = "3...2...1...";
            await Task.Delay(1000);
            readyTimer.Visibility = Visibility.Hidden;
            experimentInProgress = true;
            MainWindow.deactivateFeedback = true;
            northImg.Visibility = Visibility.Visible;
            worker.RunWorkerAsync();
        }

        private async void TrialBreak() {
            highPointHit.Visibility = Visibility.Hidden;
            northImg.Visibility = Visibility.Hidden;
            readyTimer.Visibility = Visibility.Visible;
            readyTimer.Visibility = Visibility.Visible;
            readyTimer.Content = "2...";
            await Task.Delay(1000); 
            readyTimer.Content = "2...1...";
            await Task.Delay(1000);
            readyTimer.Visibility = Visibility.Hidden;
            northImg.Visibility = Visibility.Visible;
            MainWindow.resp.SetBG();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}
