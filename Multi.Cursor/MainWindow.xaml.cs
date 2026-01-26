/********************************************************
*                                                       *
*   Copyright (C) Microsoft. All rights reserved.       *
*                                                       *
********************************************************/

using Common.Constants;
using Common.Helpers;
using Common.Settings;
using CommonUI;
using CommunityToolkit.HighPerformance;
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
using static Multi.Cursor.Experiment;
using MessageBox = System.Windows.Forms.MessageBox;
using SysIput = System.Windows.Input;
using SysWin = System.Windows;
//using WinForms = System.Windows.Forms; // Alias for Forms namespace

namespace Multi.Cursor
{

    //public partial class TouchPoint
    //{
    //    public int x { get; set; }
    //    public int y { get; set; }
    //    public int Value { get; set; }

    //    override public string ToString()
    //    {
    //        return string.Format("({0}, {1}): {2}", x, y, Value);
    //    }
    //}

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
        private int INIT_X = 10, INIT_Y = 10;

        private int TOMOPAD_COLS = 15; // Total num of cols on the surface
        private int TOMOPAD_LAST_COL = 14;
        private int TOMOPAD_ROWS = 13; // Totla num of rows on the surface
        private int TOMOPAD_LAST_ROW = 12;

        private double BASE_SPEED = 10; // 
        private double ACCEL_FACTOR = 1.6; // Acceleration factor

        private (double, double) FINGER_ACCEL_RANGE = (0.5, 20); // To avoid jumps
        private double MIN_FING_ACCEL = 0.5; // To avoid jittering

        private double NOISE_MIN_THRESH = 0.1; // Maximum of finger movement in dT
        private double NOISE_MAX_THRESH = 0.7; // Maximum of finger movement in dT

        private double INFO_LABEL_BOTTOM_RATIO = 0.02; // of the height from the bottom

        private int VERTICAL_PADDING = UITools.MM2PX(ExpLayouts.WINDOW_PADDING_MM); // Padding for the windows
        private int HORIZONTAL_PADDING = UITools.MM2PX(ExpLayouts.WINDOW_PADDING_MM); // Padding for the windows

        private int TopWindowHeight = UITools.MM2PX(ExpLayouts.TOP_WINDOW_HEIGTH_MM);
        private int SideWindowWidth = UITools.MM2PX(ExpLayouts.SIDE_WINDOW_WIDTH_MM);


        // Dead zone
        private double DEAD_ZONE_DX = 0.3;
        private double DEAD_ZONE_DY = 1.8;

        // Tip/Whole finger
        private double TIP_MAX_MASS = 1000; // < 1000 is the finger tip

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
        //private bool leftPointerActive, rightPointerActive, topPointerActive;

        // For all randoms (it's best if we use one instance)
        private Random _random;

        private bool _isTouchMouseActive = false; // Is ToMo active?
        //private bool _cursorFreezed = false;

        private Rect _mainWinRect, _leftWinRect, _topWinRect, _rightWinRect;
        private Rect _lefWinRectPadded, _topWinRectPadded, _rightWinRectPadded;
        private int _infoLabelHeight;

        //--- Classes
        //private GestureDetector _gestureDetector;
        private TouchSurface _touchSurface;

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
        private List<BlockHandler> _blockHandlers = new List<BlockHandler> ();
        private Border _startButton;
        private Rectangle _objectArea;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize logging
            Output.Init();

            // Initialize random
            _random = new Random();

            // Initialize the touch handler
            TouchMouseSensorEventManager.Handler += TouchMouseSensorHandler;

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

            // Create grid
            //_topWindow.KnollHorizontal(6, 12, Target_MouseEnter, Target_MouseLeave, Target_MouseDown, Target_MouseUp);
            //Func<Grid>[] colCreators = new Func<Grid>[]
            //{
            //    () => ColumnFactory.CreateGroupType1(combination: 1),
            //    () => ColumnFactory.CreateGroupType2(combination: 2),
            //    () => ColumnFactory.CreateGroupType3(),
            //    () => ColumnFactory.CreateGroupType1(combination: 3),
            //    () => ColumnFactory.CreateGroupType2(combination: 1),
            //    () => ColumnFactory.CreateGroupType1(combination: 6),
            //    () => ColumnFactory.CreateGroupType3(),
            //    () => ColumnFactory.CreateGroupType2(combination: 1),
            //    () => ColumnFactory.CreateGroupType1(combination: 5),
            //    () => ColumnFactory.CreateGroupType2(combination: 3),
            //    () => ColumnFactory.CreateGroupType1(combination: 2),
            //    () => ColumnFactory.CreateGroupType1(combination: 4),
            //};

            // Starts placed at the two bottom corners (to set max distance from grid buttons)
            //_topWindow.GenerateGrid(_objectConstraintRectAbsolue, colCreators);

            //_leftWindow.GenerateGrid(_objectConstraintRectAbsolue, colCreators);
            //_rightWindow.GenerateGrid(_objectConstraintRectAbsolue, colCreators);

            //_leftWindow.PlaceGrid(ColumnFactory.CreateSimpleTopGrid);

