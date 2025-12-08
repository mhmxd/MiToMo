using Common.Constants;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using static Common.Constants.ExpEnums;
using static Common.Helpers.ExpUtils;

namespace SubTask.FunctionSelection
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

        private int _gridRightX = 0;

        public TopWindow()
        {
            InitializeComponent();
            WindowSide = Side.Top;
            //this.DataContext = this; // Set DataContext for data binding

            EnableMouseInPointer(true);
            SetForegroundWindow(new WindowInteropHelper(this).Handle); // Bring this window to the foreground

            _gridNavigator = new GridNavigator(Config.FRAME_DUR_MS / 1000.0);

        }

        public override Task PlaceGrid(Func<Grid> gridCreator, double topPadding, double leftPadding)
        {
            // A TaskCompletionSource allows us to create a Task
            // that we can complete manually later.
            var tcs = new TaskCompletionSource<bool>();

            // Clear any existing columns from the canvas and the list before generating new ones
            canvas.Children.Clear();

            _buttonsGrid = gridCreator(); // Create the new column Grid

            // Set left position on the Canvas (from padding)
            Canvas.SetLeft(_buttonsGrid, leftPadding);

            // Add to the Canvas
            canvas.Children.Add(_buttonsGrid);
            //this.TrialInfo($"Grid added to canvas. Canvas size: {canvas.ActualWidth}x{canvas.ActualHeight}");

            //RegisterAllButtons(_buttonsGrid);
            //LinkButtonNeighbors();

            // Subscribe to the Loaded event to get the correct width.
            _buttonsGrid.Loaded += (sender, e) =>
            {
                try
                {
                    this.TrialInfo($"Grid loaded with ActualWidth: {_buttonsGrid.ActualWidth}, ActualHeight: {_buttonsGrid.ActualHeight}");
                    double topPosition = (this.Height - _buttonsGrid.ActualHeight) / 2;
                    _gridRightX = (int)(leftPadding + _buttonsGrid.ActualWidth);

                    Canvas.SetTop(_buttonsGrid, topPosition);

                    RegisterAllButtons(_buttonsGrid);
                    LinkButtonNeighbors();

                    FindMiddleButton();

                    // Indicate that the task is successfully completed.
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    // If any error occurs, set the exception on the TaskCompletionSource
                    tcs.SetException(ex);
                }
            };

            return tcs.Task; // Return the task to be awaited

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

        public override void ShowStart(MouseEvents mouseEvents)
        {
            base.ShowStart(mouseEvents);

            // Position the Start rectangle on the right side (based on the last button's position)
            int distanceFromEdgeMM = 20; // Distance from the right edge in mm
            double startX = _gridRightX + MM2PX(distanceFromEdgeMM); // Position Start area after the last button with some padding
            double startY = (this.Height - _startButton.Height) / 2; // Center vertically
            Canvas.SetLeft(_startButton, startX);
            Canvas.SetTop(_startButton, startY);

            // Add the rectangle to the Canvas
            this.canvas.Children.Add(_startButton);
        }

    }
}
