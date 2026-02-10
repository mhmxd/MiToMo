using Common.Constants;
using Common.Helpers;
using Common.Settings;
using CommonUI;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using static Common.Constants.ExpEnums;


namespace SubTask.PanelNavigation
{
    /// <summary>
    /// Interaction logic for SideWindow.xaml
    /// </summary>
    public partial class SideWindow : AuxWindow
    {
        public string WindowTitle { get; set; }

        private Random _random = new Random();

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("User32.dll")]
        private static extern void EnableMouseInPointer(bool fEnable);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private Point _relPos;
        public Point Rel_Pos
        {
            get { return _relPos; }
            set { _relPos = value; }
        }

        private GridNavigator _gridNavigator;


        public SideWindow(Side side, Point relPos)
        {
            InitializeComponent();
            //WindowTitle = title;
            Side = side;
            this.DataContext = this; // Set DataContext for data binding

            EnableMouseInPointer(true);
            SetForegroundWindow(new WindowInteropHelper(this).Handle);

            _relPos = relPos;

            _gridNavigator = new GridNavigator(ExpEnvironment.FRAME_DUR_MS / 1000.0);


            this.Loaded += SideWindow_Loaded; // Add this line
        }

        private void SideWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        public void ActivateGridNavigator()
        {
            _gridNavigator.Activate();
        }


        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //Console.WriteLine("MouseDown event triggered.");
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {

        }

        private void Window_MouseUp(object sender, MouseEventArgs e)
        {

        }

        public override async Task PlaceGrid(Func<Grid> gridCreator, double topPadding, double leftPadding)
        {
            // A TaskCompletionSource allows us to create a Task
            // that we can complete manually later.
            //var tcs = new TaskCompletionSource<bool>();

            //// Clear any existing columns from the canvas and the list before generating new ones
            //canvas.Children.Clear();

            //_buttonsGrid = gridCreator(); // Create the new column Grid

            //// Subscribe to the Loaded event to get the correct width.
            //_buttonsGrid.Loaded += (sender, e) =>
            //{
            //    this.TrialInfo("Grid Loaded.");
            //    try
            //    {
            //        this.TrialInfo($"Grid loaded with ActualWidth: {_buttonsGrid.ActualWidth}, ActualHeight: {_buttonsGrid.ActualHeight}");
            //        // Now ActualWidth has a valid value.
            //        double leftPosition = (this.Width - _buttonsGrid.ActualWidth) / 2;
            //        Canvas.SetLeft(_buttonsGrid, leftPosition);

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

            //// Set top position on the Canvas (from padding)
            //Canvas.SetTop(_buttonsGrid, topPadding);

            //// Add to the Canvas
            //canvas.Children.Add(_buttonsGrid);
            //this.TrialInfo($"Grid added to canvas.");

            //return tcs.Task; // Return the Task to be awaited

            // 1. Setup
            _buttonWraps.Clear(); // Critical: Remove references to old buttons
            _widthButtons.Clear();
            canvas.Children.Clear();
            _buttonsGrid = gridCreator();

            // 2. Prepare the "Wait" task BEFORE adding to the canvas
            var loadedTask = OnLoadedAsync(_buttonsGrid);

            // 3. Add to UI
            Canvas.SetTop(_buttonsGrid, topPadding);
            canvas.Children.Add(_buttonsGrid);
            this.TrialInfo($"n.Children: {canvas.Children.Count}");
            // 4. Wait for the UI to layout and render
            await loadedTask;

            // 5. Execution continues here once Loaded has fired
            double leftPosition = (this.Width - _buttonsGrid.ActualWidth) / 2;
            Canvas.SetLeft(_buttonsGrid, leftPosition);

            RegisterAllButtons(_buttonsGrid);
            LinkButtonNeighbors();
            FindMiddleButton();

        }

        // Helper method to wrap the Loaded event in a Task
        private Task OnLoadedAsync(FrameworkElement element)
        {
            var tcs = new TaskCompletionSource<bool>();

            // If it's already loaded, complete immediately
            if (element.IsLoaded) return Task.FromResult(true);

            element.Loaded += (s, e) => tcs.TrySetResult(true);

            return tcs.Task;
        }

        public override void ShowStartBtn(int btnW, int btnH, Brush btnColor, MouseEvents btnEvents)
        {
            base.ShowStartBtn(btnW, btnH, btnColor, btnEvents);

            // Position the start button at 10 mm below the bottom button of the grid
            if (_buttonsGrid != null)
            {
                double gridBottom = Canvas.GetTop(_buttonsGrid) + _buttonsGrid.ActualHeight;
                double startBtnTop = gridBottom + UITools.MM2PX(ExpLayouts.START_BUTTON_DIST_MM); // 10mm below the grid

                // Position the button
                double leftPosition = (this.Width - btnW) / 2;
                Canvas.SetLeft(_startButton, leftPosition);
                Canvas.SetTop(_startButton, startBtnTop);
                // Add to canvas
                canvas.Children.Add(_startButton);
            }
            else
            {
                this.TrialInfo("Buttons grid is not initialized, cannot position Start button.");
            }
        }

        public override int PositionStartButton(int btnSize, int prevDist)
        {
            // Set the Start button H
            _startButton.Height = btnSize;
            _startButton.Width = this.ActualWidth - 2 * UITools.MM2PX(ExpLayouts.WINDOW_PADDING_MM);

            // Position the start button at 10 mm below the bottom button of the grid
            if (_buttonsGrid != null)
            {
                double gridBottom = Canvas.GetTop(_buttonsGrid) + _buttonsGrid.ActualHeight;

                int minDist = UITools.MM2PX(ExpLayouts.START_BUTTON_DIST_MM);
                int maxDist = (int)(this.ActualHeight - gridBottom - btnSize - UITools.MM2PX(ExpLayouts.WINDOW_PADDING_MM));

                // Contineously generate a random distance until this Start button has no overlap with previous one
                int randDist;
                do
                {
                    randDist = _random.Next(minDist, maxDist);
                } while (Math.Abs(randDist - prevDist) < btnSize);

                // Set the top position
                double startBtnTop = gridBottom + randDist; // Dist below the grid

                // Position the button
                double leftPosition = (this.Width - _startButton.Width) / 2;
                Canvas.SetLeft(_startButton, leftPosition);
                Canvas.SetTop(_startButton, startBtnTop);

                // Add to canvas
                canvas.Children.Add(_startButton);
            }
            else
            {
                this.TrialInfo("Buttons grid is not initialized, cannot position Start button.");
            }

            return 0;
        }

        public override void RemoveStartBtn()
        {
            canvas.Children.Remove(_startButton);
        }



    }
}
