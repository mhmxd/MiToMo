using System;
using System.Windows;
using System.Windows.Input;

namespace SubTask.FunctionSelection
{
    /// <summary>
    /// Interaction logic for PausePopUp.xaml
    /// </summary>
    public partial class PausePopUp : Window
    {
        private Action _closeAction;

        public PausePopUp(Action closeAction)
        {
            InitializeComponent();
            _closeAction = closeAction;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //this.DialogResult = true;
            _closeAction();
            this.Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                //this.DialogResult = true;
                _closeAction();
                this.Close();
            }
        }
    }
}
