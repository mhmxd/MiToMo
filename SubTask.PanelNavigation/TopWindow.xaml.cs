using Common.Constants;
using Common.Helpers;
using Common.Settings;
using CommonUI;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SubTask.PanelNavigation
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

        public TopWindow()
        {
            InitializeComponent();
            Side = ExpEnums.Side.Top;
            //this.DataContext = this; // Set DataContext for data binding

            EnableMouseInPointer(true);
            SetForegroundWindow(new WindowInteropHelper(this).Handle); // Bring this window to the foreground

            _gridNavigator = new GridNavigator(ExpEnvironment.FRAME_DUR_MS / 1000.0);

            //foreach (int wm in Experiment.BUTTON_MULTIPLES.Values)
            //{
            //    _widthButtons.TryAdd(wm, new List<SButton>());
            //}

        }

        public override async Task PlaceGrid(Func<Grid> gridCreator, double topPadding, double leftPadding)
        {
            //return tcs.Task; // Return the task to be awaited
            // 1. Setup
            _buttonWraps.Clear(); // Critical: Remove references to old buttons
            _widthButtons.Clear();
            canvas.Children.Clear();
            _buttonsGrid = gridCreator();

            // 2. Prepare the "Wait" task BEFORE adding to the canvas
            var loadedTask = OnLoadedAsync(_buttonsGrid);

            // 3. Add to UI (will set the position later)
            canvas.Children.Add(_buttonsGrid);
            this.TrialInfo($"n.Children: {canvas.Children.Count}");
            // 4. Wait for the UI to layout and render
            await loadedTask;

            // 5. Execution continues here once Loaded has fired
            this.PositionInfo($"Grid loaded with ActualWidth: {_buttonsGrid.ActualWidth}");

            double topPosition = (this.Height - _buttonsGrid.ActualHeight) / 2;
            Canvas.SetTop(_buttonsGrid, topPosition);

            double leftPosition = leftPadding;
            Canvas.SetLeft(_buttonsGrid, leftPosition);

            // Finish the process
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

        public void DeactivateGridNavigator()
        {
            _gridNavigator.Deactivate();
        }

        public override void ShowStartBtn(int btnW, int btnH, Brush btnColor, MouseEvents btnEvents)
        {
            base.ShowStartBtn(btnW, btnH, btnColor, btnEvents);

            // Show the start button at 10mm distance from the rightmost button of the grid
            if (_buttonsGrid != null)
            {
                double gridRight = Canvas.GetLeft(_buttonsGrid) + _buttonsGrid.ActualWidth;
                double startBtnLeft = gridRight + UITools.MM2PX(ExpLayouts.START_BUTTON_DIST_MM); // 10mm to the right of the grid

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

        public override int PositionStartButton(int btnSize, int prevDis)
        {
            // Set the Start button H
            _startButton.Width = btnSize;
            _startButton.Height = this.ActualHeight - UITools.MM2PX(ExpLayouts.WINDOW_PADDING_MM) * 2;

            // Show the start button at a different position than the previous trial
            if (_buttonsGrid != null)
            {
                double gridRight = Canvas.GetLeft(_buttonsGrid) + _buttonsGrid.ActualWidth;

                // 1. Get current mouse position relative to the canvas
                Point mousePos = Mouse.GetPosition(canvas);

                int minDist = UITools.MM2PX(ExpLayouts.START_BUTTON_DIST_MM);
                int maxDist = (int)(this.ActualWidth - gridRight - btnSize - UITools.MM2PX(ExpLayouts.WINDOW_PADDING_MM));

                // Contineously generate a random distance until this Start button has no overlap with previous one
                //int randDis;
                //do
                //{
                //    randDis = _random.Next(minDist, maxDist);
                //} while (Math.Abs(randDis - prevDis) < btnSize);

                // Safety check for random range
                if (maxDist <= minDist) maxDist = minDist + 1;

                int randDis;
                int attempts = 0;
                do
                {
                    randDis = _random.Next(minDist, maxDist);

                    double potentialLeft = gridRight + randDis;
                    double potentialRight = potentialLeft + _startButton.Width;

                    // Check A: Distance from previous trial's position
                    bool tooCloseToPrev = Math.Abs(randDis - prevDis) < _startButton.Width;

                    // Check B: Is the mouse currently inside the X-range of the new position?
                    // Adding a 5px buffer for safety
                    bool underMouse = mousePos.X >= (potentialLeft - 5) && mousePos.X <= (potentialRight + 5);

                    if (!tooCloseToPrev && !underMouse)
                        break;

                    attempts++;
                } while (attempts < 100);

                // Set the left position
                double startBtnLeft = gridRight + randDis;

                // Position the button
                Canvas.SetLeft(_startButton, startBtnLeft);
                double topPosition = (this.Height - _startButton.Height) / 2;
                Canvas.SetTop(_startButton, topPosition);

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
