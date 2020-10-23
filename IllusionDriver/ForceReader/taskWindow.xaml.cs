using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;


namespace ForceReader
{
    /// <summary>
    /// Interaction logic for taskWindow.xaml
    /// </summary>
    public partial class taskWindow : Window
    {
        String participantID;
        DirectionDiscrimination directionDiscriminationTest;
        HapticMarkExp hapticMarkExperiment;
        bool allowExperiments = false;

        public taskWindow()
        {
            InitializeComponent();
        }

        private void HapticMarkCountButton(object sender, RoutedEventArgs e)
        {
            hapticMarkExperiment = new HapticMarkExp(participantID);
            hapticMarkExperiment.Show();
            MainWindow.resp.SetBG(); //recalibrates each time an experiment starts
        }

        private void DirectionDiscriminationButton(object sender, RoutedEventArgs e)
        {
            directionDiscriminationTest = new DirectionDiscrimination(participantID);
            directionDiscriminationTest.Show();
            MainWindow.resp.SetBG(); //recalibrates each time an experiment starts
        }

        private void ParticipantIDChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void BlockNumberChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {

        }


        private void SetInfo(object sender, RoutedEventArgs e)
        {
            var pattern = new Regex("[:!@#$%^&*()}{|\":?><\\;'/.,~]");
            participantID =participantIDBox.Text ;
            pattern.Replace(participantID, "");
            allowExperiments = participantID != null && participantID != "";
            if (allowExperiments)
            {
                menu1.IsEnabled = true;
                menu2.IsEnabled = true;
            }
            else
            {
                menu1.IsEnabled = false;
                menu2.IsEnabled = false;
            }
        }
    }
}
