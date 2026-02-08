/********************************************************
*                                                       *
*   Copyright (C) Microsoft. All rights reserved.       *
*                                                       *
********************************************************/

using Common.Constants;
using Common.Helpers;
using Common.Settings;
using CommonUI;
using Microsoft.Research.TouchMouseSensor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using WindowsInput;
using static Common.Constants.ExpEnums;
using static SubTask.FunctionSelection.Experiment;
using MessageBox = System.Windows.Forms.MessageBox;
using SysIput = System.Windows.Input;
using SysWin = System.Windows;


namespace SubTask.FunctionSelection
{

    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        /// <summary>
        /// Confines the cursor to a rectangular area on the screen.
        /// </summary>
        /// <param name="lpRect">A pointer to the structure that contains the screen coordinates of the confining rectangle. 
        /// If this parameter is NULL (IntPtr.Zero), the cursor is free to move anywhere on the screen.</param>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ClipCursor(ref RECT lpRect);

        /// <summary>
        /// Call with IntPtr.Zero to release the cursor clip.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ClipCursor(IntPtr lpRect); // Overload for releasing

        // Constants
        private double INFO_LABEL_BOTTOM_RATIO = 0.02; // of the height from the bottom

        private int VERTICAL_PADDING = UITools.MM2PX(ExpLayouts.WINDOW_PADDING_MM); // Padding for the windows
        private int HORIZONTAL_PADDING = UITools.MM2PX(ExpLayouts.WINDOW_PADDING_MM); // Padding for the windows

        private int TopWindowHeight = UITools.MM2PX(ExpLayouts.TOP_WINDOW_HEIGTH_MM);
        private int SideWindowWidth = UITools.MM2PX(ExpLayouts.SIDE_WINDOW_WIDTH_MM);


        //------------------------------------------------------------------------------

        //private (double min, double max) distThresh;

        private BackgroundWindow _backgroundWindow;
        private AuxWindow _topWindow;
        private AuxWindow _leftWindow;
        private AuxWindow _rightWindow;
        private AuxWindow _activeAuxWindow;

        private double _monitorHeightMM;

        private int _absLeft, _absRight, _absTop, _absBottom;
        private double thisLeft, thisTop, thisRight, thisBottom; // Absolute positions of the main window (set to not call this.)

        private TouchMouseCallback touchMouseCallback;

        byte[,] gestureShot;
        private List<(int row, int col, byte val, long time)> thumbFrames;
        private Dictionary<long, List<(double x, double y)>> seqFrames;

        //private AuxPointer _leftAuxPointer = new AuxPointer();
        //private AuxPointer _rightAuxPointer = new AuxPointer();
        //private AuxPointer _topAuxPointer = new AuxPointer();

        private Point prevEMA = new Point(-1, -1);

        private Window activeWindow;
        private double activeWidthRatio, activeHeightRatio;

        private InputSimulator inputSimulator = new InputSimulator();

        private int lastX, lastY;
        private Point lastPos;

        private Stopwatch _stopWatch = new Stopwatch();
        //private Stopwatch leftWatch = new Stopwatch();
        //private Stopwatch topWatch = new Stopwatch();
        //private Stopwatch rightWatch = new Stopwatch();

        private Stopwatch framesWatch;

        private int leftSkipTPs = 0;

        private Point prevPoint = new Point(-1, -1);

        private bool headerWritten;

        private Point mainCursorPrevPosition = new Point(-1, -1);
        private double cursorTravelDist;
        private DispatcherTimer cursorMoveTimer;
        private bool mainCursorActive;

        private Rect _mainWinRect, _leftWinRect, _topWinRect, _rightWinRect;
        private Rect _lefWinRectPadded, _topWinRectPadded, _rightWinRectPadded;
        private int _infoLabelHeight;


