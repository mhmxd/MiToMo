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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SubTask.FunctionSelection
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
