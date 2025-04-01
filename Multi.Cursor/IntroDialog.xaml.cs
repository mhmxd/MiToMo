using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Multi.Cursor
{
    /// <summary>
    /// Interaction logic for IntroDialog.xaml
    /// </summary>
    public partial class IntroDialog : Window
    {
        public int ParticipantNumber { get; private set; }
        public string Technique { get; private set; }
        public string SelectedExperiment { get; private set; }

        private bool _isClosingFromButton = false; // Begin button was pressed to close, not x

        public IntroDialog()
        {
            InitializeComponent();

            ParticipantNumberTextBox.Text = "100";
            TechniqueComboBox.ItemsSource = new string[] { Str.TOUCH_MOUSE, Str.MOUSE };
            TechniqueComboBox.SelectedValue = Str.MOUSE;
            ExperimentComboBox.ItemsSource = new string[] { Str.PRACTICE, Str.TEST };
            ExperimentComboBox.SelectedValue = Str.PRACTICE;
        }

        private void BeginButton_Click(object sender, RoutedEventArgs e)
        {
            _isClosingFromButton = true;

            ParticipantNumber = int.Parse(ParticipantNumberTextBox.Text);
            Technique = TechniqueComboBox.SelectedItem as string;
            SelectedExperiment = ExperimentComboBox.SelectedItem as string;
            DialogResult = true;
            
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isClosingFromButton)
            {
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    Environment.Exit(0); // Prevents hanging during debugging
                }
                else
                {
                    Application.Current.Shutdown();
                }
            }
            

        }
    }
}
