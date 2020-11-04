using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
        int blockBreakDuration = 30000;
        BackgroundWorker worker;
        bool recordingInProgress = false;
        bool experimentInProgress = false;
        bool answerEntered = false;

        int attemptsPerTrialLimit = 5;
        int timesHighestMarkHit = 0;
        String participantId = "";
        String hapticMarksAnswer = "";
        static string[] directionsToTest = { "n", "w", "e", "s" };
        //List<String> directionsToTest = new List<String> { "n", "e" };

        bool highHit = false;

        int dirCount = 4;
        List<int> testNumMarks = new List<int> { 4, 2, 3, 1, 3, 1, 2, 4, 1, 2, 3, 4 };
        int testNumIdx = 0;
        int sampleClicked = 0;
        List<int> numMarksToTest = new List<int> { 1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4 };
        private static Random rng = new Random();
        Regex nonNumerical = new Regex("[^0-9.]");

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
            //MainWindow.deactivateFeedback = true;
            MainWindow.deactivateFeedback = false;
            dirCount = directionsToTest.Length;
            MainWindow.hapticMarkExpInProgress = true;
            MainWindow.patternMode = 1;
            ResetHapticMarks();
            MainWindow.GenerateHapticMarks(1, testNumMarks[0]);
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
                northImg.Source = new BitmapImage(new Uri(@"C:\Users\anastasialalamentik\Desktop\HapticGlance\IllusionDriver\ForceReader\" + directionsToTest[currentDirectionIdx] + ".png"));
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
                BlockBreak();
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
                        northImg.Source = new BitmapImage(new Uri(@"C:\Users\anastasialalamentik\Desktop\HapticGlance\IllusionDriver\ForceReader\" + directionsToTest[currentDirectionIdx] + ".png"));
                    }
                }
            }
            else {
                detectRecordingStart();
                if (recordingInProgress && !answerEntered)
                {
                    String fileName = @"C: \Users\anastasialalamentik\Downloads\DataCollection\HapticMarkExp\HapticMarkExp_Participant-"+ participantId + "_Block-"+ blockNumber + "_Trial-" + trialNumber + "-direction_" + directionsToTest[currentDirectionIdx] + string.Format("-{0:yyyy-MM-dd_hh-mm-ss-tt}.csv", DateTime.Now);
                    MainWindow.setRecordValuesMode(true, fileName);
                    recordingInProgress = true;
                }
                trackHighestThresholdHit();
            }
        }

        private async void StartExperiment(object sender, RoutedEventArgs e)
        {
            startExperiment.Visibility = Visibility.Hidden;
            readyTimer.Visibility = Visibility.Visible;
            MainWindow.deactivateFeedback = false;
            Debug.WriteLine("start exp this many marks" + " " + GetCurrentDirection() + " " + numMarksToTest[currentMarksIdx]);
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
            expInstructions.Visibility = Visibility.Visible;
            getFamiliarInstructions.Visibility = Visibility.Hidden;
            worker.RunWorkerAsync();
            marksFeltAnswerBox.Focus();
            northImg.Source = new BitmapImage(new Uri(@"C:\Users\anastasialalamentik\Desktop\HapticGlance\IllusionDriver\ForceReader\" + directionsToTest[currentDirectionIdx] + ".png"));
            MainWindow.hapticMarkExpInProgress = true;
            ResetHapticMarks();
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
            marksFeltAnswerBox.Text = String.Empty;
            MainWindow.deactivateFeedback = false;
            timesHighestMarkHit = 0;
        }

        private void ResetHapticMarks()
        {
            MainWindow.GenerateHapticMarks(1, 0);
            MainWindow.GenerateHapticMarks(2, 0);
            MainWindow.GenerateHapticMarks(3, 0);
            MainWindow.GenerateHapticMarks(4, 0);
            if (currentMarksIdx < numMarksToTest.Count)
            {
                MainWindow.GenerateHapticMarks(GetCurrentDirection(), numMarksToTest[currentMarksIdx]);
            }
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
            marksFeltAnswerBox.Text = String.Empty;
            readyTimer.Text = "30 second break. You will be notified when there are 5 seconds left.";
            await Task.Delay(blockBreakDuration-5000);
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
            MainWindow.deactivateFeedback = false;
            timesHighestMarkHit = 0;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        void trackHighestThresholdHit()
        {
            String currentDirection = directionsToTest[currentDirectionIdx];
            if (currentDirection == "w" && timesHighestMarkHit < attemptsPerTrialLimit)
            {
                if (MainWindow.force.fx < -2)
                {
                    highHit = true;
                }
                if (highHit && MainWindow.force.fx > -0.05)
                {
                    timesHighestMarkHit += 1;
                    highHit = false;
                }
            }
            if (currentDirection == "e" && timesHighestMarkHit < attemptsPerTrialLimit)
            {
                if (MainWindow.force.fx > 2)
                {
                    highHit = true;
                }
                if (highHit && MainWindow.force.fx < 0.05)
                {
                    timesHighestMarkHit += 1;
                    highHit = false;
                }
            }
            if (currentDirection == "s" && timesHighestMarkHit < attemptsPerTrialLimit)
            {
                if (MainWindow.force.fy < -2)
                {
                    highHit = true;
                }
                if (highHit && MainWindow.force.fy > -0.05)
                {
                    timesHighestMarkHit += 1;
                    highHit = false;
                }
            }
            if (currentDirection == "n" && timesHighestMarkHit < attemptsPerTrialLimit)
            {
                if (MainWindow.force.fy > 2)
                {
                    highHit = true;
                }
                if (highHit && MainWindow.force.fy < 0.05)
                {
                    timesHighestMarkHit += 1;
                    highHit = false;
                }
            }
            if (timesHighestMarkHit >= attemptsPerTrialLimit)
            {
                MainWindow.deactivateFeedback = true;
                Debug.WriteLine("zonk");
            }

            //if (currentDirection == "s" || currentDirection == "n")
            //{
            //    if (Math.Abs(MainWindow.force.fy) > 2)
            //    {
            //        timesHighestMarkHit += 1;
            //    }
            //}
            //else if (currentDirection == "w" || currentDirection == "e")
            //{
            //    if (Math.Abs(MainWindow.force.fx) > 2)
            //    {
            //        timesHighestMarkHit += 1;
            //    }
            //}
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

        //private void RecordAnswer(object sender, RoutedEventArgs e)
        private void RecordAnswer()
        {
            answerEntered = true;
            hapticMarksAnswer = nonNumerical.Replace(marksFeltAnswerBox.Text, "");
            int correctAnswer = numMarksToTest[currentMarksIdx];
            Debug.WriteLine("User's answer: " +  hapticMarksAnswer + ", correct answer: " + correctAnswer);
            MainWindow.hapticMarkAnswerLine = (correctAnswer.ToString() == hapticMarksAnswer).ToString() + ", " + hapticMarksAnswer + ", " + correctAnswer;
            marksFeltAnswerBox.Text = String.Empty;
            if (recordingInProgress)
            {
                MainWindow.setRecordValuesMode(false, null);
            }
            recordingInProgress = false;
            Console.WriteLine("done... ");
            MainWindow.resp.SetBG();
        }

        private void NewSample_Click(object sender, RoutedEventArgs e)
        {
            testNumIdx += 1;
            testMarkCount.Content = testNumMarks[testNumIdx];
            MainWindow.GenerateHapticMarks(1, testNumMarks[testNumIdx]);
            ShowTickMarks(testNumMarks[testNumIdx]);
            sampleClicked += 1;
            if (sampleClicked == 9)
            {
                startExperiment.Visibility = Visibility.Visible;
                testMarkCountSelection.Visibility = Visibility.Hidden;
                testMarkCount.Visibility = Visibility.Hidden;
                getFamiliarInstructions.Visibility = Visibility.Hidden;
                expInstructions.Visibility = Visibility.Visible;
                ResetHapticMarks();
                MainWindow.hapticMarkExpInProgress = false;
                MainWindow.deactivateFeedback = true;
            }
        }

        private void ShowTickMarks(int testMarksCount)
        {
            firstTick.Visibility = Visibility.Visible;
            secondTick.Visibility = Visibility.Hidden;
            thirdTick.Visibility = Visibility.Hidden;
            fourthTick.Visibility = Visibility.Hidden;
            if (testMarksCount > 1)
            {
                secondTick.Visibility = Visibility.Visible;
                if (testMarksCount > 2)
                {
                    thirdTick.Visibility = Visibility.Visible;
                    if (testMarksCount > 3)
                    {
                        fourthTick.Visibility = Visibility.Visible;
                    }
                }
            }

        }

        private void marksFeltAnswerBox_PreviewTextInput_1(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9+]\n+");
            e.Handled = regex.IsMatch(e.Text);
        }


        private void marksFeltAnswerBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            Regex regex = new Regex("[^0-9]\n");
            if (e.Key == System.Windows.Input.Key.OemPlus || e.Key == System.Windows.Input.Key.Add)
            {
                if (marksFeltAnswerBox.Text.Any(char.IsDigit))
                {
                    RecordAnswer();
                    marksFeltAnswerBox.Text = String.Empty;
                }
            }
        }
    }
}
