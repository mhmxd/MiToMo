using System;
using System.Windows;
using System.Windows.Input;

namespace SubTask.FunctionPointSelect
{
    /// <summary>
    /// Interaction logic for EndWindow.xaml
    /// </summary>
    public partial class EndWindow : Window
    {

        public EndWindow()
        {
            InitializeComponent();
            this.KeyDown += Window_KeyDown;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    Environment.Exit(0); // Prevents hanging during debugging
                }
                else
                {
                    Application.Current.Shutdown();
                }

                // Close the current window
                //this.Close();
            }
        }
    }
}
