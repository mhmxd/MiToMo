using System;
using System.Windows;
using System.Windows.Input;

namespace SubTask.PanelNavigation
{
    /// <summary>
    /// Interaction logic for BlockEndWindow.xaml
    /// </summary>
    public partial class BlockEndWindow : Window
    {
        public Action BlockFinishedCallback { get; set; }

        public BlockEndWindow(Action blockFinishedCallback)
        {
            InitializeComponent();
            BlockFinishedText.Text = $"Block is finished.\n When ready, press blue and red buttons on the keyboard.";
            BlockFinishedCallback = blockFinishedCallback;
            this.KeyDown += BlockEndWindow_KeyDown;
        }

        private void BlockEndWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back && Keyboard.IsKeyDown(Key.LeftShift))
            {
                // Call the callback method if it's set
                BlockFinishedCallback?.Invoke();

                // Close the current window
                this.Close();
            }
        }
    }
}
