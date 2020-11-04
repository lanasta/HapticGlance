using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Threading;
using System.ComponentModel;
using System.IO.Ports;
using System.Windows.Threading;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Shapes;
using System.IO;
using System.Text;

namespace ForceReader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        taskWindow taskView = new taskWindow();

        public static bool recordValues = false;
        bool marksReady = false;
        public static string currentFilePath;
        public static HashSet<ExpData> valuesToWrite = new HashSet<ExpData>();
        public static bool deactivateFeedback = false;
        public static bool hapticMarkExpInProgress = false;
        public static String hapticMarkAnswerLine = "";
        public static double expBeginningMs = (DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;

        static string ipAddr = "192.168.1.1";
        static int port = 49152;
        public static Response resp;
        public static Force force;
        public static bool fileWritingInProgress = false;
        Force hapticForce;
        Force prevHapticForce;

        BackgroundWorker worker;
                

        bool workerOn = false;
        IPEndPoint ep;

        bool bendBottomOut = false;
        double bendBottomOutThresh = 20.5;

        bool leftBottomOutPlayed = false;

        int selectedFrictionValue = 1;
        //low values: easy movement, high values: frictional movement with bottom out vibrations
        //minimal effort vs a lot of effort pressing (fz) could mean different things
        bool messageExists = true; 
        bool itemsInSchedule = true;
        bool newEmails = true; 
        bool missedCallsExist = true; 
        bool currentlyOutOfBounds = false;

        int[] encodingCounts = { 3, 3, 3, 3 }; //left, up, right, down

        /* 0 - normal grain, 1 - bottom out, 2 - buzzing*/
        public static int patternMode = 0;

        public static void setRecordValuesMode(bool recording, string fileName) {
            if (recording) {
                currentFilePath = fileName;
                recordValues = true;
            }
            else {
                recordValues = false;
                if (valuesToWrite == null || valuesToWrite.Count == 0) return;
                saveDataToFileAndClearList();
            }
        }

        public static void saveDataToFileAndClearList() {
            Debug.WriteLine(currentFilePath);
            using (TextWriter tw = new StreamWriter(currentFilePath))
            {
                if (hapticMarkAnswerLine != "")
                {
                    tw.WriteLine(string.Format("Correct, User Answer, Correct Answer"));
                    tw.WriteLine(hapticMarkAnswerLine);
                }
                tw.WriteLine(string.Format( "Timestamp, Fx, Fy, Fz, Magnitude"));
                foreach (var data in valuesToWrite)
                {
                    tw.WriteLine(string.Format("{0}, {1}, {2}, {3}, {4}", data.timestamp.ToString(), (data.fx).ToString(), data.fy.ToString(), data.fz.ToString(), data.magnitude.ToString()));
                }
                tw.Close();
            }
            valuesToWrite = new HashSet<ExpData>();
        }

        public MainWindow()
        {
            InitializeComponent();

            resp = new Response();
            force = new Force(false);
            hapticForce = new Force(false);
            prevHapticForce = new Force(false);

            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_doWork);
            worker.ProgressChanged += new ProgressChangedEventHandler
                    (worker_ProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler
                    (worker_RunWorkerCompleted);
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            client = new UdpClient();
            ep = new IPEndPoint(IPAddress.Parse(ipAddr), port); // endpoint where server is listening
            client.Connect(ep);
        }


        int fps = 0;
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
            force.SetForce(resp, true);            
            hapticForce.SetForce(resp, false);
            if (hapticForce.isRoaming)
            {
                lCnt = 0;
                uCnt = 0;
                rCnt = 0;
                dCnt = 0;
            }
            fps++;
            ProcessForce();
        }

        void worker_doWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                Thread.Sleep(1);
                GetFrame();
                worker.ReportProgress(1);
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    worker.ReportProgress(0);
                    return;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!workerOn)
            {
                workerOn = true;
                worker.RunWorkerAsync();
            }
        }

        UdpClient client;
        private void GetFrame()
        {
            byte[] request = new byte[8];
            request[0] = 0x12;
            request[1] = 0x34;
            request[2] = 0x00;
            request[3] = 0x02;
            request[4] = 0x00;
            request[5] = 0x00;
            request[6] = 0x00;
            request[7] = 0x01;

            client.Send(request, 8);
            var receivedData = client.Receive(ref ep);

            for (int i = 0; i < 6; i++)
            {
                var data = receivedData.Skip(12 + i * 4).Take(4).ToArray();
                resp.FTData[i] = BitConverter.ToInt32(data.Reverse().ToArray(), 0);
            }
        }

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            workerOn = false;
            worker.CancelAsync();
        }

        private void calButton_Click(object sender, RoutedEventArgs e)
        {
            resp.SetBG();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            DispatcherTimer UITimer = new DispatcherTimer();
            UITimer.Interval = TimeSpan.FromMilliseconds(16);
            UITimer.Tick += UITimer_Tick;
            UITimer.Start();

            DispatcherTimer FPSTimer = new DispatcherTimer();
            FPSTimer.Interval = TimeSpan.FromMilliseconds(1000);
            FPSTimer.Tick += FPSTimer_Tick;
            FPSTimer.Start();

            SetMarks();
        }


        private void UITimer_Tick(object sender, EventArgs e)
        {
            f1.Text = force.fx.ToString("0.00");
            f2.Text = force.fy.ToString("0.00");
            f3.Text = force.fz.ToString("0.00");

            // Update cursors
            double forceScale = forcePanel.Width / 5;
            double forceX = forcePanel.Width / 2 + force.fx * forceScale + forcePanel.Margin.Left - forceCursor.Width / 2;
            double forceY = forcePanel.Height / 2 + (0 - force.fy) * forceScale + forcePanel.Margin.Top - forceCursor.Height / 2;
            forceCursor.Margin = new Thickness(forceX, forceY, 0, 0);

            if(!isOutofBound)
            {
                double hapticX = forcePanel.Width / 2 + hapticForce.fx * forceScale + forcePanel.Margin.Left - hapticCursor.Width / 2;
                double hapticY = forcePanel.Height / 2 + (0 - hapticForce.fy) * forceScale + forcePanel.Margin.Top - hapticCursor.Height / 2;
                hapticCursor.Margin = new Thickness(hapticX, hapticY, 0, 0);

            }

            //frictionLabel.Content = selectedFrictionValue.ToString();
            //frictionSlider.Value = selectedFrictionValue;

            roamArea.Width = force.dirThresh * forceScale * 2;
            roamArea.Height = force.dirThresh * forceScale * 2;
            roamArea.Margin = new Thickness(forcePanel.Width / 2 - roamArea.Width / 2,
                                            forcePanel.Margin.Top + forcePanel.Height / 2 - roamArea.Height / 2, 0, 0);

            //debugBox.Text = debugMsg;
        }

        private void SetMarks()
        {
            double[] higherCountMarks = { 0.4, 1, 1.6, 2.2, 2.8 };
            //double[] biggerGapMarks = { 0.4, 1, 1.6, 2.2, 2.8 };

            double centerX = 100;
            double centerY = 125;

            double lineWidth = 20;
            double forceScale = forcePanel.Width / 5;

            // Up
            for (int i = 0; i < 4; i++)
            {
                Line line = new Line();
                line.Visibility = Visibility.Visible;
                line.StrokeThickness = 2;
                line.Stroke = Brushes.Red;

                line.X1 = centerX - lineWidth / 2;
                line.X2 = centerX + lineWidth / 2;
                line.Y1 = centerY + higherCountMarks[i] * forceScale;
                line.Y2 = centerY + higherCountMarks[i] * forceScale;
                line.Name = "D" + i.ToString();
                hapticCanvas.Children.Add(line);
            }

            // Down
            for (int i = 0; i < 4; i++)
            {
                Line line = new Line();
                line.Visibility = Visibility.Visible;
                line.StrokeThickness = 2;
                line.Stroke = Brushes.Red;

                line.X1 = centerX - lineWidth / 2;
                line.X2 = centerX + lineWidth / 2;
                line.Y1 = centerY - higherCountMarks[i] * forceScale;
                line.Y2 = centerY - higherCountMarks[i] * forceScale;
                line.Name = "U" + i.ToString();
                hapticCanvas.Children.Add(line);
            }

            // Left
            for (int i = 0; i < 4; i++)
            {
                Line line = new Line();
                line.Visibility = Visibility.Visible;
                line.StrokeThickness = 2;
                line.Stroke = Brushes.Red;

                line.X1 = centerX - higherCountMarks[i] * forceScale;
                line.X2 = centerX - higherCountMarks[i] * forceScale;
                line.Y1 = centerY - lineWidth / 2;
                line.Y2 = centerY + lineWidth / 2;
                line.Name = "L" + i.ToString();
                hapticCanvas.Children.Add(line);
            }



            // Right
            for (int i = 0; i < 4; i++)
            {
                Line line = new Line();
                line.Visibility = Visibility.Visible;
                line.StrokeThickness = 2;
                line.Stroke = Brushes.Red;

                line.X1 = centerX + higherCountMarks[i] * forceScale;
                line.X2 = centerX + higherCountMarks[i] * forceScale;
                line.Y1 = centerY - lineWidth / 2;
                line.Y2 = centerY + lineWidth / 2;
                line.Name = "R" + i.ToString();
                hapticCanvas.Children.Add(line);
            }
            InitializeComponent();
            marksReady = true;
        }

        private void FPSTimer_Tick(object sender, EventArgs e)
        {
            fpsBox.Content = fps.ToString();
            fps = 0;
        }

        double length = 0;
        Point bend;
        private void ProcessForce()
        {
            if (!freeze) {
                bool noNotifications = !messageExists && !missedCallsExist && !newEmails && !itemsInSchedule;
                hapticForce.noHapticFeedback = noNotifications;
                if (noNotifications || DeaactivateDirectionalFeedback())
                {
                    return;
                }
                length = force.fz;
                bend = new Point(force.fx, force.fy);
            }

            if (actuator.IsOpen)
            {
                double dx = bend.X - lastBend.X;
                double dy = bend.Y - lastBend.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                double bendMag = Math.Sqrt(bend.X * bend.X + bend.Y * bend.Y);

                if (bendMag < bendBottomOutThresh)
                {
                    bendBottomOut = false;
                    if (dist > bendThresh)
                    {
                        EncodeTactilePatterns();

                        //isOutofBound = deactivateFeedback ? false : forceIsOutOfBounds();
                        double mag = Math.Sqrt(force.fx * force.fx + force.fy * force.fy);

                        PlayGrain();
                        //if (!hapticMarkExpInProgress)
                        //{
                          //  PlayGrain();
                            if (mag <= 0.4) //!isOutofBound
                            {
                                //PlayGrain();
                            }  else {
                                //PlayBottomOut();
                            }
                        //}
                        
                        lastBend = new Point(bend.X, bend.Y);
                        prevHapticForce.fx = hapticForce.fx;
                        prevHapticForce.fy = hapticForce.fy;
                    }
                }
                else
                {
                    if (!bendBottomOut && !hapticMarkExpInProgress)
                    {
                        bendBottomOut = true;
                        PlayBottomOut();
                    }
                }
            }
        }
        bool isOutofBound = false;

        private bool DeaactivateDirectionalFeedback()
        {
            hapticForce.noHapticFeedbackLeft = !messageExists && force.fx < 0 ? true : false;
            hapticForce.noHapticFeedbackUp = !newEmails && force.fy > 0 ? true : false;
            hapticForce.noHapticFeedbackRight = !missedCallsExist && force.fx > 0 ? true : false;
            hapticForce.noHapticFeedbackDown = !itemsInSchedule && force.fy < 0 ? true : false;
            return hapticForce.noHapticFeedbackLeft || hapticForce.noHapticFeedbackRight || hapticForce.noHapticFeedbackUp || hapticForce.noHapticFeedbackDown;
        }

        string debugMsg = "";
        bool doPlay = true;

        int lCnt = 0;
        int uCnt = 0;
        int rCnt = 0;
        int dCnt = 0;
        private void EncodeTactilePatterns()
        {
            if (hapticForce.fx < 0 && messageExists)
            {               
                for (int i = lCnt; i < leftMarks.Count; i ++)
                {
                    leftBottomOutPlayed = false;
                    if (leftMarks[i] < prevHapticForce.fx && leftMarks[i] > hapticForce.fx && lCnt <= encodingCounts[0] )
                    {
                        if (patternMode == 1 && !leftBottomOutPlayed) { 
                            PlayBottomOut();
                            leftBottomOutPlayed = true;
                            Debug.WriteLine(leftMarks[i] + " " + hapticForce.fx + " " + hapticForce.fy);
                        }
                        lCnt++;
                    }
                }
                return;
            }
            if (hapticForce.fy > 0 && newEmails) {
                for (int i = uCnt; i < upMarks.Count; i++)
                {
                    if (upMarks[i] > prevHapticForce.fy && upMarks[i] < hapticForce.fy)
                    {
                        if (patternMode == 1) PlayBottomOut();
                        uCnt++;
                    }
                }
                return;
            }

            if (hapticForce.fy < 0 && itemsInSchedule)
            {
                for (int i = dCnt; i < downMarks.Count; i++)
                {
                    if (downMarks[i] < prevHapticForce.fy && downMarks[i] > hapticForce.fy)
                    {
                        if (patternMode == 1) PlayBottomOut();
                        dCnt++;
                    }
                }
                return;
            }

            if (hapticForce.fx > 0 && missedCallsExist)
            {
                for (int i = rCnt; i < rightMarks.Count; i++)
                {
                    if (rightMarks[i] > prevHapticForce.fx && rightMarks[i] < hapticForce.fx)
                    {
                        if (patternMode == 1) PlayBottomOut();
                        rCnt++;
                    }
                }
                return;
            }
        }

        SerialPort actuator = new SerialPort();
        private void serialBtn_Click(object sender, RoutedEventArgs e)
        {
            actuator.BaudRate = 230400;
            actuator.PortName = portBox.Text;
            actuator.Open();
            if (actuator.IsOpen)
            {
                serialButton.Background = Brushes.Green;
            }
        }

        private void bottomOutBtn_Click(object sender, RoutedEventArgs e)
        {
            PlayBottomOut();
        }

        private bool forceIsOutOfBounds()
        {
            //check if currently out of bounds, if it is, then use a different threshold (-0.05)
            if (leftMarks.Count > 0 && force.fx < leftMarks[leftMarks.Count - 1])
            {
                return true;
            } 
            if (rightMarks.Count > 0 && force.fx > rightMarks[rightMarks.Count -1 ])
            {
                return true;
            }
            if (downMarks.Count > 0 && force.fy < downMarks[downMarks.Count - 1])
            {
                return true;
            }
            if (upMarks.Count > 0 && force.fy > upMarks[upMarks.Count - 1])
            {
                return true;
            }
            return false;
        }

        private void PlayBottomOut()
        {
             if (deactivateFeedback) return;
            actuator.Write("b");
            debugMsg += "B";
            Debug.WriteLine("b played");
        }

        private void PlayGrain()
        {
            if (doPlay || (!deactivateFeedback && hapticMarkExpInProgress))
               actuator.Write("a");
         }

        Point lastBend;
        double bendThresh = 0.1;
        private void setButton_Click(object sender, RoutedEventArgs e)
        {
            bendThresh = double.Parse(bendThreshBox.Text);
            lastBend = new Point(0, 0);

            bendBottomOutThresh = double.Parse(bendLimitBox.Text);
        }

        bool freeze = false;
        private void sendCheck_Checked(object sender, RoutedEventArgs e)
        {
            freeze = true;
        }

        private void sendCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            freeze = false;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            frictionLabel.Content = ((System.Windows.Controls.Slider)sender).Value.ToString("0");
        } 

        private void Set_Friction(object sender, RoutedEventArgs e)
        {
            selectedFrictionValue = Int32.Parse((string)(frictionLabel.Content));
            bendThresh = selectedFrictionValue <= 1 ? 0.2 : (0.2 + 0.2 * (selectedFrictionValue - 1));
            bendThreshBox.Text = bendThresh.ToString();
        }

        private void bendThreshBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

        private void setButton_Copy_Click(object sender, RoutedEventArgs e)
        {
        }

        public static List<double> leftMarks, rightMarks, upMarks, downMarks;

        public object UIHelper { get; private set; }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        public static T FindControl<T>(UIElement parent, Type targetType, string ControlName) where T : FrameworkElement
        {

            if (parent == null) return null;

            if (parent.GetType() == targetType && ((T)parent).Name == ControlName)
            {
                return (T)parent;
            }
            T result = null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                UIElement child = (UIElement)VisualTreeHelper.GetChild(parent, i);

                if (FindControl<T>(child, targetType, ControlName) != null)
                {
                    result = FindControl<T>(child, targetType, ControlName);
                    break;
                }
            }
            return result;
        }

        public void UpdateMarks()
        {
            if (!marksReady)
                return;


            string[] dirs = new string[] { "L", "U", "R", "D" };
            for (int dir = 0; dir < 4; dir++)
            {
                for (int i = 0; i < encodingCounts[dir]; i++)
                {
                    var line = FindControl<Line>(hapticCanvas, typeof(Line), dirs[dir] + i.ToString()); 
                    line.Visibility = Visibility.Visible;

                }
                for (int i = encodingCounts[dir]; i < 4; i++)
                {
                    var line = FindControl<Line>(hapticCanvas, typeof(Line), dirs[dir] + i.ToString());
                    line.Visibility = Visibility.Hidden;
                }
            }
        }

        private void leftEncodingCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            leftEncodingCount.Content = ((System.Windows.Controls.Slider)sender).Value.ToString("0");
            encodingCounts[0] = Int32.Parse(((System.Windows.Controls.Slider)sender).Value.ToString("0"));
            messageExists = encodingCounts[0] == 0 ? false : true;
            GenerateHapticMarks(1, encodingCounts[0]);

            UpdateMarks();
        }

        private void upEncodingCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            upEncodingCount.Content = ((System.Windows.Controls.Slider)sender).Value.ToString("0");
            encodingCounts[1] = Int32.Parse(((System.Windows.Controls.Slider)sender).Value.ToString("0"));
            newEmails = encodingCounts[1] == 0 ? false : true;
            GenerateHapticMarks(2, encodingCounts[1]);

            UpdateMarks();
        }

        private void rightEncodingCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            rightEncodingCount.Content = ((System.Windows.Controls.Slider)sender).Value.ToString("0");
            encodingCounts[2] = Int32.Parse(((System.Windows.Controls.Slider)sender).Value.ToString("0"));
            missedCallsExist = encodingCounts[2] == 0 ? false : true;
            GenerateHapticMarks(3, encodingCounts[2]);

            UpdateMarks();
        }

        private void downEncodingCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            downEncodingCount.Content = ((System.Windows.Controls.Slider)sender).Value.ToString("0");
            encodingCounts[3] = Int32.Parse(((System.Windows.Controls.Slider)sender).Value.ToString("0"));
            itemsInSchedule = encodingCounts[3] == 0 ? false : true;
            GenerateHapticMarks(4, encodingCounts[3]);

            UpdateMarks();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            taskView.Show();
        }

        public static void GenerateHapticMarks(int direction, int count)
        {
            double[] baseHapticMarks = { 0.5, 1, 1.5 };
            double[] higherCountMarks = { 0.4, 0.8, 1.2, 1.6, 2.0 };
            double[] biggerGapMarks = { 0.4, 1, 1.6, 2.2, 2.8 };

            List<Double> hapticMarks = new List<double>();
            for (int i = 0; i < count; i ++)
            {
                double hapticMark = higherCountMarks[i];
                hapticMarks.Add(direction == 1 || direction == 4 ? 0 - hapticMark : hapticMark);

                //double hapticMark = count < 4 ? baseHapticMarks[i] : higherCountMarks[i];
                //hapticMark = (direction == 2 || direction == 4) ? biggerGapMarks[i] : hapticMark;
                //hapticMarks.Add(direction == 1 || direction == 4 ? 0 - hapticMark : hapticMark);
            }
            if (direction == 1)
            {
                leftMarks = new List<Double>(hapticMarks);
            } else if (direction == 2)
            {
                upMarks = new List<Double>(hapticMarks);
            } else if (direction == 3)
            {
                rightMarks = new List<Double>(hapticMarks);
            } else
            {
                downMarks = new List<Double>(hapticMarks);
            }
        }
    }

  public class Force
    {
        static double cntPerForce = 4448221.5;
        public bool isRoaming = true;
        public double fx, fy, fz;
        public bool noHapticFeedback;
        public bool noHapticFeedbackUp, noHapticFeedbackDown, noHapticFeedbackLeft, noHapticFeedbackRight;
        public Force(bool noForce)
        {
            fx = 0; fy = 0; fz = 0;
        }

        bool dirSet = false;
        bool isDirSetX = false;
        public double dirThresh = 0.3;


        public void SetForce(Response resp, bool doLog)
        {
            if (noHapticFeedback)
            {
                return;
            }
            fx = 0 - (fx + Utils.lbToN(resp.GetData(1) / cntPerForce)) / 2;
            fy = 0 - (fy + Utils.lbToN(resp.GetData(0) / cntPerForce)) / 2;
            fz = 0 - (fz + Utils.lbToN(resp.GetData(2) / cntPerForce)) / 2;

            if ((noHapticFeedbackLeft && fx < 0) || (noHapticFeedbackRight && fx > 0))
            {
                fx = 0;
                fy = 0;
            }

            if ((noHapticFeedbackDown && fy < 0) || (noHapticFeedbackUp && fy > 0))
            {
                fx = 0;
                fy = 0;
            }

            if (!MainWindow.deactivateFeedback && Math.Sqrt(fx * fx + fy * fy) > dirThresh)
            {
                isRoaming = false;
                if (MainWindow.hapticMarkExpInProgress)
                {
                    if (!dirSet)
                    {
                        if (Math.Abs(fx) - Math.Abs(fy) > 0)
                        {
                            isDirSetX = true;
                        }
                        else
                        {
                            isDirSetX = false;
                        }
                        dirSet = true;
                    }
                    if (isDirSetX)
                        fy = 0;
                    else
                        fx = 0;
                }
            }
            else
            {
                isRoaming = true;
                dirSet = false;
            }
            if (!MainWindow.recordValues || !doLog) return;
            double magnitude = Math.Sqrt(fx * fx + fy * fy);
            ExpData newPoint = new ExpData(fx, fy, fz, magnitude);
            MainWindow.valuesToWrite.Add(newPoint);
        }
    }

    public class ExpData
    {
        public double timestamp;
        public double fx;
        public double fy;
        public double fz;
        public double magnitude;
   
        public ExpData(double fx, double fy, double fz, double magnitude) {
            timestamp = (DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds - MainWindow.expBeginningMs;
            this.fx = fx;
            this.fy = fy;
            this.fz = fz;
            this.magnitude = magnitude;
        }
    
    }

   public class Response
    {
        public Int32[] FTData;
        public Int32[] FTbackground;

        public Response()
        {
            FTData = new Int32[6];
            FTbackground = new Int32[6];
        }
        public void SetBG()
        {
        for (int i = 0; i < 6; i++)
                FTbackground[i] = FTData[i];
        }
        public Int32 GetData(int i)
        {            
            return FTData[i] - FTbackground[i];
        }
    }
}