        //-- Experiment
        private int _ptc = 13;
        private Experiment _experiment;
        private Block _block;
        private Trial _trial;
        private int _activeBlockNum, _activeTrialNum;
        private Stopwatch _trialtWatch = new Stopwatch();
        private Dictionary<string, long> _timestamps = new Dictionary<string, long>();
        private Dictionary<int, Point> _trialsTargetCenters = new Dictionary<int, Point>(); // Trial id to target center mapping
        private Dictionary<int, Point> _trialStartPosition = new Dictionary<int, Point>(); // Trial id to start center mapping
        private Dictionary<int, Dictionary<int, Point>> _repTrialStartPositions = new Dictionary<int, Dictionary<int, Point>>(); // Trial id: (dist: Start position)
        private Dictionary<int, int> _trialTargetIds = new Dictionary<int, int>(); // Trial id to target id (in target window)
        //private Ellipse _startCircle;
        private Rectangle _startRectangle;
        private AuxWindow _targetWindow;
        private int _auxursorSpeed = 0; // 0: normal, 1: fast (for Swipe)
        private BlockHandler _activeBlockHandler;
        private Rect _objectConstraintRectAbsolue;
        private List<BlockHandler> _blockHandlers = new List<BlockHandler>();
        private Border _startButton;
        private Rectangle _objectArea;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize windows
            InitializeWindows();

            // Set object constraint rect here and in aux windows
            _objectConstraintRectAbsolue = new Rect(
                _mainWinRect.Left + VERTICAL_PADDING + GetStartHalfWidth(),
                _mainWinRect.Top + VERTICAL_PADDING + GetStartHalfWidth(),
                _mainWinRect.Width - 2 * VERTICAL_PADDING,
                _mainWinRect.Height - 2 * VERTICAL_PADDING - _infoLabelHeight
             );

            _topWindow.SetObjectConstraintRect(_objectConstraintRectAbsolue);
            _leftWindow.SetObjectConstraintRect(_objectConstraintRectAbsolue);
            _rightWindow.SetObjectConstraintRect(_objectConstraintRectAbsolue);

            UpdateLabelPosition();

            //-- Events
            this.MouseMove += Window_MouseMove;
            this.MouseDown += Window_MouseDown;
            this.MouseUp += Window_MouseUp;
            this.MouseWheel += Window_MouseWheel;
            this.KeyDown += Window_KeyDown;

            //_leftWindow.MouseMove += Window_MouseMove;
            //_rightWindow.MouseMove += Window_MouseMove;
            //_topWindow.MouseMove += Window_MouseMove;

            MouseLeftButtonDown += Window_MouseLeftButtonDown;

            CreateExperiment(); // Create the experiment (sets _experiment)

            //--- Show the intro dialog (the choices affect the rest)
            IntroDialog introDialog = new IntroDialog() { Owner = this };
            bool result = false;
            try
            {
                result = (bool)introDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening dialog: {ex.Message}");
            }


