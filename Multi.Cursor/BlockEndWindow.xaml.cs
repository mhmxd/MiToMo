using Common.Helpers;
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

namespace Multi.Cursor
{
    /// <summary>
    /// Interaction logic for BlockEndWindow.xaml
    /// </summary>
    public partial class BlockEndWindow : Window
    {
        public Action<long> BlockFinishedCallback { get; set; }

        public BlockEndWindow(Action<long> blockFinishedCallback)
        {
            InitializeComponent();
            BlockFinishedText.Text = $"You can take a pause.";
            BlockFinishedCallback = blockFinishedCallback;
            this.KeyDown += BlockEndWindow_KeyDown;
        }

        private void BlockEndWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back && Keyboard.IsKeyDown(Key.LeftShift))
            {
                // Call the callback method if it's set
                BlockFinishedCallback?.Invoke(MTimer.GetCurrentMillis());

                // Close the current window
                this.Close();
            }
        }
    }
}
