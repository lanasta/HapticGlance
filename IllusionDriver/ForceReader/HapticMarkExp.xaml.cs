using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ForceReader
{
    public partial class HapticMarkExp : Window
    {
        int blockNumber = 1;
        int blockLimit = 3;
        int trialNumber = 1;
        int trialLimit = 10;
        int currentDirectionIdx = 0;
        int currentMarksIdx = 0;
        int blockBreakDuration = 1000;
        BackgroundWorker worker;
        bool recordingInProgress = false;
        bool experimentInProgress = false;
        bool answerEntered = false;
        String participantId = "";
        String hapticMarksAnswer = "";
        static string[] directionsToTest = { "s", "w", "e", "n" };
        //List<String> directionsToTest = new List<String> { "n", "e" };

        int dirCount = 4;
        List<int> numMarksToTest = new List<int> {1,2,3,4,2,3,4,2,3,4};
        private static Random rng = new Random(); 

        public HapticMarkExp(String participantId)
        {
            InitializeComponent();
            this.participantId = participantId;
            numMarksToTest = ShuffleArray(numMarksToTest);
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_doWork);
            worker.ProgressChanged += new ProgressChangedEventHandler
                    (worker_ProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler
                    (worker_RunWorkerCompleted);
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            MainWindow.deactivateFeedback = true;
            dirCount = directionsToTest.Length;
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
            if (currentDirectionIdx == dirCount)
            {
                trialNumber = 1;
                blockNumber += 1;
                numMarksToTest = ShuffleArray(numMarksToTest);
                currentMarksIdx = 0;
                currentDirectionIdx = 0;
                northImg.Source = new BitmapImage(new Uri(@"C:\Users\anastasialalamentik\Downloads\PseudoBend-master\PseudoBend-master\IllusionDriver\ForceReader\" + directionsToTest[currentDirectionIdx] + ".png"));
                BlockBreak();
                if (blockNumber > blockLimit)
                {
                    MainWindow.deactivateFeedback = true;
                    experimentInProgress = false;
                    expInstructions.Text = "You have completed this experiment. Please close the window.";
                    northImg.Visibility = Visibility.Hidden;
                    readyTimer.Visibility = Visibility.Hidden;
                    answerGroup.Visibility = Visibility.Hidden;
                    await Task.Delay(2000);
                    MainWindow.hapticMarkExpInProgress = false;
                    worker.CancelAsync();
                    return;
                }
            }
            if (answerEntered && currentDirectionIdx < dirCount)
            {
                trialNumber += 1;
                currentMarksIdx += 1;
                TrialBreak(trialNumber > trialLimit);
                if (trialNumber > trialLimit )
                {
                    currentDirectionIdx += 1;
                    trialNumber = 1;
                    if (currentDirectionIdx < dirCount)
                    {
                        northImg.Source = new BitmapImage(new Uri(@"C:\Users\anastasialalamentik\Downloads\PseudoBend-master\PseudoBend-master\IllusionDriver\ForceReader\" + directionsToTest[currentDirectionIdx] + ".png"));
                    }
                }
            }
            else {
                detectRecordingStart();
                if (recordingInProgress && !answerEntered)
                {
                    String fileName = @"C: \Users\anastasialalamentik\Downloads\DataCollection\HapticMarkExp_Participant-"+ participantId + "_Block-"+ blockNumber + "_Trial-" + trialNumber + "-direction_" + directionsToTest[currentDirectionIdx] + string.Format("-{0:yyyy-MM-dd_hh-mm-ss-tt}.csv", DateTime.Now);
                    MainWindow.setRecordValuesMode(true, fileName);
                    recordingInProgress = true;
                }
            }
        }

        private async void StartExperiment(object sender, RoutedEventArgs e)
        {
            startExperiment.Visibility = Visibility.Hidden;
            readyTimer.Visibility = Visibility.Visible;
            Debug.WriteLine("start exp this many marks" + " " + GetCurrentDirection() + " " + numMarksToTest[currentMarksIdx]);
            ResetHapticMarks();
            readyTimer.Text = "3...";
            await Task.Delay(1000);
            readyTimer.Text = "3...2...";
            await Task.Delay(1000);
            readyTimer.Text = "3...2...1...";
            await Task.Delay(1000);
            readyTimer.Visibility = Visibility.Hidden;
            experimentInProgress = true;
            northImg.Visibility = Visibility.Visible;
            answerGroup.Visibility = Visibility.Visible;
            worker.RunWorkerAsync();
            marksFeltAnswerBox.Focus();
            northImg.Source = new BitmapImage(new Uri(@"C:\Users\anastasialalamentik\Downloads\PseudoBend-master\PseudoBend-master\IllusionDriver\ForceReader\" + directionsToTest[currentDirectionIdx] + ".png"));
            MainWindow.hapticMarkExpInProgress = true;
            MainWindow.deactivateFeedback = false;
        }

        private async void TrialBreak(bool directionChange) {
            answerEntered = false;
            experimentInProgress = false;
            answerGroup.Visibility = Visibility.Hidden;
            northImg.Visibility = Visibility.Hidden;
            readyTimer.Visibility = Visibility.Visible;
            readyTimer.Visibility = Visibility.Visible;
            if (directionChange && currentDirectionIdx != dirCount - 1)
            {
                numMarksToTest = ShuffleArray(numMarksToTest);
                currentMarksIdx = 0;
                readyTimer.Text = "Please pay attention to the new direction.";
                await Task.Delay(3000);
            }
            readyTimer.Text = "2...";
            await Task.Delay(1000); 
            readyTimer.Text = "2...1...";
            await Task.Delay(1000);
            ResetHapticMarks();
            readyTimer.Visibility = Visibility.Hidden;
            northImg.Visibility = Visibility.Visible;
            answerGroup.Visibility = Visibility.Visible;
            MainWindow.resp.SetBG();
            experimentInProgress = true;
            marksFeltAnswerBox.Focus();
        }

        private void ResetHapticMarks()
        {
            MainWindow.GenerateHapticMarks(1, 0);
            MainWindow.GenerateHapticMarks(2, 0);
            MainWindow.GenerateHapticMarks(3, 0);
            MainWindow.GenerateHapticMarks(4, 0);
            MainWindow.GenerateHapticMarks(GetCurrentDirection(), numMarksToTest[currentMarksIdx]);
        }

        private int GetCurrentDirection()
        {
            if (currentDirectionIdx > dirCount - 1) { return 1; }
            String curDir = directionsToTest[currentDirectionIdx];
            int correspondingNumber = curDir == "w" ?1 : (curDir == "s" ? 4 : (curDir == "n" ? 2 : 3));
            return correspondingNumber; 
        }

        List<int> ShuffleArray(List<int> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                int value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            foreach (object o in list)
            {
                Console.WriteLine(o);
            }
            return list;
        }

        private async void BlockBreak()
        {
            experimentInProgress = false;
            answerEntered = false;
            ResetHapticMarks();
            answerGroup.Visibility = Visibility.Hidden;
            northImg.Visibility = Visibility.Hidden;
            readyTimer.Visibility = Visibility.Visible;
            readyTimer.Visibility = Visibility.Visible;
            readyTimer.Text = "30 second break. You will be notified when there are 5 seconds left.";
            await Task.Delay(blockBreakDuration);
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
            answerGroup.Visibility = Visibility.Visible;
            MainWindow.resp.SetBG();
            marksFeltAnswerBox.Focus();
            experimentInProgress = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        void detectRecordingStart()
        {
            String currentDirection = directionsToTest[currentDirectionIdx];
            if (currentDirection == "s" || currentDirection == "n")
            {
                manageStateChanges(MainWindow.force.fy);
            }
            else if (currentDirection == "w" || currentDirection == "e")
            {
                manageStateChanges(MainWindow.force.fx);
            }
            else
            {
                double magnitude = Math.Sqrt(MainWindow.force.fx * MainWindow.force.fx + MainWindow.force.fy * MainWindow.force.fy);
                manageStateChanges(magnitude);
            }
        }

        void manageStateChanges(double force)
        {
           if (evaluateRecordStart(force) && experimentInProgress && !recordingInProgress && !answerEntered)
            {
                recordingInProgress = true;
                MainWindow.expBeginningMs = (DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;
                Console.WriteLine("recording..." + force);
            }
        }

        bool evaluateRecordStart(double value)
        {
            String curDir = directionsToTest[currentDirectionIdx];
            if (curDir == "ne" || curDir == "nw" || curDir == "sw" || curDir == "se" || curDir == "n" || curDir == "e")
            {
                return value > 0.01;
            }
            else
            {
                return value < -0.01;
            }
        }


        private void marksFeltAnswerBox_PreviewTextInput_1(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]\n+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void RecordAnswer(object sender, RoutedEventArgs e)
        {
            answerEntered = true;
            hapticMarksAnswer = marksFeltAnswerBox.Text;
            Debug.WriteLine("answerYoohoo", hapticMarksAnswer);
            int correctAnswer = numMarksToTest[currentMarksIdx];
            MainWindow.hapticMarkAnswerLine = (correctAnswer.ToString() == hapticMarksAnswer).ToString() + ", " + hapticMarksAnswer + ", " + correctAnswer;
            marksFeltAnswerBox.Text = "";
            if (recordingInProgress)
            {
                MainWindow.setRecordValuesMode(false, null);
            }
            recordingInProgress = false;
            Console.WriteLine("done... ");
            MainWindow.resp.SetBG();
        }
    }
}