            // Set the info from the dialog
            if (result == true)
            {
                // Set the _technique mode in Config
                //_experiment.Init(introDialog.ParticipantNumber, introDialog.Technique);


                BeginExperiment();
            }

        }

        private void CreateExperiment()
        {
            double padding = UITools.MM2PX(ExpLayouts.WINDOW_PADDING_MM);
            double objHalfWidth = UITools.MM2PX(ExpLayouts.OBJ_WIDTH_MM) / 2;
            double smallButtonHalfWidthMM = ExpSizes.BUTTON_MULTIPLES[ExpStrs.x6] / 2;
            double startHalfWidth = ExpLayouts.START_BUTTON_LARGE_SIDE_MM / 2;
            double smallButtonHalfWidth = UITools.MM2PX(smallButtonHalfWidthMM);
            //double objAreaRadius = UITools.MM2PX(Experiment.REP_TRIAL_OBJ_AREA_RADIUS_MM);
            double objAreaHalfWidth = UITools.MM2PX(ExpLayouts.OBJ_AREA_WIDTH_MM / 2);

            // Distances (v.3)
            // Longest
            Point leftMostSmallButtonCenterPosition = new Point(
                padding + smallButtonHalfWidth,
                padding + smallButtonHalfWidth
             );
            Point leftMostSmallButtonCenterAbsolute = UITools.OffsetPosition(
                leftMostSmallButtonCenterPosition,
                _leftWindow.Left, _leftWindow.Top);

            Point rightMostObjAreaCenterAbsolute = new Point(
                _mainWinRect.Right - padding - objAreaHalfWidth,
                _mainWinRect.Top + padding - objAreaHalfWidth
            );
            //Point rightMostObjAreaCenterAbsolute = UITools.OffsetPosition(
            //    rightMostObjAreaCenterPosition, 
            //    this.Left, this.Top);

            double longestDistMM = UITools.DistInMM(leftMostSmallButtonCenterAbsolute, rightMostObjAreaCenterAbsolute);
            this.TrialInfo($"Main Rect: {_mainWinRect.ToString()}");
            //ShowPoint(rightMostObjAreaCenterPosition);
            //_leftWindow.ShowPoint(leftMostSmallButtonCenterPosition);
            //double longestDistMM =
            //    (ExpLayouts.SIDE_WINDOW_WIDTH_MM - ExpLayouts.WINDOW_PADDING_MM - smallButtonHalfWidthMM) +
            //    Utils.PX2MM(this.ActualWidth) - ExpLayouts.WINDOW_PADDING_MM - startHalfWidth;


            // Shortest
            Point topLeftButtonCenterPosition = new Point(
                padding + smallButtonHalfWidth,
                padding + smallButtonHalfWidth
            );
            Point topLeftButtonCenterAbsolute = UITools.OffsetPosition(topLeftButtonCenterPosition, _topWindow.Left, _topWindow.Top);

            Point topLeftObjAreaCenterPosition = new Point(
                padding + objAreaHalfWidth,
                padding + objAreaHalfWidth
            );
            Point topLeftObjAreaCenterAbsolute = UITools.OffsetPosition(topLeftObjAreaCenterPosition, this.Left, this.Top);

            double shortestDistMM = UITools.DistInMM(topLeftButtonCenterAbsolute, topLeftObjAreaCenterAbsolute);

            _experiment = new Experiment(shortestDistMM, longestDistMM);
        }

        private async void BeginExperiment()
        {
            // Set the layout (incl. placing the grid and finding positions)
            await SetupLayout(_experiment.Active_Complexity);

            // Begin the _technique
            BeginBlocksAsync();
        }

        private void UpdateLabelPosition()
        {
            if (canvas != null && infoLabel != null)
            {
                // 1/10th of the height from the bottom
                Canvas.SetBottom(infoLabel, canvas.ActualHeight * INFO_LABEL_BOTTOM_RATIO);

                // Center horizontally
                Canvas.SetLeft(infoLabel, (canvas.ActualWidth - infoLabel.ActualWidth) / 2);

                if (!canvas.Children.Contains(infoLabel)) canvas.Children.Add(infoLabel);
            }
        }

        private void InfoLabel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateLabelPosition(); // Reposition when the label's size changes (due to text update)
        }

        private void Window_KeyDown(object sender, SysIput.KeyEventArgs e)
        {
            // TEMP: for logging purposes
            if (Keyboard.IsKeyDown(Key.Space))
            {

            }

            // Exit on Shift + F5
            if (Keyboard.IsKeyDown(Key.LeftShift) && e.Key == Key.F5 ||
            Keyboard.IsKeyDown(Key.RightShift) && e.Key == Key.F5)
            {
                if (Debugger.IsAttached)
                {
                    Environment.Exit(0); // Prevents hanging during debugging
                }
                else
                {
                    SysWin.Application.Current.Shutdown();
                }
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Get all screens (monitors)
            var screens = Screen.AllScreens;

            // Set up on the second monitor (if exists)
            if (screens.Length > 1)
            {
                // Get the second monitor
                var secondScreen = screens[1];

                // Set the window position to the second monitor's working area
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Left = secondScreen.WorkingArea.Left + SideWindowWidth;
                this.Top = secondScreen.WorkingArea.Top + TopWindowHeight;
            }
        }

        private void InitializeWindows()
        {
            // Get the border width and title bar height to account for window borders
            int borderWidth = (int)(this.Width - this.ActualWidth);
            int titleBarHeight = (int)(this.Height - this.ActualHeight);

            // Get all screens (monitors)
            var screens = Screen.AllScreens;

            // Set up on the second monitor (if exists)
            if (screens.Length > 1)
            {
                // Get the second monitor
                //var secondScreen = screens[1];
                var secondScreen = screens[1];

                //-- Background window
                _backgroundWindow = new BackgroundWindow
                {
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = secondScreen.WorkingArea.Left,
                    Top = secondScreen.WorkingArea.Top,
                    Width = secondScreen.WorkingArea.Width,
                    Height = secondScreen.WorkingArea.Height,
                    WindowState = WindowState.Normal, // Start as normal to set position
                };

                // Show and then maximize
                _backgroundWindow.Show();
                _backgroundWindow.WindowState = WindowState.Maximized;

                // Set the height as mm
                //_monitorHeightMM = Utils.PX2MM(secondScreen.WorkingArea.Height);
                _monitorHeightMM = 335;

                //---

                // Set the window position to the second monitor's working area
                this.Background = UIColors.GRAY_E6E6E6;
                this.Width = _backgroundWindow.Width - (2 * SideWindowWidth);
                this.Height = _backgroundWindow.Height - TopWindowHeight;
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Left = secondScreen.WorkingArea.Left + SideWindowWidth;
                thisLeft = this.Left; // Save the left position 
                this.Top = secondScreen.WorkingArea.Top + TopWindowHeight;
                thisTop = this.Top; // Save the top position
                this.Owner = _backgroundWindow;
                //this.Topmost = true;
                this.Show();
                this._mainWinRect = UITools.GetRect(this);
                _infoLabelHeight = (int)(this.ActualHeight * INFO_LABEL_BOTTOM_RATIO + infoLabel.ActualHeight);

                // Create top window
                _topWindow = new TopWindow();
                _topWindow.Background = UIColors.GRAY_F3F3F3;
                _topWindow.Height = TopWindowHeight;
                _topWindow.Width = secondScreen.WorkingArea.Width;
                _topWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                _topWindow.Left = secondScreen.WorkingArea.Left;
                _topWindow.Top = secondScreen.WorkingArea.Top;
                _topWindow.Show();
                _topWinRect = UITools.GetRect(_topWindow);
                _topWinRectPadded = UITools.GetRect(_topWindow, VERTICAL_PADDING);
                _topWindow.Owner = this;

                // Create left window
                _leftWindow = new SideWindow(Side.Left, new Point(0, SideWindowWidth));
                _leftWindow.Background = UIColors.GRAY_F3F3F3;
                _leftWindow.Width = SideWindowWidth;
                _leftWindow.Height = this.Height;
                _leftWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                _leftWindow.Left = secondScreen.WorkingArea.Left;
                _leftWindow.Top = this.Top;
                _leftWindow.Show();
                _leftWinRect = UITools.GetRect(_leftWindow);
                _lefWinRectPadded = UITools.GetRect(_leftWindow, VERTICAL_PADDING);
                _leftWindow.Owner = this;

                //leftWinWidthRatio = leftWindow.Width / TOMOPAD_SIDE_SIZE;
                //leftWinHeightRatio = leftWindow.Height / (TOMOPAD_LAST_ROW - TOMOPAD_SIDE_SIZE);

                // Create right window
                _rightWindow = new SideWindow(Side.Right, new Point(SideWindowWidth + this.Width, SideWindowWidth));
                _rightWindow.Background = UIColors.GRAY_F3F3F3;
                _rightWindow.Width = SideWindowWidth;
                _rightWindow.Height = this.Height;
                _rightWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                _rightWindow.Left = this.Left + this.Width;
                _rightWindow.Top = this.Top;
                _rightWindow.Show();
                _rightWinRect = UITools.GetRect(_rightWindow);
                _rightWinRectPadded = UITools.GetRect(_rightWindow, VERTICAL_PADDING);
                _rightWindow.Owner = this;

                // Get the absolute position of the window
                _absLeft = SideWindowWidth;
                _absTop = TopWindowHeight;
                _absRight = _absLeft + (int)this.Width;
                _absBottom = _absTop + (int)Height;

            }


            // Set this as the active window
            activeWindow = this;

            // Synchronize window movement with main window
            this.LocationChanged += MainWindow_LocationChanged;
        }

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            //AdjustWindowPositions();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _activeBlockHandler.OnMainWindowMouseDown(sender, e);
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _activeBlockHandler.OnMainWindowMouseMove(sender, e);

        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {

            _activeBlockHandler.OnMainWindowMouseUp(sender, e);

        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //int delta = e.Delta;
            //Point mousePosition = e.GetPosition(this); // Get position relative to the window

            //if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            //{
            //    // Interpret as horizontal scroll
            //    _overlayWindow.MovePlus(delta / 100, delta / 100);
            //}
            //else
            //{
            //    // Interpret as vertical scroll
            //    _overlayWindow.RotateBeam(0, delta / 800.0);
            //}
        }

        public bool SetExperiment(ExperimentType expType)
        {
            // Make the experiment (incl. creating blocks)
            _experiment.Init(expType);

            return true;
        }

        private async Task BeginBlocksAsync()
        {
            _activeBlockNum = 1;

            if (_blockHandlers.Count > 0)
            {
                _activeBlockHandler = _blockHandlers[_activeBlockNum - 1];

                ExperiLogger.Init();

                _stopWatch.Start();

                // Show layout before starting the block
                await SetGrids(_activeBlockHandler.GetComplexity());

                // Begin the block
                _activeBlockHandler.BeginActiveBlock();
            }
            else
            {
                // Show message box with an error
                MessageBox.Show("No block handlers found. Cannot begin experiment.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void GoToNextBlock()
        {
            if (_activeBlockNum < _experiment.GetNumBlocks()) // More blocks to show
            {
                void continueAction()
                {
                    _activeBlockNum++;
                    _activeBlockHandler = _blockHandlers[_activeBlockNum - 1];

                    _activeBlockHandler.BeginActiveBlock();
                }

                this.TrialInfo($"Block finished. More to show...");
                // If need to show a break
                if (_activeBlockNum == ExpDesign.MutliFuncSelectBreakAfterBlocks)
                {
                    this.TrialInfo($"Showing break");
                    ResetAllAuxWindows();
                    ClearCanvas();

                    PausePopUp pausePopUp = new(continueAction)
                    {
                        Owner = this
                    };

                    pausePopUp.Show();
                }
                else
                {
                    continueAction();
                }

            }
            else // All blocks finished
            {

                // Show the full screen message...
                EndWindow endWindow = new()
                {
                    Owner = this
                };
                endWindow.Show();
                //MessageBoxResult dialogResult = SysWin.MessageBox.Show(
                //    "Task finished!",
                //    "End",
                //    MessageBoxButton.OK,
                //    MessageBoxImage.Information
                //);

                //if (dialogResult == MessageBoxResult.OK)
                //{
                //    if (Debugger.IsAttached)
                //    {
                //        Environment.Exit(0); // Prevents hanging during debugging
                //    }
                //    else
                //    {
                //        SysWin.Application.Current.Shutdown();
                //    }
                //}
            }

        }

        private async Task SetGrids(Complexity complexity)
        {
            // 1. Wipe the global map before starting any new registrations
            ButtonRegistry.Clear();

            // 2. Declare the tasks so they are accessible outside the switch
            Task topTask = Task.CompletedTask;
            Task leftTask = Task.CompletedTask;
            Task rightTask = Task.CompletedTask;

            // 3. Kick off all grid placements simultaneously (Parallel execution)
            switch (complexity)
            {
                case Complexity.Simple:
                    topTask = _topWindow.PlaceGrid(GridFactory.CreateSimpleTopGrid, 0, 2 * HORIZONTAL_PADDING);
                    leftTask = _leftWindow.PlaceGrid(ColumnFactory.CreateSimpleGrid, 2 * VERTICAL_PADDING, -1);
                    rightTask = _rightWindow.PlaceGrid(ColumnFactory.CreateSimpleGrid, 2 * VERTICAL_PADDING, -1);
                    break;

                case Complexity.Moderate:
                    topTask = _topWindow.PlaceGrid(GridFactory.CreateModerateTopGrid, -1, HORIZONTAL_PADDING);
                    leftTask = _leftWindow.PlaceGrid(GridFactory.CreateModerateSideGrid, VERTICAL_PADDING, -1);
                    rightTask = _rightWindow.PlaceGrid(GridFactory.CreateModerateSideGrid, VERTICAL_PADDING, -1);
                    break;

                case Complexity.Complex:
                    topTask = _topWindow.PlaceGrid(GridFactory.CreateComplexTopGrid, -1, HORIZONTAL_PADDING);
                    leftTask = _leftWindow.PlaceGrid(GridFactory.CreateComplexSideGrid, VERTICAL_PADDING, -1);
                    rightTask = _rightWindow.PlaceGrid(GridFactory.CreateComplexSideGrid, VERTICAL_PADDING, -1);
                    break;
            }

            // 4. Wait until ALL windows have fired their 'Loaded' event and called 'RegisterAllButtons'
            await Task.WhenAll(topTask, leftTask, rightTask);

            this.TrialInfo($"All grids synchronized and registered for {complexity}. ready for trials.");
        }

        public void UpdateInfoLabel(int trialNum, int nTrials, int blockNum = 0)
        {
            if (blockNum == 0) blockNum = _activeBlockNum;
            infoLabel.Text = $"Trial {trialNum}/{nTrials} --- Block {blockNum}/{_experiment.GetNumBlocks()}";
            UpdateLabelPosition();
        }

        public void UpdateInfoLabel()
        {
            int trialNum = _activeBlockHandler.GetActiveTrialNum();
            int nTrials = _activeBlockHandler.GetNumTrialsInBlock();
            infoLabel.Text = $"Trial {trialNum}/{nTrials} --- Block {_activeBlockNum}/{_experiment.GetNumBlocks()}";
            UpdateLabelPosition();
        }

        public void ShowStart(int btnW, int btnH, Brush btnColor, MouseEvents mouseEvents)
        {
            // Get the aux window
            //AuxWindow auxWindow = GetAuxWindow(panelSide);

            // Show the start
            //auxWindow.ShowStart(mouseEvents);
            _startButton = new Border
            {
                Width = btnW,
                Height = btnH,
                Background = btnColor,
                BorderBrush = Brushes.Black,
            };

            // Add label inside
            var label = new TextBlock
            {
                Text = ExpStrs.START_CAP,
                HorizontalAlignment = SysWin.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontSize = ExpLayouts.START_BUTTON_FONT_SIZE,
                Margin = new Thickness(10, 8, 10, 8) // Optional: to center the text nicely
            };
            _startButton.Child = label;

            // Temp: position start in the middle of this window
            Canvas.SetLeft(_startButton, (this.Width - _startButton.Width) / 2);
            Canvas.SetTop(_startButton, (this.Height - _startButton.Height) / 2);

            // Add event handlers
            _startButton.MouseEnter += mouseEvents.MouseEnter;
            _startButton.MouseLeave += mouseEvents.MouseLeave;
            _startButton.MouseDown += mouseEvents.MouseDown;
            _startButton.MouseUp += mouseEvents.MouseUp;

            // Add to canvas
            //Canvas.SetLeft(_startButton, this.Left);
            //Canvas.SetTop(_startButton, 0);
            canvas.Children.Add(_startButton);
        }

        public void ClearCanvas()
        {
            // Clear the canvas
            canvas.Children.Clear();
        }

        public async Task<bool> SetupLayout(Complexity complexity)
        {
            // Create a list to hold the tasks for placing the grids
            var placementTasks = new List<Task>();

            // Flag to track overall success. Assume success (true) by default.
            bool overallSuccess = true;

            switch (complexity)
            {
                // ... (your switch statement logic remains the same)
                case Complexity.Simple:
                    placementTasks.Add(_topWindow.PlaceGrid(GridFactory.CreateSimpleTopGrid, 0, 2 * HORIZONTAL_PADDING));
                    placementTasks.Add(_leftWindow.PlaceGrid(ColumnFactory.CreateSimpleGrid, 2 * VERTICAL_PADDING, -1));
                    placementTasks.Add(_rightWindow.PlaceGrid(ColumnFactory.CreateSimpleGrid, 2 * VERTICAL_PADDING, -1));
                    break;
                case Complexity.Moderate:
                    placementTasks.Add(_topWindow.PlaceGrid(GridFactory.CreateModerateTopGrid, -1, HORIZONTAL_PADDING));
                    placementTasks.Add(_leftWindow.PlaceGrid(GridFactory.CreateModerateSideGrid, VERTICAL_PADDING, -1));
                    placementTasks.Add(_rightWindow.PlaceGrid(GridFactory.CreateModerateSideGrid, VERTICAL_PADDING, -1));
                    break;
                case Complexity.Complex:
                    placementTasks.Add(_topWindow.PlaceGrid(GridFactory.CreateComplexTopGrid, -1, HORIZONTAL_PADDING));
                    placementTasks.Add(_leftWindow.PlaceGrid(GridFactory.CreateComplexSideGrid, VERTICAL_PADDING, -1));
                    placementTasks.Add(_rightWindow.PlaceGrid(GridFactory.CreateComplexSideGrid, VERTICAL_PADDING, -1));
                    break;
            }

            // Await all tasks concurrently.
            await Task.WhenAll(placementTasks);

            // Find positions for all blocks
            for (int b = 1; b <= _experiment.Blocks.Count; b++)
            {
                Block bl = _experiment.Blocks[b - 1];
                this.TrialInfo($"Setting up handler for block#{bl.Id}");

                // Use a local variable to store the handler
                BlockHandler blockHandler = new(this, bl, b);
                _blockHandlers.Add(blockHandler);
            }

            // The method now automatically returns a Task<bool> with the final value of overallSuccess.
            return overallSuccess;
        }

        public void SetTargetWindow(Side side,
            Action<Side, Object, SysIput.MouseEventArgs> windowMouseEnterHandler,
            Action<Side, Object, SysIput.MouseEventArgs> windowMouseExitHandler,
            Action<Side, Object, SysIput.MouseButtonEventArgs> windowMouseDownHandler,
            Action<Side, Object, SysIput.MouseButtonEventArgs> windowMouseUpHandler)
        {
            switch (side)
            {
                case Side.Left:
                    _targetWindow = _leftWindow;
                    break;
                case Side.Right:
                    _targetWindow = _rightWindow;
                    break;
                case Side.Top:
                    _targetWindow = _topWindow;
                    break;
                default:
                    throw new ArgumentException($"Invalid target side: {_trial.FuncSide}");
            }

            _targetWindow.MouseDown += (sender, e) => { _activeBlockHandler.OnAuxWindowMouseDown(side, sender, e); };
            _targetWindow.MouseUp += (sender, e) => { _activeBlockHandler.OnAuxWindowMouseUp(side, sender, e); };
            _targetWindow.MouseEnter += (sender, e) => { _activeBlockHandler.OnAuxWindowMouseEnter(side, sender, e); };
            _targetWindow.MouseLeave += (sender, e) => { _activeBlockHandler.OnAuxWindowMouseExit(side, sender, e); };

        }

        public AuxWindow GetAuxWindow(Side side)
        {
            switch (side)
            {
                case Side.Left:
                    return _leftWindow;
                case Side.Right:
                    return _rightWindow;
                case Side.Top:
                    return _topWindow;
                default:
                    throw new ArgumentException($"Invalid target side: {_trial.FuncSide}");
            }
        }

        public void FillStart(Brush color)
        {
            if (_startRectangle != null)
            {
                _startRectangle.Fill = color;
            }
            else
            {
                this.TrialInfo("Start rectangle is null, cannot fill it.");
            }
        }


        public void ResetAllAuxWindows()
        {
            _leftWindow.Reset();
            _rightWindow.Reset();
            _topWindow.Reset();
        }

        public void FillButtonsInAuxWindow(Side side, List<int> buttonIds, Brush color)
        {
            foreach (int buttonId in buttonIds)
            {
                FillButtonInAuxWindow(side, buttonId, color);
            }
        }

        public void FillButtonInAuxWindow(Side side, int buttonId, Brush color)
        {
            AuxWindow auxWindow = GetAuxWindow(side);
            //auxWindow.ResetButtons();
            auxWindow.FillGridButton(buttonId, color);
        }

        public void SetAuxButtonsHandlers(Side side, List<int> funcIds,
            SysIput.MouseEventHandler mouseEnterHandler,
            MouseButtonEventHandler mouseDownHandler,
            MouseButtonEventHandler mouseUpHandler,
            SysIput.MouseEventHandler mouseExitHandler,
            MouseButtonEventHandler nonFunctionDownHandler)
        {
            AuxWindow auxWindow = GetAuxWindow(side);
            auxWindow.SetGridButtonHandlers(funcIds,
                mouseEnterHandler, mouseDownHandler, mouseUpHandler,
                mouseExitHandler, nonFunctionDownHandler);
        }

        public void SetGridButtonHandlers(Side side, int targetId,
            SysIput.MouseButtonEventHandler mouseDownHandler,
            SysIput.MouseButtonEventHandler mouseUpHandler,
            MouseButtonEventHandler nonTargetDownHandler)
        {
            AuxWindow auxWindow = GetAuxWindow(side);
            auxWindow.SetGridButtonHandlers(targetId, mouseDownHandler, mouseUpHandler, nonTargetDownHandler);
        }

        public TFunction FindRandomFunction(Side side, int widthUnits)
        {
            AuxWindow auxWindow = GetAuxWindow(side);
            int id = auxWindow.SelectRandButton(widthUnits);

            if (id != -1)
            {
                this.TrialInfo($"Random function#{id} found for {widthUnits}");
                Point centerPositionInAuxWindow = auxWindow.GetGridButtonCenter(id);
                Point centerPositionAbsolute = centerPositionInAuxWindow.OffsetPosition(auxWindow.Left, auxWindow.Top);
                Point positionInAuxWindow = auxWindow.GetGridButtonPosition(id);

                return new TFunction(id, widthUnits, centerPositionAbsolute, positionInAuxWindow);
            }
            else
            {
                this.TrialInfo($"Could not find random function in {side} window!");
                return null;
            }
        }

        public List<TFunction> FindRandomFunctions(Side side, List<int> widthUnits)
        {
            this.TrialInfo($"Function widths: {widthUnits.ToStr()}");
            List<TFunction> functions = new();
            List<int> foundIds = new();
            // Find a UNIQUE function for each width
            int maxTries = 100;
            int tries = 1;
            do
            {
                tries++;
                functions.Clear();
                foundIds.Clear();
                this.TrialInfo($"Num. of Tries: {tries}");
                foreach (int widthUnit in widthUnits)
                {
                    TFunction function = FindRandomFunction(side, widthUnit);
                    this.TrialInfo($"Function found: ID {function.Id}, Width {widthUnit}");
                    functions.Add(function);
                    foundIds.Add(function.Id);
                }

            } while (foundIds.HasDuplicates() && tries < maxTries);

            return functions;
        }

        public Point GetCenterAbsolutePosition(Side side, int buttonId)
        {
            AuxWindow auxWindow = GetAuxWindow(side);
            Point centerPositionInAuxWindow = auxWindow.GetGridButtonCenter(buttonId);
            return new Point(
                centerPositionInAuxWindow.X + auxWindow.Left,
                centerPositionInAuxWindow.Y + auxWindow.Top);
        }

        public Rect GetObjAreaCenterConstraintRect()
        {
            // Square
            double padding = UITools.MM2PX(VERTICAL_PADDING);
            double objAreaHalfWidth = UITools.MM2PX(ExpLayouts.OBJ_AREA_WIDTH_MM / 2);
            return new Rect(
                this.Left + padding + objAreaHalfWidth,
                this.Top + padding + objAreaHalfWidth,
                this.Width - 2 * (padding + objAreaHalfWidth),
                this.Height - 2 * (padding + objAreaHalfWidth) - _infoLabelHeight
            );
        }

        public bool IsAuxWindowActivated(Side side)
        {
            switch (side)
            {
                case Side.Left:
                    return _activeAuxWindow == _leftWindow;
                case Side.Top:
                    return _activeAuxWindow == _topWindow;
                case Side.Right:
                    return _activeAuxWindow == _rightWindow;
                default:
                    return false; // Invalid side
            }
        }

        public int GetMiddleButtonId(Side side)
        {
            AuxWindow auxWindow = GetAuxWindow(side);
            return auxWindow.GetMiddleButtonId();
        }

        internal void EnableFunctions(Side side, List<int> list)
        {
            AuxWindow auxWindow = GetAuxWindow(side);
            auxWindow.FillGridButtons(list, UIColors.COLOR_FUNCTION_ENABLED);

        }

        internal void ChangeStartButtonText(string text)
        {
            if (_startButton != null && _startButton.Child is TextBlock label)
            {
                label.Text = text;
            }
        }

        internal void ChangeStartButtonColor(Brush color)
        {
            if (_startButton != null)
            {
                _startButton.Background = color;
            }
        }
    }
}
