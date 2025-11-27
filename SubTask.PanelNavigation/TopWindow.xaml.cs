using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using static SubTask.PanelNavigation.Output;
using SysWin = System.Windows;

namespace SubTask.PanelNavigation
{
    /// <summary>
    /// Interaction logic for TopWindow.xaml
    /// </summary>
    public partial class TopWindow : AuxWindow
    {
        private double HORIZONTAL_PADDING = Utils.MM2PX(Config.WINDOW_PADDING_MM);
        private double InterGroupGutter = Utils.MM2PX(Config.GUTTER_05MM);

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

            _gridNavigator = new GridNavigator(Config.FRAME_DUR_MS / 1000.0);

            //foreach (int wm in Experiment.BUTTON_MULTIPLES.Values)
            //{
            //    _widthButtons.TryAdd(wm, new List<SButton>());
            //}

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

            //this.TrialInfo($"Grid loaded with ActualWidth: {_buttonsGrid.ActualWidth}, ActualHeight: {_buttonsGrid.ActualHeight}");
            // Now ActualWidth has a valid value.
            //double topPosition = (this.Height - _buttonsGrid.ActualHeight) / 2;
            //Canvas.SetTop(_buttonsGrid, topPosition);

            //RegisterAllButtons(_buttonsGrid);
            //LinkButtonNeighbors();

            // Subscribe to the Loaded event to get the correct width.
            _buttonsGrid.Loaded += (sender, e) =>
            {
                try
                {
                    this.TrialInfo($"Grid loaded with ActualWidth: {_buttonsGrid.ActualWidth}, ActualHeight: {_buttonsGrid.ActualHeight}");
                    double topPosition = (this.Height - _buttonsGrid.ActualHeight) / 2;
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

        public void DeactivateGridNavigator()
        {
            _gridNavigator.Deactivate();
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

        public override void ShowStartBtn(int btnW, int btnH, Brush btnColor, MouseEvents btnEvents)
        {
            base.ShowStartBtn(btnW, btnH, btnColor, btnEvents);

            // Show the start button at 10mm distance from the rightmost button of the grid
            if (_buttonsGrid != null)
            {
                double gridRight = Canvas.GetLeft(_buttonsGrid) + _buttonsGrid.ActualWidth;
                double startBtnLeft = gridRight + Utils.MM2PX(Config.START_BTN_DIST_MM); // 10mm to the right of the grid

                // Position the button
                Canvas.SetLeft(_startButton, startBtnLeft);
                double topPosition = (this.Height - btnH) / 2;
                Canvas.SetTop(_startButton, topPosition);

                // Add to canvas
                canvas.Children.Add(_startButton);
            }
            else
            {
                this.TrialInfo("Buttons grid is not initialized, cannot position Start button.");
            }

        }

        public override void RemoveStartBtn()
        {
            canvas.Children.Remove(_startButton);
        }

    }
}
