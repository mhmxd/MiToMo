using Common.Constants;
using Common.Settings;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Common.Constants.ExpEnums;

namespace SubTask.PanelNavigation
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

            ParticipantNumberTextBlock.Text = ExpPtc.PTC_NUM.ToString();
            TechniqueComboBox.ItemsSource = new string[] { ExpStrs.TOUCH_MOUSE_TAP, ExpStrs.TOUCH_MOUSE_SWIPE, ExpStrs.MOUSE };
            TechniqueComboBox.SelectedValue = ExpStrs.MOUSE;
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
                    //ParticipantNumber = int.Parse(ParticipantNumberTextBox.Text);
                    Technique = TechniqueComboBox.SelectedItem as string;
                    SelectedExperiment = ExperimentComboBox.SelectedItem as string;
                    SelectedComplexity = (ComplexityComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                    Complexity complexity = (Complexity)Enum.Parse(typeof(Complexity), SelectedComplexity, true);

                    //_experimentSet = true;

                    BigButton.Content = "Initializing...";
                    //BigButton.IsEnabled = false;

                    _experimentSet = await Task.Run(() => ownerWindow.SetExperiment(Technique.ToString(), complexity));

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
