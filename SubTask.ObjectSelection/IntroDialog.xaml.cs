using Common.Constants;
using Common.Settings;
using System;
using System.Threading.Tasks;
using System.Windows;
using static Common.Constants.ExpEnums;

namespace SubTask.ObjectSelection
{
    /// <summary>
    /// Interaction logic for IntroDialog.xaml
    /// </summary>
    public partial class IntroDialog : Window
    {
        //public int ParticipantNumber { get; private set; }
        public string Technique { get; private set; }
        public string SelectedExperiment { get; private set; }
        public string SelectedComplexity { get; private set; }

        private bool _isClosingFromButton = false; // Begin button was pressed to close, not x

        private bool _experimentSet = false;

        public IntroDialog()
        {
            InitializeComponent();

            ParticipantNumberTextBlock.Text = ExpEnvironment.PTC_NUM.ToString();
            ExperimentComboBox.ItemsSource = new string[] { ExpStrs.PRACTICE, ExpStrs.TEST };
            ExperimentComboBox.SelectedValue = ExpStrs.PRACTICE;
        }

        private async void BeginButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (_experimentSet)
            {
                _isClosingFromButton = true;
                DialogResult = true;

                Close();
            }
            else
            {
                if (Owner is MainWindow ownerWindow)
                {
                    SelectedExperiment = ExperimentComboBox.SelectedItem as string;
                    ExperimentType expType = (ExperimentType)Enum.Parse(typeof(ExperimentType), SelectedExperiment, true);

                    BigButton.Content = "Initializing...";
                    //BigButton.IsEnabled = false;

                    _experimentSet = await Task.Run(() => ownerWindow.SetExperiment(expType));

                    if (_experimentSet)
                    {
                        BigButton.Content = "Begin";
                        //BigButton.IsEnabled = true;
                    }
                    else
                    {
                        BigButton.Content = "Retry";
                    }

                    //Dispatcher.Invoke(() =>
                    //{
                    //    // Safe to update UI elements here, e.g.,
                    //    BigButton.Content = "Begin";
                    //    BigButton.IsEnabled = true;
                    //});
                }
            }



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