            // Create Top-Simple
            //_topWindow.PlaceGrid(RowFactory.CreateSimpleTopGrid);

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
            double startHalfWidth = ExpLayouts.OBJ_WIDTH_MM / 2;
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
            this.PositionInfo($"Main Rect: {_mainWinRect.ToString()}");
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

            //double topLeftStartCenterLeft = ExpLayouts.SIDE_WINDOW_WIDTH_MM + ExpLayouts.WINDOW_PADDING_MM + startHalfWidth;
            //double topLeftStartCenterTop = ExpLayouts.TOP_WINDOW_HEIGTH_MM + ExpLayouts.WINDOW_PADDING_MM + startHalfWidth;
            //Point topLeftStartCenterAbsolute = new Point(topLeftStartCenterLeft, topLeftStartCenterTop);

            this.PositionInfo($"topLeftObjAreaCenterPosition: {topLeftButtonCenterAbsolute.Str()}");
            this.TrialInfo($"Shortest Dist = {shortestDistMM:F2}mm | Longest Dist = {longestDistMM:F2}mm");

            _experiment = new Experiment(shortestDistMM, longestDistMM);
        }

        private async void BeginExperiment()
        {
            // Set the layout (incl. placing the grid and finding positions)
            await SetupLayout(_experiment.Active_Complexity);

            // Begin the _technique
            BeginBlocks();
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
                _activeBlockHandler.LogAverageTimeOnDistances();
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

            //-- Check if the simucursor is inside target
            //if (_targetWindow.IsCursorInsideTarget())
            //{ // Simucursor inside target
            //    TargetMouseDown();
            //}
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

                this.PositionInfo($"Monitor WorkingArea H = {secondScreen.WorkingArea.Height}");
                this.PositionInfo($"BackgroundWindow Actual H (after maximize) = {_backgroundWindow.ActualHeight}");

                // Set the height as mm
                //_monitorHeightMM = Utils.PX2MM(secondScreen.WorkingArea.Height);
                _monitorHeightMM = 335;
                this.PositionInfo($"Monitor H = {secondScreen.WorkingArea.Height}");

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
                //_topWindow.MouseEnter += AuxWindow_MouseEnter;
                //_topWindow.MouseLeave += AuxWindow_MouseExit;
                //_topWindow.MouseDown += SideWindow_MouseDown;
                //_topWindow.MouseUp += SideWindow_MouseUp;
                _topWindow.Show();
                _topWinRect = UITools.GetRect(_topWindow);
                _topWinRectPadded = UITools.GetRect(_topWindow, VERTICAL_PADDING);
                _topWindow.Owner = this;

                //topWinWidthRatio = topWindow.Width / ((TOMOPAD_LAST_COL - TOMOPAD_SIDE_SIZE) - TOMOPAD_SIDE_SIZE);
                //topWinHeightRatio = topWindow.Height / TOMOPAD_SIDE_SIZE;

                // Create left window
                _leftWindow = new SideWindow(Side.Left, new Point(0, SideWindowWidth));
                _leftWindow.Background = UIColors.GRAY_F3F3F3;
                _leftWindow.Width = SideWindowWidth;
                _leftWindow.Height = this.Height;
                _leftWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                _leftWindow.Left = secondScreen.WorkingArea.Left;
                _leftWindow.Top = this.Top;
                //_leftWindow.MouseEnter += AuxWindow_MouseEnter;
                //_leftWindow.MouseLeave += AuxWindow_MouseExit;
                //_leftWindow.MouseDown += SideWindow_MouseDown;
                //_leftWindow.MouseUp += SideWindow_MouseUp;
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
                //_rightWindow.MouseEnter += AuxWindow_MouseEnter;
                //_rightWindow.MouseLeave += AuxWindow_MouseExit;
                //_rightWindow.MouseDown += SideWindow_MouseDown;
                //_rightWindow.MouseUp += SideWindow_MouseUp;
                _rightWindow.Show();
                _rightWinRect = UITools.GetRect(_rightWindow);
                _rightWinRectPadded = UITools.GetRect(_rightWindow, VERTICAL_PADDING);
                _rightWindow.Owner = this;

                //rightWinWidthRatio = rightWindow.Width / TOMOPAD_SIDE_SIZE;
                //rightWinHeightRatio = rightWindow.Height / (TOMOPAD_LAST_ROW - TOMOPAD_SIDE_SIZE);

                // Get the absolute position of the window
                _absLeft = SideWindowWidth;
                _absTop = TopWindowHeight;
                _absRight = _absLeft + (int)this.Width;
                _absBottom = _absTop + (int)Height;

                //--- Overlay window
                //var bounds = screens[1].Bounds;
                //_overlayWindow = new OverlayWindow
                //{
                //    WindowStartupLocation = WindowStartupLocation.Manual,
                //    Left = bounds.Left,
                //    Top = bounds.Top,
                //    Width = bounds.Width,
                //    Height = bounds.Height,

                //    WindowState = WindowState.Normal, // Start as normal to set position
                //};
                //WindowHelper.SetAlwaysOnTop( _overlayWindow );
                //_overlayWindow.Show();
            }


            // Set this as the active window
            activeWindow = this;

            // Synchronize window movement with main window
            this.LocationChanged += MainWindow_LocationChanged;

            // Thresholds
            //distThresh.min = Math.Sqrt(2 * (NOISE_MIN_THRESH * NOISE_MIN_THRESH));
            //distThresh.max = Math.Sqrt(2 * (NOISE_MAX_THRESH * NOISE_MAX_THRESH));
            //distThresh = (0.05, 1.0);
            //Console.WriteLine($"Dist Threshold = {distThresh}");



            //AdjustWindowPositions();
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

        //private void AuxWindow_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        //{
        //    if (sender is AuxWindow window)
        //    {
        //        Side side = window.Side;
        //        _activeBlockHandler.OnAuxWindowMouseEnter(side, sender, e);
        //    }
            

        //}

        //private void AuxWindow_MouseExit(object sender, System.Windows.Input.MouseEventArgs e)
        //{
        //    if (sender is AuxWindow window)
        //    {
        //        Side side = window.Side;
        //        _activeBlockHandler.OnAuxWindowMouseExit(side, sender, e);
        //    }

        //}

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

        /// <summary>
        /// Handle callback from mouse.  
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void TouchMouseSensorHandler(object sender, TouchMouseSensorEventArgs e)
        {
            if (_isTouchMouseActive)
            { // Only track the touch if the main cursor isn't moving
                //if (_gestureDetector == null) _gestureDetector = new GestureDetector();
                //if (_touchSurface == null) _touchSurface = new TouchSurface(_experiment.Active_Technique);

                Dispatcher.Invoke((Action<TouchMouseSensorEventArgs>)TrackTouch, e);
            }
        }

        /// <summary>
        /// Track touch
        /// </summary>
        /// <param name="e"></param>
        private void TrackTouch(TouchMouseSensorEventArgs e)
        {
            // Start frameWatch the first Time
            if (framesWatch == null)
            {
                seqFrames = new Dictionary<long, List<(double x, double y)>>();
                //leftPrevTP = (-1, -1);
                framesWatch = new Stopwatch();
                framesWatch.Start();
            }

            //long timeStamp = stopwatch.ElapsedMilliseconds;
            //gestureVector.Add((e.Image.Select(b => b / 255.0f).ToArray(), timeStamp));
            gestureShot = new byte[13, 15];
            var shotSpan = new Span2D<Byte>(gestureShot);

            // Populate the 2D array
            for (int i = 0; i < 13; i++)
            {
                for (int j = 0; j < 15; j++)
                {
                    //gestureShot[i, j] = e.Image[i * 15 + j];
                    shotSpan[i, j] = e.Image[i * 15 + j];
                    //if (gestureShot[i, j] > 200) Console.WriteLine("Touchpoint {0}, {1}", i, j);
                }
            }

            //PrintSpan(shotSpan);
            _touchSurface.Track(shotSpan);

        }

        public bool SetExperiment(string tech, TaskType taskType, Complexity complexity, ExperimentType expType)
        {
            // Make the experiment (incl. creating blocks)
            _experiment.Init(tech, taskType, complexity, expType);

            //// Find positions for all blocks
            //foreach (Block bl in _experiment.Blocks)
            //{
            //    this.TrialInfo($"Setting up handler for block#{bl.Id} with type {bl.GetObjectType()}");
            //    if (bl.GetObjectType() == TaskType.MULTI_OBJECT) // Multi-object block
            //    {
            //        this.TrialInfo($"Setting up MultiObjectBlockHandler for block#{bl.Id}");
            //        BlockHandler blockHandler = new MultiObjectBlockHandler(this, bl);
            //        bool positionsFound = blockHandler.FindPositionsForActiveBlock();
            //        if (positionsFound) _blockHandlers.Add(blockHandler);
            //        else
            //        {
            //            this.TrialInfo($"Couldn't find positions for block#{bl.Id}");
            //            return false;
            //        }
            //    }
            //    else // Single-object block
            //    {
            //        this.TrialInfo($"Setting up SingleObjectBlockHandler for block#{bl.Id}");
            //        BlockHandler blockHandler = new SingleObjectBlockHandler(this, bl);
            //        bool positionsFound = blockHandler.FindPositionsForActiveBlock();
            //        if (positionsFound) _blockHandlers.Add(blockHandler);
            //        else
            //        {
            //            this.TrialInfo($"Couldn't find positions for block#{bl.Id}");
            //            return false;
            //        }
            //    }
            //}

            //bool positionsFound = FindPositionsForAllBlocks();
            return true;
        }

        private void BeginBlocks()
        {
            _activeBlockNum = 1;
            //Block block = _experiment.GetBlock(_activeBlockNum);
            _activeBlockHandler = _blockHandlers[_activeBlockNum - 1];

            ExperiLogger.Init(_experiment.Active_Technique, _activeBlockHandler.GetBlockType());

            if (_experiment.Active_Technique.IsTomo())
            {
                _isTouchMouseActive = true;
                if (_touchSurface == null) _touchSurface = new TouchSurface(_experiment.Active_Technique);
                _touchSurface.SetGestureHandler(_activeBlockHandler);
                this.TrialInfo($"TouchSurface Initiated");
            }

            _stopWatch.Start();
            _activeBlockHandler.BeginActiveBlock();

            //if (TaskType == TaskType.REPEATING) _activeBlockHandler = new MultiObjectBlockHandler(this, block);
            //else if (TaskType == TaskType.ALTERNATING) _activeBlockHandler = new SingleObjectBlockHandler(this, block);

            //bool positionsFound = _activeBlockHandler.FindPositionsForActiveBlock();
            //if (positionsFound)
            //{
            //    UpdateInfoLabel(1, _activeBlockNum);
            //    _activeBlockHandler.BeginActiveBlock();
            //}
        }

        //private bool FindPosForRepTrial(Trial trial)
        //{
        //    int startW = UITools.MM2PX(Experiment.OBJ_WIDTH_MM);
        //    int startHalfW = startW / 2;
        //    this.TrialInfo($"Finding positions for Trial#{trial.Id} [Target = {trial.FuncSide.ToString()}, " +
        //        $"TargetMult = {trial.TargetMultiple}, D (mm) = {trial.AvgDistanceMM:F2}]");

        //    // Get the target window
        //    AuxWindow trialTargetWindow = null;
        //    Point trialTargetWindowPosition = new Point(0, 0);
        //    switch (trial.FuncSide)
        //    {
        //        case Side.Left:
        //            trialTargetWindow = _leftWindow;
        //            trialTargetWindowPosition = new Point(_leftWinRect.Left, _leftWinRect.Top);
        //            break;
        //        case Side.Right:
        //            trialTargetWindow = _rightWindow;
        //            trialTargetWindowPosition = new Point(_rightWinRect.Left, _rightWinRect.Top);
        //            break;
        //        case Side.Top:
        //            trialTargetWindow = _topWindow;
        //            trialTargetWindowPosition = new Point(_topWinRect.Left, _topWinRect.Top);
        //            break;
        //        default:
        //            throw new ArgumentException($"Invalid target side: {trial.FuncSide}");
        //    }

        //    // Set the acceptable range for the Target button
            
        //    int targetId = trialTargetWindow.SelectRandButtonByConstraints(trial.TargetMultiple, trial.DistancePX);
        //    _trialTargetIds[trial.Id] = targetId; // Map trial id to target id

        //    // Get the absolute position of the target center
        //    Point targetCenterInTargetWindow = trialTargetWindow.GetGridButtonCenter(targetId);
        //    Point targetCenterAbsolute = targetCenterInTargetWindow
        //        .OffsetPosition(trialTargetWindowPosition.x, trialTargetWindowPosition.y);

        //    // Find a Start position for each distance in the passes
        //    _repTrialStartPositions[trial.Id] = new Dictionary<int, Point>(); // Initialize the dict for this trial
        //    foreach (int dist in trial.Distances)
        //    {
        //        // Find a position for the Start
        //        Point startCenter = FindRandPointWithDist(
        //            _objectConstraintRectAbsolue,
        //            targetCenterAbsolute,
        //            dist,
        //            trial.FuncSide.GetOpposite());
        //        Point startPosition = startCenter.OffsetPosition(-startHalfW, -startHalfW);
        //        Point startPositionInMain = startPosition.OffsetPosition(-thisLeft, -thisTop); // Position relative to the main window
        //        this.TrialInfo($"Target: {targetCenterAbsolute}; Dist (px): {dist}; Start pos in main: {startPositionInMain}");
        //        if (startCenter.x == -1 && startCenter.y == -1) // Failed to find a valid position
        //        {
        //            this.TrialInfo($"No valid position found for Start for dist {dist}!");
        //            return false;
        //        }
        //        else // Valid position found
        //        {
        //            _repTrialStartPositions[trial.Id][dist] = startPositionInMain; // Add the position to the dictionary
        //        }
        //    }

        //    return true; // Valid positions found for all distances
        //}

        public Point FindRandPointWithDist(Rect rect, Point src, double dist, Side side)
        {
            this.TrialInfo($"Finding position: Rect: {rect.ToString()}; Src: {src}; Dist: {dist:F2}; Side: {side}");

            const int maxAttempts = 1000;
            const double angleSpreadDeg = 90.0; // Spread in degrees

            // 1. Find the center of the target rect
            Point center = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);

            // 2. Calculate the direction vector and base angle in radians
            double dx = center.X - src.X;
            double dy = center.Y - src.Y;
            double angleToCenter = Math.Atan2(dy, dx); // This is in radians

            // 3. Compute the spread around that angle
            double spreadRad = Tools.DegToRad(angleSpreadDeg);
            double minRad = angleToCenter - spreadRad / 2;
            double maxRad = angleToCenter + spreadRad / 2;

            for (int i = 0; i < maxAttempts; i++)
            {
                double randomRad = minRad + _random.NextDouble() * (maxRad - minRad);
                double s_x = src.X + dist * Math.Cos(randomRad);
                double s_y = src.Y + dist * Math.Sin(randomRad);
                Point candidate = new Point((int)Math.Round(s_x), (int)Math.Round(s_y));

                if (rect.Contains(candidate))
                {
                    return candidate;
                }
            }

            // No valid point found
            return new Point(-1, -1);
        }

        public void GoToNextBlock()
        {
            if (_activeBlockNum < _experiment.GetNumBlocks()) // More blocks to show
            {
                _activeBlockNum++;
                Block block = _experiment.GetBlock(_activeBlockNum);

                _activeBlockHandler = _blockHandlers[_activeBlockNum - 1];
                if (_experiment.Active_Technique.IsTomo()) _touchSurface.SetGestureHandler(_activeBlockHandler);
                
                _activeBlockHandler.BeginActiveBlock();

                //if (TaskType == TaskType.REPEATING) _activeBlockHandler = new MultiObjectBlockHandler(this, block);
                //else if (TaskType == TaskType.ALTERNATING) _activeBlockHandler = new SingleObjectBlockHandler(this, block);

                //bool positionsFound = _activeBlockHandler.FindPositionsForActiveBlock();
                //if (positionsFound) _activeBlockHandler.BeginActiveBlock();
            }
            else // All blocks finished
            {
                MessageBoxResult dialogResult = SysWin.MessageBox.Show(
                    "Technique finished!",
                    "End",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                if (dialogResult == MessageBoxResult.OK)
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

        //public void ShowStart(
        //    Point absolutePosition, Brush color,
        //    SysIput.MouseEventHandler mouseEnterHandler, SysIput.MouseEventHandler mouseLeaveHandler,
        //    MouseButtonEventHandler buttonDownHandler, MouseButtonEventHandler buttonUpHandler)
        //{
        //    // Clear the previous objects
        //    canvas.Children.Clear();

        //    // Convert the absolute position to relative position
        //    Point positionInMain = UITools.Offset(absolutePosition,
        //        - this.Left,
        //        - this.Top);

        //    // Create the square
        //    _startRectangle = new Rectangle
        //    {
        //        Width = UITools.MM2PX(ExpSizes.START_WIDTH_MM),
        //        Height = UITools.MM2PX(Experiment.START_WIDTH_MM),
        //        Fill = color
        //    };

        //    // Position the Start on the Canvas
        //    Canvas.SetLeft(_startRectangle, positionInMain.X);
        //    Canvas.SetTop(_startRectangle, positionInMain.Y);

        //    // Add event
        //    _startRectangle.MouseEnter += mouseEnterHandler;
        //    _startRectangle.MouseLeave += mouseLeaveHandler;
        //    _startRectangle.MouseDown += buttonDownHandler;
        //    _startRectangle.MouseUp += buttonUpHandler;

        //    // Add the circle to the Canvas
        //    //canvas.Children.Add(_startCircle);
        //    canvas.Children.Add(_startRectangle);
        //}


        public void ClearCanvas()
        {
            // Clear the canvas
            canvas.Children.Clear();
        }

        //public async Task<Task<bool>> SetupLayout(Complexity complexity)
        //{
        //    // Task to return when everything is set up
        //    var tcs = new TaskCompletionSource<bool>();

        //    // Create a list to hold the tasks for placing the grids
        //    var placementTasks = new List<Task>();

        //    switch (complexity)
        //    {
        //        case Complexity.Simple:
        //            placementTasks.Add(_topWindow.PlaceGrid(GridFactory.CreateSimpleTopGrid, 0, 2 * HORIZONTAL_PADDING));
        //            placementTasks.Add(_leftWindow.PlaceGrid(ColumnFactory.CreateSimpleGrid, 2 * VERTICAL_PADDING, -1));
        //            placementTasks.Add(_rightWindow.PlaceGrid(ColumnFactory.CreateSimpleGrid, 2 * VERTICAL_PADDING, -1));
        //            break;
        //        case Complexity.Moderate:
        //            placementTasks.Add(_topWindow.PlaceGrid(GridFactory.CreateModerateTopGrid, -1, HORIZONTAL_PADDING));
        //            placementTasks.Add(_leftWindow.PlaceGrid(GridFactory.CreateModerateSideGrid, VERTICAL_PADDING, -1));
        //            placementTasks.Add(_rightWindow.PlaceGrid(GridFactory.CreateModerateSideGrid, VERTICAL_PADDING, -1));
        //            break;
        //        case Complexity.Complex:
        //            placementTasks.Add(_topWindow.PlaceGrid(GridFactory.CreateTopComplexGrid, -1, HORIZONTAL_PADDING));
        //            placementTasks.Add(_leftWindow.PlaceGrid(GridFactory.CreateSideComplexGrid, VERTICAL_PADDING, -1));
        //            placementTasks.Add(_rightWindow.PlaceGrid(GridFactory.CreateSideComplexGrid, VERTICAL_PADDING, -1));
        //            break;
        //    }

        //    // Await all tasks concurrently.
        //    // The code will not proceed until all grids have been placed and their Loaded events processed.
        //    await Task.WhenAll(placementTasks);

        //    // Task is a success by default, unless a block fails to find positions
        //    tcs.SetResult(true);

        //    // Find positions for all blocks
        //    for (int b = 1; b <= _experiment.Blocks.Count; b++)
        //    {
        //        Block bl = _experiment.Blocks[b - 1];
        //        this.TrialInfo($"Setting up handler for block#{bl.Id} with type {bl.GetObjectType()}");
        //        if (bl.GetObjectType() == TaskType.MULTI_OBJECT) // Multi-object block
        //        {
        //            this.TrialInfo($"Setting up MultiObjectBlockHandler for block#{bl.Id}");
        //            BlockHandler blockHandler = new MultiObjectBlockHandler(this, bl);
        //            bool positionsFound = blockHandler.FindPositionsForActiveBlock();
        //            if (positionsFound) _blockHandlers.Add(blockHandler);
        //            else
        //            {
        //                this.TrialInfo($"Couldn't find positions for block#{bl.Id}");
        //                tcs.SetResult(false); // Indicate failure
        //            }
        //        }
        //        else // Single-object block
        //        {
        //            this.TrialInfo($"Setting up SingleObjectBlockHandler for block#{bl.Id}");
        //            BlockHandler blockHandler = new SingleObjectBlockHandler(this, bl, b);
        //            bool positionsFound = blockHandler.FindPositionsForActiveBlock();
        //            if (positionsFound) _blockHandlers.Add(blockHandler);
        //            else
        //            {
        //                this.TrialInfo($"Couldn't find positions for block#{bl.Id}");
        //                tcs.SetResult(false); // Indicate failure
        //            }
        //        }
        //    }

        //    return tcs.Task;

        //}

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
                //this.TrialInfo($"Setting up handler for block#{bl.Id} with type {bl.GetObjectType()}");

                // Use a local variable to store the handler
                BlockHandler blockHandler = null;

                if (bl.GetObjectType() == TaskType.MULTI_OBJECT) // Multi-object block
                {
                    blockHandler = new MultiObjectBlockHandler(this, bl);
                }
                else // Single-object block
                {
                    blockHandler = new SingleObjectBlockHandler(this, bl, b);
                }

                bool positionsFound = blockHandler.FindPositionsForActiveBlock();

                if (positionsFound)
                {
                    _blockHandlers.Add(blockHandler);
                }
                else
                {
                    this.PositionInfo($"Couldn't find positions for block#{bl.Id}");
                    // Set the flag to false, but DO NOT set the Task's result yet.
                    overallSuccess = false;

                    // OPTIONAL: If a single failure should stop processing immediately, use:
                    // return false; 
                }
            }

            // The method now automatically returns a Task<bool> with the final value of overallSuccess.
            return overallSuccess;
        }

        public void ShowObjectsArea(Rect areaRect, Brush areaColor, MouseEvents mouseEvents)
        {
            // Show the area rectangle
            _objectArea = new Rectangle
            {
                Width = areaRect.Width,
                Height = areaRect.Height,
                Fill = areaColor
            };

            // Position the area rectangle on the Canvas
            Canvas.SetLeft(_objectArea, areaRect.Left - this.Left);
            Canvas.SetTop(_objectArea, areaRect.Top - this.Top);

            // Add the event handler
            _objectArea.MouseEnter += mouseEvents.MouseEnter;
            _objectArea.MouseDown += mouseEvents.MouseDown;
            _objectArea.MouseUp += mouseEvents.MouseUp;
            _objectArea.MouseLeave += mouseEvents.MouseLeave;

            // Add the rectangle to the Canvas
            canvas.Children.Add(_objectArea);
        }

        public void ShowObjects(List<TObject> trialObjects, Brush objColor, MouseEvents mouseEvents)
        {
            // Create and position the objects
            foreach (TObject trObj in trialObjects)
            {
                ShowObject(trObj, objColor, mouseEvents);
            }
        }

        private void ShowObject(TObject tObject, Brush color, MouseEvents mouseEvents)
        {
            // Convert the absolute position to relative position
            Point positionInMain = UITools.Offset(tObject.Position, -this.Left, -this.Top);
            this.PositionInfo($"Showing object {tObject.Id} at {positionInMain}");
            // Create the square
            Rectangle objRectangle = new Rectangle
            {
                Tag = tObject.Id,
                Width = UITools.MM2PX(ExpLayouts.OBJ_WIDTH_MM),
                Height = UITools.MM2PX(ExpLayouts.OBJ_WIDTH_MM),
                Fill = color
            };

            // Position the object on the Canvas
            Canvas.SetLeft(objRectangle, positionInMain.X);
            Canvas.SetTop(objRectangle, positionInMain.Y);

            // Assign event handlers
            objRectangle.MouseEnter += mouseEvents.MouseEnter;
            objRectangle.MouseDown += mouseEvents.MouseDown;
            objRectangle.MouseUp += mouseEvents.MouseUp;
            objRectangle.MouseLeave += mouseEvents.MouseLeave;

            // Add the rectangle to the Canvas
            canvas.Children.Add(objRectangle);
        }

        public void ActivateAuxWindowMarker(Side window)
        {
            //this.TrialInfo($"Activating aux window: {window}");
            // Deactivate all aux windows
            _leftWindow.DeactivateGridNavigator();
            _topWindow.DeactivateGridNavigator();
            _rightWindow.DeactivateGridNavigator();

            switch (window)
            {
                case Side.Left:
                    _activeAuxWindow = _leftWindow;
                    break;
                case Side.Top:
                    _activeAuxWindow = _topWindow;
                    break;
                case Side.Right:
                    _activeAuxWindow = _rightWindow;
                    break;
            }

            _activeAuxWindow.ActivateMarker(OnFunctionMarked);
        }

        public void DeactivateAuxWindow()
        {
            _activeAuxWindow = null;
        }

        public void ShowAllAuxMarkers()
        {
            // Show all aux markers (without activation)
            _leftWindow.ShowMarker(OnFunctionMarked);
            _topWindow.ShowMarker(OnFunctionMarked);
            _rightWindow.ShowMarker(OnFunctionMarked);
        }

        private void OnFunctionMarked(int funId)
        {
            _activeBlockHandler.OnFunctionMarked(funId);
        }

        private void OnFunctionDeMarked(int funId)
        {
            _activeBlockHandler.OnFunctionUnmarked(funId);
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

            // All aux windows are treated the same (for now)
            //_targetWindow.MouseEnter += windowMouseEnterHandler;
            //_targetWindow.MouseLeave += windowMouseExitHandler;
            //_targetWindow.MouseDown += windowMouseDownHandler;
            //_targetWindow.MouseUp += windowMouseUpHandler;

            _targetWindow.MouseDown += (sender, e) => { _activeBlockHandler.OnAuxWindowMouseDown(side, sender, e);};
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

        public void FillObject(int objId, Brush color)
        {
            // Find the object by its ID in the canvas children
            foreach (var child in canvas.Children)
            {
                if (child is Rectangle rectangle && rectangle.Tag is int tag && tag == objId)
                {
                    rectangle.Fill = color;
                    return; // Exit after filling the first matching object
                }
            }
        }

        public void MarkMappedObject(int funcId)
        {
            _activeBlockHandler.MarkMappedObject(funcId);
            _activeBlockHandler.UpdateScene();
        }

        public void SetFunctionAsApplied(int funcId)
        {
            _activeBlockHandler.SetFunctionAsApplied(funcId);
            _activeBlockHandler.UpdateScene();
        }

        public void SetFunctionAsEnabled(int funcId)
        {
            _activeBlockHandler.SetFunctionAsEnabled(funcId);
            _activeBlockHandler.UpdateScene();
        }

        public void UpdateScene()
        {
            _activeBlockHandler.UpdateScene();
        }

        public void ResetTargetWindow(Side side)
        {
            if (_targetWindow != null)
            {
                _targetWindow.ResetButtons();
                _targetWindow.DeactivateGridNavigator();
            }
            else
            {
                this.TrialInfo("Target window is null, cannot reset it.");
            }
        }

        public void ResetAllAuxWindows()
        {
            _leftWindow.ResetButtons();
            _rightWindow.ResetButtons();
            _topWindow.ResetButtons();
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

        public (int, Point) GetRadomTarget(Side side, int widthUnits, int dist)
        {
            double padding = UITools.MM2PX(ExpLayouts.WINDOW_PADDING_MM);
            double objHalfWidth = UITools.MM2PX(ExpLayouts.OBJ_WIDTH_MM) / 2;
            double smallButtonHalfWidthMM = ExpSizes.BUTTON_MULTIPLES[ExpStrs.x6] / 2;
            double startHalfWidth = ExpLayouts.OBJ_WIDTH_MM / 2;
            double smallButtonHalfWidth = UITools.MM2PX(smallButtonHalfWidthMM);
            double objAreaHalfWidth = UITools.MM2PX(ExpLayouts.OBJ_AREA_WIDTH_MM / 2);

            // Find the Rect for the object area
            Rect objAreaRect = new Rect(
                this.Left + padding + objAreaHalfWidth,
                this.Top + padding + objAreaHalfWidth,
                this.Width - 2 * (padding + objAreaHalfWidth),
                this.Height - 2 * (padding + objAreaHalfWidth) - _infoLabelHeight
            );
            AuxWindow auxWindow = GetAuxWindow(side);
            int id = auxWindow.SelectRandButtonByConstraints(widthUnits, objAreaRect, dist);
            Point centerPositionInAuxWindow = auxWindow.GetGridButtonCenter(id);
            Point centerPositionAbsolute = centerPositionInAuxWindow.OffsetPosition(auxWindow.Left, auxWindow.Top);
            
            return (id,  centerPositionAbsolute); 
        }

        public TFunction FindRandomFunction(Side side, int widthUnits, Range distRange)
        {
            AuxWindow auxWindow = GetAuxWindow(side);
            int id = auxWindow.SelectRandButtonByConstraints(widthUnits, distRange);

            Point centerPositionInAuxWindow = auxWindow.GetGridButtonCenter(id);
            Point centerPositionAbsolute = centerPositionInAuxWindow.OffsetPosition(auxWindow.Left, auxWindow.Top);
            Point positionInAuxWindow = auxWindow.GetGridButtonPosition(id);

            return new TFunction(id, widthUnits, centerPositionAbsolute, positionInAuxWindow);
        }

        public List<TFunction> FindRandomFunctions(Side side, List<int> widthUnits, Range distRange)
        {
            //this.TrialInfo($"Function widths: {widthUnits.ToStr()}");
            List<TFunction> functions = new List<TFunction>();
            List<int> foundIds = new List<int>();
            // Find a UNIQUE function for each width
            int maxTries = 100;
            int tries = 1;
            do
            {
                tries++;
                functions.Clear();
                foundIds.Clear();
                //this.TrialInfo($"Num. of Tries: {tries}");
                foreach (int widthUnit in widthUnits)
                {
                    TFunction function = FindRandomFunction(side, widthUnit, distRange);
                    //this.TrialInfo($"Function found: ID {function.Id}, Width {widthUnit}");
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

        public bool IsTechniqueToMo()
        {
            return _experiment.Active_Technique == Technique.TOMO_SWIPE
                || _experiment.Active_Technique == Technique.TOMO_TAP;
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

        public int FunctionIdUnderMarker(Side side, List<int> ids)
        {
            foreach (int id in ids)
            {
                if (IsMarkerOnButton(side, id)) return id;
            }

            return -1;
        }

        public bool IsMarkerOnButton(Side side, int buttonId)
        {
            AuxWindow auxWindow = GetAuxWindow(side);
            return auxWindow.IsNavigatorOnButton(buttonId);
        }

        public void MoveMarker(TouchPoint touchPoint, Action<int> OnFunctionMarked, Action<int> OnFunctionDeMarked)
        {
            _activeAuxWindow?.MoveMarker(touchPoint, OnFunctionMarked, OnFunctionDeMarked);

        }

        public void StopAuxNavigator()
        {
            _activeAuxWindow?.StopGridNavigator();
        }

        //public Technique GetActiveTechnique()
        //{
        //    return _experiment.Active_Technique;
        //}

        public void ShowStartTrialButton(Rect objAreaRect, MouseEvents mouseEvents)
        {
            //canvas.Children.Clear(); // Clear the canvas before adding the button
            int padding = UITools.MM2PX(ExpLayouts.WINDOW_PADDING_MM);

            // Create the "button" as a Border with text inside
            _startButton = new Border
            {
                Width = UITools.MM2PX(ExpLayouts.START_BUTTON_SMALL_DIM_MM.W),
                Height = UITools.MM2PX(ExpLayouts.START_BUTTON_SMALL_DIM_MM.H),
                Background = UIColors.LIGHT_PURPLE,
                BorderBrush = Brushes.Black,
                //BorderThickness = new Thickness(2),
                //CornerRadius = new CornerRadius(6)
            };

            // Add label inside
            var label = new TextBlock
            {
                Text = "Start",
                HorizontalAlignment = SysWin.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontSize = ExpLayouts.START_BUTTON_FONT_SIZE,
                Margin = new Thickness(10, 8, 10, 8) // Optional: to center the text nicely
            };

            _startButton.Child = label;

            // Add event handlers
            _startButton.MouseEnter += mouseEvents.MouseEnter;
            _startButton.MouseDown += mouseEvents.MouseDown;
            _startButton.MouseUp += mouseEvents.MouseUp;
            _startButton.MouseLeave += mouseEvents.MouseLeave;

            // Button is centered-align to the obj area
            Point startTrialButtonPosition = new Point(0, 0);
            Point objAreaPosition = new Point(objAreaRect.X - this.Left, objAreaRect.Y - this.Top);
            startTrialButtonPosition.X = objAreaPosition.X + (objAreaRect.Width - _startButton.Width) / 2;

            // If there is no space above or below the obj area, show the button on the other side
            double aboveY = objAreaPosition.Y - _startButton.Height;
            double belowY = objAreaPosition.Y + objAreaRect.Height;
            bool showAbove = _random.Next(2) == 0; // Randomly decide whether to show above or below
            if (showAbove)
            {
                if (objAreaPosition.Y - _startButton.Height < this.Top + padding) // No space above
                {
                    // Show below the area
                    startTrialButtonPosition.Y = belowY;
                }
                else
                {
                    // Show above the object area
                    startTrialButtonPosition.Y = aboveY;
                }
            }
            else // Show below
            {
                // If no space below, show above
                if (objAreaRect.Bottom + _startButton.Height > _mainWinRect.Bottom - padding - infoLabel.Height)
                {
                    // Show above the object area
                    startTrialButtonPosition.Y = aboveY;
                }
                else
                {
                    // Show below the area
                    startTrialButtonPosition.Y = belowY;
                }
            }

            //Canvas.SetLeft(startTrialButton, (this.Width - startTrialButton.Width) / 2);
            //Canvas.SetTop(startTrialButton, (this.Height - startTrialButton.Height) / 2);
            //this.TrialInfo($"Area Position: {objAreaRect.ToString()}; Start Trial button position: {startTrialButtonPosition.ToStr()}");
            Canvas.SetLeft(_startButton, startTrialButtonPosition.X);
            Canvas.SetTop(_startButton, startTrialButtonPosition.Y);

            canvas.Children.Add(_startButton);
        }

        public void ColorStartButton(Brush color)
        {
            // Check if the _startButton is added to the canvas and if yes, change its fill
            if (canvas.Children.Contains(_startButton))
            {
                _startButton.Background = color;
            }
        }

        public void RemoveStartTrialButton()
        {
            if (_startButton != null && canvas.Children.Contains(_startButton))
            {
                canvas.Children.Remove(_startButton);
            }
        }

        public int GetMiddleButtonId(Side side)
        {
            AuxWindow auxWindow = GetAuxWindow(side);
            return auxWindow.GetMiddleButtonId();
        }
    }
}
