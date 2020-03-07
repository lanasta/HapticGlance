using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// <summary>
    /// Interaction logic for taskWindow.xaml
    /// </summary>
    public partial class taskWindow : Window
    {
        DirectionDiscrimination directionDiscriminationTest = new DirectionDiscrimination();

        public taskWindow()
        {
            InitializeComponent();
        }

        private void HapticMarkCountButton(object sender, RoutedEventArgs e)
        {

        }

        private void DirectionDiscriminationButton(object sender, RoutedEventArgs e)
        {
            directionDiscriminationTest = new DirectionDiscrimination();
            directionDiscriminationTest.Show();
            MainWindow.resp.SetBG(); //recalibrates each time an experiment starts
        }

        private void FeedbackPreferenceButton(object sender, RoutedEventArgs e)
        {

        }
    }
}
