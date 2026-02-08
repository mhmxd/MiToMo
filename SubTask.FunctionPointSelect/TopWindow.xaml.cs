using Common.Helpers;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SubTask.FunctionPointSelect
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

        public TopWindow()
        {
            InitializeComponent();
            Side = Common.Constants.ExpEnums.Side.Top;
            //this.DataContext = this; // Set DataContext for data binding

        }

        public override Task PlaceGrid(Func<Grid> gridCreator, double topPadding, double leftPadding)
        {
            // A TaskCompletionSource allows us to create a Task
            // that we can complete manually later.
            var tcs = new TaskCompletionSource<bool>();

            // Clear any existing columns from the canvas and the list before generating new ones
            canvas.Children.Clear();
            _buttonWraps.Clear(); // Ensure the dictionary is empty before Block 2 starts
            _widthButtons.Clear();

            _buttonsGrid = gridCreator(); // Create the new column Grid

            // Set left position on the Canvas (from padding)
            Canvas.SetLeft(_buttonsGrid, leftPadding);

            // Add to the Canvas
            canvas.Children.Add(_buttonsGrid);
            //this.TrialInfo($"Grid added to canvas. Canvas size: {canvas.ActualWidth}x{canvas.ActualHeight}");

            // Use a local reference to capture the specific grid instance for this task
            Grid currentGrid = _buttonsGrid;

            currentGrid.Loaded += (sender, e) =>
            {
                try
                {
                    // Use 'sender' to be 100% sure we are talking to the grid that just loaded
                    Grid loadedGrid = sender as Grid;

                    double topPosition = (this.Height - loadedGrid.ActualHeight) / 2;
                    Canvas.SetTop(loadedGrid, topPosition);

                    // Register only the grid that was just successfully placed in the canvas
                    RegisterAllButtons(loadedGrid);

                    LinkButtonNeighbors();
                    FindMiddleButton();

                    tcs.SetResult(true);
                }
                catch (Exception ex) { tcs.SetException(ex); }
            };

            // Subscribe to the Loaded event to get the correct width.
            //_buttonsGrid.Loaded += (sender, e) =>
            //{
            //    try
            //    {
            //        this.PositionInfo($"Grid loaded with ActualWidth: {_buttonsGrid.ActualWidth}, ActualHeight: {_buttonsGrid.ActualHeight}");
            //        double topPosition = (this.Height - _buttonsGrid.ActualHeight) / 2;
            //        Canvas.SetTop(_buttonsGrid, topPosition);

            //        RegisterAllButtons(_buttonsGrid);
            //        LinkButtonNeighbors();

            //        FindMiddleButton();

            //        // Indicate that the task is successfully completed.
            //        tcs.SetResult(true);
            //    }
            //    catch (Exception ex)
            //    {
            //        // If any error occurs, set the exception on the TaskCompletionSource
            //        tcs.SetException(ex);
            //    }
            //};

            return tcs.Task; // Return the task to be awaited

        }

    }
}
