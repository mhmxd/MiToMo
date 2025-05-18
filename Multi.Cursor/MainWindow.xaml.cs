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
using Seril = Serilog.Log;
using Serilog;
using MessageBox = System.Windows.Forms.MessageBox;
using System.Numerics;
using System.Threading.Tasks;
using System.Runtime.CompilerServices; // Alias Serilog's Log class

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

        private double INFO_LABEL_BOTTOM_RATIO = 0.02; // of the height from the bottom

        private int PADDING = Utils.MM2PX(Config.WINDOW_PADDING_MM); // Padding for the windows


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

        private double _monitorHeightMM;

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
        private bool _auxursorFreezed = true;

        private Rect _mainWinRect, _leftWinRect, _topWinRect, _rightWinRect;
        private Rect _lefWinRectPadded, _topWinRectPadded, _rightWinRectPadded;
        private int  _infoLabelHeight;

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
        private int _auxursorSpeed = 0; // 0: normal, 1: fast (for Swipe)
        //private Enums _targetSideWindowDir;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize logging
            Output.Init();

            // Initialize random
            _random = new Random();

            TouchMouseSensorEventManager.Handler += TouchMouseSensorHandler;

            InitializeWindows();

            UpdateLabel();

            //-- Events
            this.MouseMove += Window_MouseMove;
            this.MouseDown += Window_MouseDown;
            this.MouseUp += Window_MouseUp;
            this.MouseWheel += Window_MouseWheel;

            this.KeyDown += Window_KeyDown;
            
            _leftWindow.MouseMove += Window_MouseMove;
            _rightWindow.MouseMove += Window_MouseMove;
            _topWindow.MouseMove += Window_MouseMove;

            MouseLeftButtonDown += Window_MouseLeftButtonDown;

            //-- Create a default Experiment
            double longestDistMM =
                _monitorHeightMM - (2 * Config.WINDOW_PADDING_MM)
                - (Experiment.START_WIDTH_MM / 2) - (Experiment.Max_Target_Width_MM / 2);
            double shortestDistMM = 2 * Config.WINDOW_PADDING_MM
                + (Experiment.START_WIDTH_MM / 2) + (Experiment.Max_Target_Width_MM / 2);
            PositionInfo<MainWindow>($"Monitor H = {_monitorHeightMM} | Min D = {shortestDistMM} | Max D = {longestDistMM}");
            _experiment = new Experiment(shortestDistMM, longestDistMM);
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
                // Set the technique mode in Config
                _experiment.Init(introDialog.ParticipantNumber, introDialog.Technique);
                BeginTechnique();
            }

        }

        private void UpdateLabel()
        {
            if (canvas != null && infoLabel != null)
            {
                // 1/10th of the height from the bottom
                Canvas.SetBottom(infoLabel, canvas.ActualHeight * INFO_LABEL_BOTTOM_RATIO);

                // Center horizontally
                Canvas.SetLeft(infoLabel, (canvas.ActualWidth - infoLabel.ActualWidth) / 2);
                //Canvas.SetLeft(infoLabel, 400);

                if (!canvas.Children.Contains(infoLabel)) canvas.Children.Add(infoLabel);
            }
        }

        private void InfoLabel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateLabel(); // Reposition when the label's size changes (due to text update)
        }

        private void Window_KeyDown(object sender, SysIput.KeyEventArgs e)
        {
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

            //switch (e.Key)
            //{
            //    case SysIput.Key.Right:
            //        _overlayWindow.RotateBeam(2, 0);
            //        break;
            //    case SysIput.Key.Left:
            //        _overlayWindow.RotateBeam(-2, 0);
            //        break;
            //    case SysIput.Key.Up:
            //        _overlayWindow.RotateBeam(0, -2);
            //        break;
            //    case SysIput.Key.Down:
            //        _overlayWindow.RotateBeam(0, 2);
            //        break;
            //}


        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
             
            //-- Check if the simucursor is inside target
            if (_targetSideWindow.IsCursorInsideTarget())
            { // Simucursor inside target
                TargetMouseDown();
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

                Outlog<MainWindow>().Information($"Monitor WorkingArea H = {Config.ACTIVE_SCREEN.WorkingArea.Height}");
                Outlog<MainWindow>().Information($"BackgroundWindow Actual H (after maximize) = {_backgroundWindow.ActualHeight}");

                // Set the height as mm
                //_monitorHeightMM = Utils.PX2MM(Config.ACTIVE_SCREEN.WorkingArea.Height);
                _monitorHeightMM = 335;
                Outlog<MainWindow>().Information($"Monitor H = {Config.ACTIVE_SCREEN.WorkingArea.Height}");

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
                this._mainWinRect = Utils.GetRect(this);
                _infoLabelHeight = (int)(this.ActualHeight * INFO_LABEL_BOTTOM_RATIO + infoLabel.ActualHeight);

                // Create top window
                _topWindow = new SideWindow("Top Window", new Point(_sideWindowSize, 0));
                _topWindow.Background = Config.GRAY_F3F3F3;
                _topWindow.Height = _sideWindowSize;
                _topWindow.Width = Config.ACTIVE_SCREEN.WorkingArea.Width;
                _topWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                _topWindow.Left = Config.ACTIVE_SCREEN.WorkingArea.Left;
                _topWindow.Top = Config.ACTIVE_SCREEN.WorkingArea.Top;
                _topWindow.MouseDown += SideWindow_MouseDown;
                _topWindow.MouseUp += SideWindow_MouseUp;
                _topWindow.Show();
                _topWinRect = Utils.GetRect(_topWindow);
                _topWinRectPadded = Utils.GetRect(_topWindow, PADDING);

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
                _leftWindow.MouseDown += SideWindow_MouseDown;
                _leftWindow.MouseUp += SideWindow_MouseUp;
                _leftWindow.Show();
                _leftWinRect = Utils.GetRect(_leftWindow);
                _lefWinRectPadded = Utils.GetRect(_leftWindow, PADDING);

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
                _rightWindow.MouseDown += SideWindow_MouseDown;
                _rightWindow.MouseUp += SideWindow_MouseUp;
                _rightWindow.Show();
                _rightWinRect = Utils.GetRect(_rightWindow);
                _rightWinRectPadded = Utils.GetRect(_rightWindow, PADDING);

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

        private void SideWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_timestamps.ContainsKey(Str.FIRST_MOVE)) // Trial is officially started (to prevent accidental click at the beginning)
            {
                if (_timestamps.ContainsKey(Str.START1_RELEASE)) // Phase 2: Aiming for Target (missing because Target is not pressed)
                {
                    EndTrial(RESULT.MISS);
                }
                else // Phase 1: Aiming for Start (missing because here is Window!)
                {
                    EndTrial(RESULT.NO_START);
                }
            }
        }

        private void SideWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_timestamps.ContainsKey(Str.TARGET_PRESS)) // Released outside target
            {
                EndTrial(RESULT.MISS);
            }
        }

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            //AdjustWindowPositions();
        }

        private void Window_TouchDown(object sender, TouchEventArgs e)
        {
            var touchPoint = e.GetTouchPoint(this);
            Seril.Debug($"TouchDown at ({touchPoint.Position.X}, {touchPoint.Position.Y})");
        }

        private void Window_TouchUp(object sender, TouchEventArgs e)
        {
            var touchPoint = e.GetTouchPoint(this);
            Seril.Debug($"TouchUp at ({touchPoint.Position.X}, {touchPoint.Position.Y})");
        }

        private void Window_TouchMove(object sender, TouchEventArgs e)
        {
            var touchPoint = e.GetTouchPoint(this);
            Seril.Debug($"TouchMove at ({touchPoint.Position.X}, {touchPoint.Position.Y})");
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TrialInfo<MainWindow>($"{_timestamps.Stringify()}");
            if (_timestamps.ContainsKey(Str.FIRST_MOVE)) // Trial is officially started (to prevent accidental click at the beginning)
            {
                if (_timestamps.ContainsKey(Str.TARGET_RELEASE)) // Phase 3: Window press => Outside Start
                {
                    EndTrial(RESULT.MISS);
                }
                else if (_timestamps.ContainsKey(Str.START1_RELEASE)) // Phase 2: Aiming for Target
                {

                    if (_targetSideWindow.IsCursorInsideTarget()) // Auxursor in target
                    {
                        TargetMouseDown();
                    }
                    else // Pressed outside target => MISS
                    {
                        EndTrial(RESULT.MISS);
                    }
                }
                else // Phase 1: Aiming for Start (missing because here is Window!)
                {
                    //EndTrial(RESULT.NO_START);
                    EndTrial(RESULT.NO_START);
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
                    TargetMouseDown();
                }
                else
                {
                    // Nothing for now
                }

                // Release the cursor
                _cursorFreezed = !UnfreezeCursor();
            }
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _timestamps.TryAdd(Str.FIRST_MOVE, _stopWatch.ElapsedMilliseconds);
            //bool notMovedYet = !_timestamps.ContainsKey(Str.FIRST_MOVE);
            //if (notMovedYet)
            //{
            //    _timestamps[Str.FIRST_MOVE] = _stopWatch.ElapsedMilliseconds;
            //}

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

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TrialInfo<MainWindow>($"{_timestamps.Stringify()}");
            if (_timestamps.ContainsKey(Str.TARGET_PRESS)) // Target is pressed => Was release outside or inside?
            {
                if (_activeSideWindow.IsCursorInsideTarget()) // Released inside Target => Next phase
                {
                    // Add timestamp
                    _timestamps.TryAdd(Str.TARGET_RELEASE, _trialtWatch.ElapsedMilliseconds);

                    // Deactive Target and auxursor
                    _activeSideWindow.ColorTarget(Brushes.Red);
                    _activeSideWindow.DeactivateCursor();
                    
                    // Activate Start again
                    _startRectangle.Fill = Brushes.Green;
                } else // Released outside Target => MISS
                {
                    EndTrial(RESULT.MISS);
                }

            } 
            else if (_timestamps.ContainsKey(Str.START1_PRESS)) // Pressed inside, but released outside Start => MISS
            {
                EndTrial(RESULT.MISS);
            }
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
            
            return Utils.Offset(cursorPos, _leftWindow.Width, _topWindow.Height);
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
                if (_touchSurface == null) _touchSurface = new TouchSurface(this, _experiment.Active_Technique);

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

        private void BeginTechnique()
        {
            Outlog<MainWindow>().Information($"Auxursor? {_experiment.IsTechAuxCursor()}");
            if (_experiment.IsTechAuxCursor()) // Touch-mouse techniques
            {
                _touchMouseActive = true;
                ShowAxursors();
            }
            else // Mouse
            {
                _touchMouseActive = false;
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
            // Begin
            _activeBlockNum = 1;
            _block = _experiment.GetBlock(_activeBlockNum);
            if (_block != null) // Got the block
            {
                _activeTrialNum = 1;
                _trial = _block.GetTrial(_activeTrialNum);
                if (_trial != null) ShowTrial();
            }
            else // Block was null for some reason
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

        public bool FindPositionsForAllBlocks()
        { 
            // Go through all blocks
            foreach (Block block in _experiment.Blocks)
            {
                bool positionsFound = FindPosForTrials(block); // Try to find valid positions for all trials in block
                if (!positionsFound) return false; // No valid positions found for this block
            }

            // Check
            TrialInfo<MainWindow>($"Block#2: {_experiment.GetBlock(2).GetTrial(1).StartPosition.ToStr()}");

            return true; // All blocks have valid positions
        }


        /// <summary>
        /// Find positions for all the trials in the block
        /// </summary>
        private bool FindPosForTrials(Block block)
        {
            
            // (Positions all relative to screen)
            int padding = Utils.MM2PX(Config.WINDOW_PADDING_MM);
            int startW = Utils.MM2PX(Experiment.START_WIDTH_MM);
            int startHalfW = startW / 2;

            foreach (Trial trial in block.Trials) // Go through trials
            {
                PositionInfo<MainWindow>($"Finding positions for Trial#{trial.Id} [Target = {trial.TargetLocation}, D = {trial.DistanceMM:F2}]");
                int targetW = Utils.MM2PX(trial.TargetWidthMM);
                int targetHalfW = targetW / 2;
                int dist = trial.DistancePX;

                // Set the active side window and find Start and Target positions
                Point startPositionInMainWin = new Point();
                Point targetPositionInSideWin = new Point();
                List<Point> jumpPositions = new List<Point>();

                // Get the bounds for the Start and Target
                Rect startCenterBounds = new Rect(
                    _mainWinRect.Left + padding + startHalfW,
                    _mainWinRect.Top + padding + startHalfW,
                    _mainWinRect.Width - 2 * (padding + startHalfW),
                    _mainWinRect.Height - 2 * (padding + startHalfW) - _infoLabelHeight);

                int nRetries = 100;
                bool validPosFound = false;

                for (int t = 0; t < nRetries; t++)
                {
                    switch (trial.TargetLocation)
                    {
                        case Location.Left:
                            _targetSideWindow = _leftWindow;
                            Rect targetCenterBounds = new Rect(
                                _leftWinRect.Left + padding + targetHalfW,
                                _leftWinRect.Top + padding + targetHalfW,
                                _leftWinRect.Width - 2 * padding - targetHalfW,
                                _leftWinRect.Height - 2 * padding - targetHalfW);
                            jumpPositions.Add(new Point(
                                _leftWinRect.Left + _leftWinRect.Width / 2, 
                                _leftWinRect.Top + _leftWinRect.Height / 4)); // Top
                            jumpPositions.Add(new Point(
                                _leftWinRect.Left + _leftWinRect.Width / 2, 
                                _leftWinRect.Top + _leftWinRect.Height * 3 / 4)); // Bottom

                            (startPositionInMainWin, targetPositionInSideWin) = LeftTargetPositionElements(
                                startW, targetW, dist, startCenterBounds, targetCenterBounds, jumpPositions);

                            break;

                        case Location.Right:
                            _targetSideWindow = _rightWindow;
                            targetCenterBounds = new Rect(
                                _rightWinRect.Left + padding + targetHalfW,
                                _rightWinRect.Top + padding + targetHalfW,
                                _rightWinRect.Width - 2 * padding - targetHalfW,
                                _rightWinRect.Height - 2 * padding - targetHalfW);
                            jumpPositions.Add(new Point(
                                _rightWinRect.Left + _rightWinRect.Width / 2,
                                _rightWinRect.Top + _rightWinRect.Height / 4)); // Top
                            jumpPositions.Add(new Point(
                                _rightWinRect.Left + _rightWinRect.Width / 2,
                                _rightWinRect.Top + _rightWinRect.Height * 3 / 4)); // Bottom

                            (startPositionInMainWin, targetPositionInSideWin) = RightTargetPositionElements(
                                startW, targetW, dist, startCenterBounds, targetCenterBounds, jumpPositions);
                            // Check target position
                            //if (!_rightWinRect.Contains(targetPositionInSideWin)) // Target not properly positioned
                            //{
                            //    continue;
                            //}

                            break;

                        case Location.Top:
                            _targetSideWindow = _topWindow;
                            targetCenterBounds = new Rect(
                                _topWinRect.Left + padding + targetHalfW,
                                _topWinRect.Top + padding + targetHalfW,
                                _topWinRect.Width - 2 * padding - targetHalfW,
                                _topWinRect.Height - 2 * padding - targetHalfW);
                            jumpPositions.Add(new Point(
                                _topWinRect.Left + _topWinRect.Width / 4, 
                                _topWinRect.Top + _topWinRect.Height / 2)); // Left
                            jumpPositions.Add(new Point(
                                _topWinRect.Left + _topWinRect.Width / 2,
                                _topWinRect.Top + _topWinRect.Height / 2)); // Middle
                            jumpPositions.Add(new Point(
                                _topWinRect.Left + _topWinRect.Width * 3 / 4,
                                _topWinRect.Top + _topWinRect.Height / 2)); // Right

                            (startPositionInMainWin, targetPositionInSideWin) = TopTargetPositionElements(
                                startW, targetW, dist, startCenterBounds, targetCenterBounds, jumpPositions);
                            // Check target position
                            //if (!_topWinRect.Contains(targetPositionInSideWin)) // Target not properly positioned
                            //{
                            //    Outlog<MainWindow>().Error($"Target position not valid for Trial#{trial.Id} - {_topWinRect}");
                            //    continue;
                            //}

                            break;
                    }

                    if (startPositionInMainWin.X == 0) return false; // Failed to find a valid position

                    PositionInfo<MainWindow>("----------- Valid positions found! ------------");
                    trial.StartPosition = startPositionInMainWin;
                    trial.TargetPosition = targetPositionInSideWin;
                    validPosFound = true;
                    PositionInfo<MainWindow>($"St.P: {Output.GetString(startPositionInMainWin)}");
                    PositionInfo<MainWindow>($"{trial.TargetLocation.ToString()} " +
                        $"-- Tgt.P: {Output.GetString(targetPositionInSideWin)}");
                    break;

                    // Check if the Start positions are valid
                    //Outlog<MainWindow>().Information($"Main Rect: {_mainWinRect.GetCorners()}");
                    //if (_mainWinRect.Contains(startPositionInMainWin)) // Valid positions found
                    //{
                    //    Outlog<MainWindow>().Information("Valid positions found! ----------------------");
                    //    trial.StartPosition = startPositionInMainWin;
                    //    trial.TargetPosition = targetPositionInSideWin;
                    //    validPosFound = true;
                    //    Outlog<MainWindow>().Debug($"St.P: {Output.GetString(startPositionInMainWin)}");
                    //    Outlog<MainWindow>().Debug($"{trial.TargetLocation.ToString()} " +
                    //        $"-- Tgt.P: {Output.GetString(targetPositionInSideWin)}");
                    //    break;
                    //} 

                }

                if (!validPosFound) // No valid position found for this trial (after retries)
                {
                    PositionInfo<MainWindow>($"Couldn't find a valid Start position for Trial#{trial.Id} - {trial.TargetLocation}");
                    return false;
                }
                
            }

            PositionInfo<MainWindow>($"Block#{block.Id}: Valid positions found for all trials.");
            return true; // All trials have valid positions
        }

        private void GoToNextBlock()
        {
            _activeBlockNum++;
            _block = _experiment.GetBlock(_activeBlockNum);
            _activeTrialNum = 1;
            _trial = _block.GetTrial(_activeTrialNum);
            ShowTrial();
        }

        private void GoToNextTrial()
        {
            TrialInfo<MainWindow>($"Block#{_activeBlockNum}: {_block.ToString()}");
            _activeTrialNum++;
            _trial = _block.GetTrial(_activeTrialNum);
            ShowTrial();
        }

        private void ShowTrial()
        {
            // Clear everything
            ClearCanvas();
            ClearSideWindowCanvases();
            _timestamps.Clear();

            // If Auxursor => Deactivate all
            if (_experiment.IsTechAuxCursor()) DeactivateAuxursors();

            // Show the info
            TrialInfo<MainWindow>("---------------------------------------------------------------------");
            TrialInfo<MainWindow>($"Trial#{_trial.Id} | Direction = {_trial.TargetLocation} | Dist = {_trial.DistanceMM:F2}");
            infoLabel.Text = $"Trial {_activeTrialNum} | Block {_activeBlockNum} ";
            UpdateLabel();
            
            // Start the stopwatch
            _trialtWatch.Restart();

            // Add trial show timestamp
            _timestamps[Str.TRIAL_SHOW] = _trialtWatch.ElapsedMilliseconds;

            // Show Start and Target
            TrialInfo<MainWindow>($"Start position: {_trial.StartPosition}");
            int startW = Utils.MM2PX(Experiment.START_WIDTH_MM);
            int targetW = Utils.MM2PX(_trial.TargetWidthMM);
            // Start logging
            ToMoLogger.StartTrialGesturesLog(_trial.Id, _trial.TargetWidthMM, _trial.DistanceMM, _trial.StartPosition, _trial.TargetPosition);
            // Show Start
            ShowStart(_trial.StartPosition, startW, Brushes.Green,
                Start_MouseEnter, Start_MouseLeave, Start_MouseDown, Start_MouseUp);

            // Set the target window and show the Target
            switch (_trial.TargetLocation)
            {
                case Location.Left: _targetSideWindow = _leftWindow; break;
                case Location.Right: _targetSideWindow = _rightWindow; break;
                case Location.Top: _targetSideWindow = _topWindow; break;
            }

            _targetSideWindow.ShowTarget(_trial.TargetPosition, targetW, Config.GRAY_A0A0A0,
                Target_MouseEnter, Target_MouseLeave, Target_MouseDown, Target_MouseUp);
        }

        private (Point, Point) LeftTargetPositionElements(
            int startW, int targetW, 
            int dist, Rect startCenterBounds, Rect targetCenterBounds,
            List<Point> jumpPositions)
        {
            Point startCenterPosition = new Point();
            Point targetCenterPosition = new Point();
            Point startPosition = new Point();
            int startHalfW = startW / 2;
            int targetHalfW = targetW / 2;

            //--- v.5
            int nRetries = 100;
            for (int retry = 0; retry < nRetries; retry++)
            {
                //int stYMinPossible = (int)(
                //    targetCenterBounds.Top + Sqrt(Pow(dist, 2)
                //    - Pow(startCetnerBounds.Right - targetCenterBounds.Right, 2)));
                //int stYMin = (int)Max(stYMinPossible, startCetnerBounds.Top);
                //int stYMax = (int)startCetnerBounds.Bottom;
                //int stPosY = _random.Next(stYMin, stYMax);

                //int stXMinPossible = (int)(
                //    targetCenterBounds.Left + Sqrt(Pow(dist, 2)
                //    - Pow(startCetnerBounds.Top - targetCenterBounds.Top, 2)));
                //int stXMin = (int)Max(stXMinPossible, startCetnerBounds.Left);
                //int stXMax = (int)Min(targetCenterBounds.Right + dist, startCetnerBounds.Right);
                //int stPosX = _random.Next(stXMin, stXMax);

                //int possibleStartYMin = (int)(targetCenterBounds.Top - dist);
                //int possibleStartYMax = (int)(targetCenterBounds.Bottom + dist);

                int stYMin = (int)startCenterBounds.Top;
                int stYMax = (int)startCenterBounds.Bottom;

                int stPosY = _random.Next(stYMin, stYMax); // Add +1 because Next() is exclusive of the upper bound

                int possibleStartXMin = (int)(targetCenterBounds.Left - dist);
                int possibleStartXMax = (int)(targetCenterBounds.Right + dist);

                int stXMin = (int)Max(targetCenterBounds.Left + dist, startCenterBounds.Left);
                int stXMax = (int)Min(targetCenterBounds.Right + dist, startCenterBounds.Right);

                int stPosX = _random.Next(stXMin, stXMax + 1); // Add +1 because Next() is exclusive of the upper bound

                PositionInfo<MainWindow>($"--- Found Start: {stPosX}, {stPosY}");

                // Choose a random Y for target
                int tgRandY = _random.Next((int)targetCenterBounds.Top, (int)targetCenterBounds.Bottom);

                // Solve for X
                int tgPossibleX1 = stPosX + (int)(Sqrt(Pow(dist, 2) - Pow(stPosY - tgRandY, 2)));
                int tgPossibleX2 = stPosX - (int)(Sqrt(Pow(dist, 2) - Pow(stPosY - tgRandY, 2)));

                //Outlog<MainWindow>().Information($"Possible Target 1: {tgPossibleX1}, {tgRandY}");
                //Outlog<MainWindow>().Information($"Possible Target 2: {tgPossibleX2}, {tgRandY}");
                PositionInfo<MainWindow>($"Target Center Bounds: {targetCenterBounds.GetCorners()}");

                // Check which possible X positions are within the target bounds and doesn't lie inside jump positions
                Rect possibleTarget1 = new Rect(
                    tgPossibleX1 - targetHalfW,
                    tgRandY - targetHalfW,
                    targetW, targetW);
                Rect possibleTarget2 = new Rect(
                    tgPossibleX2 - targetHalfW,
                    tgRandY - targetHalfW,
                    targetW, targetW);
                PositionInfo<MainWindow>($"Possible Tgt1: {possibleTarget1.GetCorners()}");
                PositionInfo<MainWindow>($"Possible Tgt2: {possibleTarget2.GetCorners()}");
                //Outlog<MainWindow>().Information($"Target Window Bounds: {_leftWindow.GetCorners(padding)}");
                bool isPos1Valid = 
                    _lefWinRectPadded.Contains(possibleTarget1)
                    && Utils.ContainsNot(possibleTarget1, jumpPositions);
                bool isPos2Valid =
                    _lefWinRectPadded.Contains(possibleTarget2)
                    && Utils.ContainsNot(possibleTarget2, jumpPositions);

                PositionInfo<MainWindow>($"Position 1 valid: {isPos1Valid}");
                PositionInfo<MainWindow>($"Position 2 valid: {isPos2Valid}");

                if (isPos1Valid && !isPos2Valid) // Only position 1 is valid
                {
                    startCenterPosition = new Point(stPosX, stPosY);
                    //targetCenterPosition = new Point(tgPossibleX1, tgRandY);

                    // Convert to top-left and respective window coordinates
                    startPosition = Utils.Offset(startCenterPosition, -startHalfW, -startHalfW);
                    Point startPositionInMainWin = Utils.Offset(startPosition,
                        -_mainWinRect.Left,
                        -_mainWinRect.Top);
                    //Point targetPosition = Utils.Offset(targetCenterPosition, -targetHalfW, -targetHalfW);
                    Point targetPosition = possibleTarget1.TopLeft;
                    Point targetPositionInSideWin = Utils.Offset(targetPosition,
                        -_leftWinRect.Left,
                        -_leftWinRect.Top);
                    PositionInfo<MainWindow>($"Found -> Start: {startPositionInMainWin} - Target: {targetPositionInSideWin}");
                    return (startPositionInMainWin, targetPositionInSideWin);

                }
                else if (!isPos1Valid && isPos2Valid) // Only position 2 is valid
                {
                    startCenterPosition = new Point(stPosX, stPosY);
                    //targetCenterPosition = new Point(tgPossibleX2, tgRandY);

                    // Convert to top-left and respective window coordinates
                    startPosition = Utils.Offset(startCenterPosition, -startHalfW, -startHalfW);
                    Point startPositionInMainWin = Utils.Offset(startPosition,
                        -_mainWinRect.Left,
                        -_mainWinRect.Top);
                    //Point targetPosition = Utils.Offset(targetCenterPosition, -targetHalfW, -targetHalfW);
                    Point targetPosition = possibleTarget2.TopLeft;
                    Point targetPositionInSideWin = Utils.Offset(targetPosition,
                        -_leftWinRect.Left,
                        -_leftWinRect.Top);
                    PositionInfo<MainWindow>($"Found -> Start: {startPositionInMainWin} - Target: {targetPositionInSideWin}");
                    return (startPositionInMainWin, targetPositionInSideWin);
                }
                else if (isPos1Valid && isPos2Valid) // Both positions are valid => choose one by random
                {
                    startCenterPosition = new Point(stPosX, stPosY);
                    //targetCenterPosition = new Point(_random.NextDouble() < 0.5 ? tgPossibleX1 : tgPossibleX2, tgRandY);

                    // Convert to top-left and respective window coordinates
                    startPosition = Utils.Offset(startCenterPosition, -startHalfW, -startHalfW);
                    Point startPositionInMainWin = Utils.Offset(startPosition,
                        -_mainWinRect.Left,
                        -_mainWinRect.Top);
                    //Point targetPosition = Utils.Offset(targetCenterPosition, -targetHalfW, -targetHalfW);
                    Point targetPosition = _random.NextDouble() < 0.5 ? possibleTarget1.TopLeft : possibleTarget2.TopLeft;
                    Point targetPositionInSideWin = Utils.Offset(targetPosition,
                        -_leftWinRect.Left,
                        -_leftWinRect.Top);
                    PositionInfo<MainWindow>($"Found -> Start: {startPositionInMainWin} - Target: {targetPositionInSideWin}");
                    return (startPositionInMainWin, targetPositionInSideWin);
                }
            }

            PositionInfo<MainWindow>("Failed to find a valid placement within the retry limit.");
            return (new Point(), new Point()); // Indicate failure
        }

        private (Point, Point) RightTargetPositionElements(
            int startW, int targetW,
            int dist, Rect startCenterBounds, Rect targetCenterBounds,
            List<Point> jumpPositions)
        {
            Point startCenterPosition = new Point();
            Point targetCenterPosition = new Point();
            Point startPosition = new Point();
            int startHalfW = startW / 2;
            int targetHalfW = targetW / 2;

            //--- v.5
            int nRetries = 100;
            for (int retry = 0; retry < nRetries; retry++)
            {
                //int stYMinPossible = (int)(
                //    targetCenterBounds.Top + Sqrt(Pow(dist, 2)
                //    - Pow(startCenterBounds.Left - targetCenterBounds.Left, 2)));
                //int stYMin = (int)Max(stYMinPossible, startCenterBounds.Top);
                int stYMin = (int)startCenterBounds.Top;
                int stYMax = (int)startCenterBounds.Bottom;
                int stPosY = _random.Next(stYMin, stYMax);

                int stXMin = (int)Max(targetCenterBounds.Left - dist, startCenterBounds.Left);
                //int stXMaxPossible = (int)(
                //    targetCenterBounds.Right + Sqrt(Pow(dist, 2)
                //    - Pow(startCenterBounds.Top - targetCenterBounds.Top, 2)));
                int stXMax = (int)Min(targetCenterBounds.Right - dist, startCenterBounds.Right);
                //int stXMax = (int)Min(stXMaxPossible, startCenterBounds.Right);
                int stPosX = _random.Next(stXMin, stXMax);

                PositionInfo<MainWindow>($"Found Start: {stPosX}, {stPosY}");

                // Choose a random Y for target
                int tgRandY = _random.Next((int)targetCenterBounds.Top, (int)targetCenterBounds.Bottom);

                // Solve for X
                int tgPossibleX1 = stPosX + (int)(Sqrt(Pow(dist, 2) - Pow(stPosY - tgRandY, 2)));
                int tgPossibleX2 = stPosX - (int)(Sqrt(Pow(dist, 2) - Pow(stPosY - tgRandY, 2)));

                //Outlog<MainWindow>().Information($"Possible Target 1: {tgPossibleX1}, {tgRandY}");
                //Outlog<MainWindow>().Information($"Possible Target 2: {tgPossibleX2}, {tgRandY}");
                PositionInfo<MainWindow>($"Target Center Bounds: {targetCenterBounds.GetCorners()}");

                // Check which possible X positions are within the target bounds and doesn't lie inside jump positions
                Rect possibleTarget1 = new Rect(
                    tgPossibleX1 - targetHalfW,
                    tgRandY - targetHalfW,
                    targetW, targetW);
                Rect possibleTarget2 = new Rect(
                    tgPossibleX2 - targetHalfW,
                    tgRandY - targetHalfW,
                    targetW, targetW);
                PositionInfo<MainWindow>($"Possible Tgt1: {possibleTarget1.GetCorners()}");
                PositionInfo<MainWindow>($"Possible Tgt2: {possibleTarget2.GetCorners()}");
                //Outlog<MainWindow>().Information($"Target Window Bounds: {_rightWindow.GetCorners(padding)}");

                bool isPos1Valid =
                    _rightWinRectPadded.Contains(possibleTarget1)
                    && Utils.ContainsNot(possibleTarget1, jumpPositions);
                bool isPos2Valid =
                    _rightWinRectPadded.Contains(possibleTarget2)
                    && Utils.ContainsNot(possibleTarget2, jumpPositions);

                PositionInfo<MainWindow>($"Position 1 valid: {isPos1Valid}");
                PositionInfo<MainWindow>($"Position 2 valid: {isPos2Valid}");

                if (isPos1Valid && !isPos2Valid) // Only position 1 is valid
                {
                    startCenterPosition = new Point(stPosX, stPosY);
                    //targetCenterPosition = new Point(tgPossibleX1, tgRandY);

                    // Convert to top-left and respective window coordinates
                    startPosition = Utils.Offset(startCenterPosition, -startHalfW, -startHalfW);
                    Point startPositionInMainWin = Utils.Offset(startPosition,
                        -_mainWinRect.Left,
                        -_mainWinRect.Top);
                    //Point targetPosition = Utils.Offset(targetCenterPosition, -targetHalfW, -targetHalfW);
                    Point targetPosition = possibleTarget1.TopLeft;
                    Point targetPositionInSideWin = Utils.Offset(targetPosition,
                        -_rightWinRect.Left,
                        -_rightWinRect.Top);
                    PositionInfo<MainWindow>($"Found -> Start: {startPositionInMainWin} - Target: {targetPositionInSideWin}");
                    return (startPositionInMainWin, targetPositionInSideWin);

                }
                else if (!isPos1Valid && isPos2Valid) // Only position 2 is valid
                {
                    startCenterPosition = new Point(stPosX, stPosY);
                    //targetCenterPosition = new Point(tgPossibleX2, tgRandY);

                    // Convert to top-left and respective window coordinates
                    startPosition = Utils.Offset(startCenterPosition, -startHalfW, -startHalfW);
                    Point startPositionInMainWin = Utils.Offset(startPosition,
                        -_mainWinRect.Left,
                        -_mainWinRect.Top);
                    //Point targetPosition = Utils.Offset(targetCenterPosition, -targetHalfW, -targetHalfW);
                    Point targetPosition = possibleTarget2.TopLeft;
                    Point targetPositionInSideWin = Utils.Offset(targetPosition,
                        -_rightWinRect.Left,
                        -_rightWinRect.Top);
                    PositionInfo<MainWindow>($"Found -> Start: {startPositionInMainWin} - Target: {targetPositionInSideWin}");
                    return (startPositionInMainWin, targetPositionInSideWin);
                }
                else if (isPos1Valid && isPos2Valid) // Both positions are valid => choose one by random
                {
                    startCenterPosition = new Point(stPosX, stPosY);
                    //targetCenterPosition = new Point(_random.NextDouble() < 0.5 ? tgPossibleX1 : tgPossibleX2, tgRandY);

                    // Convert to top-left and respective window coordinates
                    startPosition = Utils.Offset(startCenterPosition, -startHalfW, -startHalfW);
                    Point startPositionInMainWin = Utils.Offset(startPosition,
                        -_mainWinRect.Left,
                        -_mainWinRect.Top);
                    //Point targetPosition = Utils.Offset(targetCenterPosition, -targetHalfW, -targetHalfW);
                    Point targetPosition = _random.NextDouble() < 0.5 ? possibleTarget1.TopLeft : possibleTarget2.TopLeft;
                    Point targetPositionInSideWin = Utils.Offset(targetPosition,
                        -_rightWinRect.Left,
                        -_rightWinRect.Top);
                    PositionInfo<MainWindow>($"Found -> Start: {startPositionInMainWin} - Target: {targetPositionInSideWin}");
                    return (startPositionInMainWin, targetPositionInSideWin);
                }
            }

            PositionInfo<MainWindow>("Failed to find a valid placement within the retry limit.");
            return (new Point(), new Point()); // Indicate failure

        }

        private (Point, Point) TopTargetPositionElements(
            int startW, int targetW,
            int dist, Rect startCenterBounds, Rect targetCenterBounds,
            List<Point> jumpPositions)
        {
            Point targetCenterPosition = new Point();
            Point startCenterPosition = new Point();
            Point startPosition = new Point();
            int startHalfW = startW / 2;
            int targetHalfW = targetW / 2;

            //--- v.5
            PositionInfo<MainWindow>($"Start center bounds: {startCenterBounds.GetCorners()}");
            int nRetries = 100;
            for (int retry = 0; retry < nRetries; retry++)
            {
                int stXMin = (int)startCenterBounds.Left;
                int stXMax = (int)startCenterBounds.Right;
                //int stYMinPossible = (int)(
                //    targetCenterBounds.Top + 
                //    Sqrt(Pow(dist, 2) - Pow(startCetnerBounds.Left - targetCenterBounds.Right, 2)));
                //int stYMin = (int)Max(stYMinPossible, startCetnerBounds.Top);
                int stYMin = (int)startCenterBounds.Top;
                int stYMax = (int)Min(targetCenterBounds.Bottom + dist, startCenterBounds.Bottom);

                int stPosY = _random.Next(stYMin, stYMax);
                int stPosX = _random.Next(stXMin, stXMax);

                PositionInfo<MainWindow>($"Found Start: {stPosX}, {stPosY}");

                // Choose a random Y for target
                int tgRandY = _random.Next((int)targetCenterBounds.Top, (int)targetCenterBounds.Bottom);

                // Solve for X
                int tgPossibleX1 = stPosX + (int)(Sqrt(Pow(dist, 2) - Pow(stPosY - tgRandY, 2)));
                int tgPossibleX2 = stPosX - (int)(Sqrt(Pow(dist, 2) - Pow(stPosY - tgRandY, 2)));

                //Outlog<MainWindow>().Information($"Possible Target 1: {tgPossibleX1}, {tgRandY}");
                //Outlog<MainWindow>().Information($"Possible Target 2: {tgPossibleX2}, {tgRandY}");
                PositionInfo<MainWindow>($"Target Center Bounds: {targetCenterBounds.GetCorners()}");

                // Check which possible X positions are within the target bounds and doesn't lie inside jump positions
                Rect possibleTarget1 = new Rect(
                    tgPossibleX1 - targetHalfW,
                    tgRandY - targetHalfW,
                    targetW, targetW);
                Rect possibleTarget2 = new Rect(
                    tgPossibleX2 - targetHalfW,
                    tgRandY - targetHalfW,
                    targetW, targetW);
                PositionInfo<MainWindow>($"Possible Tgt1: {possibleTarget1.GetCorners()}");
                PositionInfo<MainWindow>($"Possible Tgt2: {possibleTarget2.GetCorners()}");
                //Outlog<MainWindow>().Information($"Target Window Bounds: {_topWindow.GetCorners(padding)}");
                bool isPos1Valid =
                    _topWinRectPadded.Contains(possibleTarget1)
                    && Utils.ContainsNot(possibleTarget1, jumpPositions);
                bool isPos2Valid =
                    _topWinRectPadded.Contains(possibleTarget2)
                    && Utils.ContainsNot(possibleTarget2, jumpPositions);

                PositionInfo<MainWindow>($"Position 1 valid: {isPos1Valid}");
                PositionInfo<MainWindow>($"Position 2 valid: {isPos2Valid}");

                if (isPos1Valid && !isPos2Valid) // Only position 1 is valid
                {
                    startCenterPosition = new Point(stPosX, stPosY);
                    
                    //targetCenterPosition = new Point(tgPossibleX1, tgRandY);

                    // Convert to top-left and respective window coordinates
                    startPosition = Utils.Offset(startCenterPosition, -startHalfW, -startHalfW);
                    Point startPositionInMainWin = Utils.Offset(startPosition,
                        -_mainWinRect.Left,
                        -_mainWinRect.Top);
                    //Point targetPosition = Utils.Offset(targetCenterPosition, -targetHalfW, -targetHalfW);
                    Point targetPosition = possibleTarget1.TopLeft;
                    Point targetPositionInSideWin = Utils.Offset(targetPosition,
                        -_topWinRect.Left,
                        -_topWinRect.Top);
                    PositionInfo<MainWindow>($"Found -> Start: {startPositionInMainWin} - Target: {targetPositionInSideWin}");
                    return (startPositionInMainWin, targetPositionInSideWin);

                }
                else if (!isPos1Valid && isPos2Valid) // Only position 2 is valid
                {
                    startCenterPosition = new Point(stPosX, stPosY);
                    //targetCenterPosition = new Point(tgPossibleX2, tgRandY);

                    // Convert to top-left and respective window coordinates
                    startPosition = Utils.Offset(startCenterPosition, -startHalfW, -startHalfW);
                    Point startPositionInMainWin = Utils.Offset(startPosition,
                        -_mainWinRect.Left,
                        -_mainWinRect.Top);
                    //Point targetPosition = Utils.Offset(targetCenterPosition, -targetHalfW, -targetHalfW);
                    Point targetPosition = possibleTarget2.TopLeft;
                    Point targetPositionInSideWin = Utils.Offset(targetPosition,
                        -_topWinRect.Left,
                        -_topWinRect.Top);
                    PositionInfo<MainWindow>($"Found -> Start: {startPositionInMainWin} - Target: {targetPositionInSideWin}");
                    return (startPositionInMainWin, targetPositionInSideWin);
                }
                else if (isPos1Valid && isPos2Valid) // Both positions are valid => choose one by random
                {
                    startCenterPosition = new Point(stPosX, stPosY);
                    //targetCenterPosition = new Point(_random.NextDouble() < 0.5 ? tgPossibleX1 : tgPossibleX2, tgRandY);

                    // Convert to top-left and respective window coordinates
                    startPosition = Utils.Offset(startCenterPosition, -startHalfW, -startHalfW);
                    Point startPositionInMainWin = Utils.Offset(startPosition,
                        -_mainWinRect.Left,
                        -_mainWinRect.Top);
                    //Point targetPosition = Utils.Offset(targetCenterPosition, -targetHalfW, -targetHalfW);
                    Point targetPosition = _random.NextDouble() < 0.5 ? possibleTarget1.TopLeft : possibleTarget2.TopLeft;
                    Point targetPositionInSideWin = Utils.Offset(targetPosition,
                        -_topWinRect.Left,
                        -_topWinRect.Top);
                    PositionInfo<MainWindow>($"Found -> Start: {startPositionInMainWin} - Target: {targetPositionInSideWin}");
                    return (startPositionInMainWin, targetPositionInSideWin);
                }
            }

            PositionInfo<MainWindow>("Failed to find a valid placement within the retry limit.");
            return (new Point(), new Point()); // Indicate failure
        }

        //private Rect GetWindowBoundsForShapeCenter(Rect windowRect, int padding, int shapeW)
        //{
        //    int shapeHalfW = shapeW / 2;
        //    int infoLabelHeight = (int)(this.ActualHeight*INFO_LABEL_BOTTOM_RATIO + infoLabel.ActualHeight);

        //    return new Rect(
        //        window.Left + padding + shapeHalfW,
        //        window.Top + padding + shapeHalfW, 
        //        window.Width - 2*padding, 
        //        window.Height - 2*padding - infoLabelHeight);
        //}


        private void EndTrial(RESULT result)
        {
            // Play sounds
            switch (result)
            {
                case RESULT.NO_START:
                    Sounder.PlayStartMiss();
                    break;
                case RESULT.MISS:
                    Sounder.PlayTargetMiss();
                    break;
                case RESULT.HIT:
                    Sounder.PlayHit();
                    break;
            }

            _timestamps[Str.TRIAL_END] =_stopWatch.ElapsedMilliseconds;
            TrialInfo<MainWindow>($"Trial#{_activeTrialNum} ended: {result}");

            // Freeze auxursor until Start is clicked in the next trial
            _auxursorFreezed = true;

            // Decide on result
            if (result == RESULT.HIT) { EndTrialHit(); }
            else if (result == RESULT.MISS) { EndTrialMiss(); }
            else // Start no clicked 
            { 
                // Repeat the trial
            }

        }

        private void EndTrialHit()
        {
            if (_activeTrialNum < _block.GetNumTrials()) // More trials to show
            {
                GoToNextTrial();
            }
            else // Block finished
            {
                if (_activeBlockNum < _experiment.GetNumBlocks()) // More blocks to show
                {
                    // Show end of block window
                    BlockEndWindow blockEndWindow = new BlockEndWindow(_activeBlockNum, GoToNextBlock);
                    blockEndWindow.Owner = this;
                    blockEndWindow.ShowDialog();
                }
                else // All blocks finished
                {
                    PositionInfo<MainWindow>("Technique finished!");
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
        }

        private void EndTrialMiss()
        {
            _block.ShuffleBackTrial(_activeTrialNum);
            GoToNextTrial();
        }

        private void Start_MouseEnter(object sender, SysIput.MouseEventArgs e)
        {
            //--- Set the time and state
            if (_timestamps.ContainsKey(Str.TARGET_RELEASE))
            { // Return from target
                _timestamps[Str.START2_LAST_ENTRY] = _trialtWatch.ElapsedMilliseconds;
            } else
            { // First time
                _timestamps[Str.START1_LAST_ENTRY] = _trialtWatch.ElapsedMilliseconds;
            }
            
        }

        private void Start_MouseLeave(object sender, SysIput.MouseEventArgs e)
        {
            
        }

        private void Start_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TrialInfo<MainWindow>($"{_timestamps.Stringify()}");
            if (_timestamps.ContainsKey(Str.TARGET_RELEASE)) // Phase 3 (Target hit, Start click again => End trial)
            {
                EndTrial(RESULT.HIT);
            }
            else if (_timestamps.ContainsKey(Str.START1_RELEASE)) // Phase 2: Start already clicked, it's actually Aux click
            {
                if (_targetSideWindow.IsCursorInsideTarget()) // Inside target => Target hit
                {
                    TargetMouseDown();
                }
                else // Pressed outside target => MISS
                {
                    TrialInfo<MainWindow>($"Pressed outside Target!");
                    EndTrial(RESULT.MISS);
                }
            }
            else // Phae 1: First Start press
            {
                _timestamps[Str.START1_PRESS] = _trialtWatch.ElapsedMilliseconds;
                
            }

            e.Handled = true; // Prevents the event from bubbling up to the parent element (Window)

            //if (_experiment.IsTechRadiusor())
            //{

            //    //--- First click on Start ----------------------------------------------------
            //    // Show line at the cursor position
            //    Point cursorScreenPos = FindCursorScreenPos(e);
            //    Point overlayCursorPos = FindCursorDestWinPos(cursorScreenPos, _overlayWindow);

            //    _overlayWindow.ShowBeam(cursorScreenPos);
            //    _radiusorActive = true;

            //    // Freeze main cursor
            //    _cursorFreezed = FreezeCursor();
            //}

            //if (Experiment.Active_Technique == Technique.Mouse)
            //{
            //    if (_timestamps.ContainsKey(Str.TARGT_PRESS)) // Target hit, Start click again => End trial
            //    {
            //        EndTrial(RESULT.HIT);
            //        return;
            //    }
            //}


        }

        private void Start_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TrialInfo<MainWindow>($"{_timestamps.Stringify()}");
            if (_timestamps.ContainsKey(Str.TARGET_PRESS)) // Already pressed with Auxursor => Check if release is also inside
            {
                if (_targetSideWindow.IsCursorInsideTarget())
                {
                    TargetMouseUp();
                } 
                else // Released outside Target => MISS
                {
                    TrialInfo<MainWindow>($"Released outside target");
                    EndTrial(RESULT.MISS);
                }

            } 
            else if (_timestamps.ContainsKey(Str.START1_PRESS)) // First time
            {
                _timestamps[Str.START1_RELEASE] = _trialtWatch.ElapsedMilliseconds;
                _targetSideWindow.ColorTarget(Brushes.Green);
                _startRectangle.Fill = Brushes.Red;

                // Enable Auxursor activation
                _auxursorFreezed = false;
            }
            else // Started from inside, but released outside Start => End on No_Start
            {
                EndTrial(RESULT.NO_START);
            }

            e.Handled= true;
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
        private void Target_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TargetMouseDown();

            e.Handled = true; // Prevents the event from bubbling up to the parent element (Window)
        }

        private void TargetMouseDown()
        {
            if (_timestamps.ContainsKey(Str.START1_RELEASE)) // Correct sequence
            {
                // Set the time
                _timestamps[Str.TARGET_PRESS] = _trialtWatch.ElapsedMilliseconds;
            }
            else // Clicked Target before Start => NO_START
            {
                EndTrial(RESULT.NO_START);
            }
            
        }

        private void TargetMouseUp()
        {
            if (_timestamps.ContainsKey(Str.TARGET_PRESS)) // Only act on release when the press was recorded
            {
                // Set the time and state
                _timestamps[Str.TARGET_RELEASE] = _trialtWatch.ElapsedMilliseconds;
                //_trialState = Str.TARGET_RELEASE;

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
            }
            
        }

        private void Target_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TargetMouseUp();
            e.Handled = true; // Prevents the event from bubbling up to the parent element (Window)
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

        private void DeactivateAuxursors()
        {
            _leftWindow.DeactivateCursor();
            _topWindow.DeactivateCursor();
            _rightWindow.DeactivateCursor();   
        }

        private void ActivateSideWin(Location window, Location tapLoc)
        {
            switch (window)
            {
                case Location.Left:
                    _activeSideWindow = _leftWindow;
                    _leftWindow.ShowCursor(tapLoc);
                    _leftWindow.ActivateCursor();
                    _topWindow.DeactivateCursor();
                    _rightWindow.DeactivateCursor();
                    break;
                case Location.Top:
                    _activeSideWindow = _topWindow;
                    _topWindow.ShowCursor(tapLoc);
                    _topWindow.ActivateCursor();
                    _leftWindow.DeactivateCursor();
                    _rightWindow.DeactivateCursor();
                    break;
                case Location.Right:
                    _activeSideWindow = _rightWindow;
                    _rightWindow.ShowCursor(tapLoc);
                    _rightWindow.ActivateCursor();
                    _leftWindow.DeactivateCursor();
                    _topWindow.DeactivateCursor();
                    break;
            }
        }

        private void ShowAxursors()
        {
            // Show all Auxursors (deactivated) in the middle of the side windows
            _leftWindow.ShowCursor(Location.Middle);
            _topWindow.ShowCursor(Location.Middle);
            _rightWindow.ShowCursor(Location.Middle);
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
            if (_experiment.Active_Technique == Technique.Auxursor_Tap && !_auxursorFreezed)
            {
                ActivateSideWin(Location.Top, Location.Left);
            }
        }

        public void IndexMove(TouchPoint indPoint)
        {

            if (_experiment.IsTechAuxCursor())
            {
                if (_activeSideWindow != null)
                {
                    _activeSideWindow.UpdateCursor(indPoint);
                }
            }

            //if (_radiusorActive)
            //{
            //    bool beamRotated = _overlayWindow.RotateBeam(indPoint);

            //}

        }

        public void IndexMove(double dX, double dY)
        {
            throw new NotImplementedException();
        }

        public void IndexUp()
        {
            if (_experiment.IsTechAuxCursor() && _activeSideWindow != null)
            {
                _activeSideWindow.StopCursor();
            }
            //_lastPlusPointerPos.X = -1;
            _lastRotPointerPos.X = -1;

        }

        public void ThumbSwipe(Direction dir)
        {
            if (_experiment.Active_Technique == Technique.Auxursor_Swipe)
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

        public void ThumbUp()
        {
            //_lastRotPointerPos.X = -1;
            _lastPlusPointerPos.X = -1;
        }

        public void ThumbTap(Location tapLoc)
        {
            if (_experiment.Active_Technique == Technique.Auxursor_Tap && !_auxursorFreezed)
            {
                ActivateSideWin(Location.Left, tapLoc);
            }
            
        }

        public void MiddleTap()
        {
            if (_experiment.Active_Technique == Technique.Auxursor_Tap && !_auxursorFreezed)
            {
                ActivateSideWin(Location.Top, Location.Middle);
            }
        }

        public void RingTap()
        {
            if (_experiment.Active_Technique == Technique.Auxursor_Tap && !_auxursorFreezed)
            {
                ActivateSideWin(Location.Top, Location.Right); // Right side of the top window
                //ActivateSide(Direction.Up, tapDir);
            }
        }

        public void PinkyTap(Location tapLoc)
        {
            if (_experiment.Active_Technique == Technique.Auxursor_Tap && !_auxursorFreezed)
            {
                ActivateSideWin(Location.Right, tapLoc);
            }
        }
    }
}
