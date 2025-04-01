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
using MessageBox = System.Windows.Forms.MessageBox; // Alias Serilog's Log class

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

        private double _relLeft, _relRight, _relTop, _relBottom;

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

        private FeatureLogger fLogger; // Singleton

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
        private Direction _targetSideWindowDir;

        //private enum TrialState
        //{
        //    SHOWN, CURSOR_MOVED, 
        //    ENTERED_START, CLICKED_START, EXITED_START,
        //    ENTERED_TARGET, CLICKED_TARGET, EXITED_TARGET,
        //    REENTERED_START
        //}
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
                    _experiment.Active_Technique = Technique.Mouse;
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
                    _overlayWindow.RotateLine(2, 0);
                    break;
                case SysIput.Key.Left:
                    _overlayWindow.RotateLine(-2, 0);
                    break;
                case SysIput.Key.Up:
                    _overlayWindow.RotateLine(0, -2);
                    break;
                case SysIput.Key.Down:
                    _overlayWindow.RotateLine(0, 2);
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
                _relLeft = _sideWindowSize;
                _relTop = _sideWindowSize;
                _relRight = _relLeft + Width;
                _relBottom = _relTop + Height;

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

            //var fixedPosition = new System.Drawing.Point(100, 100); // X, Y coordinate
            //WinForms.Cursor.Position = fixedPosition;

            //var mousePosition = e.GetPosition(leftWindow);
            //Console.WriteLine($"Cursor Position: {mousePosition}");
            //var screenPoint = PointToScreen(mousePosition);

            //// Get the screen where the cursor is located
            //Screen currentScreen = Screen.FromPoint(new SysDraw.Point((int)screenPoint.X, (int)screenPoint.Y));

            //// Log the screen bounds and cursor position

            //Console.WriteLine($"Screen Bounds: {currentScreen.Bounds}");

            //// Ensure the cursor stays within the screen bounds
            //var screenBounds = currentScreen.Bounds;
            //var clampedX = Math.Max(screenBounds.Left, Math.Min(screenPoint.X, screenBounds.Right));
            //var clampedY = Math.Max(screenBounds.Top, Math.Min(screenPoint.Y, screenBounds.Bottom));

            //// Update cursor position
            //WinForms.Cursor.Position = new SysDraw.Point((int)clampedX, (int)clampedY);

            //if (prevPoint.X != -1)
            //{
            //    leftWindow.MoveSimCursor(point.X - prevPoint.X, point.Y - prevPoint.Y);
            //}

            //prevPoint = point;

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
                _overlayWindow.RotateLine(0, delta / 800.0);
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

            // Get the touch points
            //List<(int row, int col, byte val)> touchpoints = GetTouchPoints(shotSpan);
            //PrintSpan(shotSpan);

            _touchSurface.Track(shotSpan);

            //List<TouchPoint> touchPoints = _touchSurface.GetTouchPoints();


            //List<TouchPoint> topTouchPoints = new List<TouchPoint>();
            //List<TouchPoint> leftTouchPoints = new List<TouchPoint>();
            //List<TouchPoint> rightTouchPoints = new List<TouchPoint>();
            //List<TouchPoint> middleTouchPoints = new List<TouchPoint>();

            //int minRightCol = Config.LAST_TOUCH_COL - Config.LAST_RIGHT_TOUCH_COL;
            //Console.WriteLine($"Num. Touch Points: {pointers.Count()}");
            //leftPointerActive = rightPointerActive = topPointerActive = false;
            //TouchPoint leftTouchPoint = null; TouchPoint rightTouchPoint = null; TouchPoint topTouchPoint = null;

            //--- Detect gestures
            //_gestureDetector.Detect(touchPoints, OnTap);

            ////--- Set the pointers
            //foreach (TouchPoint tp in touchPoints)
            //{
            //    // Left points
            //    if (tp.IsCenterColInRange(0, Config.LAST_LEFT_TOUCH_COL))
            //    {
            //        leftTouchPoints.Add(tp);
            //    }

            //    // Right points
            //    if (tp.IsCenterColInRange(minRightCol, Config.LAST_TOUCH_COL))
            //    {
            //        rightTouchPoints.Add(tp);
            //    }

            //    // Top points
            //    //if (tp.IsCenterRowInRange(0, Config.LAST_TOP_TOUCH_ROW)
            //    //    && tp.IsCenterColInRange(Config.LAST_LEFT_TOUCH_COL, minRightCol))
            //    //{
            //    //    topTouchPoints.Add(tp);
            //    //}

            //    // Middle pointers
            //    if (tp.IsCenterColInRange(Config.LAST_LEFT_TOUCH_COL, minRightCol))
            //    {
            //        middleTouchPoints.Add(tp);
            //    }
            //}

            ////-- Organize middle pointers
            //int currentNumMiddlePoints = middleTouchPoints.Count();
            //if (currentNumMiddlePoints > 0)
            //{
            //    // Number of pointers has changed => re-evaluate the pointers
            //    if (currentNumMiddlePoints != _lastNumMiddleFingers)
            //    {
            //        // Reset positioning
            //        _lastMiddlePointerPos = new Point(-1, -1); // Reset positioning

            //        // Distinguish action pointer (leftmost)
            //        _actionPointerInd = 0;
            //        _actionPointer = new Pointer();
            //        for (int i = 1; i < currentNumMiddlePoints; i++)
            //        {
            //            // New pointer to the left
            //            if (middleTouchPoints[i].GetX() < middleTouchPoints[_actionPointerInd].GetX())
            //            {
            //                _actionPointerInd = i;
            //            }
            //        }

            //        _lastNumMiddleFingers = currentNumMiddlePoints;
            //    } else
            //    { // No changes in the middle points
            //        (double dX, double dY) = _actionPointer.Update(middleTouchPoints[_actionPointerInd]);
            //        //TRACK_LOG.Informati
            //        //on($"dX = {dX}, dY = {dY}");
            //    }

            //} else
            //{ // No pointers
            //    _actionPointerInd = -1;
            //    _actionPointer = null;
            //    _lastNumMiddleFingers = 0;
            //    _lastMiddlePointerPos = new Point(-1, -1); // Reset positioning
            //}


            //--- Manage left points
            //if (leftTouchPoints.Count > 0)
            //{
            //    ShowRadiusor();
            //    //--- Radiusor
            //    Point fingerPos = leftTouchPoints[0].GetCenterOfMass();
            //    if (_lastLeftPointerPos.Y == -1)
            //    { // First point
            //        //TRACK_LOG.Information($"First point = {fingerPos:F3}");
            //        _lastLeftPointerPos = fingerPos;
            //    }
            //    else
            //    {
            //        double dY = fingerPos.Y - _lastLeftPointerPos.Y;

            //        //TRACK_LOG.Information($"dAngle = {dAngle:F3}");
            //        if (Abs(dY) > 0.02)
            //        {
            //            double dAngle = -dY * 10; // Up => rotate right
            //            _overlayWindow.RotateLine(dAngle);
            //        }


            //        _lastLeftPointerPos = fingerPos;
            //    }
            //}
            //else
            //{ // Left fingers off => reset
            //    _lastLeftPointerPos = new Point(-1, -1);
            //}

            //--- Control the cross with middle pointers
            //if (_actionPointerInd != -1 && _lineVisible)
            //{
            //    Point actionPointerPos = middleTouchPoints[_actionPointerInd].GetCenterOfMass();
            //    TRACK_LOG.Information($"AP: {actionPointerPos.Y}, LMP: {_lastMiddlePointerPos.Y}");
            //    if (_lastMiddlePointerPos.X == -1)
            //    { // First point
            //        TRACK_LOG.Information("First point");
            //        _lastMiddlePointerPos = actionPointerPos;
            //    }
            //    else
            //    {
            //        // Calculate distance to move the cross
            //        double dX = actionPointerPos.X - _lastMiddlePointerPos.X;
            //        double dY = actionPointerPos.Y - _lastMiddlePointerPos.Y;
            //        TRACK_LOG.Information($"dX = {dX}, dY = {dY}");
            //        if (Abs(dX) > 0.02 || Abs(dY) > 0.02)
            //        {
            //            _overlayWindow.MoveCross(dX * 10, dY * 10);
            //        }

            //        _lastMiddlePointerPos = actionPointerPos;
            //    }

            //}

            //--- Manage top points
            //if (topTouchPoints.Count > 0)
            //{
            //    if (topTouchPoints.Count > 1)
            //    { // Freeze if more fingers are on the top touch-part
            //        _topAuxPointer.Freeze();
            //        _lastTopPointerPos = new Point(-1, -1);
            //    }
            //    else
            //    {
            //        //_topAuxPointer.Unfreeze();

            //        // KF
            //        //(double dX, double dY) = _topAuxPointer.Move(topTouchPoints[0]);
            //        //_topWindow.MoveAuxCursor(dX * Config.MAPPING_GAIN, dY * Config.MAPPING_GAIN);

            //        //--- Radiusor
            //        Point fingerPos = topTouchPoints[0].GetCenterOfMass();
            //        if (_lastTopPointerPos.X == -1)
            //        { // First point
            //            TRACK_LOG.Information($"First point = {fingerPos:F3}");
            //            _lastTopPointerPos = fingerPos;
            //        }
            //        else
            //        {
            //            // Calculate distance to move the cross
            //            double dX = fingerPos.X - _lastLeftPointerPos.X;
            //            double dY = fingerPos.Y - _lastLeftPointerPos.Y;
            //            _overlayWindow.MoveCross(dX, dY);

            //            _lastTopPointerPos = fingerPos;
            //        }

            //    }
            //}
            //else
            //{ // Top fingers off => reset
            //    _lastTopPointerPos = new Point(-1, -1);
            //}

        }

        private double EMA(List<double> points, double alpha)
        {
            if (points.Count() == 1) return points[0]; 
            else return alpha * points.Last() + (1- alpha) * EMA(points.GetRange(0, points.Count() - 1), alpha); 
        }


        private void ShowTrial()
        {
            // Trial info
            TRIAL_LOG.Information(_trial.ToString());

            // Clear everything
            ClearCanvas();
            ClearSideWindowCanvases();
            _timestamps.Clear();

            // Start the stopwatch
            _trialtWatch.Restart();

            // Positions all relative to screen
            Point targetPosition = new Point();
            Point startPosition = new Point();
            int marginPX = Utils.MM2PX(Config.WINDOW_MARGIN_MM);
            int startRadius = Utils.MM2PX(Experiment.START_CIRCLE_WIDTH_MM);

            // Set the active side window
            switch (_trial.SideWindowDir)
            {
                case Direction.Left: 
                    _targetSideWindow = _leftWindow; 
                    _targetSideWindowDir = Direction.Left; 
                    break;
                case Direction.Right: 
                    _targetSideWindow = _rightWindow; 
                    _targetSideWindowDir = Direction.Right; 
                    break;
                case Direction.Up: 
                    _targetSideWindow = _topWindow; 
                    _targetSideWindowDir = Direction.Up; 
                    break;
            }
            TRIAL_LOG.Debug("Target Window: " + _trial.SideWindowDir);

            //--- Show and find target position (returned position is rel. to side window)
            // Target is firstly 
            Point targetPositionInSideWin = _targetSideWindow.ShowTarget(
                _trial.TargetWidthMM, Brushes.Blue,
                Target_MouseEnter, Target_MouseLeave, Target_ButtonDown, Target_ButtonUp);
            // Convert to screen coordinates
            targetPosition = new Point(
                targetPositionInSideWin.X + _targetSideWindow.Rel_Pos.X,
                targetPositionInSideWin.Y + _targetSideWindow.Rel_Pos.Y);
            TRIAL_LOG.Debug("Target Position = " + targetPosition.ToString());

            //--- Create a list of possible positions to randomly choose from
            // Limits
            int Xmin = (int)(_relLeft + marginPX + startRadius);
            int Xmax = (int)(_relRight - marginPX - startRadius);
            int Ymin = (int)(_relTop + marginPX + startRadius);
            int Ymax = (int)(_relBottom - marginPX - startRadius);
            TRIAL_LOG.Debug($"Limits = {Xmin}, {Xmax}, {Ymin}, {Ymax}");
            // Go through angles and find fitting ones
            List<Point> validStartPositions = new List<Point>();
            for (double angle = 0; angle < 2 * Math.PI; angle += 0.1) // Sample angles
            {
                double x = targetPosition.X + _trial.DistancePX * Math.Cos(angle);
                double y = targetPosition.Y + _trial.DistancePX * Math.Sin(angle);
                Point candidate = new Point(x, y);

                if (Utils.IsInside(candidate, Xmin, Xmax, Ymin, Ymax))
                    validStartPositions.Add(candidate);
            }

            startPosition = new Point();
            if (validStartPositions.Count() > 0)
            {
                startPosition = validStartPositions[_random.Next(validStartPositions.Count())];
            } else
            {
                TRIAL_LOG.Error("No valid position found!");
            }

            //--- Set start position (random angle based on the distance)


            //bool startPositionValid = false;
            //while (!startPositionValid)
            //{
            //    double theta = RandomAngle(targetPosition);

            //    // Compute target circle position
            //    startPosition.X = targetPosition.X + _activeTrial.DistancePX * Math.Cos(theta);
            //    startPosition.Y = targetPosition.Y + _activeTrial.DistancePX * Math.Sin(theta);
            //    _trialLog.Information(startPosition.ToString());
            //    // Ensure target is inside the window
            //    startPositionValid = Utils.IsInside(startPosition, Xmin, Xmax, Ymin, Ymax);
            //}

            //--- Show the start
            // Convert position (rel. to screen) to window position (for showing)
            startPosition.Offset(-this._relLeft, -this._relTop);
            ShowStart(
                startPosition, Utils.MM2PX(Experiment.START_CIRCLE_WIDTH_MM), Brushes.Green,
                Start_MouseEnter, Start_MouseLeave, Start_MouseDown, Start_MouseUp);
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
                    TRIAL_LOG.Information("HIT");
                }
                else
                {
                    // Don't do anything for now
                    TRIAL_LOG.Information("MISS");
                }
            }

            if (_experiment.IsTechRadiusor())
            {
                Point crossPos = _overlayWindow.GetCrossPosition();
                Point crossScreenPos = _overlayWindow.PointToScreen(crossPos);
                Point crossSideWinPos = _targetSideWindow.PointFromScreen(crossScreenPos);

                GESTURE_LOG.Debug($"Target Dir: {_targetSideWindowDir}");
                GESTURE_LOG.Debug($"Cross OvW Pos: {Output.GetString(crossPos)}");
                GESTURE_LOG.Debug($"Cross SiW Pos: {Output.GetString(crossSideWinPos)}");

                // Trial result
                if (_targetSideWindow.IsPointInsideTarget(crossSideWinPos)) // Cross in target
                {
                    TargetButtonDown();
                    TRIAL_LOG.Information("HIT");
                }
                else
                {
                    // Don't do anything for now
                    TRIAL_LOG.Information("MISS");
                }
            }
  
        }


        private void EndTrial(RESULT result)
        {
            TRIAL_LOG.Information(result.ToString());
            if (_activeTrialNum == _block.GetNumTrials()) // Was last trial
            {
                TRIAL_LOG.Information("End of block");
                if (_activeBlockNum == _experiment.GetNumBlocks()) // Was last block
                {
                    TRIAL_LOG.Information("End of technique");
                } else
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
            TRIAL_LOG.Information("Start MouseDown");

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

                _overlayWindow.ShowLine(cursorScreenPos);
                _radiusorActive = true;
            }

            if (_experiment.Active_Technique == Technique.Mouse)
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


            //if (_timestamps.ContainsKey(Str.START_LAST_RE_ENTRY))
            //{ // Second time
            //    // End trial
            //    _timestamps[Str.TRIAL_END] = _trialtWatch.ElapsedMilliseconds;
            //    double trialTime = (_timestamps[Str.TRIAL_END] - _timestamps[Str.START_UP]) / 1000.0; 
            //    TRIAL_LOG.Information($"Trial Time = {trialTime}");
            //    EndTrial(RESULT.HIT);

            //} else
            //{ // First time


            //}

            //e.Handled = true; // Don't propagate to the window

        }

        private void Start_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TRIAL_LOG.Information("Start MouseUp");

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
                _overlayWindow.HideLine();
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

        private void ShowStart(
            Point position, int width, Brush color, 
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

            // Create the circle
            //_startCircle = new Ellipse
            //{
            //    Width = width,
            //    Height = width,
            //    Fill = color
            //};

            // Position the Start on the Canvas
            //Canvas.SetLeft(_startCircle, position.X - width / 2); // Center alignment
            //Canvas.SetTop(_startCircle, position.Y - width / 2);
            Canvas.SetLeft(_startRectangle, position.X - width / 2); // Center alignment
            Canvas.SetTop(_startRectangle, position.Y - width / 2);

            // Add event
            //_startCircle.MouseEnter += mouseEnterHandler;
            //_startCircle.MouseLeave += mouseLeaveHandler;
            //_startCircle.MouseDown += buttonDownHandler;
            //_startCircle.MouseUp += buttonUpHandler;
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

        private void ActivateSide(Direction dir)
        {
            switch (dir)
            {
                case Direction.Left:
                    _activeSideWindow = _leftWindow;
                    _leftWindow.ActivateCursor();
                    _topWindow.DeactivateCursor();
                    _rightWindow.DeactivateCursor();
                    GESTURE_LOG.Information("Left window activated!");
                    break;
                case Direction.Up:
                    _activeSideWindow = _topWindow;
                    _topWindow.ActivateCursor();
                    _leftWindow.DeactivateCursor();
                    _rightWindow.DeactivateCursor();
                    GESTURE_LOG.Information("Top window activated!");
                    break;
                case Direction.Right:
                    _activeSideWindow = _rightWindow;
                    _rightWindow.ActivateCursor();
                    _leftWindow.DeactivateCursor();
                    _topWindow.DeactivateCursor();
                    GESTURE_LOG.Information("Right window activated!");
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

        public void IndexPointDown(TouchPoint indPoint)
        {
            //if (Technique == 1)
            //{
            //    if (_activeSideWindow != null)
            //    {
            //        _activeSideWindow.MoveAuxPointer(indPoint);
            //    }
            //}
        }

        public void IndexPointMove(TouchPoint indPoint)
        {
            
            if (_experiment.IsTechAuxCursor())
            {
                if (_activeSideWindow != null)
                {
                    _activeSideWindow.MoveAuxPointer(indPoint);
                }
            }

            if (_experiment.IsTechRadiusor() && _radiusorActive)
            {
                _overlayWindow.RotateLine(indPoint);
                // Rotate the Radiusor
                //Point fingerPos = indPoint.GetCenterOfMass();
                //if (_lastRotPointerPos.X == -1)
                //{ // First point
                //  //TRACK_LOG.Information($"First point = {fingerPos:F3}");
                //    _lastRotPointerPos = fingerPos;
                //}
                //else
                //{

                //    // Use both axes to rotate
                //    //double angle = _overlayWindow.GetAngle();

                //    double dX = fingerPos.X - _lastRotPointerPos.X;
                //    double dY = fingerPos.Y - _lastRotPointerPos.Y;

                //    //double weightX = Abs(Cos(angle * PI / 180));
                //    //double weightY = Abs(Sin(angle * PI / 180));

                //    //// Apply a threshold to prevent small last-moment jitters from affecting rotation
                //    //if (Abs(dX) < 0.2 && Abs(dY) < 0.2)
                //    //{
                //    //    return; // Ignore tiny movements right before lift-off
                //    //}

                //    //// Reverse dY effect when the line is on the right side (angle ≤ 90° or angle ≥ 270°)
                //    //if (angle > 90 && angle < 270)
                //    //{
                //    //    dY = -dY; // Flip dY effect on the left side
                //    //}

                //    //double dAngle = (-dY * weightY + dX * weightX) * 10;
                //    //GESTURE_LOG.Information($"Current angle = {angle:F3} | dY = {dY:F3} | dAn = {dAngle:F3}");
                //    //_overlayWindow.RotateLine(dAngle);

                //    if (dX > 0.2 || dY > 0.2) _overlayWindow.RotateLine(dX, dY);

                //    _lastRotPointerPos = fingerPos;
                //}

                // Move the plus
                //Point fingerPos = indPoint.GetCenterOfMass();
                //if (_lastPlusPointerPos.X == -1)
                //{ // First point
                //    GESTURE_LOG.Debug($"Index First point = {fingerPos:F3}");
                //    _lastPlusPointerPos = fingerPos;
                //}
                //else
                //{
                //    // Calculate distance to move the cross
                //    double dX = fingerPos.X - _lastPlusPointerPos.X;
                //    double dY = fingerPos.Y - _lastPlusPointerPos.Y;
                //    _overlayWindow.MovePlus(dX * 10, dY * 10);

                //    _lastPlusPointerPos = fingerPos;
                //}
            }

        }

        public void IndexPointMove(double dX, double dY)
        {
            throw new NotImplementedException();
        }

        public void IndexPointUp()
        {
            //_lastPlusPointerPos.X = -1;
            _lastRotPointerPos.X = -1;
        }

        public void ThumbSwipe(Direction dir)
        {
            if (_experiment.Active_Technique == Technique.Auxursor_Swipe)
            {
                ActivateSide(dir);
            }
            
        }

        public void ThumbMove(TouchPoint thumbPoint)
        {
            if (_experiment.Active_Technique == Technique.Radiusor && _radiusorActive) // Radiusor
            {
                // Move the plus
                _overlayWindow.MovePlus(thumbPoint);
                //Point fingerPos = thumbPoint.GetCenterOfMass();
                //if (_lastPlusPointerPos.X == -1)
                //{ // First point
                //    GESTURE_LOG.Debug($"Index First point = {fingerPos:F3}");
                //    _lastPlusPointerPos = fingerPos;
                //}
                //else
                //{
                //    // Calculate distance to move the cross
                //    double dX = fingerPos.X - _lastPlusPointerPos.X;
                //    double dY = fingerPos.Y - _lastPlusPointerPos.Y;

                //    if (Abs(dX) > 0.2 || Abs(dY) > 0.2)
                //    {
                //        _overlayWindow.MovePlus(dX * 10, dY * 10);
                //    }
                    

                //    _lastPlusPointerPos = fingerPos;
                //}

                // Move the raduisor line
                //Point fingerPos = thumbPoint.GetCenterOfMass();
                //if (_lastRotPointerPos.X == -1)
                //{ // First point
                //  //TRACK_LOG.Information($"First point = {fingerPos:F3}");
                //    _lastRotPointerPos = fingerPos;
                //}
                //else
                //{

                //    // Use both axes to rotate
                //    double angle = _overlayWindow.GetAngle();

                //    double dX = fingerPos.X - _lastRotPointerPos.X;
                //    double dY = fingerPos.Y - _lastRotPointerPos.Y;

                //    double weightX = 0;
                //    double weightY = 0;

                //    if (Abs(dX) > 0.1 || Abs(dY) > 0.1)
                //    {
                //        weightX = Abs(Cos(angle * PI / 180));   
                //    }

                //    if (Abs(dY) > 0.05)
                //    {
                //        weightY = Abs(Sin(angle * PI / 180)); 
                //    }

                //    double dAngle = (-dY * weightY + dX * weightX) * 10; // Adjust rotation based on orientation
                //    _overlayWindow.RotateLine(dAngle);

                //    _lastRotPointerPos = fingerPos;
                //}


            }
        }

        public void ThumbUp(TouchPoint indPoint)
        {
            //_lastRotPointerPos.X = -1;
            _lastPlusPointerPos.X = -1;
        }

        public void ThumbTap()
        {
            if (_experiment.Active_Technique == Technique.Auxursor_Tap)
            {
                GESTURE_LOG.Information("Thumb Tapped!");
                ActivateSide(Direction.Left);
            }
            
        }

        public void MiddleTap()
        {
            if (_experiment.Active_Technique == Technique.Auxursor_Tap)
            {
                GESTURE_LOG.Information("Middle Tapped!");
                ActivateSide(Direction.Up);
            }
        }

        public void RingTap()
        {
            if (_experiment.Active_Technique == Technique.Auxursor_Tap)
            {
                GESTURE_LOG.Information("Ring Tapped!");
                ActivateSide(Direction.Right);
            }
        }
    }
}
