using Common.Settings;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using static Common.Constants.ExpEnums;

namespace SubTask.ObjectSelection
{
    /// <summary>
    /// Interaction logic for TopWindow.xaml
    /// </summary>
    public partial class TopWindow : AuxWindow
    {

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("User32.dll")]
        private static extern void EnableMouseInPointer(bool fEnable);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private Random _random = new Random();

        //private GridNavigator _gridNavigator;
        //private List<Grid> _gridColumns = new List<Grid>(); // List of grid columns
        //private static Dictionary<int, List<SButton>> _widthButtons = new Dictionary<int, List<SButton>>(); // Dictionary to hold buttons by their width multiples
        //private SButton _targetButton; // Currently selected button (if any)

        // Boundary of the grid (encompassing all buttons)
        //double _gridMinX = double.MaxValue;
        //double _gridMinY = double.MaxValue;
        //double _gridMaxX = double.MinValue;
        //double _gridMaxY = double.MinValue;

        public TopWindow()
        {
            InitializeComponent();
            Side = Side.Top;
            //this.DataContext = this; // Set DataContext for data binding

            EnableMouseInPointer(true);
            SetForegroundWindow(new WindowInteropHelper(this).Handle); // Bring this window to the foreground

            _gridNavigator = new GridNavigator(ExpEnvironment.FRAME_DUR_MS / 1000.0);

            //foreach (int wm in Experiment.BUTTON_MULTIPLES.Values)
            //{
            //    _widthButtons.TryAdd(wm, new List<SButton>());
            //}

        }

        public override void ShowPoint(Point p)
        {
            // Create a small circle to represent the point
            Ellipse pointCircle = new Ellipse
            {
                Width = 5, // Diameter of the circle
                Height = 5,
                Fill = Brushes.Red // Color of the circle
            };
            // Position the circle on the canvas
            Canvas.SetLeft(pointCircle, p.X - pointCircle.Width / 2);
            Canvas.SetTop(pointCircle, p.Y - pointCircle.Height / 2);
            // Add the circle to the canvas
            if (this.canvas != null)
            {
                this.canvas.Children.Add(pointCircle);
            }
            else
            {
                this.TrialInfo("Canvas is not initialized, cannot show point.");
            }
        }

    }
}
