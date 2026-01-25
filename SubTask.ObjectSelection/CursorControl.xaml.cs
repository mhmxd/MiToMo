using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace SubTask.ObjectSelection
{
    /// <summary>
    /// Interaction logic for CursorControl.xaml
    /// </summary>
    public partial class CursorControl : UserControl
    {
        public CursorControl()
        {
            InitializeComponent();
        }

        private void OnMouseMove(object sender, MouseEventArgs args)
        {
            var cursorPoint = args.GetPosition(this);
            Console.WriteLine($"Cursor Position: {cursorPoint}");
        }
    }
}
