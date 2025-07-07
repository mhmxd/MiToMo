/********************************************************
*                                                       *
*   Copyright (C) Microsoft. All rights reserved.       *
*                                                       *
********************************************************/

using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Helpers;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.Research.TouchMouseSensor;
using NumSharp;
using NumSharp.Utilities;
using Serilog;
using Serilog.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
//using Tensorflow;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
//using static Tensorflow.tensorflow;
using System.Windows.Media;
//using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using WindowsInput;
using static Multi.Cursor.Experiment;
using static Multi.Cursor.Output;
using static Multi.Cursor.Utils;
using static System.Math;
using MessageBox = System.Windows.Forms.MessageBox;
using Seril = Serilog.Log;
using SysDraw = System.Drawing;
using SysIput = System.Windows.Input;
using SysWin = System.Windows;
using WinForms = System.Windows.Forms; // Alias for Forms namespace

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

        private int VERTICAL_PADDING = Utils.MM2PX(Config.WINDOW_PADDING_MM); // Padding for the windows
        private int HORIZONTAL_PADDING = Utils.MM2PX(Config.WINDOW_PADDING_MM); // Padding for the windows

        private int TopWindowHeight = Utils.MM2PX(Config.TOP_WINDOW_HEIGTH_MM);
        private int SideWindowWidth = Utils.MM2PX(Config.SIDE_WINDOW_WIDTH_MM);


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
        private OverlayWindow _overlayWindow;

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

        private bool _isTouchMouseActive = false; // Is ToMo active?
        //private bool _cursorFreezed = false;

        private Rect _mainWinRect, _leftWinRect, _topWinRect, _rightWinRect;
        private Rect _lefWinRectPadded, _topWinRectPadded, _rightWinRectPadded;
        private int _infoLabelHeight;

        //--- Radiusor
        private int _actionPointerInd = -1;
        private Pointer _actionPointer;
        private Point _lastRotPointerPos = new Point(-1, -1);
        private Point _lastPlusPointerPos = new Point(-1, -1);
        private Point _lastMiddlePointerPos = new Point(-1, -1);
        private int _lastNumMiddleFingers = 0;
        private bool _radiusorActive = false;

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
        private BlockHandler _blockHandler;
        private Rect _startConstraintRectAbsolue;
        private List<BlockHandler> _blockHandlers = new List<BlockHandler> ();

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

            // Set Start constraint
            _startConstraintRectAbsolue = new Rect(
                _mainWinRect.Left + VERTICAL_PADDING + GetStartHalfWidth(),
                _mainWinRect.Top + VERTICAL_PADDING + GetStartHalfWidth(),
                _mainWinRect.Width - 2 * VERTICAL_PADDING,
                _mainWinRect.Height - 2 * VERTICAL_PADDING - _infoLabelHeight
             );

            // Create grid
            //_topWindow.KnollHorizontal(6, 12, Target_MouseEnter, Target_MouseLeave, Target_MouseDown, Target_MouseUp);
            Func<Grid>[] colCreators = new Func<Grid>[]
            {
                () => ColumnFactory.CreateGroupType1(combination: 1),
                () => ColumnFactory.CreateGroupType2(combination: 2),
                () => ColumnFactory.CreateGroupType3(),
                () => ColumnFactory.CreateGroupType1(combination: 3),
                () => ColumnFactory.CreateGroupType2(combination: 1),
                () => ColumnFactory.CreateGroupType1(combination: 6),
                () => ColumnFactory.CreateGroupType3(),
                () => ColumnFactory.CreateGroupType2(combination: 1),
                () => ColumnFactory.CreateGroupType1(combination: 5),
                () => ColumnFactory.CreateGroupType2(combination: 3),
                () => ColumnFactory.CreateGroupType1(combination: 2),
                () => ColumnFactory.CreateGroupType1(combination: 4),
            };

            // Starts placed at the two bottom corners (to set max distance from grid buttons)
            _topWindow.GenerateGrid(_startConstraintRectAbsolue, colCreators);


            // Create left grid
            //Func<Grid>[] leftGroupCreators = new Func<Grid>[]
            //{
            //    () => RowFactory.CreateGroupType1(combination: 1),
            //    () => RowFactory.CreateGroupType1(combination: 3),
            //    () => RowFactory.CreateGroupType2(combination: 2),
            //    () => RowFactory.CreateGroupType2(combination: 4),
            //    () => RowFactory.CreateGroupType2(combination: 6),
            //    () => RowFactory.CreateGroupType1(combination: 1),
            //    () => RowFactory.CreateGroupType1(combination: 3),
            //    () => RowFactory.CreateGroupType2(combination: 5),
            //    () => RowFactory.CreateGroupType2(combination: 2),
            //    () => RowFactory.CreateGroupType2(combination: 2),
            //    () => RowFactory.CreateGroupType1(combination: 6),
            //};

            _leftWindow.GenerateGrid(_startConstraintRectAbsolue, colCreators);

            // Create right grid
            //Func<Grid>[] rightGroupCreators = new Func<Grid>[]
            //{
            //    () => RowFactory.CreateGroupType1(combination: 1),
            //    () => RowFactory.CreateGroupType1(combination: 3),
            //    () => RowFactory.CreateGroupType2(combination: 2),
            //    () => RowFactory.CreateGroupType1(combination: 4),
            //    () => RowFactory.CreateGroupType2(combination: 6),
            //    () => RowFactory.CreateGroupType1(combination: 1),
            //    () => RowFactory.CreateGroupType2(combination: 3),
            //    () => RowFactory.CreateGroupType2(combination: 5),
            //    () => RowFactory.CreateGroupType1(combination: 2),
            //    () => RowFactory.CreateGroupType2(combination: 2),
            //    () => RowFactory.CreateGroupType2(combination: 6),
            //};

            _rightWindow.GenerateGrid(_startConstraintRectAbsolue, colCreators);


            UpdateLabelsPosition();

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

            //-- Create a default Experiment
            //double longestDistMM =
            //    _monitorHeightMM - (2 * Config.WINDOW_PADDING_MM)
            //    - (Experiment.START_WIDTH_MM / 2) - (Experiment.Max_Target_Width_MM / 2);
            //double shortestDistMM = 2 * Config.WINDOW_PADDING_MM
            //    + (Experiment.START_WIDTH_MM / 2) + (Experiment.Max_Target_Width_MM / 2);
            //double shortestDistMM = 
            //    (Experiment.Max_Target_Width_MM/2.0) 
            //    + (2*Config.WINDOW_PADDING_MM) 
            //    + (Experiment.START_WIDTH_MM/2.0);
            //double longestDistMM = 
            //    (Experiment.Min_Target_Width_MM/2.0) 
            //    + (2 * Config.WINDOW_PADDING_MM) 
            //    + (Utils.PX2MM(this.Height - _infoLabelHeight) - Config.WINDOW_PADDING_MM);

            CreateExperiment(); // Create the experiment

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
                //_experiment.Init(introDialog.ParticipantNumber, introDialog.Technique);
                ToMoLogger.Init(_experiment.Participant_Number, _experiment.Active_Technique);

                BeginTechnique();
            }

        }

        private void CreateExperiment()
        {
            // Shortest distance is diagonal from the top left corner of top window to top left corner of main
            // Measured
            //double shortestDistMM = 150;
            //double startHW = Utils.MmToDips(Experiment.START_WIDTH_MM) / 2.0;
            //Point srcPoint = new Point(
            //    _topWinRectPadded.Left + HorizontalPadding, 
            //    _topWinRectPadded.Top + VerticalPadding);
            //Point destPoint = new Point(
            //    _mainWinRect.Left + HorizontalPadding + startHW, 
            //    _mainWinRect.Top + VerticalPadding + startHW);
            //double shortestDistMM = Utils.DipsToMm(Utils.Dist(srcPoint, destPoint));

            //double shortestDistMM = 
            //    Config.SIDE_WINDOW_WIDTH_MM 
            //    + Experiment.START_WIDTH_MM / 2;
            //double shortestDistMM = 
            //    (Config.TOP_WINDOW_HEIGTH_MM - Config.WINDOW_PADDING_MM - Config.GRID_ROW_HEIGHT_MM/2)
            //    + Config.WINDOW_PADDING_MM
            //    + (Experiment.START_WIDTH_MM / 2);

            // Longest distance is based on the height
            //double longestDistMM =
            //    Config.GRID_ROW_HEIGHT_MM / 2
            //    + Config.WINDOW_PADDING_MM
            //    + (Utils.PX2MM(this.Height - _infoLabelHeight) - Experiment.START_WIDTH_MM / 2);

            // Distances (v.3)
            // Longest
            double smallButtonHalfWidth = Experiment.BUTTON_MULTIPLES[Str.x6] / 2;
            double startHalfWidth = START_WIDTH_MM / 2;
            double longestDistMM =
                (Config.SIDE_WINDOW_WIDTH_MM - Config.WINDOW_PADDING_MM - smallButtonHalfWidth) +
                Utils.PX2MM(this.ActualWidth) - Config.WINDOW_PADDING_MM - startHalfWidth;

            // Shortest
            double topLeftButtonCenterLeft = Config.WINDOW_PADDING_MM + smallButtonHalfWidth;
            double topLeftButtonCenterTop = Config.WINDOW_PADDING_MM + smallButtonHalfWidth;
            Point topLeftButtonCenterAbsolute = new Point(topLeftButtonCenterLeft, topLeftButtonCenterTop);

            double topLeftStartCenterLeft = Config.SIDE_WINDOW_WIDTH_MM + Config.WINDOW_PADDING_MM + startHalfWidth;
            double topLeftStartCenterTop = Config.TOP_WINDOW_HEIGTH_MM + Config.WINDOW_PADDING_MM + startHalfWidth;
            Point topLeftStartCenterAbsolute = new Point(topLeftStartCenterLeft, topLeftStartCenterTop);
            
            double shortestDistMM = Utils.Dist(topLeftButtonCenterAbsolute, topLeftStartCenterAbsolute);

            this.TrialInfo($"topLeftButtonCenterAbsolute: {topLeftButtonCenterAbsolute.ToStr()}");
            this.TrialInfo($"Shortest Dist = {shortestDistMM:F2}mm | Longest Dist = {longestDistMM:F2}mm");

            _experiment = new Experiment(shortestDistMM, longestDistMM);
        }

        private void UpdateLabelsPosition()
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
            UpdateLabelsPosition(); // Reposition when the label's size changes (due to text update)
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

            //        break;
            //    case SysIput.Key.Left:

            //        break;
            //    case SysIput.Key.Up:
            //        break;
            //    case SysIput.Key.Down:
            //        break;
            //}


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
                this.Left = Config.ACTIVE_SCREEN.WorkingArea.Left + SideWindowWidth;
                this.Top = Config.ACTIVE_SCREEN.WorkingArea.Top + TopWindowHeight;
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
                this.Width = _backgroundWindow.Width - (2 * SideWindowWidth);
                this.Height = _backgroundWindow.Height - TopWindowHeight;
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Left = Config.ACTIVE_SCREEN.WorkingArea.Left + SideWindowWidth;
                thisLeft = this.Left; // Save the left position 
                this.Top = Config.ACTIVE_SCREEN.WorkingArea.Top + TopWindowHeight;
                thisTop = this.Top; // Save the top position
                this.Owner = _backgroundWindow;
                //this.Topmost = true;
                this.Show();
                this._mainWinRect = Utils.GetRect(this);
                _infoLabelHeight = (int)(this.ActualHeight * INFO_LABEL_BOTTOM_RATIO + infoLabel.ActualHeight);

                // Create top window
                //_topWindow = new SideWindow("Top Window", new Point(_sideWindowSize, 0));
                //_topWindow.Background = Config.GRAY_F3F3F3;
                //_topWindow.Height = _sideWindowSize;
                //_topWindow.Width = Config.ACTIVE_SCREEN.WorkingArea.Width;
                //_topWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                //_topWindow.Left = Config.ACTIVE_SCREEN.WorkingArea.Left;
                //_topWindow.Top = Config.ACTIVE_SCREEN.WorkingArea.Top;
                //_topWindow.MouseDown += SideWindow_MouseDown;
                //_topWindow.MouseUp += SideWindow_MouseUp;
                //_topWindow.Show();
                //_topWinRect = Utils.GetRect(_topWindow);
                //_topWinRectPadded = Utils.GetRect(_topWindow, HorizontalPadding);
                _topWindow = new TopWindow();
                _topWindow.Background = Config.GRAY_F3F3F3;
                _topWindow.Height = TopWindowHeight;
                _topWindow.Width = Config.ACTIVE_SCREEN.WorkingArea.Width;
                _topWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                _topWindow.Left = Config.ACTIVE_SCREEN.WorkingArea.Left;
                _topWindow.Top = Config.ACTIVE_SCREEN.WorkingArea.Top;
                //_topWindow.MouseDown += SideWindow_MouseDown;
                //_topWindow.MouseUp += SideWindow_MouseUp;
                _topWindow.Show();
                _topWinRect = Utils.GetRect(_topWindow);
                _topWinRectPadded = Utils.GetRect(_topWindow, VERTICAL_PADDING);

                //topWinWidthRatio = topWindow.Width / ((TOMOPAD_LAST_COL - TOMOPAD_SIDE_SIZE) - TOMOPAD_SIDE_SIZE);
                //topWinHeightRatio = topWindow.Height / TOMOPAD_SIDE_SIZE;

                // Create left window
                _leftWindow = new SideWindow("Left Window", new Point(0, SideWindowWidth));
                _leftWindow.Background = Config.GRAY_F3F3F3;
                _leftWindow.Width = SideWindowWidth;
                _leftWindow.Height = this.Height;
                _leftWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                _leftWindow.Left = Config.ACTIVE_SCREEN.WorkingArea.Left;
                _leftWindow.Top = this.Top;
                //_leftWindow.MouseDown += SideWindow_MouseDown;
                //_leftWindow.MouseUp += SideWindow_MouseUp;
                _leftWindow.Show();
                _leftWinRect = Utils.GetRect(_leftWindow);
                _lefWinRectPadded = Utils.GetRect(_leftWindow, VERTICAL_PADDING);

                //leftWinWidthRatio = leftWindow.Width / TOMOPAD_SIDE_SIZE;
                //leftWinHeightRatio = leftWindow.Height / (TOMOPAD_LAST_ROW - TOMOPAD_SIDE_SIZE);

                // Create right window
                _rightWindow = new SideWindow("Right Window", new Point(SideWindowWidth + this.Width, SideWindowWidth));
                _rightWindow.Background = Config.GRAY_F3F3F3;
                _rightWindow.Width = SideWindowWidth;
                _rightWindow.Height = this.Height;
                _rightWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                _rightWindow.Left = this.Left + this.Width;
                _rightWindow.Top = this.Top;
                //_rightWindow.MouseDown += SideWindow_MouseDown;
                //_rightWindow.MouseUp += SideWindow_MouseUp;
                _rightWindow.Show();
                _rightWinRect = Utils.GetRect(_rightWindow);
                _rightWinRectPadded = Utils.GetRect(_rightWindow, VERTICAL_PADDING);

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



        //private void SideWindow_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    if (_timestamps.ContainsKey(Str.FIRST_MOVE)) // Trial is officially started (to prevent accidental click at the beginning)
        //    {
        //        if (_timestamps.ContainsKey(Str.START_RELEASE_ONE)) // Phase 2: Aiming for Target (missing because Target is not pressed)
        //        {
        //            EndTrial(Result.MISS);
        //        }
        //        else // Phase 1: Aiming for Start (missing because here is Window!)
        //        {
        //            EndTrial(Result.NO_START);
        //        }
        //    }
        //}

        //private void SideWindow_MouseUp(object sender, MouseButtonEventArgs e)
        //{
        //    if (_timestamps.ContainsKey(Str.TARGET_PRESS)) // Released outside target
        //    {
        //        EndTrial(Result.MISS);
        //    }
        //}

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            //AdjustWindowPositions();
        }

        //private void Window_TouchDown(object sender, TouchEventArgs e)
        //{
        //    var touchPoint = e.GetTouchPoint(this);
        //    Seril.Debug($"TouchDown at ({touchPoint.Position.X}, {touchPoint.Position.Y})");
        //}

        //private void Window_TouchUp(object sender, TouchEventArgs e)
        //{
        //    var touchPoint = e.GetTouchPoint(this);
        //    Seril.Debug($"TouchUp at ({touchPoint.Position.X}, {touchPoint.Position.Y})");
        //}

        //private void Window_TouchMove(object sender, TouchEventArgs e)
        //{
        //    var touchPoint = e.GetTouchPoint(this);
        //    Seril.Debug($"TouchMove at ({touchPoint.Position.X}, {touchPoint.Position.Y})");
        //}

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _blockHandler.OnMainWindowMouseDown(sender, e);

            //_blockHandler.OnMainWindowMouseDown(sender, e);
            //this.TrialInfo($"{_timestamps.Stringify()}");
            //if (_timestamps.ContainsKey(Str.FIRST_MOVE)) // Trial is officially started (to prevent accidental click at the beginning)
            //{
            //    if (_timestamps.ContainsKey(Str.TARGET_RELEASE)) // Phase 3: Window press => Outside Start
            //    {
            //        EndTrial(Result.MISS);
            //    }
            //    else if (_timestamps.ContainsKey(Str.START_RELEASE_ONE)) // Phase 2: Aiming for Target
            //    {
            //        if (_targetWindow.IsNavigatorOnButton(_trialTargetIds[_trial.Id]))
            //        {
            //            TargetMouseDown();
            //        }
            //        else
            //        {
            //            EndTrial(Result.MISS);
            //        }
            //        //if (_targetWindow.IsCursorInsideTarget()) // Auxursor in target
            //        //{
            //        //    TargetMouseDown();
            //        //}
            //        //else // Pressed outside target => MISS
            //        //{
            //        //    EndTrial(Result.MISS);
            //        //}
            //    }
            //    else // Phase 1: Aiming for Start (missing because here is Window!)
            //    {
            //        //EndTrial(Result.NO_START);
            //        EndTrial(Result.NO_START);
            //    }

            //}


            //if (_experiment.IsTechRadiusor())
            //{
            //    Point crossPos = _overlayWindow.GetCrossPosition();
            //    Point crossScreenPos = _overlayWindow.PointToScreen(crossPos);
            //    Point crossSideWinPos = _targetWindow.PointFromScreen(crossScreenPos);

            //    // Trial result
            //    //if (_targetWindow.IsPointInsideTarget(crossSideWinPos)) // Cross in target
            //    //{
            //    //    TargetMouseDown();
            //    //}
            //    //else
            //    //{
            //    //    // Nothing for now
            //    //}

            //    // Release the cursor
            //    //_cursorFreezed = !UnfreezeCursor();
            //}
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _blockHandler.OnMainWindowMouseMove(sender, e);

            //_timestamps.TryAdd(Str.FIRST_MOVE, _stopWatch.ElapsedMilliseconds);
            ////bool notMovedYet = !_timestamps.ContainsKey(Str.FIRST_MOVE);
            ////if (notMovedYet)
            ////{
            ////    _timestamps[Str.FIRST_MOVE] = _stopWatch.ElapsedMilliseconds;
            ////}

            //// Freeze other cursors when the mouse moves (outside a threshold)
            //Point currentPos = System.Windows.Input.Mouse.GetPosition(null);
            //cursorTravelDist = Utils.Dist(mainCursorPrevPosition, currentPos);
            ////Print($"Dist = {cursorTravelDist}");
            //// Moved more than the threshold => Deactivate side cursors
            //if (cursorTravelDist > Utils.MM2PX(Config.MIN_CURSOR_MOVE_MM))
            //{
            //    //Print("Mouse moved significantly");
            //    //mainCursorActive = true;
            //    //_activeSideWindow = null;

            //    // Freeze the side cursors
            //    //_leftAuxPointer.Freeze();
            //    //_rightAuxPointer.Freeze();
            //    //_topAuxPointer.Freeze();
            //}

            //mainCursorPrevPosition = currentPos;

        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {

            _blockHandler.OnMainWindowMouseUp(sender, e);
            
            //this.TrialInfo($"{_timestamps.Stringify()}");
            //if (_timestamps.ContainsKey(Str.TARGET_PRESS)) // Target is pressed => Was release outside or inside?
            //{
            //    if (_targetWindow.IsNavigatorOnButton(_trialTargetIds[_trial.Id]))
            //    {
            //        TargetMouseUp();
            //    }
            //    else
            //    {
            //        EndTrial(Result.MISS);
            //    }
            //    //if (_activeAuxWindow.IsCursorInsideTarget()) // Released inside Target => Next phase
            //    //{
            //    //    // Add timestamp
            //    //    _timestamps.TryAdd(Str.TARGET_RELEASE, _trialtWatch.ElapsedMilliseconds);

            //    //    // Deactive Target and auxursor
            //    //    _activeAuxWindow.ColorTarget(Brushes.Red);
            //    //    _activeAuxWindow.DeactivateCursor();

            //    //    // Activate Start again
            //    //    _startRectangle.Fill = Brushes.Green;
            //    //} else // Released outside Target => MISS
            //    //{
            //    //    EndTrial(Result.MISS);
            //    //}

            //}
            //else if (_timestamps.ContainsKey(Str.START_PRESS_ONE)) // Pressed inside, but released outside Start => MISS
            //{
            //    EndTrial(Result.MISS);
            //}
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



        //private void CursorMoveTimer_Tick(object sender, EventArgs e)
        //{
        //    //Print("Tick");
        //    cursorTravelDist = 0; // Reset the distance
        //    mainCursorActive = false; // Main cursor is not active anymore
        //}

        //private void ShowRadiusor()
        //{
        //    //-- Get cursor position relative to the screen
        //    //Point cursorPos = GetCursorPos();
        //    //Point cursorScreenPos = Utils.Offset(cursorPos, _leftWindow.Width, _topWindow.Height);

        //    //_overlayWindow.ShowLine(GetCursorScreenPosition());
        //}

        //private Point FindCursorScreenPos(MouseButtonEventArgs e)
        //{
        //    // Cursor position relative to the screen
        //    Point cursorPos = e.GetPosition(this);

        //    return Utils.Offset(cursorPos, _leftWindow.Width, _topWindow.Height);
        //}

        //private Point FindCursorDestWinPos(Point screenPos, Window destWin)
        //{
        //    return Utils.Offset(screenPos, -destWin.Width, -destWin.Height);
        //}

        //private Point FindCursorSrcDestWinPos(Window srcWin, Window destWin, Point srcP)
        //{
        //    Point screenPos = Utils.Offset(srcP, srcWin.Width, srcWin.Height);
        //    return Utils.Offset(screenPos, -destWin.Width, -destWin.Height);

        //}

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

        public bool SetExperiment(int ptc, string tech)
        {
            _experiment.Init(ptc, tech);
            // Find positions for all blocks
            foreach (Block bl in _experiment.Blocks)
            {
                if (bl.BlockType == Block.BLOCK_TYPE.REPEATING)
                {
                    BlockHandler blockHandler = new RepeatingBlockHandler(this, bl);
                    bool positionsFound = blockHandler.FindPositionsForActiveBlock();
                    if (positionsFound) _blockHandlers.Add(blockHandler);
                    else
                    {
                        this.TrialInfo($"Couldn't find positions for block#{bl.Id}");
                        return false;
                    }
                }
                else if (bl.BlockType == Block.BLOCK_TYPE.ALTERNATING)
                {
                    BlockHandler blockHandler = new AlternatingBlockHandler(this, bl);
                    bool positionsFound = blockHandler.FindPositionsForActiveBlock();
                    if (positionsFound) _blockHandlers.Add(blockHandler);
                    else
                    {
                        this.TrialInfo($"Couldn't find positions for block#{bl.Id}");
                        return false;
                    }
                }
            }

            //bool positionsFound = FindPositionsForAllBlocks();
            return true;
        }

        //private double EMA(List<double> points, double alpha)
        //{
        //    if (points.Count() == 1) return points[0];
        //    else return alpha * points.Last() + (1 - alpha) * EMA(points.GetRange(0, points.Count() - 1), alpha);
        //}

        private void BeginTechnique()
        {


            // Set the cursor in the middle of the window
            //int centerX = (int)(Left + Width / 2);
            //int centerY = (int)(Top + Height / 2);
            //SetCursorPos(centerX, centerY);
            //mainCursorPrevPosition = System.Windows.Input.Mouse.GetPosition(null);

            //// Set the timer for cursor movements (to not track movement indefinitely)
            //cursorMoveTimer = new DispatcherTimer();
            //cursorMoveTimer.Interval = TimeSpan.FromSeconds(Config.TIME_CURSOR_MOVE_RESET);
            //cursorMoveTimer.Tick += CursorMoveTimer_Tick;
            //cursorMoveTimer.Start();

            // Calibrate Kalman
            //TestKalman testKalman = new TestKalman("C:\\Users\\User\\Documents\\MIDE\\Data\\move.csv");
            //testKalman.Test();

            //------------------------------------------------
            // Begin
            simulator = new TouchSimulator();

            _activeBlockNum = 1;
            //Block block = _experiment.GetBlock(_activeBlockNum);
            _blockHandler = _blockHandlers[_activeBlockNum - 1];
            this.TrialInfo($"Technique: {_experiment.IsTechAuxCursor()}");
            if (_experiment.IsTechAuxCursor())
            {
                _isTouchMouseActive = true;
                if (_touchSurface == null) _touchSurface = new TouchSurface(_experiment.Active_Technique);
                _touchSurface.SetGestureReceiver(_blockHandler);
            }

            _stopWatch.Start();
            _blockHandler.BeginActiveBlock();
            //if (block.BlockType == Block.BLOCK_TYPE.REPEATING) _blockHandler = new RepeatingBlockHandler(this, block);
            //else if (block.BlockType == Block.BLOCK_TYPE.ALTERNATING) _blockHandler = new AlternatingBlockHandler(this, block);

            //bool positionsFound = _blockHandler.FindPositionsForActiveBlock();
            //if (positionsFound)
            //{
            //    UpdateInfoLabel(1, _activeBlockNum);
            //    _blockHandler.BeginActiveBlock();
            //}
        }

        private bool FindStartPosition()
        {
            return false;
        }

        public bool FindPositionsForAllBlocks()
        {
            _trialTargetIds.Clear();
            _trialStartPosition.Clear();
            //this.TrialInfo($"Number of blocks = {_experiment.Blocks.Count}");
            // Go through all blocks
            //foreach (Block block in _experiment.Blocks)
            //{
            //    if (block.BlockType == Block.BLOCK_TYPE.REPEATING)
            //    {
            //        foreach (Trial trial in block.Trials)
            //        {
            //            bool positionsFound = FindPosForRepTrial(trial); // Try to find valid positions for all trials in block
            //            this.TrialInfo($"-----------------------------------------------------");
            //            if (!positionsFound) return false; // No valid positions found for this block
            //        }
            //    }

            //    //foreach (Trial trial in block.Trials)
            //    //{
            //    //    bool positionsFound = FindPosForGridTrial(trial); // Try to find valid positions for all trials in block
            //    //    this.TrialInfo($"Trial#{trial.Id} positions found: {positionsFound}");
            //    //    this.TrialInfo($"-----------------------------------------------------");
            //    //    if (!positionsFound) return false; // No valid positions found for this block
            //    //}
            //}

            return true; // All blocks have valid positions
        }

        private bool FindPosForRepTrial(Trial trial)
        {
            int startW = Utils.MM2PX(Experiment.START_WIDTH_MM);
            int startHalfW = startW / 2;
            this.TrialInfo($"Finding positions for Trial#{trial.Id} [Target = {trial.TargetSide.ToString()}, " +
                $"TargetMult = {trial.TargetMultiple}, D (mm) = {trial.DistanceMM:F2}]");

            // Get the target window
            AuxWindow trialTargetWindow = null;
            Point trialTargetWindowPosition = new Point(0, 0);
            switch (trial.TargetSide)
            {
                case Side.Left:
                    trialTargetWindow = _leftWindow;
                    trialTargetWindowPosition = new Point(_leftWinRect.Left, _leftWinRect.Top);
                    break;
                case Side.Right:
                    trialTargetWindow = _rightWindow;
                    trialTargetWindowPosition = new Point(_rightWinRect.Left, _rightWinRect.Top);
                    break;
                case Side.Top:
                    trialTargetWindow = _topWindow;
                    trialTargetWindowPosition = new Point(_topWinRect.Left, _topWinRect.Top);
                    break;
                default:
                    throw new ArgumentException($"Invalid target side: {trial.TargetSide}");
            }

            // Set the acceptable range for the Target button
            
            int targetId = trialTargetWindow.SelectRandButtonByConstraints(trial.TargetMultiple, trial.DistancePX);
            _trialTargetIds[trial.Id] = targetId; // Map trial id to target id

            // Get the absolute position of the target center
            Point targetCenterInTargetWindow = trialTargetWindow.GetGridButtonCenter(targetId);
            Point targetCenterAbsolute = targetCenterInTargetWindow
                .OffsetPosition(trialTargetWindowPosition.X, trialTargetWindowPosition.Y);

            // Find a Start position for each distance in the passes
            _repTrialStartPositions[trial.Id] = new Dictionary<int, Point>(); // Initialize the dict for this trial
            foreach (int dist in trial.Distances)
            {
                // Find a position for the Start
                Point startCenter = FindRandPointWithDist(
                    _startConstraintRectAbsolue,
                    targetCenterAbsolute,
                    dist,
                    trial.TargetSide.GetOpposite());
                Point startPosition = startCenter.OffsetPosition(-startHalfW, -startHalfW);
                Point startPositionInMain = startPosition.OffsetPosition(-thisLeft, -thisTop); // Position relative to the main window
                this.TrialInfo($"Target: {targetCenterAbsolute}; Dist (px): {dist}; Start pos in main: {startPositionInMain}");
                if (startCenter.X == -1 && startCenter.Y == -1) // Failed to find a valid position
                {
                    this.TrialInfo($"No valid position found for Start for dist {dist}!");
                    return false;
                }
                else // Valid position found
                {
                    _repTrialStartPositions[trial.Id][dist] = startPositionInMain; // Add the position to the dictionary
                }
            }

            return true; // Valid positions found for all distances
        }

        //private bool FindPosForGridTrial(Trial trial)
        //{
        //    int startW = Utils.MM2PX(Experiment.START_WIDTH_MM);
        //    int startHalfW = startW / 2;
        //    this.TrialInfo($"Finding positions for Trial#{trial.Id} [Target = {trial.TargetSide.ToString()}, " +
        //        $"TargetMult = {trial.TargetMultiple}, D (mm) = {trial.DistanceMM:F2}]");
        //    // Select a random element in the target window
        //    AuxWindow trialTargetWindow = null;
        //    Point trialTargetWindowPosition = new Point(0, 0);
        //    switch (trial.TargetSide)
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
        //            throw new ArgumentException($"Invalid target side: {trial.TargetSide}");
        //    }

        //    int targetId = trialTargetWindow.SelectRandButtonByConstraints(trial.TargetMultiple, trial.DistancePX);
        //    _trialTargetIds.Add(trial.Id, targetId); // Map trial id to target id
        //    //this.TrialInfo(_trialTargetIds.Stringify<int, int>());
        //    // Get the absolute position of the target center
        //    Point targetCenterInTargetWindow = trialTargetWindow.GetGridButtonCenter(targetId);
        //    Point targetCenterAbsolute = targetCenterInTargetWindow
        //        .OffsetPosition(trialTargetWindowPosition.X, trialTargetWindowPosition.Y);

        //    // Find a position for the Start
        //    Rect startConstraints = new Rect(
        //        _mainWinRect.Left + VERTICAL_PADDING + startHalfW,
        //        _mainWinRect.Top + VERTICAL_PADDING + startHalfW,
        //        _mainWinRect.Width - 2 * VERTICAL_PADDING,
        //        _mainWinRect.Height - 2 * VERTICAL_PADDING - _infoLabelHeight);

        //    Point startCenter = FindRandPointWithDist(
        //        startConstraints,
        //        targetCenterAbsolute,
        //        trial.DistancePX,
        //        trial.TargetSide.GetOpposite());

        //    Point startPosition = startCenter.OffsetPosition(-startHalfW, -startHalfW);
        //    Point startPositionInMain = startPosition.OffsetPosition(-thisLeft, -thisTop); // Position relative to the main window

        //    this.TrialInfo($"Target: {targetCenterAbsolute}; Dist (px): {trial.DistancePX}; Start: {startCenter}");

        //    if (startCenter.X == -1 && startCenter.Y == -1) // Failed to find a valid position
        //    {
        //        this.TrialInfo($"No valid position found for Start!");
        //        return false;
        //    }
        //    else // Valid position found
        //    {
        //        //_activeTrial.StartPosition = startCenter.OffsetPosition(-this.Left, -this.Top);
        //        //_activeTrial.TargetPosition = targetCenterInSideWin;
        //        _trialStartPosition.Add(trial.Id, startPositionInMain);
        //        return true;
        //    }
        //}


        ///// <summary>
        ///// Find positions for all the trials in the block
        ///// </summary>
        //private bool FindPosForTrials(Block block)
        //{

        //    // (Positions all relative to screen)
        //    int padding = Utils.MM2PX(Config.WINDOW_PADDING_MM);
        //    int startW = Utils.MM2PX(Experiment.START_WIDTH_MM);
        //    int startHalfW = startW / 2;

        //    foreach (Trial trial in block.Trials) // Go through trials
        //    {
        //        PositionInfo<MainWindow>($"Finding positions for Trial#{trial.Id} [Target = {trial.TargetSide}, D = {trial.DistanceMM:F2}]");
        //        int targetW = Utils.MM2PX(trial.TargetWidthMM);
        //        int targetHalfW = targetW / 2;
        //        double dist = trial.DistancePX;

        //        // Set the active side window and find Start and Target positions
        //        Point startPositionInMainWin = new Point();
        //        Point targetPositionInSideWin = new Point();
        //        List<Point> jumpPositions = new List<Point>();

        //        // Get the bounds for the Start and Target
        //        Rect startCenterBounds = new Rect(
        //            _mainWinRect.Left + padding + startHalfW,
        //            _mainWinRect.Top + padding + startHalfW,
        //            _mainWinRect.Width - 2 * (padding + startHalfW),
        //            _mainWinRect.Height - 2 * (padding + startHalfW) - _infoLabelHeight);

        //        int nRetries = 100;
        //        bool validPosFound = false;

        //        for (int t = 0; t < nRetries; t++)
        //        {
        //            switch (trial.TargetSide)
        //            {
        //                case Side.Left:
        //                    _targetWindow = _leftWindow;
        //                    Rect targetCenterBounds = new Rect(
        //                        _leftWinRect.Left + padding + targetHalfW,
        //                        _leftWinRect.Top + padding + targetHalfW,
        //                        _leftWinRect.Width - 2 * padding - targetHalfW,
        //                        _leftWinRect.Height - 2 * padding - targetHalfW);
        //                    jumpPositions.Add(new Point(
        //                        _leftWinRect.Left + _leftWinRect.Width / 2,
        //                        _leftWinRect.Top + _leftWinRect.Height / 4)); // Top
        //                    jumpPositions.Add(new Point(
        //                        _leftWinRect.Left + _leftWinRect.Width / 2,
        //                        _leftWinRect.Top + _leftWinRect.Height * 3 / 4)); // Down

        //                    //(startPositionInMainWin, targetPositionInSideWin) = LeftTargetPositionElements(
        //                    //    startW, targetW, dist, startCenterBounds, targetCenterBounds, jumpPositions);

        //                    break;

        //                case Side.Right:
        //                    _targetWindow = _rightWindow;
        //                    targetCenterBounds = new Rect(
        //                        _rightWinRect.Left + padding + targetHalfW,
        //                        _rightWinRect.Top + padding + targetHalfW,
        //                        _rightWinRect.Width - 2 * padding - targetHalfW,
        //                        _rightWinRect.Height - 2 * padding - targetHalfW);
        //                    jumpPositions.Add(new Point(
        //                        _rightWinRect.Left + _rightWinRect.Width / 2,
        //                        _rightWinRect.Top + _rightWinRect.Height / 4)); // Top
        //                    jumpPositions.Add(new Point(
        //                        _rightWinRect.Left + _rightWinRect.Width / 2,
        //                        _rightWinRect.Top + _rightWinRect.Height * 3 / 4)); // Down

        //                    //(startPositionInMainWin, targetPositionInSideWin) = RightTargetPositionElements(
        //                    //    startW, targetW, dist, startCenterBounds, targetCenterBounds, jumpPositions);
        //                    // Check target position
        //                    //if (!_rightWinRect.Contains(targetPositionInSideWin)) // Target not properly positioned
        //                    //{
        //                    //    continue;
        //                    //}

        //                    break;

        //                case Side.Top:
        //                    _targetWindow = _topWindow;
        //                    targetCenterBounds = new Rect(
        //                        _topWinRect.Left + padding + targetHalfW,
        //                        _topWinRect.Top + padding + targetHalfW,
        //                        _topWinRect.Width - 2 * padding - targetHalfW,
        //                        _topWinRect.Height - 2 * padding - targetHalfW);
        //                    jumpPositions.Add(new Point(
        //                        _topWinRect.Left + _topWinRect.Width / 4,
        //                        _topWinRect.Top + _topWinRect.Height / 2)); // Left
        //                    jumpPositions.Add(new Point(
        //                        _topWinRect.Left + _topWinRect.Width / 2,
        //                        _topWinRect.Top + _topWinRect.Height / 2)); // Middle
        //                    jumpPositions.Add(new Point(
        //                        _topWinRect.Left + _topWinRect.Width * 3 / 4,
        //                        _topWinRect.Top + _topWinRect.Height / 2)); // Right

        //                    //(startPositionInMainWin, targetPositionInSideWin) = TopTargetPositionElements(
        //                    //    startW, targetW, dist, startCenterBounds, targetCenterBounds, jumpPositions);
        //                    // Check target position
        //                    //if (!_topWinRect.Contains(targetPositionInSideWin)) // Target not properly positioned
        //                    //{
        //                    //    Outlog<MainWindow>().Error($"Target position not valid for Trial#{trial.Id} - {_topWinRect}");
        //                    //    continue;
        //                    //}

        //                    break;
        //            }

        //            if (startPositionInMainWin.X == 0) return false; // Failed to find a valid position

        //            PositionInfo<MainWindow>("----------- Valid positions found! ------------");
        //            //trial.StartPosition = startPositionInMainWin;
        //            //trial.TargetPosition = targetPositionInSideWin;
        //            validPosFound = true;
        //            PositionInfo<MainWindow>($"St.P: {startPositionInMainWin.ToStr()}");
        //            PositionInfo<MainWindow>($"{trial.TargetSide.ToString()} " +
        //                $"-- Tgt.P: {targetPositionInSideWin.ToStr()}");
        //            break;

        //            // Check if the Start positions are valid
        //            //Outlog<MainWindow>().Information($"Main Rect: {_mainWinRect.GetCorners()}");
        //            //if (_mainWinRect.Contains(startPositionInMainWin)) // Valid positions found
        //            //{
        //            //    Outlog<MainWindow>().Information("Valid positions found! ----------------------");
        //            //    trial.StartPosition = startPositionInMainWin;
        //            //    trial.TargetPosition = targetPositionInSideWin;
        //            //    validPosFound = true;
        //            //    Outlog<MainWindow>().Debug($"St.P: {Output.GetString(startPositionInMainWin)}");
        //            //    Outlog<MainWindow>().Debug($"{trial.TargetSide.ToString()} " +
        //            //        $"-- Tgt.P: {Output.GetString(targetPositionInSideWin)}");
        //            //    break;
        //            //} 

        //        }

        //        if (!validPosFound) // No valid position found for this trial (after retries)
        //        {
        //            PositionInfo<MainWindow>($"Couldn't find a valid Start position for Trial#{trial.Id} - {trial.TargetSide}");
        //            return false;
        //        }

        //    }

        //    PositionInfo<MainWindow>($"Block#{block.Id}: Valid positions found for all trials.");
        //    return true; // All trials have valid positions
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
            double spreadRad = DegToRad(angleSpreadDeg);
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

            //const int maxAttempts = 10000;

            //// Define angle ranges for each direction in degrees
            //int minDeg, maxDeg;

            //switch (side)
            //{
            //    case Side.Left:
            //        minDeg = 135;
            //        maxDeg = 225;
            //        break;
            //    case Side.Right:
            //        minDeg = -45; // or 315
            //        maxDeg = 45;
            //        break;
            //    case Side.Top:
            //        minDeg = 225;
            //        maxDeg = 315;
            //        break;
            //    case Side.Down:
            //        minDeg = 45;
            //        maxDeg = 135;
            //        break;
            //    case Side.Middle:
            //        return src; // Or return a small offset, or random jitter around src
            //    default:
            //        throw new ArgumentOutOfRangeException(nameof(side), "Unknown Side value");
            //}

            //double minRad = DegToRad(minDeg);
            //double maxRad = DegToRad(maxDeg);

            //for (int i = 0; i < maxAttempts; i++)
            //{
            //    double randomRad = minRad + (_random.NextDouble() * (maxRad - minRad));
            //    double s_x = src.X + dist * Math.Cos(randomRad);
            //    double s_y = src.Y + dist * Math.Sin(randomRad);
            //    Point candidate = new Point((int)Math.Round(s_x), (int)Math.Round(s_y));

            //    if (rect.Contains(candidate))
            //    {
            //        return candidate;
            //    }
            //}

            //return new Point(-1, -1);
        }

        public void GoToNextBlock()
        {
            if (_activeBlockNum < _experiment.GetNumBlocks()) // More blocks to show
            {
                _activeBlockNum++;
                Block block = _experiment.GetBlock(_activeBlockNum);

                _blockHandler = _blockHandlers[_activeBlockNum - 1];
                _touchSurface.SetGestureReceiver(_blockHandler);
                _blockHandler.BeginActiveBlock();

                //if (block.BlockType == Block.BLOCK_TYPE.REPEATING) _blockHandler = new RepeatingBlockHandler(this, block);
                //else if (block.BlockType == Block.BLOCK_TYPE.ALTERNATING) _blockHandler = new AlternatingBlockHandler(this, block);

                //bool positionsFound = _blockHandler.FindPositionsForActiveBlock();
                //if (positionsFound) _blockHandler.BeginActiveBlock();
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

        //private void BeginBlock()
        //{
        //    // Start logging
        //    ToMoLogger.StartBlockLog(_activeBlockNum);
        //}

        //private void ShowRepTrial()
        //{
        //    this.TrialInfo($"Showing rep Trial#{_trial.Id} | Target side: {_trial.TargetSide} | First dist: {_trial.Distances.First()}");

        //    // Useful values
        //    int padding = Utils.MM2PX(Config.WINDOW_PADDING_MM);
        //    int startW = Utils.MM2PX(Experiment.START_WIDTH_MM);
        //    int startHalfW = startW / 2;

        //    // Set the target window based on the trial's target side
        //    switch (_trial.TargetSide)
        //    {
        //        case Side.Left:
        //            _targetWindow = _leftWindow;
        //            break;
        //        case Side.Right:
        //            _targetWindow = _rightWindow;
        //            break;
        //        case Side.Top:
        //            _targetWindow = _topWindow;
        //            break;
        //        default:
        //            throw new ArgumentException($"Invalid target side: {_trial.TargetSide}");
        //    }

        //    // Color the target button
        //    //this.TrialInfo(_trialTargetIds.Stringify<int, int>());
        //    _targetWindow.FillGridButton(_trialTargetIds[_trial.Id], Config.TARGET_UNAVAILABLE_COLOR);
        //    _targetWindow.SetGridButtonHandlers(_trialTargetIds[_trial.Id], Target_MouseDown, Target_MouseUp);

        //    // Show Start
        //    ShowStart(_repTrialStartPositions[_trial.Id].Values.First());
        //}

        //private void GoToNextTrial()
        //{
        //    //this.TrialInfo($"Block#{_activeBlockNum}: {_block.ToString()}");
        //    _activeTrialNum++;
        //    _trial = _block.GetTrial(_activeTrialNum);
        //    //ShowActiveTrial();

        //    // Clear
        //    ClearGrid();
        //    ClearStart();
        //    _timestamps.Clear();
        //    //if (_block.BlockType == BLOCK_TYPE.REPEATING) // If repeating the side => reset the Target and show another
        //    //{
        //    //    //_targetWindow.ResetElements();

        //    //}

        //    ShowGridTrial(); // Show the grid trial first
        //}

        //private void ShowGridTrial()
        //{
        //    this.TrialInfo($"Showing Trial#{_trial.Id} - Target: {_trial.TargetSide}, Dist: {_trial.DistancePX}");

        //    // Useful values
        //    int padding = Utils.MM2PX(Config.WINDOW_PADDING_MM);
        //    int startW = Utils.MM2PX(Experiment.START_WIDTH_MM);
        //    int startHalfW = startW / 2;

        //    // Set the target window based on the trial's target side
        //    switch (_trial.TargetSide)
        //    {
        //        case Side.Left:
        //            _targetWindow = _leftWindow;
        //            break;
        //        case Side.Right:
        //            _targetWindow = _rightWindow;
        //            break;
        //        case Side.Top:
        //            _targetWindow = _topWindow;
        //            break;
        //        default:
        //            throw new ArgumentException($"Invalid target side: {_trial.TargetSide}");
        //    }

        //    // Color the target button
        //    //this.TrialInfo(_trialTargetIds.Stringify<int, int>());
        //    _targetWindow.FillGridButton(_trialTargetIds[_trial.Id], Config.TARGET_UNAVAILABLE_COLOR);
        //    _targetWindow.SetGridButtonHandlers(_trialTargetIds[_trial.Id], Target_MouseDown, Target_MouseUp);

        //    // Show Start
        //    ShowStart(_trialStartPosition[_trial.Id]);

        //    // Set the target window
        //    //switch (_activeTrial.TargetSide)
        //    //{
        //    //    case Side.Left: _targetWindow = _leftWindow; break;
        //    //    case Side.Right:
        //    //        _targetWindow = _rightWindow;
        //    //        break;
        //    //    case Side.Top: 
        //    //        _targetWindow = _topWindow;

        //    //        // Choose a Target (using the width)
        //    //        //(_activeTrial.TargetKey, Point targetCenterInSideWin) =
        //    //        //    _targetWindow.GetRandomElementByWidth(_activeTrial.TargetWidthMM);
        //    //        //Point targetCenterAbsolute = targetCenterInSideWin
        //    //        //    .OffsetPosition(_targetWindow.Left, _targetWindow.Top);
        //    //        //this.TrialInfo($"Target W: {_activeTrial.TargetWidthMM}, Position: {targetCenterAbsolute.ToString()}");

        //    //        // Color the Target
        //    //        //_targetWindow.ColorElement(_activeTrial.TargetKey, Config.TARGET_UNAVAILABLE_COLOR);

        //    //        // Select a random element in the target window
        //    //        //Point targetCenterInTargetWindow = _topWindow.SelectRandButtonByConstraints(
        //    //        //    Experiment.BUTTON_WIDTHS_MULTIPLES[1], Target_MouseDown, Target_MouseUp);
        //    //        //Point targetCenterAbsolute = targetCenterInTargetWindow
        //    //        //    .OffsetPosition(_topWindow.Left, _topWindow.Top);
        //    //        //_trialsTargetCenters.Add(_activeTrial.Id, targetCenterAbsolute);

        //    //        //// Find a position for the Start
        //    //        //Rect startConstraints = new Rect(
        //    //        //    _mainWinRect.Left + padding + startHalfW,
        //    //        //    _mainWinRect.Top + padding + startHalfW,
        //    //        //    _mainWinRect.Width - 2 * padding,
        //    //        //    _mainWinRect.Height - 2 * padding - _infoLabelHeight);

        //    //        //Point startCenter = Utils.FindRandPointWithDist(
        //    //        //    startConstraints,
        //    //        //    targetCenterAbsolute,
        //    //        //    _activeTrial.DistancePX,
        //    //        //    0, 180);

        //    //        //this.TrialInfo($"Start Rect: {startConstraints.ToString()}; Target Pos: {targetCenterAbsolute}; Dist: {_activeTrial.DistancePX}");

        //    //        //if (startCenter.X == -1 && startCenter.Y == -1) // Failed to find a valid position
        //    //        //{
        //    //        //    this.TrialInfo($"No valid position found for Start!");
        //    //        //}
        //    //        //else // Valid position found
        //    //        //{
        //    //        //    this.TrialInfo($"Start Position: {startCenter.ToString()}");
        //    //        //    _activeTrial.StartPosition = startCenter.OffsetPosition(-this.Left, -this.Top);
        //    //        //    //_activeTrial.TargetPosition = targetCenterInSideWin;

        //    //        //    ShowStart();
        //    //        //}

        //    //        //// TEMP
        //    //        //_targetWindow.SelectElement(0, 0);


        //    //        break;
        //    //}

        //    // Select a random element in the target window
        //    //Point targetCenterInTopWindow = _targetWindow.SelectRandButtonByConstraints(
        //    //    Experiment.BUTTON_WIDTHS_MULTIPLES[1], Target_MouseDown, Target_MouseUp);

        //    //// Get the absolute position of the target center
        //    //Point targetCenterAbsolute = targetCenterInTopWindow
        //    //    .OffsetPosition(_topWindow.Left, _topWindow.Top);
        //    //_trialsTargetCenters.Add(_activeTrial.Id, targetCenterAbsolute);

        //    //// Find a position for the Start
        //    //Rect startConstraints = new Rect(
        //    //    _mainWinRect.Left + padding + startHalfW,
        //    //    _mainWinRect.Top + padding + startHalfW,
        //    //    _mainWinRect.Width - 2 * padding,
        //    //    _mainWinRect.Height - 2 * padding - _infoLabelHeight);

        //    //Point startCenter = Utils.FindRandPointWithDist(
        //    //    startConstraints,
        //    //    targetCenterAbsolute,
        //    //    _activeTrial.DistancePX,
        //    //    0, 180);

        //    //this.TrialInfo($"Start Rect: {startConstraints.ToString()}; Target Pos: {targetCenterAbsolute}; Dist: {_activeTrial.DistancePX}");

        //    //if (startCenter.X == -1 && startCenter.Y == -1) // Failed to find a valid position
        //    //{
        //    //    this.TrialInfo($"No valid position found for Start!");
        //    //}
        //    //else // Valid position found
        //    //{
        //    //    this.TrialInfo($"Start Position: {startCenter.ToString()}");
        //    //    _activeTrial.StartPosition = startCenter.OffsetPosition(-this.Left, -this.Top);
        //    //    //_activeTrial.TargetPosition = targetCenterInSideWin;

        //    //    ShowStart();
        //    //}

        //}

        //private void ShowActiveTrial()
        //{
        //    // Clear everything
        //    ClearStart();
        //    ClearTargets();
        //    HideAuxursors();
        //    _timestamps.Clear();

        //    // If Auxursor => Deactivate all
        //    //if (_experiment.IsTechAuxCursor()) DeactivateAuxursors();

        //    // Show the info
        //    this.TrialInfo("---------------------------------------------------------------------");
        //    this.TrialInfo($"Trial#{_activeTrial.Id} | Direction = {_activeTrial.TargetSide} | Dist = {_activeTrial.DistanceMM:F2}");
        //    infoLabel.Text = $"Trial {_activeTrialNum} | Block {_activeBlockNum} ";
        //    UpdateLabel();

        //    // Start the stopwatch
        //    _trialtWatch.Restart();

        //    // Add trial show timestamp
        //    _timestamps[Str.TRIAL_SHOW] = _trialtWatch.ElapsedMilliseconds;

        //    // Show Start and Target
        //    this.TrialInfo($"Start position: {_activeTrial.StartPosition}");
        //    int startW = Utils.MM2PX(Experiment.START_WIDTH_MM);
        //    int targetW = Utils.MM2PX(_activeTrial.TargetWidthMM);
        //    // Start logging
        //    ToMoLogger.StartTrialLogs(_activeTrialNum, _activeTrial.Id, _activeTrial.TargetWidthMM, _activeTrial.DistanceMM, _activeTrial.StartPosition, _activeTrial.TargetPosition);
        //    // Show Start
        //    ShowStart(_activeTrial.StartPosition, startW, Brushes.Green,
        //        Start_MouseEnter, Start_MouseLeave, Start_MouseDown, Start_MouseUp);

        //    // Set the target window and show the Target
        //    switch (_activeTrial.TargetSide)
        //    {
        //        case Side.Left: _targetWindow = _leftWindow; break;
        //        case Side.Right: _targetWindow = _rightWindow; break;
        //        case Side.Top: _targetWindow = _topWindow; break;
        //    }

        //    _targetWindow.ShowTarget(_activeTrial.TargetPosition, targetW, Config.GRAY_A0A0A0,
        //        Target_MouseEnter, Target_MouseLeave, Target_MouseDown, Target_MouseUp);
        //}

        public void UpdateInfoLabel(int trialNum, int nTrials, int blockNum = 0)
        {
            if (blockNum == 0) blockNum = _activeBlockNum;
            infoLabel.Text = $"Trial {trialNum}/{nTrials} --- Block {blockNum}/{_experiment.GetNumBlocks()}";
            UpdateLabelsPosition();
        }

        //private (Point, Point) LeftTargetPositionElements(
        //    int startW, int targetW,
        //    int dist, Rect startCenterBounds, Rect targetCenterBounds,
        //    List<Point> jumpPositions)
        //{
        //    Point startCenterPosition = new Point();
        //    Point targetCenterPosition = new Point();
        //    Point startPosition = new Point();
        //    int startHalfW = startW / 2;
        //    int targetHalfW = targetW / 2;

        //    //--- v.5
        //    int nRetries = 100;
        //    for (int retry = 0; retry < nRetries; retry++)
        //    {
        //        //int stYMinPossible = (int)(
        //        //    targetCenterBounds.Top + Sqrt(Pow(dist, 2)
        //        //    - Pow(startCetnerBounds.Right - targetCenterBounds.Right, 2)));
        //        //int stYMin = (int)Max(stYMinPossible, startCetnerBounds.Top);
        //        //int stYMax = (int)startCetnerBounds.Down;
        //        //int stPosY = _random.Next(stYMin, stYMax);

        //        //int stXMinPossible = (int)(
        //        //    targetCenterBounds.Left + Sqrt(Pow(dist, 2)
        //        //    - Pow(startCetnerBounds.Top - targetCenterBounds.Top, 2)));
        //        //int stXMin = (int)Max(stXMinPossible, startCetnerBounds.Left);
        //        //int stXMax = (int)Min(targetCenterBounds.Right + dist, startCetnerBounds.Right);
        //        //int stPosX = _random.Next(stXMin, stXMax);

        //        //int possibleStartYMin = (int)(targetCenterBounds.Top - dist);
        //        //int possibleStartYMax = (int)(targetCenterBounds.Down + dist);

        //        int stYMin = (int)startCenterBounds.Top;
        //        int stYMax = (int)startCenterBounds.Bottom;

        //        int stPosY = _random.Next(stYMin, stYMax); // Add +1 because Next() is exclusive of the upper bound

        //        int possibleStartXMin = (int)(targetCenterBounds.Left - dist);
        //        int possibleStartXMax = (int)(targetCenterBounds.Right + dist);

        //        int stXMin = (int)Max(targetCenterBounds.Left + dist, startCenterBounds.Left);
        //        int stXMax = (int)Min(targetCenterBounds.Right + dist, startCenterBounds.Right);

        //        int stPosX = _random.Next(stXMin, stXMax + 1); // Add +1 because Next() is exclusive of the upper bound

        //        PositionInfo<MainWindow>($"--- Found Start: {stPosX}, {stPosY}");

        //        // Choose a random Y for target
        //        int tgRandY = _random.Next((int)targetCenterBounds.Top, (int)targetCenterBounds.Bottom);

        //        // Solve for X
        //        int tgPossibleX1 = stPosX + (int)(Sqrt(Pow(dist, 2) - Pow(stPosY - tgRandY, 2)));
        //        int tgPossibleX2 = stPosX - (int)(Sqrt(Pow(dist, 2) - Pow(stPosY - tgRandY, 2)));

        //        //Outlog<MainWindow>().Information($"Possible Target 1: {tgPossibleX1}, {tgRandY}");
        //        //Outlog<MainWindow>().Information($"Possible Target 2: {tgPossibleX2}, {tgRandY}");
        //        PositionInfo<MainWindow>($"Target Center Bounds: {targetCenterBounds.GetCorners()}");

        //        // Check which possible X positions are within the target bounds and doesn't lie inside jump positions
        //        Rect possibleTarget1 = new Rect(
        //            tgPossibleX1 - targetHalfW,
        //            tgRandY - targetHalfW,
        //            targetW, targetW);
        //        Rect possibleTarget2 = new Rect(
        //            tgPossibleX2 - targetHalfW,
        //            tgRandY - targetHalfW,
        //            targetW, targetW);
        //        PositionInfo<MainWindow>($"Possible Tgt1: {possibleTarget1.GetCorners()}");
        //        PositionInfo<MainWindow>($"Possible Tgt2: {possibleTarget2.GetCorners()}");
        //        //Outlog<MainWindow>().Information($"Target Window Bounds: {_leftWindow.GetCorners(padding)}");
        //        bool isPos1Valid =
        //            _lefWinRectPadded.Contains(possibleTarget1)
        //            && Utils.ContainsNot(possibleTarget1, jumpPositions);
        //        bool isPos2Valid =
        //            _lefWinRectPadded.Contains(possibleTarget2)
        //            && Utils.ContainsNot(possibleTarget2, jumpPositions);

        //        PositionInfo<MainWindow>($"Position 1 valid: {isPos1Valid}");
        //        PositionInfo<MainWindow>($"Position 2 valid: {isPos2Valid}");

        //        if (isPos1Valid && !isPos2Valid) // Only position 1 is valid
        //        {
        //            startCenterPosition = new Point(stPosX, stPosY);
        //            //targetCenterPosition = new Point(tgPossibleX1, tgRandY);

        //            // Convert to top-left and respective window coordinates
        //            startPosition = Utils.Offset(startCenterPosition, -startHalfW, -startHalfW);
        //            Point startPositionInMainWin = Utils.Offset(startPosition,
        //                -_mainWinRect.Left,
        //                -_mainWinRect.Top);
        //            //Point targetPosition = Utils.Offset(targetCenterPosition, -targetHalfW, -targetHalfW);
        //            Point targetPosition = possibleTarget1.TopLeft;
        //            Point targetPositionInSideWin = Utils.Offset(targetPosition,
        //                -_leftWinRect.Left,
        //                -_leftWinRect.Top);
        //            PositionInfo<MainWindow>($"Found -> Start: {startPositionInMainWin} - Target: {targetPositionInSideWin}");
        //            return (startPositionInMainWin, targetPositionInSideWin);

        //        }
        //        else if (!isPos1Valid && isPos2Valid) // Only position 2 is valid
        //        {
        //            startCenterPosition = new Point(stPosX, stPosY);
        //            //targetCenterPosition = new Point(tgPossibleX2, tgRandY);

        //            // Convert to top-left and respective window coordinates
        //            startPosition = Utils.Offset(startCenterPosition, -startHalfW, -startHalfW);
        //            Point startPositionInMainWin = Utils.Offset(startPosition,
        //                -_mainWinRect.Left,
        //                -_mainWinRect.Top);
        //            //Point targetPosition = Utils.Offset(targetCenterPosition, -targetHalfW, -targetHalfW);
        //            Point targetPosition = possibleTarget2.TopLeft;
        //            Point targetPositionInSideWin = Utils.Offset(targetPosition,
        //                -_leftWinRect.Left,
        //                -_leftWinRect.Top);
        //            PositionInfo<MainWindow>($"Found -> Start: {startPositionInMainWin} - Target: {targetPositionInSideWin}");
        //            return (startPositionInMainWin, targetPositionInSideWin);
        //        }
        //        else if (isPos1Valid && isPos2Valid) // Both positions are valid => choose one by random
        //        {
        //            startCenterPosition = new Point(stPosX, stPosY);
        //            //targetCenterPosition = new Point(_random.NextDouble() < 0.5 ? tgPossibleX1 : tgPossibleX2, tgRandY);

        //            // Convert to top-left and respective window coordinates
        //            startPosition = Utils.Offset(startCenterPosition, -startHalfW, -startHalfW);
        //            Point startPositionInMainWin = Utils.Offset(startPosition,
        //                -_mainWinRect.Left,
        //                -_mainWinRect.Top);
        //            //Point targetPosition = Utils.Offset(targetCenterPosition, -targetHalfW, -targetHalfW);
        //            Point targetPosition = _random.NextDouble() < 0.5 ? possibleTarget1.TopLeft : possibleTarget2.TopLeft;
        //            Point targetPositionInSideWin = Utils.Offset(targetPosition,
        //                -_leftWinRect.Left,
        //                -_leftWinRect.Top);
        //            PositionInfo<MainWindow>($"Found -> Start: {startPositionInMainWin} - Target: {targetPositionInSideWin}");
        //            return (startPositionInMainWin, targetPositionInSideWin);
        //        }
        //    }

        //    PositionInfo<MainWindow>("Failed to find a valid placement within the retry limit.");
        //    return (new Point(), new Point()); // Indicate failure
        //}

        //private (Point, Point) RightTargetPositionElements(
        //    int startW, int targetW,
        //    int dist, Rect startCenterBounds, Rect targetCenterBounds,
        //    List<Point> jumpPositions)
        //{
        //    Point startCenterPosition = new Point();
        //    Point targetCenterPosition = new Point();
        //    Point startPosition = new Point();
        //    int startHalfW = startW / 2;
        //    int targetHalfW = targetW / 2;

        //    //--- v.5
        //    int nRetries = 100;
        //    for (int retry = 0; retry < nRetries; retry++)
        //    {
        //        //int stYMinPossible = (int)(
        //        //    targetCenterBounds.Top + Sqrt(Pow(dist, 2)
        //        //    - Pow(startCenterBounds.Left - targetCenterBounds.Left, 2)));
        //        //int stYMin = (int)Max(stYMinPossible, startCenterBounds.Top);
        //        int stYMin = (int)startCenterBounds.Top;
        //        int stYMax = (int)startCenterBounds.Bottom;
        //        int stPosY = _random.Next(stYMin, stYMax);

        //        int stXMin = (int)Max(targetCenterBounds.Left - dist, startCenterBounds.Left);
        //        //int stXMaxPossible = (int)(
        //        //    targetCenterBounds.Right + Sqrt(Pow(dist, 2)
        //        //    - Pow(startCenterBounds.Top - targetCenterBounds.Top, 2)));
        //        int stXMax = (int)Min(targetCenterBounds.Right - dist, startCenterBounds.Right);
        //        //int stXMax = (int)Min(stXMaxPossible, startCenterBounds.Right);
        //        int stPosX = _random.Next(stXMin, stXMax);

        //        PositionInfo<MainWindow>($"Found Start: {stPosX}, {stPosY}");

        //        // Choose a random Y for target
        //        int tgRandY = _random.Next((int)targetCenterBounds.Top, (int)targetCenterBounds.Bottom);

        //        // Solve for X
        //        int tgPossibleX1 = stPosX + (int)(Sqrt(Pow(dist, 2) - Pow(stPosY - tgRandY, 2)));
        //        int tgPossibleX2 = stPosX - (int)(Sqrt(Pow(dist, 2) - Pow(stPosY - tgRandY, 2)));

        //        //Outlog<MainWindow>().Information($"Possible Target 1: {tgPossibleX1}, {tgRandY}");
        //        //Outlog<MainWindow>().Information($"Possible Target 2: {tgPossibleX2}, {tgRandY}");
        //        PositionInfo<MainWindow>($"Target Center Bounds: {targetCenterBounds.GetCorners()}");

        //        // Check which possible X positions are within the target bounds and doesn't lie inside jump positions
        //        Rect possibleTarget1 = new Rect(
        //            tgPossibleX1 - targetHalfW,
        //            tgRandY - targetHalfW,
        //            targetW, targetW);
        //        Rect possibleTarget2 = new Rect(
        //            tgPossibleX2 - targetHalfW,
        //            tgRandY - targetHalfW,
        //            targetW, targetW);
        //        PositionInfo<MainWindow>($"Possible Tgt1: {possibleTarget1.GetCorners()}");
        //        PositionInfo<MainWindow>($"Possible Tgt2: {possibleTarget2.GetCorners()}");
        //        //Outlog<MainWindow>().Information($"Target Window Bounds: {_rightWindow.GetCorners(padding)}");

        //        bool isPos1Valid =
        //            _rightWinRectPadded.Contains(possibleTarget1)
        //            && Utils.ContainsNot(possibleTarget1, jumpPositions);
        //        bool isPos2Valid =
        //            _rightWinRectPadded.Contains(possibleTarget2)
        //            && Utils.ContainsNot(possibleTarget2, jumpPositions);

        //        PositionInfo<MainWindow>($"Position 1 valid: {isPos1Valid}");
        //        PositionInfo<MainWindow>($"Position 2 valid: {isPos2Valid}");

        //        if (isPos1Valid && !isPos2Valid) // Only position 1 is valid
        //        {
        //            startCenterPosition = new Point(stPosX, stPosY);
        //            //targetCenterPosition = new Point(tgPossibleX1, tgRandY);

        //            // Convert to top-left and respective window coordinates
        //            startPosition = Utils.Offset(startCenterPosition, -startHalfW, -startHalfW);
        //            Point startPositionInMainWin = Utils.Offset(startPosition,
        //                -_mainWinRect.Left,
        //                -_mainWinRect.Top);
        //            //Point targetPosition = Utils.Offset(targetCenterPosition, -targetHalfW, -targetHalfW);
        //            Point targetPosition = possibleTarget1.TopLeft;
        //            Point targetPositionInSideWin = Utils.Offset(targetPosition,
        //                -_rightWinRect.Left,
        //                -_rightWinRect.Top);
        //            PositionInfo<MainWindow>($"Found -> Start: {startPositionInMainWin} - Target: {targetPositionInSideWin}");
        //            return (startPositionInMainWin, targetPositionInSideWin);

        //        }
        //        else if (!isPos1Valid && isPos2Valid) // Only position 2 is valid
        //        {
        //            startCenterPosition = new Point(stPosX, stPosY);
        //            //targetCenterPosition = new Point(tgPossibleX2, tgRandY);

        //            // Convert to top-left and respective window coordinates
        //            startPosition = Utils.Offset(startCenterPosition, -startHalfW, -startHalfW);
        //            Point startPositionInMainWin = Utils.Offset(startPosition,
        //                -_mainWinRect.Left,
        //                -_mainWinRect.Top);
        //            //Point targetPosition = Utils.Offset(targetCenterPosition, -targetHalfW, -targetHalfW);
        //            Point targetPosition = possibleTarget2.TopLeft;
        //            Point targetPositionInSideWin = Utils.Offset(targetPosition,
        //                -_rightWinRect.Left,
        //                -_rightWinRect.Top);
        //            PositionInfo<MainWindow>($"Found -> Start: {startPositionInMainWin} - Target: {targetPositionInSideWin}");
        //            return (startPositionInMainWin, targetPositionInSideWin);
        //        }
        //        else if (isPos1Valid && isPos2Valid) // Both positions are valid => choose one by random
        //        {
        //            startCenterPosition = new Point(stPosX, stPosY);
        //            //targetCenterPosition = new Point(_random.NextDouble() < 0.5 ? tgPossibleX1 : tgPossibleX2, tgRandY);

        //            // Convert to top-left and respective window coordinates
        //            startPosition = Utils.Offset(startCenterPosition, -startHalfW, -startHalfW);
        //            Point startPositionInMainWin = Utils.Offset(startPosition,
        //                -_mainWinRect.Left,
        //                -_mainWinRect.Top);
        //            //Point targetPosition = Utils.Offset(targetCenterPosition, -targetHalfW, -targetHalfW);
        //            Point targetPosition = _random.NextDouble() < 0.5 ? possibleTarget1.TopLeft : possibleTarget2.TopLeft;
        //            Point targetPositionInSideWin = Utils.Offset(targetPosition,
        //                -_rightWinRect.Left,
        //                -_rightWinRect.Top);
        //            PositionInfo<MainWindow>($"Found -> Start: {startPositionInMainWin} - Target: {targetPositionInSideWin}");
        //            return (startPositionInMainWin, targetPositionInSideWin);
        //        }
        //    }

        //    PositionInfo<MainWindow>("Failed to find a valid placement within the retry limit.");
        //    return (new Point(), new Point()); // Indicate failure

        //}

        //private (Point, Point) TopTargetPositionElements(
        //    int startW, int targetW,
        //    int dist, Rect startCenterBounds, Rect targetCenterBounds,
        //    List<Point> jumpPositions)
        //{
        //    Point targetCenterPosition = new Point();
        //    Point startCenterPosition = new Point();
        //    Point startPosition = new Point();
        //    int startHalfW = startW / 2;
        //    int targetHalfW = targetW / 2;

        //    //--- v.5
        //    PositionInfo<MainWindow>($"Start center bounds: {startCenterBounds.GetCorners()}");
        //    int nRetries = 100;
        //    for (int retry = 0; retry < nRetries; retry++)
        //    {
        //        int stXMin = (int)startCenterBounds.Left;
        //        int stXMax = (int)startCenterBounds.Right;
        //        //int stYMinPossible = (int)(
        //        //    targetCenterBounds.Top + 
        //        //    Sqrt(Pow(dist, 2) - Pow(startCetnerBounds.Left - targetCenterBounds.Right, 2)));
        //        //int stYMin = (int)Max(stYMinPossible, startCetnerBounds.Top);
        //        int stYMin = (int)startCenterBounds.Top;
        //        int stYMax = (int)Min(targetCenterBounds.Bottom + dist, startCenterBounds.Bottom);

        //        int stPosY = _random.Next(stYMin, stYMax);
        //        int stPosX = _random.Next(stXMin, stXMax);

        //        PositionInfo<MainWindow>($"Found Start: {stPosX}, {stPosY}");

        //        // Choose a random Y for target
        //        int tgRandY = _random.Next((int)targetCenterBounds.Top, (int)targetCenterBounds.Bottom);

        //        // Solve for X
        //        int tgPossibleX1 = stPosX + (int)(Sqrt(Pow(dist, 2) - Pow(stPosY - tgRandY, 2)));
        //        int tgPossibleX2 = stPosX - (int)(Sqrt(Pow(dist, 2) - Pow(stPosY - tgRandY, 2)));

        //        //Outlog<MainWindow>().Information($"Possible Target 1: {tgPossibleX1}, {tgRandY}");
        //        //Outlog<MainWindow>().Information($"Possible Target 2: {tgPossibleX2}, {tgRandY}");
        //        PositionInfo<MainWindow>($"Target Center Bounds: {targetCenterBounds.GetCorners()}");

        //        // Check which possible X positions are within the target bounds and doesn't lie inside jump positions
        //        Rect possibleTarget1 = new Rect(
        //            tgPossibleX1 - targetHalfW,
        //            tgRandY - targetHalfW,
        //            targetW, targetW);
        //        Rect possibleTarget2 = new Rect(
        //            tgPossibleX2 - targetHalfW,
        //            tgRandY - targetHalfW,
        //            targetW, targetW);
        //        PositionInfo<MainWindow>($"Possible Tgt1: {possibleTarget1.GetCorners()}");
        //        PositionInfo<MainWindow>($"Possible Tgt2: {possibleTarget2.GetCorners()}");
        //        //Outlog<MainWindow>().Information($"Target Window Bounds: {_topWindow.GetCorners(padding)}");
        //        bool isPos1Valid =
        //            _topWinRectPadded.Contains(possibleTarget1)
        //            && Utils.ContainsNot(possibleTarget1, jumpPositions);
        //        bool isPos2Valid =
        //            _topWinRectPadded.Contains(possibleTarget2)
        //            && Utils.ContainsNot(possibleTarget2, jumpPositions);

        //        PositionInfo<MainWindow>($"Position 1 valid: {isPos1Valid}");
        //        PositionInfo<MainWindow>($"Position 2 valid: {isPos2Valid}");

        //        if (isPos1Valid && !isPos2Valid) // Only position 1 is valid
        //        {
        //            startCenterPosition = new Point(stPosX, stPosY);

        //            //targetCenterPosition = new Point(tgPossibleX1, tgRandY);

        //            // Convert to top-left and respective window coordinates
        //            startPosition = Utils.Offset(startCenterPosition, -startHalfW, -startHalfW);
        //            Point startPositionInMainWin = Utils.Offset(startPosition,
        //                -_mainWinRect.Left,
        //                -_mainWinRect.Top);
        //            //Point targetPosition = Utils.Offset(targetCenterPosition, -targetHalfW, -targetHalfW);
        //            Point targetPosition = possibleTarget1.TopLeft;
        //            Point targetPositionInSideWin = Utils.Offset(targetPosition,
        //                -_topWinRect.Left,
        //                -_topWinRect.Top);
        //            PositionInfo<MainWindow>($"Found -> Start: {startPositionInMainWin} - Target: {targetPositionInSideWin}");
        //            return (startPositionInMainWin, targetPositionInSideWin);

        //        }
        //        else if (!isPos1Valid && isPos2Valid) // Only position 2 is valid
        //        {
        //            startCenterPosition = new Point(stPosX, stPosY);
        //            //targetCenterPosition = new Point(tgPossibleX2, tgRandY);

        //            // Convert to top-left and respective window coordinates
        //            startPosition = Utils.Offset(startCenterPosition, -startHalfW, -startHalfW);
        //            Point startPositionInMainWin = Utils.Offset(startPosition,
        //                -_mainWinRect.Left,
        //                -_mainWinRect.Top);
        //            //Point targetPosition = Utils.Offset(targetCenterPosition, -targetHalfW, -targetHalfW);
        //            Point targetPosition = possibleTarget2.TopLeft;
        //            Point targetPositionInSideWin = Utils.Offset(targetPosition,
        //                -_topWinRect.Left,
        //                -_topWinRect.Top);
        //            PositionInfo<MainWindow>($"Found -> Start: {startPositionInMainWin} - Target: {targetPositionInSideWin}");
        //            return (startPositionInMainWin, targetPositionInSideWin);
        //        }
        //        else if (isPos1Valid && isPos2Valid) // Both positions are valid => choose one by random
        //        {
        //            startCenterPosition = new Point(stPosX, stPosY);
        //            //targetCenterPosition = new Point(_random.NextDouble() < 0.5 ? tgPossibleX1 : tgPossibleX2, tgRandY);

        //            // Convert to top-left and respective window coordinates
        //            startPosition = Utils.Offset(startCenterPosition, -startHalfW, -startHalfW);
        //            Point startPositionInMainWin = Utils.Offset(startPosition,
        //                -_mainWinRect.Left,
        //                -_mainWinRect.Top);
        //            //Point targetPosition = Utils.Offset(targetCenterPosition, -targetHalfW, -targetHalfW);
        //            Point targetPosition = _random.NextDouble() < 0.5 ? possibleTarget1.TopLeft : possibleTarget2.TopLeft;
        //            Point targetPositionInSideWin = Utils.Offset(targetPosition,
        //                -_topWinRect.Left,
        //                -_topWinRect.Top);
        //            PositionInfo<MainWindow>($"Found -> Start: {startPositionInMainWin} - Target: {targetPositionInSideWin}");
        //            return (startPositionInMainWin, targetPositionInSideWin);
        //        }
        //    }

        //    PositionInfo<MainWindow>("Failed to find a valid placement within the retry limit.");
        //    return (new Point(), new Point()); // Indicate failure
        //}

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

        //private void EndTrial(Result result)
        //{
        //    this.TrialInfo($"Trial#{_activeTrialNum} ended: {result}");

        //    // Play sounds
        //    switch (result)
        //    {
        //        case Result.NO_START:
        //            Sounder.PlayStartMiss();
        //            break;
        //        case Result.MISS:
        //            Sounder.PlayTargetMiss();
        //            break;
        //        case Result.HIT:
        //            Sounder.PlayHit();
        //            break;
        //    }

        //    _timestamps[Str.TRIAL_END] = _stopWatch.ElapsedMilliseconds;

        //    // Decide on result
        //    if (result == Result.HIT) { EndTrialHit(); }
        //    else if (result == Result.MISS) { EndTrialMiss(); }
        //    else // Start no clicked 
        //    {
        //        // Repeat the trial
        //    }

        //}

        //private void EndTrialHit()
        //{
        //    // Log the time
        //    double duration = (_timestamps[Str.TRIAL_END] - _timestamps[Str.START_RELEASE_ONE]) / 1000.0;
        //    ToMoLogger.LogTrialEvent($"Trial#{_trial.Id}: {duration:F2}");

        //    // Clear the highlight
        //    _targetWindow.DeactivateGridNavigator();

        //    if (_activeTrialNum < _block.GetNumTrials()) // More trials to show
        //    {
        //        GoToNextTrial();
        //    }
        //    else // Block finished
        //    {
        //        if (_activeBlockNum < _experiment.GetNumBlocks()) // More blocks to show
        //        {
        //            // Show end of block window
        //            BlockEndWindow blockEndWindow = new BlockEndWindow(GoToNextBlock);
        //            blockEndWindow.Owner = this;
        //            blockEndWindow.ShowDialog();
        //        }
        //        else // All blocks finished
        //        {
        //            PositionInfo<MainWindow>("Technique finished!");
        //            MessageBoxResult dialogResult = SysWin.MessageBox.Show(
        //                "Technique finished!",
        //                "End",
        //                MessageBoxButton.OK,
        //                MessageBoxImage.Information
        //            );

        //            if (dialogResult == MessageBoxResult.OK)
        //            {
        //                if (Debugger.IsAttached)
        //                {
        //                    Environment.Exit(0); // Prevents hanging during debugging
        //                }
        //                else
        //                {
        //                    SysWin.Application.Current.Shutdown();
        //                }
        //            }
        //        }

        //    }
        //}

        //private void EndTrialMiss()
        //{
        //    _block.ShuffleBackTrial(_activeTrialNum);
        //    GoToNextTrial();
        //}

        //private void Start_MouseEnter(object sender, SysIput.MouseEventArgs e)
        //{
        //    //--- Set the time and state
        //    if (_timestamps.ContainsKey(Str.TARGET_RELEASE))
        //    { // Return from target
        //        _timestamps[Str.START2_LAST_ENTRY] = _trialtWatch.ElapsedMilliseconds;
        //    }
        //    else
        //    { // First time
        //        _timestamps[Str.START1_LAST_ENTRY] = _trialtWatch.ElapsedMilliseconds;
        //    }

        //}

        //private void Start_MouseLeave(object sender, SysIput.MouseEventArgs e)
        //{

        //}

        //private void Start_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    this.TrialInfo($"{_timestamps.Stringify()}");
        //    if (_timestamps.ContainsKey(Str.TARGET_RELEASE)) // Phase 3 (Target hit, Start click again => End trial)
        //    {
        //        EndTrial(Result.HIT);
        //    }
        //    else if (_timestamps.ContainsKey(Str.START_RELEASE_ONE)) // Phase 2: Start already clicked, it's actually Aux click
        //    {
        //        this.TrialInfo($"Target Id: {_trialTargetIds[_trial.Id]}");
        //        if (_targetWindow.IsNavigatorOnButton(_trialTargetIds[_trial.Id]))
        //        {
        //            TargetMouseDown();
        //        }
        //        else
        //        {
        //            EndTrial(Result.MISS);
        //        }
        //        //if (_targetWindow.IsCursorInsideTarget()) // Inside target => Target hit
        //        //{
        //        //    TargetMouseDown();
        //        //}
        //        //else // Pressed outside target => MISS
        //        //{
        //        //    this.TrialInfo($"Pressed outside Target!");
        //        //    EndTrial(Result.MISS);
        //        //}
        //    }
        //    else // Phae 1: First Start press
        //    {
        //        _timestamps[Str.START_PRESS_ONE] = _trialtWatch.ElapsedMilliseconds;
        //    }

        //    e.Handled = true; // Prevents the event from bubbling up to the parent element (Window)

        //    //if (_experiment.IsTechRadiusor())
        //    //{

        //    //    //--- First click on Start ----------------------------------------------------
        //    //    // Show line at the cursor position
        //    //    Point cursorScreenPos = FindCursorScreenPos(e);
        //    //    Point overlayCursorPos = FindCursorDestWinPos(cursorScreenPos, _overlayWindow);

        //    //    _overlayWindow.ShowBeam(cursorScreenPos);
        //    //    _radiusorActive = true;

        //    //    // Freeze main cursor
        //    //    _cursorFreezed = FreezeCursor();
        //    //}

        //    //if (Experiment.Active_Technique == Technique.Mouse)
        //    //{
        //    //    if (_timestamps.ContainsKey(Str.TARGT_PRESS)) // Target hit, Start click again => End trial
        //    //    {
        //    //        EndTrial(Result.HIT);
        //    //        return;
        //    //    }
        //    //}


        //}

        //private void Start_MouseUp(object sender, MouseButtonEventArgs e)
        //{
        //    this.TrialInfo($"{_timestamps.Stringify()}");
        //    if (_timestamps.ContainsKey(Str.TARGET_PRESS)) // Already pressed with Auxursor => Check if release is also inside
        //    {
        //        if (_targetWindow.IsNavigatorOnButton(_trialTargetIds[_trial.Id]))
        //        {
        //            TargetMouseUp();
        //        }
        //        else
        //        {
        //            EndTrial(Result.MISS);
        //        }
        //        //if (_targetWindow.IsCursorInsideTarget())
        //        //{
        //        //    TargetMouseUp();
        //        //} 
        //        //else // Released outside Target => MISS
        //        //{
        //        //    this.TrialInfo($"Released outside target");
        //        //    EndTrial(Result.MISS);
        //        //}

        //    }
        //    else if (_timestamps.ContainsKey(Str.START_PRESS_ONE)) // First time
        //    {
        //        _timestamps[Str.START_RELEASE_ONE] = _trialtWatch.ElapsedMilliseconds;

        //        //_targetWindow.ColorElement(_activeTrial.TargetKey, Config.TARGET_AVAILABLE_COLOR);
        //        _startRectangle.Fill = Config.START_UNAVAILABLE_COLOR;
        //        _targetWindow.FillGridButton(_trialTargetIds[_trial.Id], Config.TARGET_AVAILABLE_COLOR);
        //        //_targetWindow.MakeTargetAvailable(); // Target is now available for clicking

        //    }
        //    else // Started from inside, but released outside Start => End on No_Start
        //    {
        //        EndTrial(Result.NO_START);
        //    }

        //    e.Handled = true;
        //}

        //private void Target_MouseEnter(object sender, SysIput.MouseEventArgs e)
        //{

        //}

        //private void Target_MouseLeave(object sender, SysIput.MouseEventArgs e)
        //{

        //}

        /// <summary>
        /// Used for standard mouse
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void Target_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    TargetMouseDown();
        //    //e.Handled = true; // Prevents the event from bubbling up to the parent element (Window)
        //}

        //private void TargetMouseDown()
        //{
        //    this.TrialInfo($"{_timestamps.Stringify()}");
        //    if (_timestamps.ContainsKey(Str.START_RELEASE_ONE)) // Correct sequence
        //    {
        //        // Set the time
        //        _timestamps[Str.TARGET_PRESS] = _trialtWatch.ElapsedMilliseconds;

        //        // Change colors
        //        _targetWindow.FillGridButton(_trialTargetIds[_trial.Id], Config.TARGET_UNAVAILABLE_COLOR);
        //        _startRectangle.Fill = Config.START_AVAILABLE_COLOR;
        //    }
        //    else // Clicked Target before Start => NO_START
        //    {
        //        EndTrial(Result.NO_START);
        //    }

        //}

        //private void TargetMouseUp()
        //{
        //    this.TrialInfo($"{_timestamps.Stringify()}");
        //    if (_timestamps.ContainsKey(Str.TARGET_PRESS)) // Only act on release when the press was recorded
        //    {
        //        // Set the time and state
        //        _timestamps[Str.TARGET_RELEASE] = _trialtWatch.ElapsedMilliseconds;
        //        //_trialState = Str.TARGET_RELEASE;

        //        //--- Change the colors
        //        //_targetWindow.ColorElement(_activeTrial.TargetKey, Config.TARGET_UNAVAILABLE_COLOR);
        //        _startRectangle.Fill = Config.START_AVAILABLE_COLOR; // Change Start to green
        //        _targetWindow.FillGridButton(_trialTargetIds[_trial.Id], Config.TARGET_UNAVAILABLE_COLOR);
        //        //_targetWindow.ColorTarget(Brushes.Red);
        //        //_startCircle.Fill = Brushes.Green; // Change Start back to green


        //        // No need for the cursor anymore
        //        //if (_experiment.IsTechRadiusor())
        //        //{
        //        //    _overlayWindow.HideBeam();
        //        //    _radiusorActive = false;
        //        //}

        //        //if (_experiment.IsTechAuxCursor())
        //        //{
        //        //    _activeAuxWindow.DeactivateCursor();
        //        //}
        //    }

        //}

        //private void Target_MouseUp(object sender, MouseButtonEventArgs e)
        //{
        //    TargetMouseUp();
        //    //e.Handled = true; // Prevents the event from bubbling up to the parent element (Window)
        //}


        ///// <summary>
        ///// Show the start circle
        ///// </summary>
        ///// <param name="position">Top left</param>
        ///// <param name="width">In px</param>
        ///// <param name="color"></param>
        ///// <param name="mouseEnterHandler"></param>
        ///// <param name="mouseLeaveHandler"></param>
        ///// <param name="buttonDownHandler"></param>
        ///// <param name="buttonUpHandler"></param>
        //public void ShowStart(
        //    Point position, double width, Brush color,
        //    SysIput.MouseEventHandler mouseEnterHandler, SysIput.MouseEventHandler mouseLeaveHandler,
        //    MouseButtonEventHandler buttonDownHandler, MouseButtonEventHandler buttonUpHandler)
        //{

        //    // Create the square
        //    _startRectangle = new Rectangle
        //    {
        //        Width = width,
        //        Height = width,
        //        Fill = color
        //    };

        //    // Position the Start on the Canvas
        //    Canvas.SetLeft(_startRectangle, position.X);
        //    Canvas.SetTop(_startRectangle, position.Y);

        //    // Add event
        //    _startRectangle.MouseEnter += mouseEnterHandler;
        //    _startRectangle.MouseLeave += mouseLeaveHandler;
        //    _startRectangle.MouseDown += buttonDownHandler;
        //    _startRectangle.MouseUp += buttonUpHandler;

        //    // Add the circle to the Canvas
        //    //canvas.Children.Add(_startCircle);
        //    canvas.Children.Add(_startRectangle);
        //}

        public void ShowStart(
            Point absolutePosition, Brush color,
            SysIput.MouseEventHandler mouseEnterHandler, SysIput.MouseEventHandler mouseLeaveHandler,
            MouseButtonEventHandler buttonDownHandler, MouseButtonEventHandler buttonUpHandler)
        {
            // Clear the previous Start
            ClearStart();

            // Convert the absolute position to relative position
            Point positionInMain = Utils.Offset(absolutePosition,
                - this.Left,
                - this.Top);

            // Create the square
            _startRectangle = new Rectangle
            {
                Width = Utils.MM2PX(Experiment.START_WIDTH_MM),
                Height = Utils.MM2PX(Experiment.START_WIDTH_MM),
                Fill = color
            };

            // Position the Start on the Canvas
            Canvas.SetLeft(_startRectangle, positionInMain.X);
            Canvas.SetTop(_startRectangle, positionInMain.Y);

            // Add event
            _startRectangle.MouseEnter += mouseEnterHandler;
            _startRectangle.MouseLeave += mouseLeaveHandler;
            _startRectangle.MouseDown += buttonDownHandler;
            _startRectangle.MouseUp += buttonUpHandler;

            // Add the circle to the Canvas
            //canvas.Children.Add(_startCircle);
            canvas.Children.Add(_startRectangle);
        }

        //private void ShowStart(Point startPosition)
        //{
        //    this.TrialInfo($"Showing Start at {startPosition}");
        //    double startW = Utils.MM2PX(Experiment.START_WIDTH_MM);

        //    // Create the square
        //    _startRectangle = new Rectangle
        //    {
        //        Width = startW,
        //        Height = startW,
        //        Fill = Config.START_AVAILABLE_COLOR
        //    };

        //    // Position the Start on the Canvas
        //    Canvas.SetLeft(_startRectangle, startPosition.X);
        //    Canvas.SetTop(_startRectangle, startPosition.Y);

        //    // Add event
        //    _startRectangle.MouseEnter += Start_MouseEnter;
        //    _startRectangle.MouseLeave += Start_MouseLeave;
        //    _startRectangle.MouseDown += Start_MouseDown;
        //    _startRectangle.MouseUp += Start_MouseUp;

        //    // Add the circle to the Canvas
        //    //canvas.Children.Add(_startCircle);
        //    canvas.Children.Add(_startRectangle);
        //}

        //public static Point GetCursorScreenPosition()
        //{
        //    POINT point;
        //    GetCursorPos(out point);
        //    return new Point(point.X, point.Y);
        //}

        //private bool FreezeCursor()
        //{
        //    if (GetCursorPos(out POINT currentPos))
        //    {
        //        // Define a 1x1 pixel rectangle at the current cursor position
        //        RECT clipRect = new RECT
        //        {
        //            Left = currentPos.X,
        //            Top = currentPos.Y,
        //            Right = currentPos.X + 1, // Make it a tiny box
        //            Bottom = currentPos.Y + 1
        //        };

        //        // Apply the clip
        //        if (ClipCursor(ref clipRect))
        //        {
        //            // Optional: Store the current clip state if you need to restore it later,
        //            // but usually, you just want to fully unclip.
        //            // GetClipCursor(out _originalClipRect);
        //            return true;
        //        }
        //        else
        //        {
        //            // Handle potential error (e.g., log GetLastError())
        //            Console.WriteLine($"ClipCursor failed. Win32 Error Code: {Marshal.GetLastWin32Error()}");
        //            return false;
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine($"GetCursorPos failed. Win32 Error Code: {Marshal.GetLastWin32Error()}");
        //        return false;
        //    }
        //}

        //public static bool UnfreezeCursor()
        //{
        //    // Release the clip by passing IntPtr.Zero (null pointer)
        //    if (!ClipCursor(IntPtr.Zero))
        //    {
        //        Console.WriteLine($"ClipCursor(IntPtr.Zero) failed. Win32 Error Code: {Marshal.GetLastWin32Error()}");
        //        return false;
        //    }

        //    return true;
        //}

        private void ClearStart()
        {
            canvas.Children.Remove(_startRectangle);
        }

        //private void ClearGrid()
        //{
        //    _targetWindow.ResetGridSelection();
        //}

        //private void ClearTargets()
        //{
        //    _leftWindow.ClearTarget();
        //    //_topWindow.ClearTarget();
        //    _rightWindow.ClearTarget();
        //}

        private void HideAuxursors()
        {
            //_leftWindow.HideCursor();
            ////_topWindow.HideCursor();
            //_rightWindow.HideCursor();
        }

        //private void DeactivateAuxursors()
        //{
        //    _leftWindow.DeactivateCursor();
        //    //_topWindow.DeactivateCursor();
        //    _rightWindow.DeactivateCursor();   
        //}

        public void ActivateAuxGridNavigator(Side window)
        {
            this.TrialInfo($"Activating aux window: {window}");
            // Deactivate all aux windows
            _leftWindow.DeactivateGridNavigator();
            _topWindow.DeactivateGridNavigator();
            _rightWindow.DeactivateGridNavigator();

            switch (window)
            {
                case Side.Left:
                    _activeAuxWindow = _leftWindow;
                    _activeAuxWindow.ActivateGridNavigator();
                    break;
                case Side.Top:
                    _activeAuxWindow = _topWindow;
                    _activeAuxWindow.ActivateGridNavigator();
                    break;
                case Side.Right:
                    _activeAuxWindow = _rightWindow;
                    _activeAuxWindow.ActivateGridNavigator();
                    break;
            }
        }

        private void ActivateAuxWindow(Side window, Side tapLoc)
        {
            switch (window)
            {
                case Side.Left:
                    _activeAuxWindow = _leftWindow;
                    HideAuxursors();
                    //_leftWindow.ShowCursor(tapLoc);
                    //_leftWindow.ActivateCursor();
                    //_topWindow.DeactivateCursor();
                    //_rightWindow.DeactivateCursor();
                    break;
                case Side.Top:
                    _activeAuxWindow = _topWindow;
                    HideAuxursors();
                    //_topWindow.ShowCursor(tapLoc);
                    //_topWindow.ActivateGridNavigator();
                    //_leftWindow.DeactivateCursor();
                    //_rightWindow.DeactivateCursor();
                    break;
                case Side.Right:
                    _activeAuxWindow = _rightWindow;
                    HideAuxursors();
                    //_rightWindow.ShowCursor(tapLoc);
                    //_rightWindow.ActivateCursor();
                    //_leftWindow.DeactivateCursor();
                    //_topWindow.DeactivateCursor();
                    break;
            }
        }

        //private void ShowAxursors()
        //{
        //    // Show all Auxursors (deactivated) in the middle of the side windows
        //    _leftWindow.ShowCursor(Side.Middle);
        //    //_topWindow.ShowCursor(Side.Middle);
        //    _rightWindow.ShowCursor(Side.Middle);
        //}


        //public void LeftPress()
        //{
        //    //if (_technique == 1) _activeSideWindow = _leftWindow;
        //    //if (_technique == 2)
        //    //{
        //    //    //-- Get cursor position relative to the screen
        //    //    Point cursorPos = new Point(WinForms.Cursor.Position.X, WinForms.Cursor.Position.Y);
        //    //    Point cursorScreenPos = Utils.Offset(cursorPos, _leftWindow.Width, _topWindow.Height);

        //    //    _overlayWindow.ShowLine(GetCursorScreenPosition());
        //    //}
        //}

        //public void RightPress()
        //{

        //}

        //public void TopPress()
        //{

        //}

        //public void LeftMove(double dX, double dY)
        //{

        //}

        //public void IndexDown(TouchPoint indPoint)
        //{

        //}

        //public void IndexTap()
        //{
        //    if (_experiment.Active_Technique == Technique.Auxursor_Tap)
        //    {
        //        //ActivateAuxWindow(Side.Top, Side.Left);
        //        ActivateAuxGridNavigator(Side.Top);
        //    }

        //    this.TrialInfo($"Active side: {_activeAuxWindow}");

        //    //if (_experiment.Active_Technique == Technique.Auxursor_Tap 
        //    //    && _timestamps.ContainsKey(Str.START_PRESS_ONE) 
        //    //    && _touchSurface.IsFingerActive(TouchSurface.Finger.Middle)
        //    //    && _touchSurface.IsFingerActive(TouchSurface.Finger.Ring))
        //    //{
        //    //    ActivateAuxWindow(Side.Top, Side.Left);
        //    //}
        //}

        //public void IndexMove(TouchPoint indPoint)
        //{
        //    if (_experiment.IsTechAuxCursor() && _activeAuxWindow != null)
        //    {
        //        _activeAuxWindow.MoveGridNavigator(indPoint);
        //        //if (_activeAuxWindow != null)
        //        //{
        //        //    _activeAuxWindow.UpdateCursor(indPoint);
        //        //}
        //    }

        //    //if (_radiusorActive)
        //    //{
        //    //    bool beamRotated = _overlayWindow.RotateBeam(indPoint);

        //    //}

        //}

        //public void IndexMove(double dX, double dY)
        //{
        //    throw new NotImplementedException();
        //}

        //public void IndexUp()
        //{
        //    if (_experiment.IsTechAuxCursor() && _activeAuxWindow != null)
        //    {
        //        _activeAuxWindow.StopGridNavigator();
        //    }

        //    //_lastPlusPointerPos.X = -1;
        //    //_lastRotPointerPos.X = -1;

        //}

        //public void ThumbSwipe(Direction dir)
        //{
        //    this.TrialInfo($"Technique: {_experiment.Active_Technique}, Direction: {dir}");

        //    if (_experiment.Active_Technique == Technique.Auxursor_Swipe)
        //    {
        //        switch (dir)
        //        {
        //            case Direction.Left:
        //                ActivateAuxGridNavigator(Side.Left);
        //                break;
        //            case Direction.Right:
        //                ActivateAuxGridNavigator(Side.Right);
        //                break;
        //            case Direction.Up:
        //                ActivateAuxGridNavigator(Side.Top);
        //                break;
        //        }
        //    }

        //}

        //public void ThumbMove(TouchPoint thumbPoint)
        //{
        //    if (_radiusorActive) // Radiusor
        //    {
        //        // Move the plus
        //        bool plusMoved = _overlayWindow.MovePlus(thumbPoint);

        //        //if (plusMoved) _cursorFreezed = FreezeCursor();
        //        //else _cursorFreezed = !UnfreezeCursor();

        //    }
        //}

        //public void ThumbUp()
        //{
        //    //_lastRotPointerPos.X = -1;
        //    _lastPlusPointerPos.X = -1;
        //}

        //public void ThumbTap(Side tapLoc)
        //{
        //    if (_experiment.Active_Technique == Technique.Auxursor_Tap)
        //    {
        //        ActivateAuxGridNavigator(Side.Left); // Left side of the left window
        //    }

        //}

        //public void MiddleTap()
        //{
        //    if (_experiment.Active_Technique == Technique.Auxursor_Tap)
        //    {
        //        ActivateAuxGridNavigator(Side.Right);
        //    }
        //}

        //public void RingTap()
        //{
        //    if (_experiment.Active_Technique == Technique.Auxursor_Tap
        //        && _timestamps.ContainsKey(Str.START_PRESS_ONE)
        //        && _touchSurface.IsFingerActive(TouchSurface.Finger.Index)
        //        && _touchSurface.IsFingerActive(TouchSurface.Finger.Middle)
        //        && _touchSurface.IsFingerInactive(TouchSurface.Finger.Pinky))
        //    {
        //        //ActivateAuxGridNavigator(Side.Top); // Right side of the top window
        //        //ActivateSide(Direction.Up, tapDir);
        //    }
        //}

        //public void PinkyTap(Side tapLoc)
        //{
        //    if (_experiment.Active_Technique == Technique.Auxursor_Tap
        //        && _timestamps.ContainsKey(Str.START_PRESS_ONE)
        //        && _touchSurface.IsFingerActive(TouchSurface.Finger.Index)
        //        && _touchSurface.IsFingerActive(TouchSurface.Finger.Ring))
        //    {
        //        ActivateAuxWindow(Side.Right, tapLoc);
        //    }
        //}

        public void SetTargetWindow(Side side, 
            MouseButtonEventHandler windowMouseDownHandler, MouseButtonEventHandler windowMouseUpHandler)
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
                    throw new ArgumentException($"Invalid target side: {_trial.TargetSide}");
            }

            // All aux windows are treated the same (for now)
            _leftWindow.MouseDown += windowMouseDownHandler;
            _leftWindow.MouseUp += windowMouseUpHandler;
            _rightWindow.MouseDown += windowMouseDownHandler;
            _rightWindow.MouseUp += windowMouseUpHandler;
            _topWindow.MouseDown += windowMouseDownHandler;
            _topWindow.MouseUp += windowMouseUpHandler;

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
                    throw new ArgumentException($"Invalid target side: {_trial.TargetSide}");
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

        public void ResetTargetWindow(Side side)
        {
            if (_targetWindow != null)
            {
                _targetWindow.ResetGridSelection();
                _targetWindow.DeactivateGridNavigator();
            }
            else
            {
                this.TrialInfo("Target window is null, cannot reset it.");
            }
        }

        public void FillButtonInTargetWindow(Side side, int buttonId, Brush color) 
        {
            AuxWindow auxWindow = GetAuxWindow(side);
            auxWindow.FillGridButton(buttonId, color);
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
            AuxWindow auxWindow = GetAuxWindow(side);
            int id = auxWindow.SelectRandButtonByConstraints(widthUnits, dist);
            Point centerPositionInAuxWindow = auxWindow.GetGridButtonCenter(id);
            Point centerPositionAbsolute = centerPositionInAuxWindow.OffsetPosition(auxWindow.Left, auxWindow.Top);
            
            return (id,  centerPositionAbsolute); 
        }

        public (int, Point) GetRadomTarget(Side side, int widthUnits, Range distRange)
        {
            AuxWindow auxWindow = GetAuxWindow(side);
            int id = auxWindow.SelectRandButtonByConstraints(widthUnits, distRange);
            Point centerPositionInAuxWindow = auxWindow.GetGridButtonCenter(id);
            Point centerPositionAbsolute = centerPositionInAuxWindow.OffsetPosition(auxWindow.Left, auxWindow.Top);

            return (id, centerPositionAbsolute);
        }

        public Point GetCenterAbsolutePosition(Side side, int buttonId)
        {
            AuxWindow auxWindow = GetAuxWindow(side);
            Point centerPositionInAuxWindow = auxWindow.GetGridButtonCenter(buttonId);
            return new Point(
                centerPositionInAuxWindow.X + auxWindow.Left,
                centerPositionInAuxWindow.Y + auxWindow.Top);
        }

        public Rect GetStartConstraintRect()
        {
            double startHalfW = Utils.MM2PX(Experiment.START_WIDTH_MM / 2.0);
            return new Rect(
                this.Left + VERTICAL_PADDING + startHalfW,
                this.Top + VERTICAL_PADDING + startHalfW,
                this.Width - 2 * (VERTICAL_PADDING + startHalfW),
                this.Height - 2 * (VERTICAL_PADDING + startHalfW) - _infoLabelHeight
                );
        }

        public bool IsTechniqueToMo()
        {
            return _experiment.Active_Technique == Experiment.Technique.Auxursor_Swipe
                || _experiment.Active_Technique == Experiment.Technique.Auxursor_Tap;
        }

        public bool IsGridNavigatorOnButton(Side side, int buttonId)
        {
            AuxWindow auxWindow = GetAuxWindow(side);
            return auxWindow.IsNavigatorOnButton(buttonId);
        }

        public void MoveAuxNavigator(TouchPoint touchPoint)
        {
            _activeAuxWindow?.MoveGridNavigator(touchPoint);
        }

        public void StopAuxNavigator()
        {
            _activeAuxWindow?.StopGridNavigator();
        }

        public Technique GetActiveTechnique()
        {
            return _experiment.Active_Technique;
        }
    }
}
