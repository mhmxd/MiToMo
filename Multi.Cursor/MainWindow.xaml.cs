/********************************************************
*                                                       *
*   Copyright (C) Microsoft. All rights reserved.       *
*                                                       *
********************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
//using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Research.TouchMouseSensor;
using CommunityToolkit.HighPerformance;
using System.IO;
using System.Windows.Markup;
using SysIput = System.Windows.Input;
using SysWin = System.Windows;
using System.Text;
using CommunityToolkit.HighPerformance.Helpers;
//using Tensorflow;
using System.Runtime.InteropServices;
using NumSharp;
using Microsoft.ML;
using Microsoft.ML.Data;
using System.Resources;
using System.Xml.Linq;
using System.Reflection;
using System.Diagnostics.Eventing.Reader;
using System.Windows.Shapes;
using System.Windows.Controls;
using WindowsInput;
using System.Windows.Forms;
using NumSharp.Utilities;
using System.Security.Cryptography;
using static System.Math;
using WinForms = System.Windows.Forms; // Alias for Forms namespace
using SysDraw = System.Drawing;
using Serilog.Core;
using System.Windows.Threading;
//using static Tensorflow.tensorflow;
using System.Windows.Media;
using System.Windows.Input;
using static Multi.Cursor.Output;
using static Multi.Cursor.Utils;
using static Multi.Cursor.Experiment;
using SeriLog = Serilog.Log;
using Serilog;
using MessageBox = System.Windows.Forms.MessageBox;
using System.Numerics; // Alias Serilog's Log class

namespace Multi.Cursor
{

    //public partial class TouchPoint
    //{
    //    public int X { get; set; }
    //    public int Y { get; set; }
    //    public int Value { get; set; }

    //    override public string ToString()
    //    {
    //        return string.Format("({0}, {1}): {2}", X, Y, Value);
    //    }
    //}

    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : Window, ToMoGestures
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
        /// <param name="lpRect">A pointer to the structure that contains the screen coordinates of the confining rectangle. If this parameter is NULL (IntPtr.Zero), the cursor is free to move anywhere on the screen.</param>
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

        
        // Dead zone
        private double DEAD_ZONE_DX = 0.3;
        private double DEAD_ZONE_DY = 1.8;

        // Tip/Whole finger
        private double TIP_MAX_MASS = 1000; // < 1000 is the finger tip

        //------------------------------------------------------------------------------

        //private (double min, double max) distThresh;

        private BackgroundWindow _backgroundWindow;
        private SideWindow _topWindow;
        private SideWindow _leftWindow; 
        private SideWindow _rightWindow;
        private SideWindow _activeSideWindow;
        private int _sideWindowSize = Utils.MM2PX(Config.SIDE_WINDOW_SIZE_MM);
        private OverlayWindow _overlayWindow;

        private int _absLeft, _absRight, _absTop, _absBottom;

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

        private double topWinWidthRatio, topWinHeightRatio;
        private double leftWinWidthRatio, leftWinHeightRatio;
        private double rightWinWidthRatio, rightWinHeightRatio;

        private Stopwatch _stopWatch = new Stopwatch();
        //private Stopwatch leftWatch = new Stopwatch();
        //private Stopwatch topWatch = new Stopwatch();
        //private Stopwatch rightWatch = new Stopwatch();

        private Stopwatch framesWatch;

        private List<TouchPoint> leftPointerFrames = new List<TouchPoint>();
        private List<TouchPoint> rightPointerFrames = new List<TouchPoint>();
        private List<TouchPoint> topPointerFrames = new List<TouchPoint>();

        private int leftSkipTPs = 0;

        private TouchSimulator simulator;
        private Point prevPoint = new Point(-1, -1);

        private bool headerWritten;

        private Point mainCursorPrevPosition = new Point(-1, -1);
        private double cursorTravelDist;
        private DispatcherTimer cursorMoveTimer;
        private bool mainCursorActive;
        //private bool leftPointerActive, rightPointerActive, topPointerActive;

        // For all randoms (it's best if we use one instance)
        private Random _random;

        private bool _touchMouseActive = false; // Is ToMo active?
        private bool _cursorFreezed = false;

        //--- Radiusor
        private int _actionPointerInd = -1;
        private Pointer _actionPointer;
        private Point _lastRotPointerPos = new Point(-1, -1);
        private Point _lastPlusPointerPos = new Point(-1, -1);
        private Point _lastMiddlePointerPos = new Point(-1, -1);
        private int _lastNumMiddleFingers = 0;
        private bool _radiusorActive = false;

        //--- Jump Appear
        private GestureDetector _gestureDetector;
        private TouchSurface _touchSurface;

        //-- Experiment
        private int _ptc = 13;
        private Experiment _experiment;
        private Block _block;
        private Trial _trial;
        private int _activeBlockNum, _activeTrialNum;
        private Stopwatch _trialtWatch = new Stopwatch();
        private Dictionary<string, long> _timestamps = new Dictionary<string, long>();
        //private Ellipse _startCircle;
        private Rectangle _startRectangle;
        private SideWindow _targetSideWindow;
        //private Enums _targetSideWindowDir;

        private string _trialState = ""; // Uses Str constants

        public MainWindow()
        {
            InitializeComponent();

            // Initialize random
            _random = new Random();

            TouchMouseSensorEventManager.Handler += TouchMouseSensorHandler;

            InitializeWindows();

            //-- Events
            this.MouseMove += Window_MouseMove;
            this.MouseDown += Window_MouseDown;
            this.MouseWheel += Window_MouseWheel;
            this.KeyDown += Window_KeyDown;
            _leftWindow.MouseMove += Window_MouseMove;
            _rightWindow.MouseMove += Window_MouseMove;
            _topWindow.MouseMove += Window_MouseMove;

            MouseLeftButtonDown += Window_MouseLeftButtonDown;

            //-- Create a default Experiment
            _experiment = new Experiment();

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

            
            //bool? result = introDialog.ShowDialog();
            //TRACK_LOG.Information($"Result = ", result);
            if (result == true)
            {
                _experiment = new Experiment(introDialog.ParticipantNumber, 1);

                if (introDialog.Technique == Str.TOUCH_MOUSE)
                { // Touch-mouse

                    _touchMouseActive = true;

                    // Init cursors
                    //_leftAuxPointer._kf = new KalmanFilter(Config.FRAME_DUR_MS / 1000.0);
                    //_rightAuxPointer._kf = new KalmanFilter(Config.FRAME_DUR_MS / 1000.0);
                    //_topAuxPointer._kf = new KalmanFilter(Config.FRAME_DUR_MS / 1000.0);

                    // if technique is auxursor => show aux cursors (in the middle of the windows)
                    if (_experiment.IsTechAuxCursor())
                    {
                        _leftWindow.ShowSimCursorInMiddle();
                        _topWindow.ShowSimCursorInMiddle();
                        _rightWindow.ShowSimCursorInMiddle();
                    }
                    

                } else
                { // Mouse

                    _touchMouseActive = false;
                    Experiment.Active_Technique = Technique.Mouse;
                    // Nothing for now
                }
            }

            _stopWatch.Start();

            simulator = new TouchSimulator();

            // Set the cursor in the middle of the window
            int centerX = (int)(Left + Width / 2);
            int centerY = (int)(Top + Height / 2);
            SetCursorPos(centerX, centerY);
            mainCursorPrevPosition = System.Windows.Input.Mouse.GetPosition(null);

            // Set the timer for cursor movements (to not track movement indefinitely)
            cursorMoveTimer = new DispatcherTimer();
            cursorMoveTimer.Interval = TimeSpan.FromSeconds(Config.TIME_CURSOR_MOVE_RESET);
            cursorMoveTimer.Tick += CursorMoveTimer_Tick;
            cursorMoveTimer.Start();

            // Calibrate Kalman
            //TestKalman testKalman = new TestKalman("C:\\Users\\User\\Documents\\MIDE\\Data\\move.csv");
            //testKalman.Test();

            //------------------------------------------------
            // Begin experiment
            _activeBlockNum = 1;
            _activeTrialNum = 1;
            _block = _experiment.GetBlock(_activeBlockNum);
            if (_block != null) // Got the block
            {
                _trial = _block.GetTrial(_activeTrialNum);
                if (_trial != null) ShowTrial();

            } else // Block was null for some reason
            {
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    Environment.Exit(0); // Prevents hanging during debugging
                }
                else
                {
                    System.Windows.Application.Current.Shutdown();
                }
            }
        }

        private void Window_KeyDown(object sender, SysIput.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case SysIput.Key.Right:
                    _overlayWindow.RotateBeam(2, 0);
                    break;
                case SysIput.Key.Left:
                    _overlayWindow.RotateBeam(-2, 0);
                    break;
                case SysIput.Key.Up:
                    _overlayWindow.RotateBeam(0, -2);
                    break;
                case SysIput.Key.Down:
                    _overlayWindow.RotateBeam(0, 2);
                    break;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
             
            //-- Check if the simucursor is inside target
            if (_targetSideWindow.IsAuxCursorInsideTarget())
            { // Simucursor inside target
                TargetButtonDown();
            }
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
                this.Left = Config.ACTIVE_SCREEN.WorkingArea.Left + _sideWindowSize;
                this.Top = Config.ACTIVE_SCREEN.WorkingArea.Top + _sideWindowSize;
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
                Config.ACTIVE_SCREEN = screens[1];

                //-- Background window
                _backgroundWindow = new BackgroundWindow
                {
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = Config.ACTIVE_SCREEN.WorkingArea.Left,
                    Top = Config.ACTIVE_SCREEN.WorkingArea.Top,
                    Width = Config.ACTIVE_SCREEN.WorkingArea.Width,
                    Height = Config.ACTIVE_SCREEN.WorkingArea.Height,
                    WindowState = WindowState.Normal, // Start as normal to set position
                };

                // Show and then maximize
                _backgroundWindow.Show();
                _backgroundWindow.WindowState = WindowState.Maximized;
                //---

                // Set the window position to the second monitor's working area
                this.Background = Config.GRAY_E6E6E6;
                this.Width = _backgroundWindow.Width - (2 * _sideWindowSize);
                this.Height = _backgroundWindow.Height - _sideWindowSize;
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Left = Config.ACTIVE_SCREEN.WorkingArea.Left + _sideWindowSize;
                this.Top = Config.ACTIVE_SCREEN.WorkingArea.Top + _sideWindowSize;
                this.Owner = _backgroundWindow;
                //this.Topmost = true;
                this.Show();

                // Create top window
                _topWindow = new SideWindow("Top Window", new Point(_sideWindowSize, 0));
                _topWindow.Background = Config.GRAY_F3F3F3;
                _topWindow.Height = _sideWindowSize;
                _topWindow.Width = Config.ACTIVE_SCREEN.WorkingArea.Width;
                _topWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                _topWindow.Left = Config.ACTIVE_SCREEN.WorkingArea.Left;
                _topWindow.Top = Config.ACTIVE_SCREEN.WorkingArea.Top;
                _topWindow.Show();

                //topWinWidthRatio = topWindow.Width / ((TOMOPAD_LAST_COL - TOMOPAD_SIDE_SIZE) - TOMOPAD_SIDE_SIZE);
                //topWinHeightRatio = topWindow.Height / TOMOPAD_SIDE_SIZE;

                // Create left window
                _leftWindow = new SideWindow("Left Window", new Point(0, _sideWindowSize));
                _leftWindow.Background = Config.GRAY_F3F3F3;
                _leftWindow.Width = _sideWindowSize;
                _leftWindow.Height = this.Height;
                _leftWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                _leftWindow.Left = Config.ACTIVE_SCREEN.WorkingArea.Left;
                _leftWindow.Top = this.Top;
                _leftWindow.Show();

                //leftWinWidthRatio = leftWindow.Width / TOMOPAD_SIDE_SIZE;
                //leftWinHeightRatio = leftWindow.Height / (TOMOPAD_LAST_ROW - TOMOPAD_SIDE_SIZE);

                // Create right window
                _rightWindow = new SideWindow("Right Window", new Point(_sideWindowSize + this.Width, _sideWindowSize));
                _rightWindow.Background = Config.GRAY_F3F3F3;
                _rightWindow.Width = _sideWindowSize;
                _rightWindow.Height = this.Height;
                _rightWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                _rightWindow.Left = this.Left + this.Width;
                _rightWindow.Top = this.Top;
                _rightWindow.Show();

                //rightWinWidthRatio = rightWindow.Width / TOMOPAD_SIDE_SIZE;
                //rightWinHeightRatio = rightWindow.Height / (TOMOPAD_LAST_ROW - TOMOPAD_SIDE_SIZE);

                // Get the absolute position of the window
                _absLeft = _sideWindowSize;
                _absTop = _sideWindowSize;
                _absRight = _absLeft + (int)this.Width;
                _absBottom = _absTop + (int)Height;

                //--- Overlay window
                var bounds = screens[1].Bounds;
                _overlayWindow = new OverlayWindow
                {
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = bounds.Left,
                    Top = bounds.Top,
                    Width = bounds.Width,
                    Height = bounds.Height,
                    
                    WindowState = WindowState.Normal, // Start as normal to set position
                };
                WindowHelper.SetAlwaysOnTop( _overlayWindow );
                _overlayWindow.Show();
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

        private void Window_TouchDown(object sender, TouchEventArgs e)
        {
            var touchPoint = e.GetTouchPoint(this);
            TRACK_LOG.Debug($"TouchDown at ({touchPoint.Position.X}, {touchPoint.Position.Y})");
        }

        private void Window_TouchUp(object sender, TouchEventArgs e)
        {
            var touchPoint = e.GetTouchPoint(this);
            TRACK_LOG.Debug($"TouchUp at ({touchPoint.Position.X}, {touchPoint.Position.Y})");
        }

        private void Window_TouchMove(object sender, TouchEventArgs e)
        {
            var touchPoint = e.GetTouchPoint(this);
            TRACK_LOG.Debug($"TouchMove at ({touchPoint.Position.X}, {touchPoint.Position.Y})");
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {

            ToMo_ButtonDown();
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Freeze other cursors when the mouse moves (outside a threshold)
            Point currentPos = System.Windows.Input.Mouse.GetPosition(null);
            cursorTravelDist = Utils.Dist(mainCursorPrevPosition, currentPos);
            //Print($"Dist = {cursorTravelDist}");
            // Moved more than the threshold => Deactivate side cursors
            if (cursorTravelDist > Utils.MM2PX(Config.MIN_CURSOR_MOVE_MM)) 
            { 
                //Print("Mouse moved significantly");
                //mainCursorActive = true;
                //_activeSideWindow = null;

                // Freeze the side cursors
                //_leftAuxPointer.Freeze();
                //_rightAuxPointer.Freeze();
                //_topAuxPointer.Freeze();
            }

            mainCursorPrevPosition = currentPos;

        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            int delta = e.Delta;
            Point mousePosition = e.GetPosition(this); // Get position relative to the window

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                // Interpret as horizontal scroll
                _overlayWindow.MovePlus(delta/100, delta/100);
            }
            else
            {
                // Interpret as vertical scroll
                _overlayWindow.RotateBeam(0, delta / 800.0);
            }
        }

        private void CursorMoveTimer_Tick(object sender, EventArgs e)
        {
            //Print("Tick");
            cursorTravelDist = 0; // Reset the distance
            mainCursorActive = false; // Main cursor is not active anymore
        }

        private void ShowRadiusor()
        {
            //-- Get cursor position relative to the screen
            //Point cursorPos = GetCursorPos();
            //Point cursorScreenPos = Utils.Offset(cursorPos, _leftWindow.Width, _topWindow.Height);

            //_overlayWindow.ShowLine(GetCursorScreenPosition());
        }

        private Point FindCursorScreenPos(MouseButtonEventArgs e)
        {
            // Cursor position relative to the screen
            Point cursorPos = e.GetPosition(this);
            
            return Utils.Offset(cursorPos, _leftWindow.Width, _topWindow.Height); ;
        }

        private Point FindCursorDestWinPos(Point screenPos, Window destWin)
        {
            return Utils.Offset(screenPos, -destWin.Width, -destWin.Height);
        }

        private Point FindCursorSrcDestWinPos(Window srcWin, Window destWin, Point srcP)
        {
            Point screenPos = Utils.Offset(srcP, srcWin.Width, srcWin.Height);
            return Utils.Offset(screenPos, - destWin.Width, - destWin.Height);

        }

        /// <summary>
        /// Handle callback from mouse.  
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void TouchMouseSensorHandler(object sender, TouchMouseSensorEventArgs e)
        {
            //Print($"Main cursor active? {mainCursorActive}");
            if (_touchMouseActive) 
            { // Only track the touch if the main cursor isn't moving
                if (_gestureDetector == null) _gestureDetector = new GestureDetector();
                if (_touchSurface == null) _touchSurface = new TouchSurface(this);

                Dispatcher.Invoke((Action<TouchMouseSensorEventArgs>)TrackTouch, e);
            }
        }

        /// <summary>
        /// Track touch
        /// </summary>
        /// <param name="e"></param>
        private void TrackTouch(TouchMouseSensorEventArgs e)
        {
            // Start frameWatch the first time
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

        private double EMA(List<double> points, double alpha)
        {
            if (points.Count() == 1) return points[0]; 
            else return alpha * points.Last() + (1- alpha) * EMA(points.GetRange(0, points.Count() - 1), alpha); 
        }

        /// <summary>
        /// Find positions for all the trials in the block
        /// </summary>
        private void FindPosForTrials()
        {
            // Find the positions for the trials
            // 1. Get the distance between the start and target
            // 2. Get the angle to the target
            // 3. Get the position of the target
            // 4. Get the position of the start
            // 5. Show the start and target
        }

        private void ShowTrial()
        {
            // Clear everything
            ClearCanvas();
            ClearSideWindowCanvases();
            _timestamps.Clear();

            // Start the stopwatch
            _trialtWatch.Restart();

            // Positions all relative to screen
            int padding = Utils.MM2PX(Config.WINDOW_PADDING_MM);
            int startW = Utils.MM2PX(Experiment.START_WIDTH_MM);  
            int targetW = Utils.MM2PX(_trial.TargetWidthMM);
            int dist = Utils.MM2PX(_trial.DistanceMM);

            // Set the active side window and find Start and Target positions
            Point startPositionInMainWin = new Point(); 
            Point targetPositionInSideWin = new Point();
            List<Point> jumpPositions = new List<Point>();
            _trial.SideWindow = Location.Top; // TEMP
            switch (_trial.SideWindow)
            {
                case Location.Left: 
                    _targetSideWindow = _leftWindow; 
                    //_targetSideWindowDir = Location.Left;
                    jumpPositions.Add(new Point(_leftWindow.Width / 2, _leftWindow.Height / 4)); // Top
                    jumpPositions.Add(new Point(_leftWindow.Width / 2, _leftWindow.Height * 3 / 4)); // Bottom
                    (startPositionInMainWin, targetPositionInSideWin) = LeftTargetPositionElements(
                        padding, startW, targetW, dist, jumpPositions);
                    break;
                case Location.Right: 
                    _targetSideWindow = _rightWindow; 
                    //_targetSideWindowDir = Location.Right;
                    jumpPositions.Add(new Point(_rightWindow.Width / 2, _rightWindow.Height / 4)); // Top
                    jumpPositions.Add(new Point(_rightWindow.Width / 2, _rightWindow.Height * 3 / 4)); // Bottom
                    (startPositionInMainWin, targetPositionInSideWin) = RightTargetPositionElements(
                        padding, startW, targetW, dist, jumpPositions);
                    break;
                case Location.Top: 
                    _targetSideWindow = _topWindow; 
                    //_targetSideWindowDir = Location.Top;
                    jumpPositions.Add(new Point(_topWindow.Width / 4, _topWindow.Height / 2)); // Left
                    jumpPositions.Add(new Point(_topWindow.Width / 2, _topWindow.Height / 2)); // Middle
                    jumpPositions.Add(new Point(_topWindow.Width * 3 / 4, _topWindow.Height / 2)); // Right
                    (startPositionInMainWin, targetPositionInSideWin) = TopTargetPositionElements(
                        padding, startW, targetW, dist, jumpPositions);
                    break;
            }

            // Show Start and Target
            ShowStart(
                startPositionInMainWin, startW, Brushes.Green,
                Start_MouseEnter, Start_MouseLeave, Start_MouseDown, Start_MouseUp); 

            _targetSideWindow.ShowTarget(targetPositionInSideWin, _trial.TargetWidthMM, Brushes.Blue,
                Target_MouseEnter, Target_MouseLeave, Target_ButtonDown, Target_ButtonUp);
        }

        private (Point, Point) LeftTargetPositionElements(int padding, int startW, int targetW, 
            int dist, List<Point> jumpPositions)
        {
            Point targetCenterPosition = new Point();
            Point startPosition = new Point();
            int startHalfW = startW / 2;
            int targetHalfW = targetW / 2;

            // Target boundaries in the side window
            int targetXMinInSideWin = Max(
                padding + targetHalfW,
                (int)_targetSideWindow.Width - (dist - padding - startHalfW));
            int targetXMaxInSideWin = (int)_targetSideWindow.Width - padding - targetHalfW;
            int targetYMinInSideWin = padding + targetHalfW;
            int targetYMaxInSideWin = (int)_targetSideWindow.Height - padding - targetHalfW;

            // Choose a random position for Target (check until it is not the jump position)
            Point targetCenterPosInSideWin = new Point();
            Rect possibleTargetInSideWin = new Rect();
            for (int i = 0; i < 1000; i++)
            {
                targetCenterPosInSideWin = new Point(
                    _random.Next(targetXMinInSideWin, targetXMaxInSideWin),
                    _random.Next(targetYMinInSideWin, targetYMaxInSideWin));

                possibleTargetInSideWin = new Rect(
                    targetCenterPosInSideWin.X - targetHalfW,
                    targetCenterPosInSideWin.Y - targetHalfW,
                    targetW, targetW);

                if (Utils.ContainsNot(possibleTargetInSideWin, jumpPositions))
                {
                    break; // Found a valid position
                }
                else
                {
                    return (new Point(), new Point()); // No valid position found
                }
            }

            // Convert center position to screen coordinates
            targetCenterPosition = Utils.Offset(targetCenterPosInSideWin,
                _targetSideWindow.Left, _targetSideWindow.Top);

            // Find the min/max angles to the Start
            int startCenterYMin = (int)this.Top + padding + startHalfW;
            int startCenterYMax = (int)(this.Top + this.Height) - padding - startHalfW;
            int startCenterXMin = (int)this.Left + padding + startHalfW;
            double topAngle = Atan2(startCenterYMin - targetCenterPosition.Y,
                Sqrt(Pow(dist, 2) - Pow(startCenterYMin - targetCenterPosition.Y, 2)));
            double bottomAngle = Atan2(startCenterYMax - targetCenterPosition.Y,
                Sqrt(Pow(dist, 2) - Pow(startCenterYMax - targetCenterPosition.Y, 2)));
            double topPossibleAngle = Atan2(
                Sqrt(Pow(dist, 2) - Pow(startCenterXMin - targetCenterPosition.X, 2)),
                startCenterXMin - targetCenterPosition.X);
            double bottomPossibleAngle = Atan2(
                -Sqrt(Pow(dist, 2) - Pow(startCenterXMin - targetCenterPosition.X, 2)),
                startCenterXMin - targetCenterPosition.X);
            if (double.IsNaN(topAngle)) topAngle = topPossibleAngle;
            if (double.IsNaN(bottomAngle)) bottomAngle = bottomPossibleAngle;
            topAngle = Utils.NormalizeAngleRadian(topAngle);
            bottomAngle = Utils.NormalizeAngleRadian(bottomAngle);
            GESTURE_LOG.Verbose($"Angles: {Utils.RadToDeg(topAngle):F3}, {Utils.RadToDeg(bottomAngle):F3}");

            // Get a random angle
            double randAngle = Utils.RandAngleClockwise(topAngle, bottomAngle);
            Point startCenterPosition = new Point(
                targetCenterPosition.X + dist * Cos(randAngle),
                targetCenterPosition.Y + dist * Sin(randAngle));
            GESTURE_LOG.Verbose($"Random Angle = {randAngle:F3}");
            // Convert start center position to screen coordinates
            startPosition = Utils.Offset(startCenterPosition, -startHalfW, -startHalfW);
            Point startPositionInMainWin = Utils.Offset(startPosition, 
                -this.Left, 
                -this.Top);

            // Convert target center position to screen coordinates
            Point targetPosition = Utils.Offset(targetCenterPosition, -targetHalfW, -targetHalfW);
            Point targetPositionInSideWin = Utils.Offset(targetPosition,
                -_targetSideWindow.Left,
                -_targetSideWindow.Top);

            // Return the start and target positions
            return (startPositionInMainWin, targetPositionInSideWin);
        }

        private (Point, Point) RightTargetPositionElements(int padding, int startW, int targetW,
            int dist, List<Point> jumpPositions)
        {
            Point targetCenterPosition = new Point();
            Point startPosition = new Point();
            int startHalfW = startW / 2;
            int targetHalfW = targetW / 2;

            // Target boundaries in the side window
            int targetXMinInSideWin = padding + targetHalfW;
            int targetXMaxInSideWin = Min(padding + targetHalfW, dist - padding - startHalfW);
            int targetYMinInSideWin = padding + targetHalfW;
            int targetYMaxInSideWin = (int)_targetSideWindow.Height - padding - targetHalfW;

            // Choose a random position for Target (check until it is not the jump position)
            Point targetCenterPosInSideWin = new Point();
            Rect possibleTargetInSideWin = new Rect();
            for (int i = 0; i < 1000; i++)
            {
                targetCenterPosInSideWin = new Point(
                    _random.Next(targetXMinInSideWin, targetXMaxInSideWin),
                    _random.Next(targetYMinInSideWin, targetYMaxInSideWin));

                possibleTargetInSideWin = new Rect(
                    targetCenterPosInSideWin.X - targetHalfW,
                    targetCenterPosInSideWin.Y - targetHalfW,
                    targetW, targetW);

                if (Utils.ContainsNot(possibleTargetInSideWin, jumpPositions))
                {
                    break; // Found a valid position
                }
                else
                {
                    return (new Point(), new Point()); // No valid position found
                }
            }

            // Convert center position to screen coordinates
            targetCenterPosition = Utils.Offset(targetCenterPosInSideWin,
                _targetSideWindow.Left, _targetSideWindow.Top);

            // Find the min/max angles to the Start
            int startCenterYMin = (int)this.Top + padding + startHalfW;
            int startCenterYMax = (int)(this.Top + this.Height) - padding - startHalfW;
            int startCenterXMin = (int)this.Left + padding + startHalfW;
            int startCenterXMax = (int)this.Left + (int)this.Width - padding - startHalfW;
            double topAngle = Atan2(startCenterYMin - targetCenterPosition.Y,
                -Sqrt(Pow(dist, 2) - Pow(startCenterYMin - targetCenterPosition.Y, 2)));
            double bottomAngle = Atan2(startCenterYMax - targetCenterPosition.Y,
                -Sqrt(Pow(dist, 2) - Pow(startCenterYMax - targetCenterPosition.Y, 2)));
            double topPossibleAngle = Atan2(
                Sqrt(Pow(dist, 2) - Pow(startCenterXMax - targetCenterPosition.X, 2)),
                startCenterXMin - targetCenterPosition.X);
            double bottomPossibleAngle = Atan2(
                -Sqrt(Pow(dist, 2) - Pow(startCenterXMax - targetCenterPosition.X, 2)),
                startCenterXMax - targetCenterPosition.X);
            if (double.IsNaN(topAngle)) topAngle = topPossibleAngle;
            if (double.IsNaN(bottomAngle)) bottomAngle = bottomPossibleAngle;
            topAngle = Utils.NormalizeAngleRadian(topAngle);
            bottomAngle = Utils.NormalizeAngleRadian(bottomAngle);
            GESTURE_LOG.Verbose($"Angles: {topAngle:F2}, {bottomAngle:F2}");

            // Get a random angle
            double randAngle = Utils.RandAngleClockwise(topAngle, bottomAngle);
            Point startCenterPosition = new Point(
                targetCenterPosition.X + dist * Cos(randAngle),
                targetCenterPosition.Y + dist * Sin(randAngle));
            GESTURE_LOG.Information($"Random Angle = {randAngle:F2}");
            // Convert start center position to screen coordinates
            startPosition = Utils.Offset(startCenterPosition, -startHalfW, -startHalfW);
            Point startPositionInMainWin = Utils.Offset(startPosition,
                -this.Left,
                -this.Top);

            // Convert target center position to screen coordinates
            Point targetPosition = Utils.Offset(targetCenterPosition, -targetHalfW, -targetHalfW);
            Point targetPositionInSideWin = Utils.Offset(targetPosition,
                -_targetSideWindow.Left,
                -_targetSideWindow.Top);

            // Return the start and target positions
            return (startPositionInMainWin, targetPositionInSideWin);
        }

        private (Point, Point) TopTargetPositionElements(int padding, int startW, int targetW,
            int dist, List<Point> jumpPositions)
        {
            Point targetCenterPosition = new Point();
            Point startCenterPosition = new Point();
            Point startPosition = new Point();
            int startHalfW = startW / 2;
            int targetHalfW = targetW / 2;

            //--- New way (v.4!)
            int targetMinX = padding + targetHalfW;
            int targetMaxX = (int)_topWindow.Width - padding - targetHalfW;
            int targetMinY = (int)(_topWindow.Top) + padding + targetHalfW;
            int targetMaxY = (int)(_topWindow.Top + _topWindow.Height) - padding - targetHalfW;

            int startMinY = (int)this.Top + padding + startHalfW;
            int startMaxY = (int)(this.Top + this.Height) - padding - startHalfW;
            int startMinX = (int)this.Left + padding + startHalfW;
            int startMaxX = (int)(this.Left + this.Width) - padding - startHalfW;

            Rect topRect = new Rect(
                targetMinX, targetMinY,
                targetMaxX - targetMinX, targetMaxY - targetMinY);

            Rect mainRect = new Rect(
                startMinX, startMinY,
                startMaxX - startMinX, startMaxY - startMinY);

            // Find min angle as Target is in on lower left and Start on Ymin
            double minTheta = Asin((double)(startMinY - targetMaxY) / dist);
            GESTURE_LOG.Verbose($"Min Angle = {Utils.RadToDeg(minTheta):F2}");

            // Find max angle as Target is in on upper right and Start on Ymax
            double maxTheta = PI - minTheta;
            GESTURE_LOG.Verbose($"Max Angle = {Utils.RadToDeg(maxTheta):F2}");

            for (int i = 0; i < 1000; i++)
            {
                // Randomly place target
                double targetCenterX = _random.NextDouble() * (targetMaxX - targetMinX) + targetMinX;
                double targetCenterY = _random.NextDouble() * (targetMaxY - targetMinY) + targetMinY;

                // Choose a random angle
                double randAngle = Utils.RandAngleClockwise(minTheta, maxTheta);
                GESTURE_LOG.Verbose($"Random Angle = {Utils.RadToDeg(randAngle):F2}");

                // Calculate potential start center
                double potentialStartX = targetCenterX + dist * Cos(randAngle);
                double potentialStartY = targetCenterY + dist * Sin(randAngle);
                GESTURE_LOG.Verbose($"Potential Start = ({potentialStartX:F2}, {potentialStartY:F2})");

                // Check if potential start center is within main window usable area
                startCenterPosition = new Point(potentialStartX, potentialStartY);
                targetCenterPosition = new Point(targetCenterX, targetCenterY);
                // Convert target center position to screen coordinates
                Point targetPosition = Utils.Offset(targetCenterPosition, -targetHalfW, -targetHalfW);
                Point targetPositionInSideWin = Utils.Offset(targetPosition,
                    -_targetSideWindow.Left,
                    -_targetSideWindow.Top);

                Rect targetInSideWinRect = new Rect(
                    targetPositionInSideWin.X,
                    targetPositionInSideWin.Y,
                    targetW, targetW);

                if (mainRect.Contains(startCenterPosition) && 
                    
                    Utils.ContainsNot(targetInSideWinRect, jumpPositions))
                {
                    
                    // Convert start center position to screen coordinates
                    startPosition = Utils.Offset(startCenterPosition, -startHalfW, -startHalfW);
                    Point startPositionInMainWin = Utils.Offset(startPosition,
                        -this.Left,
                        -this.Top);
                    

                    return (startPositionInMainWin, targetPositionInSideWin);

                }
            }

            GESTURE_LOG.Information("Failed to find a valid placement within the retry limit.");
            return (new Point(), new Point()); // Indicate failure
        }

        private void ActivateLeftCursor()
        {
            _activeSideWindow = _leftWindow;
        }

        private void ToMo_ButtonDown()
        {
            if (_experiment.IsTechAuxCursor())
            {
                // Trial result
                if (_targetSideWindow.IsAuxCursorInsideTarget()) // Auxursor in target
                {
                    TargetButtonDown();
                }
                else
                {
                    // Don't do anything for now
                }
            }

            if (_experiment.IsTechRadiusor())
            {
                Point crossPos = _overlayWindow.GetCrossPosition();
                Point crossScreenPos = _overlayWindow.PointToScreen(crossPos);
                Point crossSideWinPos = _targetSideWindow.PointFromScreen(crossScreenPos);

                // Trial result
                if (_targetSideWindow.IsPointInsideTarget(crossSideWinPos)) // Cross in target
                {
                    TargetButtonDown();
                }
                else
                {
                    // Nothing for now
                }

                // Release the cursor
                 _cursorFreezed = !UnfreezeCursor();
            }
  
        }


        private void EndTrial(RESULT result)
        {
            if (_activeTrialNum == _block.GetNumTrials()) // Was last trial
            {
                if (_activeBlockNum == _experiment.GetNumBlocks()) // Was last block
                {
                    // Do nothing for now
                }
                else
                {
                    _activeBlockNum++;
                    _block = _experiment.GetBlock(_activeBlockNum);
                    _activeTrialNum = 1;
                    _trial = _block.GetTrial(_activeTrialNum);
                    ShowTrial();
                }
            } else
            {
                _activeTrialNum++;
                _trial = _block.GetTrial(_activeTrialNum);
                ShowTrial();
            }
        }

        private void Start_MouseEnter(object sender, SysIput.MouseEventArgs e)
        {
            //--- Set the time and state
            if (_timestamps.ContainsKey(Str.TARGET_UP))
            { // Return from target
                _timestamps[Str.START_LAST_RE_ENTRY] = _trialtWatch.ElapsedMilliseconds;
            } else
            { // First time
                _timestamps[Str.START_LAST_ENTRY] = _trialtWatch.ElapsedMilliseconds;
            }
            
        }

        private void Start_MouseLeave(object sender, SysIput.MouseEventArgs e)
        {
            
        }

        private void Start_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_experiment.IsTechAuxCursor())
            {
                if (_timestamps.ContainsKey(Str.TARGET_DOWN)) // Target hit, Start click again => End trial
                {
                    EndTrial(RESULT.HIT);
                    return;
                }

                if (_timestamps.ContainsKey(Str.START_UP)) // Start already clicked, it's actually aux click
                {
                    ToMo_ButtonDown();
                    return;

                }

            }

            if (_experiment.IsTechRadiusor())
            {
                if (_timestamps.ContainsKey(Str.TARGET_DOWN)) // Target hit, Start click again => End trial
                {
                    EndTrial(RESULT.HIT);
                    return;
                }

                if (_timestamps.ContainsKey(Str.START_UP)) // Start already clicked, it's actually cross click
                {
                    ToMo_ButtonDown();
                    return;

                }

                //--- First click on Start ----------------------------------------------------
                // Show line at the cursor position
                Point cursorScreenPos = FindCursorScreenPos(e);
                Point overlayCursorPos = FindCursorDestWinPos(cursorScreenPos, _overlayWindow);

                _overlayWindow.ShowBeam(cursorScreenPos);
                _radiusorActive = true;

                // Freeze main cursor
                _cursorFreezed = FreezeCursor();
            }

            if (Experiment.Active_Technique == Technique.Mouse)
            {
                if (_timestamps.ContainsKey(Str.TARGET_DOWN)) // Target hit, Start click again => End trial
                {
                    EndTrial(RESULT.HIT);
                    return;
                }
            }

            //--- First click in any technique
            _targetSideWindow.ColorTarget(Brushes.Green);
            //_startCircle.Fill = Brushes.Red;
            _startRectangle.Fill = Brushes.Red;

            _timestamps[Str.START_PRESS] = _trialtWatch.ElapsedMilliseconds;

        }

        private void Start_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_timestamps.ContainsKey(Str.START_PRESS)) // First time
            { 
                _timestamps[Str.START_UP] = _trialtWatch.ElapsedMilliseconds;
            }
        }

        private void Target_MouseEnter(object sender, SysIput.MouseEventArgs e)
        {

        }

        private void Target_MouseLeave(object sender, SysIput.MouseEventArgs e)
        {

        }

        /// <summary>
        /// Used for standard mouse
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Target_ButtonDown(object sender, MouseButtonEventArgs e)
        {
            TargetButtonDown();
        }

        private void TargetButtonDown()
        {
            // Set the time
            _timestamps[Str.TARGET_DOWN] = _trialtWatch.ElapsedMilliseconds;

            //--- Change the colors
            _targetSideWindow.ColorTarget(Brushes.Red);
            //_startCircle.Fill = Brushes.Green; // Change Start back to green
            _startRectangle.Fill = Brushes.Green; // Change Start back to green

            // No need for the cursor anymore
            if (_experiment.IsTechRadiusor())
            {
                _overlayWindow.HideBeam();
                _radiusorActive = false;
            }

            if (_experiment.IsTechAuxCursor())
            {
                _activeSideWindow.DeactivateCursor();
            }

            //_activeSideWindow = null;

        }

        private void Target_ButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Set the time and state
            _timestamps[Str.TARGET_UP] = _trialtWatch.ElapsedMilliseconds;
            _trialState = Str.TARGET_UP;
        }

        /// <summary>
        /// Show the start circle
        /// </summary>
        /// <param name="position">Top left</param>
        /// <param name="width">In px</param>
        /// <param name="color"></param>
        /// <param name="mouseEnterHandler"></param>
        /// <param name="mouseLeaveHandler"></param>
        /// <param name="buttonDownHandler"></param>
        /// <param name="buttonUpHandler"></param>
        private void ShowStart(
            Point position, double width, Brush color, 
            SysIput.MouseEventHandler mouseEnterHandler, SysIput.MouseEventHandler mouseLeaveHandler,
            MouseButtonEventHandler buttonDownHandler, MouseButtonEventHandler buttonUpHandler)
        {

            // Create the square
            _startRectangle = new Rectangle
            {
                Width = width,
                Height = width,
                Fill = color
            };

            // Position the Start on the Canvas
            Canvas.SetLeft(_startRectangle, position.X);
            Canvas.SetTop(_startRectangle, position.Y);

            // Add event
            _startRectangle.MouseEnter += mouseEnterHandler;
            _startRectangle.MouseLeave += mouseLeaveHandler;
            _startRectangle.MouseDown += buttonDownHandler;
            _startRectangle.MouseUp += buttonUpHandler;

            // Add the circle to the Canvas
            //canvas.Children.Add(_startCircle);
            canvas.Children.Add(_startRectangle);
        }

        public static Point GetCursorScreenPosition()
        {
            POINT point;
            GetCursorPos(out point);
            return new Point(point.X, point.Y);
        }

        private bool FreezeCursor()
        {
            if (GetCursorPos(out POINT currentPos))
            {
                // Define a 1x1 pixel rectangle at the current cursor position
                RECT clipRect = new RECT
                {
                    Left = currentPos.X,
                    Top = currentPos.Y,
                    Right = currentPos.X + 1, // Make it a tiny box
                    Bottom = currentPos.Y + 1
                };

                // Apply the clip
                if (ClipCursor(ref clipRect))
                {
                    // Optional: Store the current clip state if you need to restore it later,
                    // but usually, you just want to fully unclip.
                    // GetClipCursor(out _originalClipRect);
                    return true;
                }
                else
                {
                    // Handle potential error (e.g., log GetLastError())
                    Console.WriteLine($"ClipCursor failed. Win32 Error Code: {Marshal.GetLastWin32Error()}");
                    return false;
                }
            }
            else
            {
                Console.WriteLine($"GetCursorPos failed. Win32 Error Code: {Marshal.GetLastWin32Error()}");
                return false;
            }
        }

        public static bool UnfreezeCursor()
        {
            // Release the clip by passing IntPtr.Zero (null pointer)
            if (!ClipCursor(IntPtr.Zero))
            {
                Console.WriteLine($"ClipCursor(IntPtr.Zero) failed. Win32 Error Code: {Marshal.GetLastWin32Error()}");
                return false;
            }

            return true;
        }

        private void ClearCanvas()
        {
            canvas.Children.Clear();
        }

        private void ClearSideWindowCanvases()
        {
            _leftWindow.ClearCanvas();
            _topWindow.ClearCanvas();
            _rightWindow.ClearCanvas();
        }

        private void ActivateSideWin(Location window, Location tapLoc)
        {
            switch (window)
            {
                case Location.Left:
                    _activeSideWindow = _leftWindow;
                    _leftWindow.ActivateCursor(tapLoc);
                    _topWindow.DeactivateCursor();
                    _rightWindow.DeactivateCursor();
                    break;
                case Location.Top:
                    _activeSideWindow = _topWindow;
                    _topWindow.ActivateCursor(tapLoc);
                    _leftWindow.DeactivateCursor();
                    _rightWindow.DeactivateCursor();
                    break;
                case Location.Right:
                    _activeSideWindow = _rightWindow;
                    _rightWindow.ActivateCursor(tapLoc);
                    _leftWindow.DeactivateCursor();
                    _topWindow.DeactivateCursor();
                    break;
            }
        }


        public void LeftPress()
        {
            //if (_technique == 1) _activeSideWindow = _leftWindow;
            //if (_technique == 2)
            //{
            //    //-- Get cursor position relative to the screen
            //    Point cursorPos = new Point(WinForms.Cursor.Position.X, WinForms.Cursor.Position.Y);
            //    Point cursorScreenPos = Utils.Offset(cursorPos, _leftWindow.Width, _topWindow.Height);

            //    _overlayWindow.ShowLine(GetCursorScreenPosition());
            //}
        }

        public void RightPress()
        {
            
        }

        public void TopPress()
        {
            
        }

        public void LeftMove(double dX, double dY)
        {
            
        }

        public void IndexDown(TouchPoint indPoint)
        {
            //if (Technique == 1)
            //{
            //    if (_activeSideWindow != null)
            //    {

            //        _activeSideWindow.MoveAuxPointer(indPoint);
            //    }
            //}
        }

        public void IndexTap()
        {
            ActivateSideWin(Location.Top, Location.Left);
        }

        public void IndexMove(TouchPoint indPoint)
        {
            
            if (_experiment.IsTechAuxCursor())
            {
                if (_activeSideWindow != null)
                {
                    _activeSideWindow.UpdateAuxursor(indPoint);
                }
            }

            if (_radiusorActive)
            {
                bool beamRotated = _overlayWindow.RotateBeam(indPoint);

            }

        }

        public void IndexMove(double dX, double dY)
        {
            throw new NotImplementedException();
        }

        public void IndexUp()
        {
            if (_experiment.IsTechAuxCursor() && _activeSideWindow != null)
            {
                _activeSideWindow.StopAuxursor();
            }
            //_lastPlusPointerPos.X = -1;
            _lastRotPointerPos.X = -1;

        }

        public void ThumbSwipe(Direction dir)
        {
            if (Experiment.Active_Technique == Technique.Auxursor_Swipe)
            {
                switch (dir)
                {
                    case Direction.Left:
                        ActivateSideWin(Location.Left, Location.Middle);
                        break;
                    case Direction.Right:
                        ActivateSideWin(Location.Right, Location.Middle);
                        break;
                    case Direction.Up:
                        ActivateSideWin(Location.Top, Location.Middle);
                        break;
                }
            }
            
        }

        public void ThumbMove(TouchPoint thumbPoint)
        {
            if (_radiusorActive) // Radiusor
            {
                // Move the plus
                bool plusMoved = _overlayWindow.MovePlus(thumbPoint);

                if (plusMoved) _cursorFreezed = FreezeCursor();
                else _cursorFreezed = !UnfreezeCursor();

            }
        }

        public void ThumbUp(TouchPoint indPoint)
        {
            //_lastRotPointerPos.X = -1;
            _lastPlusPointerPos.X = -1;
        }

        public void ThumbTap(Location tapLoc)
        {
            if (Experiment.Active_Technique == Technique.Auxursor_Tap)
            {
                ActivateSideWin(Location.Left, tapLoc);
            }
            
        }

        public void MiddleTap()
        {
            if (Experiment.Active_Technique == Technique.Auxursor_Tap)
            {
                ActivateSideWin(Location.Top, Location.Middle);
            }
        }

        public void RingTap()
        {
            if (Experiment.Active_Technique == Technique.Auxursor_Tap)
            {
                ActivateSideWin(Location.Top, Location.Right); // Right side of the top window
                //ActivateSide(Direction.Up, tapDir);
            }
        }

        public void LittleTap(Location tapLoc)
        {
            if (Experiment.Active_Technique == Technique.Auxursor_Tap)
            {
                ActivateSideWin(Location.Right, tapLoc);
            }
        }
    }
}
