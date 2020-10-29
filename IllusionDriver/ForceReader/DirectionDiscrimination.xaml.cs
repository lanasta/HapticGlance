using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ForceReader
{
    public partial class DirectionDiscrimination : Window
    {
        int blockNumber = 1;
        int blockLimit = 3;
        int trialNumber = 1;
        int trialLimit = 10;
        int currentDirectionIdx = 0;
        int blockBreakDuration = 30000;
        BackgroundWorker worker;
        bool recordingInProgress = false;
        bool experimentInProgress = false;
        bool lowValueHit = false;
        bool highValueHit = false;
        String participantId = "";
        int numberOfDirections;
        List<String> directionsToTest = new List<String>{ "n", "e", "s", "w", "ne", "se", "sw", "nw" };
        //List<String> directionsToTest = new List<String> { "n", "e" };

        private static Random rng = new Random();

        public DirectionDiscrimination(String participantId)
        {
            InitializeComponent();
            this.participantId = participantId;
            directionsToTest = ShuffleArray(directionsToTest);
            numberOfDirections = directionsToTest.Count;
            northImg.Source = new BitmapImage(new Uri(@"C:\Users\anastasialalamentik\Desktop\HapticGlance\IllusionDriver\ForceReader\" + directionsToTest[currentDirectionIdx] + ".png"));
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_doWork);
            worker.ProgressChanged += new ProgressChangedEventHandler
                    (worker_ProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler
                    (worker_RunWorkerCompleted);
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
        }

        List<String> ShuffleArray(List<String> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                String value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            foreach (object o in list)
            {
                Console.WriteLine(o);
            }
            return list;
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

        async void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!experimentInProgress) return;
            if (currentDirectionIdx == numberOfDirections)
            {
                trialNumber = 1;
                blockNumber += 1;
                directionsToTest = ShuffleArray(directionsToTest);
                currentDirectionIdx = 0;
                northImg.Source = new BitmapImage(new Uri(@"C:\Users\anastasialalamentik\Desktop\HapticGlance\IllusionDriver\ForceReader\" + directionsToTest[currentDirectionIdx] + ".png"));
                BlockBreak();
                if (blockNumber > blockLimit)
                {
                    MainWindow.deactivateFeedback = true;
                    recordingInProgress = false;
                    experimentInProgress = false;
                    expInstructions.Text = "You have completed this experiment. Please close the window and move on to the Haptic Marks experiment.";
                    await Task.Delay(1000);
                    northImg.Visibility = Visibility.Hidden;
                    readyTimer.Visibility = Visibility.Hidden;
                    highPointHit.Visibility = Visibility.Hidden;
                    worker.CancelAsync();
                }
                return;
            }
            if (highValueHit && lowValueHit && currentDirectionIdx < numberOfDirections)
            {
                trialNumber += 1;
                TrialBreak(trialNumber > trialLimit);
                if (trialNumber > trialLimit )
                {
                    currentDirectionIdx += 1;
                    trialNumber = 1;
                    if (currentDirectionIdx < numberOfDirections)
                    {
                        northImg.Source = new BitmapImage(new Uri(@"C:\Users\anastasialalamentik\Desktop\HapticGlance\IllusionDriver\ForceReader\" + directionsToTest[currentDirectionIdx] + ".png"));
                    }
                }
                highValueHit = false;
                lowValueHit = false;
            }
            else {
                detectRecordingStart();
                if (recordingInProgress && (!highValueHit && !lowValueHit))
                {
                    String fileName = @"C: \Users\anastasialalamentik\Downloads\DataCollection\DD\DirectionDiscrimination_Participant-"+ participantId + "_Block-"+ blockNumber + "_Trial-" + trialNumber + "-direction_" + directionsToTest[currentDirectionIdx] + string.Format("-{0:yyyy-MM-dd_hh-mm-ss-tt}.csv", DateTime.Now);
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
            } else
            {
                double magnitude = Math.Sqrt(MainWindow.force.fx * MainWindow.force.fx + MainWindow.force.fy * MainWindow.force.fy);
                manageStateChanges(magnitude);
            }
        }

        void manageStateChanges(double force) {
            if (highValueHit && Math.Abs(force) < 0.02)
            {
                lowValueHit = true;
                if (recordingInProgress) {
                    MainWindow.setRecordValuesMode(false, null);
                }
                recordingInProgress = false;
                Console.WriteLine("done... " + force);
                MainWindow.resp.SetBG();
            }
            else if (Math.Abs(force) > getDirectionalThreshold() && recordingInProgress && !highValueHit)
            {
                highValueHit = true;
                highPointHit.Visibility = Visibility.Visible;
                Console.WriteLine("top hit... " + force);
            }
            else if (evaluateRecordStart(force) && experimentInProgress && !recordingInProgress && !lowValueHit && !highValueHit)
            {
                recordingInProgress = true;
                MainWindow.expBeginningMs = (DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;
                Console.WriteLine("recording..." + force);
            }
        }

        double getDirectionalThreshold()
        {
            String currentDirection = directionsToTest[currentDirectionIdx];
            if (currentDirection.Equals("ne") || currentDirection.Equals("sw") || currentDirection.Equals("s") || currentDirection.Equals("e") || currentDirection.Equals("w"))
            {
                return 0.3;
            } else
            {
                return 0.45;
            }
        }


        bool evaluateRecordStart(double value)
        {
            String curDir = directionsToTest[currentDirectionIdx];
            if (curDir == "ne" || curDir == "nw" || curDir == "sw" || curDir == "se" || curDir == "n" || curDir == "e")
            {
                return value > 0.01;
            } else
            {
                return value < -0.01;
            }
        }

        private async void StartExperiment(object sender, RoutedEventArgs e)
        {
            startExperiment.Visibility = Visibility.Hidden;
            readyTimer.Visibility = Visibility.Visible;
            readyTimer.Text = "3...";
            await Task.Delay(1000);
            readyTimer.Text = "3...2...";
            await Task.Delay(1000);
            readyTimer.Text = "3...2...1...";
            await Task.Delay(1000);
            readyTimer.Visibility = Visibility.Hidden;
            experimentInProgress = true;
            MainWindow.deactivateFeedback = true;
            northImg.Visibility = Visibility.Visible;
            worker.RunWorkerAsync();
        }

        private async void TrialBreak(bool directionChange) {
            experimentInProgress = false;
            highPointHit.Visibility = Visibility.Hidden;
            northImg.Visibility = Visibility.Hidden;
            readyTimer.Visibility = Visibility.Visible;
            readyTimer.Visibility = Visibility.Visible;
            if (directionChange && currentDirectionIdx != numberOfDirections - 1)
            {
                readyTimer.Text = "Please pay attention to the new direction.";
                await Task.Delay(3000);
            }
            readyTimer.Text = "2...";
            await Task.Delay(1000); 
            readyTimer.Text = "2...1...";
            await Task.Delay(1000);
            readyTimer.Visibility = Visibility.Hidden;
            northImg.Visibility = Visibility.Visible;
            MainWindow.resp.SetBG();
            experimentInProgress = true;
        }

        private async void BlockBreak()
        {
            experimentInProgress = false;
            highPointHit.Visibility = Visibility.Hidden;
            northImg.Visibility = Visibility.Hidden;
            readyTimer.Visibility = Visibility.Visible;
            readyTimer.Visibility = Visibility.Visible;     
            readyTimer.Text = "30 second break. You will be notified when there are 5 seconds left.";
            await Task.Delay(blockBreakDuration - 5000);
            readyTimer.Text = "5...";
            await Task.Delay(1000);
            readyTimer.Text = "4...";
            await Task.Delay(1000);
            readyTimer.Text = "3...";
            await Task.Delay(1000);
            readyTimer.Text = "2...";
            await Task.Delay(1000);
            readyTimer.Text = "1...";
            await Task.Delay(1000);
            readyTimer.Visibility = Visibility.Hidden;
            northImg.Visibility = Visibility.Visible;
            MainWindow.resp.SetBG();
            experimentInProgress = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}
