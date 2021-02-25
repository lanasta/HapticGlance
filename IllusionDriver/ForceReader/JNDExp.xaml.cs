using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ForceReader
{
    public partial class JNDExp : Window
    {
        int blockNumber = 1;
        int blockLimit = 1;
        int trialNumber = 1;
        int trialLimit = 10000;
        BackgroundWorker worker;
        bool recordingInProgress = false;
        bool experimentInProgress = false;
        bool answerEntered = false;

        int attemptsPerTrialLimit = 1;
        int timesHighestMarkHit = 0;
        String participantId = "";

        double firstHapticMark = -0.5;
        double hapticMarkDistance = 0.6;
        double startingInterval = 0.1;
        int numReversals = 0;
        int accuracyStreak = 0;
        bool staircaseGoingUp = false;
        double lastTrialSpeed = 0;
        bool highHit = false;

        String summaryFileName = "";
        String summaryFileContent = "TrialNum, ReversalCount, HapticMark1, HapticMark2, JND\n";
        int sampleMarks = 2;
        int reversalsLimit = 12;
          
        public JNDExp(String participantId)
        {
            InitializeComponent();
            this.participantId = participantId;
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_doWork);
            worker.ProgressChanged += new ProgressChangedEventHandler
                    (worker_ProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler
                    (worker_RunWorkerCompleted);
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            ResetHapticMarks();
            MainWindow.deactivateFeedback = false;
            MainWindow.JNDExpInProgress = true;
            MainWindow.patternMode = 1;
            MainWindow.GenerateTwoHapticMarks(1, hapticMarkDistance, false);
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
            if (timesHighestMarkHit < attemptsPerTrialLimit && !MainWindow.checkIfSpeedAcceptable()&& !highHit)
            {
                ShowAnswerSelection(false);
                timesHighestMarkHit = 0;
                speedWarningLabel.Visibility = Visibility.Visible;
                if (MainWindow.speedToOneNewton == 0)
                {
                    speedWarningLabel.Text = "Please release your thumb and re-attempt the trial.";
                }
                else if (MainWindow.speedToOneNewton < MainWindow.acceptableSpeedLow)
                {
                    speedWarningLabel.Text = "You moved too slow at " + MainWindow.speedToOneNewton.ToString("0.00") + " N/s, move faster at 1.5 - 3 N/s";
                }
                else
                {
                    speedWarningLabel.Text = "You moved too fast at " + MainWindow.speedToOneNewton.ToString("0.00") + " N/s, move slower at 1.5 - 3 N/s";
                }
            }
            if (answerEntered)
            {
                trialNumber += 1;
                timesHighestMarkHit = 0;
                if (blockNumber > blockLimit)
                {
                    experimentInProgress = false;
                    speedWarningLabel.Text = "You have completed this experiment. Please close the window.";
                    northImg.Visibility = Visibility.Hidden;
                    readyTimer.Visibility = Visibility.Hidden;
                    ShowAnswerSelection(false);
                    await Task.Delay(2000);
                    MainWindow.JNDExpInProgress = false;
                    worker.CancelAsync();
                    return;
                }
                TrialBreak(trialNumber > trialLimit);
            }
            else {
                detectRecordingStart();
                if (recordingInProgress && !answerEntered)
                {
                    String fileName = @"C: \Users\anastasialalamentik\Downloads\DataCollection\JNDExp\JNDExp_Participant-"+ participantId + "_Block-" + blockNumber + "_Trial-" + trialNumber + string.Format("-{0:yyyy-MM-dd_hh-mm-ss-tt}.csv", DateTime.Now);
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
            testMarkCountSelection.Visibility = Visibility.Hidden;
            switchSampleButton.Visibility = Visibility.Hidden;
            sampleMessage.Visibility = Visibility.Hidden;
            readyTimer.Text = "3...";
            await Task.Delay(1000);
            readyTimer.Text = "3...2...";
            await Task.Delay(1000);
            readyTimer.Text = "3...2...1...";
            await Task.Delay(1000);
            readyTimer.Visibility = Visibility.Hidden;
            experimentInProgress = true;
            northImg.Visibility = Visibility.Visible;
            worker.RunWorkerAsync();
            northImg.Source = new BitmapImage(new Uri(@"C:\Users\anastasialalamentik\Desktop\HapticGlance\IllusionDriver\ForceReader\w.png"));
            testMarkCountSelection.Visibility = Visibility.Visible;
            MainWindow.JNDExpInProgress = true;
            MainWindow.deactivateFeedback = false;
            MainWindow.hapticMarkOutOfBounds = false;
            MainWindow.noHapticMarks = false;
            MainWindow.GenerateTwoHapticMarks(1, hapticMarkDistance, false);
            summaryFileName = @"C: \Users\anastasialalamentik\Downloads\DataCollection\JNDExp\JNDExp_Participant-" + participantId + "-Summary_Block-" + blockNumber + string.Format("-{0:yyyy-MM-dd_hh-mm-ss-tt}.csv", DateTime.Now);
        }

        private async void TrialBreak(bool experimentDone) {
           
            answerEntered = false;
            experimentInProgress = false;
            testMarkCountSelection.Visibility = Visibility.Hidden;
            northImg.Visibility = Visibility.Hidden;
            speedWarningLabel.Text = "";
            MainWindow.GenerateTwoHapticMarks(1, hapticMarkDistance, false);
            if (experimentDone)
            {
                trialNumber = 1;
                Debug.WriteLine("block number: " + blockNumber + ", final distance: " + hapticMarkDistance + ", starting interval: " + startingInterval + ", # reversals: " + numReversals);
                getFamiliarInstructions.Text = "You are done with the experiment";
                System.IO.File.WriteAllText(summaryFileName, summaryFileContent);
                summaryFileName = @"C: \Users\anastasialalamentik\Downloads\DataCollection\JNDExp\JNDExp_Participant-" + participantId + "-Summary_Block-" + blockNumber + string.Format("-{0:yyyy-MM-dd_hh-mm-ss-tt}.csv", DateTime.Now);
                summaryFileContent = "TrialNum, ReversalCount, HapticMark1, HapticMark2, JND\n";
                experimentInProgress = false;
                speedWarningLabel.Text = "You have completed this experiment. Please close the window.";
                MainWindow.GenerateTwoHapticMarks(1, hapticMarkDistance, true);
                MainWindow.JNDExpInProgress = false;
                worker.CancelAsync();
                return;
            }
            readyTimer.Visibility = Visibility.Visible;
            readyTimer.Text = "2...";
            await Task.Delay(1000); 
            readyTimer.Text = "2...1...";
            await Task.Delay(1000);
            readyTimer.Visibility = Visibility.Hidden;
            testMarkCountSelection.Visibility = Visibility.Visible;
            MainWindow.resp.SetBG();
            experimentInProgress = true;
            timesHighestMarkHit = 0;
            ShowAnswerSelection(false);
            highHit = false;
        }

        private void ResetHapticMarks()
        {
            MainWindow.GenerateHapticMarks(1, 0);
            MainWindow.GenerateHapticMarks(2, 0);
            MainWindow.GenerateHapticMarks(3, 0);
            MainWindow.GenerateHapticMarks(4, 0);
        }
         

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        void trackHighestThresholdHit()
        {
            String currentDirection = "w";
            if (answerEntered || !MainWindow.checkIfSpeedAcceptable()) return;
            double topValue = Math.Abs(firstHapticMark) + hapticMarkDistance;
            if (currentDirection == "w" && timesHighestMarkHit < attemptsPerTrialLimit)
            {
                if (MainWindow.force.fx <= - (topValue) || MainWindow.currentMagnitude >= topValue) 
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
                if (MainWindow.force.fx >= topValue)
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
                if (MainWindow.force.fy < - (topValue))
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
                if (MainWindow.force.fy > topValue)
                {
                    highHit = true;
                }
                if (highHit && MainWindow.force.fy < 0.05)
                {
                    timesHighestMarkHit += 1;
                    highHit = false;
                }
            }
            if (timesHighestMarkHit >= attemptsPerTrialLimit && MainWindow.speedToOneNewton != lastTrialSpeed)
            {
                MainWindow.GenerateTwoHapticMarks(1, hapticMarkDistance, true);
                speedWarningLabel.Visibility = Visibility.Hidden;
                highHit = true;
                ShowAnswerSelection(true);
            }
        } 

        void detectRecordingStart()
        {
            String currentDirection = "w";
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
            String curDir = "w";
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
            timesHighestMarkHit = 0;
            trialNumber += 1;
            if (recordingInProgress)
            {
                MainWindow.setRecordValuesMode(false, null);
            }
            recordingInProgress = false;
            Console.WriteLine("done... ");
            MainWindow.resp.SetBG();
            lastTrialSpeed = MainWindow.speedToOneNewton;
            ShowAnswerSelection(false);
            highHit = false;
        }

        private void ShowAnswerSelection(bool show)
        {
            yesButton.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            noButton.Visibility = show ? Visibility.Visible : Visibility.Hidden;
        }


        //If the user keeps saying no, then the interval will increase. 
        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            accuracyStreak += 1;
            if (staircaseGoingUp)
            {
                numReversals += 1;
                staircaseGoingUp = false;
                if (numReversals == 4)
                {
                    startingInterval = startingInterval / 2;
                }
            }
            MainWindow.jndAnswerLine = "Yes, " + (numReversals).ToString() + ", " + hapticMarkDistance + ", " + firstHapticMark + ", " + (firstHapticMark - hapticMarkDistance) + ", " + startingInterval;
            if (accuracyStreak == 2)
            {
                accuracyStreak = 0;
                if (hapticMarkDistance - startingInterval <= 0)
                {
                    startingInterval = startingInterval / 2;
                }
                hapticMarkDistance -= startingInterval;
                //if hit 0.5, half the interval, and make the second mark 0.6
            }
            summaryFileContent += trialNumber.ToString() + ", " + (numReversals).ToString() + ", " + firstHapticMark + ", " + (firstHapticMark - hapticMarkDistance) + ", " + hapticMarkDistance + "\n";
            RecordAnswer();
            TrialBreak(numReversals == reversalsLimit);
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!staircaseGoingUp)
            {
                numReversals += 1;
                staircaseGoingUp = true;
                if (numReversals == 4)
                {
                    startingInterval = startingInterval / 2;
                }
            }
            MainWindow.jndAnswerLine = "No, " + (numReversals).ToString() + ", " + hapticMarkDistance + ", " + firstHapticMark + ", " + (firstHapticMark - hapticMarkDistance) + ", " + startingInterval;
            hapticMarkDistance += startingInterval;
            accuracyStreak = 0;
            summaryFileContent += trialNumber.ToString() + ", " + (numReversals).ToString() + ", " + firstHapticMark + ", " + (firstHapticMark - hapticMarkDistance) + ", " + hapticMarkDistance + "\n";
            RecordAnswer();
            TrialBreak(numReversals == reversalsLimit);
        }

        private void switchSampleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sampleMarks == 2)
            {
                MainWindow.GenerateTwoHapticMarks(1, 0, false);
                sampleMessage.Content = "You should be able to feel 1 haptic mark.";
                switchSampleButton.Content = "Sample 2 haptic marks";
                sampleMarks = 1;
                secondTick.Visibility = Visibility.Hidden;
            } else
            {
                MainWindow.GenerateTwoHapticMarks(1, hapticMarkDistance, false);
                sampleMessage.Content = "You should be able to feel 2 haptic marks.";
                switchSampleButton.Content = "Sample 1 haptic mark";
                secondTick.Visibility = Visibility.Visible;
                sampleMarks = 2;
            }


        }
    }
}
