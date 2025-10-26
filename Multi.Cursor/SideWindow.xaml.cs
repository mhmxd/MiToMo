using SkiaSharp;
using SkiaSharp.Views.WPF;
using Svg.Skia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using WindowsInput;
using static Multi.Cursor.Output;
using Seril = Serilog.Log;

namespace Multi.Cursor
{
    /// <summary>
    /// Interaction logic for SideWindow.xaml
    /// </summary>
    public partial class SideWindow : AuxWindow
    {
        public string WindowTitle { get; set; }

        private Random _random = new Random();

        private double HorizontalPadding = Utils.MM2PX(Config.WINDOW_PADDING_MM);
        private double VerticalPadding = Utils.MM2PX(Config.WINDOW_PADDING_MM); // Padding for the top and bottom of the grid
        
        private double InterGroupGutter = Utils.MM2PX(Config.GUTTER_05MM);
        private double WithinGroupGutter = Utils.MM2PX(Config.GUTTER_05MM);

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("User32.dll")]
        private static extern void EnableMouseInPointer(bool fEnable);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);


        private int windowWidth, windowHeight;

        private bool _isCursorVisible;
        private Point _lastCursorPos = new Point(0, 0);

        private InputSimulator inputSimulator = new InputSimulator();

        private Rectangle _target = new Rectangle();
        private int targetHalfW;
        private Point _relPos;
        public Point Rel_Pos
        {
            get { return _relPos; }
            set { _relPos = value; }
        }

        private Auxursor _auxursor;
        private GridNavigator _gridNavigator;
        private (int colInd, int rowInd) _selectedElement = (0, 0);

        private TranslateTransform _cursorTransform;

        private Dictionary<string, Element> _gridElements = new Dictionary<string, Element>(); // Key: "C{col}-R{row}", Value: Rectangle element
        //private Dictionary<int, string> _elementWidths = new Dictionary<int, string>(); // Key: Width (px), Value: Element Key

        //private Grid[] _gridColumns = new Grid[4]; // List of grid columns
        private List<Grid> _gridGroups = new List<Grid>(); // List of grid rows
        //private List<Grid> _gridColumns = new List<Grid>(); // List of grid columns
        //private Grid _gridCol1, _gridCol2, _gridCol3; // Grid columns

        public SideWindow(Side side, Point relPos)
        {
            InitializeComponent();
            //WindowTitle = title;
            Side = side;
            this.DataContext = this; // Set DataContext for data binding

            EnableMouseInPointer(true);
            SetForegroundWindow(new WindowInteropHelper(this).Handle);

            _relPos = relPos;

            _auxursor = new Auxursor(Config.FRAME_DUR_MS / 1000.0);
            _gridNavigator = new GridNavigator(Config.FRAME_DUR_MS / 1000.0);

            //foreach (int wm in Experiment.BUTTON_MULTIPLES.Values)
            //{
            //    _widthButtons.TryAdd(wm, new List<SButton>());
            //}

            //_cursorTransform = (TranslateTransform)FindResource("CursorTransform");

            this.Loaded += SideWindow_Loaded; // Add this line
        }

        private void SideWindow_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        /// <summary>
        /// Show the target for measurement purposes
        /// </summary>
        /// <param name="widthMM"></param>
        /// <param name="fill"></param>
        /// <returns></returns>
        public void ShowDummyTarget(double widthMM, Brush fill)
        {

            // Radius in pixels
            //const double PPI = 109;
            //const double MM_IN_INCH = 25.4;
            int targetWidth = Utils.MM2PX(widthMM);

            // Get canvas dimensions
            int canvasWidth = (int)canvas.ActualWidth;
            int canvasHeight = (int)canvas.ActualHeight;

            // Ensure the Target stays fully within bounds (min/max for top-left)
            int marginPX = Utils.MM2PX(Config.WINDOW_PADDING_MM);
            int minX = marginPX;
            int maxX = canvasWidth - marginPX - targetWidth;
            int minY = marginPX;
            int maxY = canvasHeight - marginPX - targetWidth;

            // Create the target
            targetHalfW = targetWidth / 2;
            _target = new Rectangle
            {
                Width = targetWidth,
                Height = targetWidth,
                Fill = fill,
            };

            // Longest dist
            Canvas.SetLeft(_target, (canvasWidth - targetWidth)/2);
            Canvas.SetTop(_target, minY);
            // Shortest dist
            //Canvas.SetLeft(_target, maxX - targetWidth);
            //Canvas.SetTop(_target, minY);

            // Add the circle to the Canvas
            canvas.Children.Add(_target);

            // Set index
            Canvas.SetZIndex(_target, 0);
            //Canvas.SetZIndex(activeCursor, 1);
            //Canvas.SetZIndex(inactiveCursor, 1);
        }

        public void ColorTarget(Brush color)
        {
            _target.Fill = color;
        }

        public bool IsCursorInsideTarget()
        {
            // Get circle's center
            //double centerX = Canvas.GetLeft(_target) + targetHalfW;
            //double centerY = Canvas.GetTop(_target) + targetHalfW;

            //// Calculate distance from the point to the circle's center
            //double distance = Math.Sqrt(
            //    Math.Pow(_cursorTransform.x - centerX, 2) + Math.Pow(_cursorTransform.y - centerY, 2)
            //    );

            //// Check if the distance is less than or equal to the radius
            //return distance <= targetHalfW;

            return false; // TEMP
        }

        /// <summary>
        /// Check if the point is positioned inside the target
        /// </summary>
        /// <param name="p">Point (window coordinates)</param>
        /// <returns></returns>
        public bool IsPointInsideTarget(Point p)
        {
            // Target position
            double targetLeft = Canvas.GetLeft(_target);
            double targetTop = Canvas.GetTop(_target);

            // Get the Rect from _target
            Rect targetRect = new Rect(targetLeft, targetTop, _target.Width, _target.Height);
          
            // Get circle's center
            //double centerX = Canvas.GetLeft(_target) + _targetRadius;
            //double centerY = Canvas.GetTop(_target) + _targetRadius;
            //TRIAL_LOG.Information($"Target Center: {centerX}, {centerY}");
            // Calculate distance from the point to the circle's center
            //double distance = Math.Sqrt(
            //    Math.Pow(p.x - centerX, 2) + Math.Pow(p.y - centerY, 2)
            //    );

            // Check if the distance is less than or equal to the radius
            return targetRect.Contains(p); 
        }

        public void ShowCursor(int x, int y)
        {

            // Show the simulated cursor
            //inactiveCursor.Visibility = Visibility.Hidden;
            //activeCursor.Visibility = Visibility.Visible;
            //_cursorTransform.x = x;
            //_cursorTransform.y = y;
        }

        public void ShowCursor(Point p)
        {
            ShowCursor((int)p.X, (int)p.Y);
        }

        public void ShowCursor(Side location)
        {
            Point position = new Point();

            switch (location)
            {
                case Side.Left:
                    position.X = canvas.ActualWidth / 4.0; // Middle of the left
                    position.Y = canvas.ActualHeight / 2.0; // Middle of height
                    break;
                case Side.Top:
                    position.X = canvas.ActualWidth / 2.0; // Middle of width
                    position.Y = canvas.ActualHeight / 4.0; // Middle of the top
                    break;
                case Side.Down:
                    position.X = canvas.ActualWidth / 2.0; // Middle of width
                    position.Y = canvas.ActualHeight * 3 / 4.0; // Middle of the top
                    break;
                case Side.Right:
                    position.X = canvas.ActualWidth * 3 / 4.0; // Middle of the right
                    position.Y = canvas.ActualHeight / 2.0; // Middle of height
                    break;
                case Side.Middle:
                    position.X = canvas.ActualWidth / 2.0; // Middle of width
                    position.Y = canvas.ActualHeight / 2.0; // Middle of height
                    break;
            }
            
            //inactiveCursor.Visibility = Visibility.Visible;
            //activeCursor.Visibility = Visibility.Visible;

            _cursorTransform.X = position.X;
            _cursorTransform.Y = position.Y;

            //_cursorTransform.x = position.x;
            //_cursorTransform.y = 10;
        }

        public void ActivateGridNavigator()
        {
            _gridNavigator.Activate();
        }

        public void ActivateCursor()
        {
            //inactiveCursor.Visibility = Visibility.Hidden;
            //activeCursor.Visibility = Visibility.Visible;
            //_auxursor.Activate();
        }

        public void DeactivateCursor()
        {
            //activeCursor.Visibility = Visibility.Hidden;
            //inactiveCursor.Visibility = Visibility.Visible;
            //_auxursor.Deactivate();
        }

        public void UpdateCursor(TouchPoint tp)
        {
            //(double dX, double dY) = _auxursor.Update(tp);
            //// Only move if above the threshold
            //double moveMagnitude = Math.Sqrt(dX * dX + dY * dY);
            //if (moveMagnitude >= Config.MIN_MOVEMENT_THRESHOLD)
            //{
            //    MoveCursor(dX, dY);
            //}

            (int dGridX, int dGridY) = _gridNavigator.Update(tp);

            // Apply the calculated movement to the grid's current position
            if (dGridX != 0 || dGridY != 0)
            {
                this.TrialInfo($"Grid movement: dX = {dGridX}, dY = {dGridY}");
                MoveSelection(dGridX, dGridY);
            }

        }

        public void StopCursor()
        {
            _auxursor.Stop();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //Console.WriteLine("MouseDown event triggered.");
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            var cursorPoint = e.GetPosition(this);
            
            _cursorTransform.X = cursorPoint.X;
            _cursorTransform.Y = cursorPoint.Y;
        }

        private void Window_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void PositionCursor(int x, int y)
        {
            var windowPosition = PointToScreen(new Point(0, 0));
            Seril.Debug($"Set pos X = {(int)(windowPosition.X + x)}, Y = {(int)(windowPosition.Y + y)}");
            SetCursorPos((int)(windowPosition.X + x), (int)(windowPosition.Y + y));
        }

        public void MoveCursor(double dX, double dY)
        {
            // Potential new position
            PositionInfo<SideWindow>($"Position before moving: {_cursorTransform.X:F2}, {_cursorTransform.Y:F2}");
            PositionInfo<SideWindow>($"Movement: {dX:F2}, {dY:F2}");

            double potentialX = _cursorTransform.X + dX;
            double potentialY = _cursorTransform.Y + dY;
            PositionInfo<SideWindow>($"Potential Pos: {potentialX:F2}, {potentialY:F2}");

            // x: Within boundaries
            if (potentialX < 0)
            {
                dX = -_cursorTransform.X + 3;
            }
            else if (potentialX > ActualWidth) 
            {
                dX = windowWidth - 10 - _cursorTransform.X;
            }

            // y: Within boundaries
            if (potentialY < 0)
            {
                dY = -_cursorTransform.Y + 3;
            }
            else if (potentialY > ActualHeight)
            {
                dY = windowHeight - 10 - _cursorTransform.Y;
            }

            // Move the cursor
            _cursorTransform.X += dX;
            _cursorTransform.Y += dY;

            _lastCursorPos.X = _cursorTransform.X;
            _lastCursorPos.Y = _cursorTransform.Y;

            // Check if entered the target
            if (IsCursorInsideTarget())
            {
                // To-do: call target enter methods
            }

            // Grid

        }

        public void MoveCursor(int dX, int dY)
        {

            //Console.WriteLine("WW = {0}, WH = {1}", windowWidth, windowHeight);
            // Get the relative cursor position
            Point relativeCursorPos = Mouse.GetPosition(this);
            int currentX = (int)relativeCursorPos.X;
            int currentY = (int)relativeCursorPos.Y;

            // Potential new position
            int potentialX = currentX + dX;
            int potentialY = currentY + dY;

            // Only move the cursor while it is inside the window
            if (currentX >= 0 && currentY >= 0)
            {
                // x: Within boundaries
                if (potentialX < 0) 
                {
                    dX = -currentX; // Don't stick it all the way
                } else if (potentialX > windowWidth)
                {
                    dX = windowWidth - currentX;
                }

                // y: Within boundaries
                if (potentialY < 0)
                {
                    dY = -currentY;
                }
                else if (potentialY > windowHeight)
                {
                    dY = windowHeight - currentY;
                }

                // Move the cursor
                inputSimulator.Mouse.MoveMouseBy(dX, dY);
            }
        }

        public void HideCursor()
        {
            //inactiveCursor.Visibility = Visibility.Hidden;
            //activeCursor.Visibility = Visibility.Hidden;
            //_auxursor.Deactivate();
            //MOUSE.OverrideCursor = Cursors.None;
        }

        public bool HasCursor()
        {
            return _isCursorVisible;
        }

        public void ClearTarget()
        {
            canvas.Children.Remove(_target);
        }

        //public override void GenerateGrid(Rect startConstraintsRectAbsolute, params Func<Grid>[] groupCreators)
        //{
        //    _objectConstraintRectAbsolute = startConstraintsRectAbsolute;

        //    // Clear any existing columns from the canvas and the list before generating new ones
        //    canvas.Children.Clear();
        //    _gridGroups.Clear();
        //    _buttonInfos.Clear();
        //    this.TrialInfo($"Generating grid");
        //    double currentTopPosition = VerticalPadding; // Start with the initial padding
        //    double leftGroupLeft = HorizontalPadding;
        //    double rightGroupLeft = HorizontalPadding + ColumnFactory.MAX_GROUP_WITH + InterGroupGutter;

        //    // Left column
        //    foreach (var group in groupCreators)
        //    {
        //        // Create the row
        //        Grid newGroup = group();
        //        // Set its position on the Canvas
        //        Canvas.SetTop(newGroup, currentTopPosition);
        //        Canvas.SetLeft(newGroup, leftGroupLeft);
        //        // Add to the Canvas
        //        canvas.Children.Add(newGroup);
        //        // Add to our internal list for tracking/future reference
        //        _gridGroups.Add(newGroup);
        //        // Force a layout pass on the newly added column to get its ActualWidth
        //        newGroup.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        //        newGroup.Arrange(new Rect(newGroup.DesiredSize));
        //        // Register buttons in this row
        //        //RegisterButtons(newGroup);
        //        // Update the top position for the next row
        //        currentTopPosition += ColumnFactory.COLUMN_HEIGHT + InterGroupGutter;
        //    }

        //    // Right column
        //    currentTopPosition = VerticalPadding; // Reset the top position for the right column
        //    groupCreators.ShiftElementsInPlace(2);
        //    foreach (var group in groupCreators)
        //    {
        //        // Create the row
        //        Grid newGroup = group();
        //        // Set its position on the Canvas
        //        Canvas.SetTop(newGroup, currentTopPosition);
        //        Canvas.SetLeft(newGroup, rightGroupLeft);
        //        // Add to the Canvas
        //        canvas.Children.Add(newGroup);
        //        // Add to our internal list for tracking/future reference
        //        _gridGroups.Add(newGroup);
        //        // Force a layout pass on the newly added column to get its ActualWidth
        //        newGroup.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        //        newGroup.Arrange(new Rect(newGroup.DesiredSize));
        //        // Register buttons in this row
        //        //RegisterButtons(newGroup);
        //        // Update the top position for the next row
        //        currentTopPosition += ColumnFactory.COLUMN_HEIGHT + InterGroupGutter;
        //    }


        //    //for (int i = 0; i < groupCreators.Count() - 1; i++)
        //    //{
        //    //    // Create the row
        //    //    Grid leftGroup = groupCreators[i]();
        //    //    Grid rightGroup = groupCreators[i + 1]();

        //    //    // Set its position on the Canvas
        //    //    Canvas.SetTop(leftGroup, currentTopPosition);
        //    //    Canvas.SetTop(rightGroup, currentTopPosition);

        //    //    Canvas.SetLeft(leftGroup, leftGroupLeft);
        //    //    Canvas.SetLeft(rightGroup, rightGroupLeft);

        //    //    // Add to the Canvas
        //    //    canvas.Children.Add(leftGroup);
        //    //    canvas.Children.Add(rightGroup);

        //    //    // Add to our internal list for tracking/future reference
        //    //    _gridGroups.Add(leftGroup);
        //    //    _gridGroups.Add(rightGroup);

        //    //    // Update the top position for the next row
        //    //    currentTopPosition += ColumnFactory.COLUMN_HEIGHT + InterGroupGutter; 

        //    //}

        //    //foreach (var createGroupFunc in groupCreators)
        //    //{
        //    //    Grid newGroup = createGroupFunc(); // Create the new group

        //    //    // Set its position on the Canvas
        //    //    Canvas.SetTop(newGroup, currentTopPosition);
        //    //    Canvas.SetLeft(newGroup, HorizontalPadding); // Assuming all columns start at the same left padding

        //    //    // Add to the Canvas
        //    //    canvas.Children.Add(newGroup);

        //    //    // Add to our internal list for tracking/future reference
        //    //    _gridGroups.Add(newGroup);

        //    //    // Force a layout pass on the newly added column to get its ActualWidth
        //    //    // This is crucial because the next column's position depends on this one's actual size.
        //    //    newGroup.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        //    //    newGroup.Arrange(new Rect(newGroup.DesiredSize));

        //    //    // Register buttons in this row
        //    //    //RegisterButtons(newGroup);

        //    //    // Update the currentLeftPosition for the next column, adding the current column's width and the 2*gutter
        //    //    currentTopPosition += newGroup.ActualHeight + VerticalPadding;

        //    //    //Debug.WriteLine($"Added column. Current left: {currentLeftPosition} DIPs. Column width: {newColumnGrid.ActualWidth}");
        //    //}

        //    //Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
        //    //{
        //    //    RegisterAllButtons(); // Register all buttons after all groups are created
        //    //    LinkButtonNeighbors(); // Link neighbors after all buttons are registered
        //    //}));
        //}

        //private void RegisterAllButtons()
        //{
        //    // Iterate through all groups (Grids) in the window
        //    foreach (Grid group in _gridGroups)
        //    {
        //        RegisterButtons(group); // Register buttons for each group
        //    }
        //    //this.TrialInfo($"Total buttons registered: {_allButtons.Count}");

        //    int middleId = FindMiddleButtonId();
        //    if (middleId != -1)
        //    {
        //        _lastMarkedButtonId = middleId; // Set the last highlighted button to the middle button
        //    }
        //    else
        //    {
        //        this.TrialInfo("No middle button found in the grid.");
        //    }
        //}

        //private void RegisterButtons(Grid group)
        //{
        //    //this.TrialInfo($"Registering buttons in group with {group.Children.Count} children...");

        //    // Iterate through all direct children of the Grid column
        //    foreach (UIElement childOfGroup in group.Children)
        //    {
        //        // We know our rows are StackPanels
        //        if (childOfGroup is StackPanel rowStackPanel)
        //        {
        //            // Iterate through all children of the StackPanel (which should be buttons or in-row gutters)
        //            foreach (UIElement childOfRow in rowStackPanel.Children)
        //            {
        //                // Check if the child is an SButton
        //                if (childOfRow is SButton button)
        //                {
        //                    _widthButtons[button.WidthMultiple].Add(button); // Add the button to the dictionary with its width as the key
        //                    _buttonInfos[button.Id] = new ButtonInfo(button);
        //                    //_allButtons.Add(button.Id, button); // Add to the list of all buttons

        //                    //foreach (int wm in _widthButtons.Keys)
        //                    //{
        //                    //    string ids = string.Join(", ", _widthButtons[wm].Select(b => b.Id.ToString()));
        //                    //    this.TrialInfo($"WM {wm} -> {ids}");
        //                    //}
        //                    // Add button position to the dictionary
        //                    // Get the transform from the button to the Window (or the root visual)
        //                    GeneralTransform transformToWindow = button.TransformToVisual(Window.GetWindow(button));
        //                    // Get the point representing the top-left corner of the button relative to the Window
        //                    Point positionInWindow = transformToWindow.Transform(new Point(0, 0));
        //                    _buttonInfos[button.Id].Position = positionInWindow;
        //                    //_buttonPositions.Add(button.Id, positionInWindow); // Store the position of the button
        //                    //this.TrialInfo($"Button Position: {positionInWindow}");
        //                    if (positionInWindow.x <= _topLeftButtonPosition.x && positionInWindow.y <= _topLeftButtonPosition.y)
        //                    {
        //                        //this.TrialInfo($"Top-left button position updated: {positionInWindow} for button ID#{button.Id}");
        //                        _topLeftButtonPosition = positionInWindow; // Update the top-left button position
        //                        //_lastMarkedButtonId = button.Id; // Set the last highlighted button to this one
        //                    }

        //                    Rect buttonRect = new Rect(positionInWindow.x, positionInWindow.y, button.ActualWidth, button.ActualHeight);
        //                    _buttonInfos[button.Id].Rect = buttonRect;
        //                    //_buttonRects.Add(button.Id, buttonRect); // Store the rect for later

        //                    // Set possible distance range to the Start positions
        //                    Point buttonCenterAbsolute =
        //                        positionInWindow
        //                        .OffsetPosition(button.ActualWidth / 2, button.ActualHeight / 2)
        //                        .OffsetPosition(this.Left, this.Top);

        //                    //double distToStartTL = Utils.Dist(buttonCenterAbsolute, _objectConstraintRectAbsolute.TopLeft);
        //                    //double distToStartTR = Utils.Dist(buttonCenterAbsolute, _objectConstraintRectAbsolute.TopRight);
        //                    //double distToStartLL = Utils.Dist(buttonCenterAbsolute, _objectConstraintRectAbsolute.BottomLeft);
        //                    //double distToStartLR = Utils.Dist(buttonCenterAbsolute, _objectConstraintRectAbsolute.BottomRight);

        //                    //double[] dists = { distToStartTL, distToStartTR, distToStartLL, distToStartLR };
        //                    //_buttonInfos[button.Id].DistToStartRange = new Range(dists.Min(), dists.Max());

        //                    // Correct way of finding min and max dist
        //                    _buttonInfos[button.Id].DistToStartRange = GetMinMaxDistances(buttonCenterAbsolute, _objectConstraintRectAbsolute);

        //                    // Update min/max x and y for grid bounds
        //                    _gridMinX = Math.Min(_gridMinX, buttonRect.Left);
        //                    _gridMinY = Math.Min(_gridMinY, buttonRect.Top);
        //                    _gridMaxX = Math.Max(_gridMaxX, buttonRect.Right);
        //                    _gridMaxY = Math.Max(_gridMaxY, buttonRect.Bottom);

        //                    if (positionInWindow.x <= _topLeftButtonPosition.x && positionInWindow.y <= _topLeftButtonPosition.y)
        //                    {
        //                        //this.TrialInfo($"Top-left button position updated: {positionInWindow} for button ID#{button.Id}");
        //                        _topLeftButtonPosition = positionInWindow; // Update the top-left button position
        //                        //_lastMarkedButtonId = button.Id; // Set the last highlighted button to this one
        //                    }

        //                    //this.TrialInfo($"Registered button ID#{button.Id}, Wx{button.WidthMultiple} | Position: {positionInWindow}");
        //                }
        //            }
        //        }
        //    }

        //    //this.TrialInfo($"Finished registering buttons in group. Current allButtons count: {_allButtons.Count}");
        //    // Set the first button as the highlighted button
        //    //_lastMarkedButtonId = _widthButtons.FirstOrDefault().Value.FirstOrDefault()?.Id ?? -1; // Get the first button ID or -1 if no buttons are present
        //}

        /// <summary>
        /// Calculates and stores the spatial neighbor links for every button
        /// by setting the neighbor IDs directly on each SButton instance.
        /// </summary>
        //private void LinkButtonNeighbors()
        //{
        //    this.TrialInfo("Linking neighbor IDs for all buttons...");
        //    //if (_allButtons.Count == 0) return;
        //    if (_buttonInfos.Count == 0) return;

        //    // For each button in the grid...
        //    foreach (int buttonId in _buttonInfos.Keys)
        //    {
        //        // ...find its neighbor in each of the four directions.
        //        SButton topNeighbor = GetNeighbor(_buttonInfos[buttonId].Button, Side.Top);
        //        SButton bottomNeighbor = GetNeighbor(_buttonInfos[buttonId].Button, Side.Down);
        //        SButton leftNeighbor = GetNeighbor(_buttonInfos[buttonId].Button, Side.Left);
        //        SButton rightNeighbor = GetNeighbor(_buttonInfos[buttonId].Button, Side.Right);

        //        // Get the ID of each neighbor, or -1 if the neighbor is null.
        //        int topId = topNeighbor?.Id ?? -1;
        //        int bottomId = bottomNeighbor?.Id ?? -1;
        //        int leftId = leftNeighbor?.Id ?? -1;
        //        int rightId = rightNeighbor?.Id ?? -1;

        //        // Call the method on the button to store its neighbor IDs.
        //        _buttonInfos[buttonId].Button.SetNeighbors(topId, bottomId, leftId, rightId);
        //    }

        //    //foreach (SButton button in _allButtons.Values)
        //    //{
        //    //    // ...find its neighbor in each of the four directions.
        //    //    SButton topNeighbor = GetNeighbor(button, Side.Top);
        //    //    SButton bottomNeighbor = GetNeighbor(button, Side.Down);
        //    //    SButton leftNeighbor = GetNeighbor(button, Side.Left);
        //    //    SButton rightNeighbor = GetNeighbor(button, Side.Right);

        //    //    // Get the ID of each neighbor, or -1 if the neighbor is null.
        //    //    int topId = topNeighbor?.Id ?? -1;
        //    //    int bottomId = bottomNeighbor?.Id ?? -1;
        //    //    int leftId = leftNeighbor?.Id ?? -1;
        //    //    int rightId = rightNeighbor?.Id ?? -1;

        //    //    // Call the method on the button to store its neighbor IDs.
        //    //    button.SetNeighbors(topId, bottomId, leftId, rightId);
        //    //}
        //    //this.TrialInfo($"Finished linking neighbors for {_allButtons.Count} buttons.");
        //}

        private void AddElementToCanvas(Element element, int left, int top)
        {
            Canvas.SetLeft(element, left);
            Canvas.SetTop(element, top);
            canvas.Children.Add(element);
            Conlog<SideWindow>($"Adding element: {element.Id}, Left: {left}, Top: {top}, W: {element.ElementWidth}, H: {element.ElementHeight}");
            _gridElements.Add(element.Id, element); // Cast to Element if needed
        }

        //public (string, Point) GetRandomElementByWidth(double widthMM)
        //{
        //    int widthPX = Utils.MM2PX(widthMM);
        //    foreach (Element element in _gridElements.Values)
        //    {
        //        Conlog<SideWindow>($"Element: {element.Id}, Width: {element.ElementWidth}");
        //    }

        //    // 1. Filter the list to get only elements with the target width
        //    List<Element> matchingElements = _gridElements.Values
        //                                    .Where(e => e.ElementWidth == widthPX)
        //                                    .ToList(); // Convert to List to use indexing

        //    // 2. Check if any matching elements were found
        //    if (matchingElements.Count == 0)
        //    {
        //        this.TrialInfo($"No elements found with width: {widthPX}");
        //        return ("", new Point());
        //    }

        //    // 3. Select a random index
        //    int randomIndex = _random.Next(matchingElements.Count);

        //    // 4. Return the element at the random index
        //    string key = matchingElements[randomIndex].Id;
        //    return (key, GetElementCenter(key));
        //}

        public void SelectElement()
        {
            // De-select all elements first
            foreach (var element in _gridElements.Values)
            {
                element.ElementStroke = Config.BUTTON_DEFAULT_BORDER_COLOR;
                element.ElementStrokeThickness = Config.ELEMENT_BORDER_THICKNESS;
            }

            string elementKey = $"C{_selectedElement.colInd}-R{_selectedElement.rowInd}";
            if (_gridElements.ContainsKey(elementKey))
            {
                Element element = _gridElements[elementKey];
                element.ElementStroke = Config.ELEMENT_HIGHLIGHT_COLOR;
                element.ElementStrokeThickness = Config.ELEMENT_BORDER_THICKNESS;
            }
            else
            {
                Console.WriteLine($"Element {elementKey} not found.");
            }
        }

        public void SelectElement(int rowId, int colId)
        {
            _selectedElement.rowInd = rowId;
            _selectedElement.colInd = colId;
            SelectElement();
        }

        public void MoveSelection(int dCol, int dRow)
        {
            _selectedElement.rowInd += dRow;
            _selectedElement.colInd += dCol;
            if (_selectedElement.rowInd < 0) _selectedElement.rowInd = 0;
            if (_selectedElement.colInd < 0) _selectedElement.colInd = 0;
            SelectElement();
        }

        public void ColorElement(string elementId, Brush color)
        {
            this.TrialInfo($"Element Key: {elementId}");
            if (_gridElements.ContainsKey(elementId))
            {
                Element element = _gridElements[elementId];
                element.ElementFill = color;
            }
            else
            {
                Console.WriteLine($"Element {elementId} not found.");
            }
        }

        public void ResetElements()
        {
            foreach (Element element in _gridElements.Values)
            {
                element.ElementFill = Config.BUTTON_DEFAULT_FILL_COLOR; // Reset to default color
            }
        }

        public Point GetElementCenter(string key)
        {
            Element element = _gridElements[key];

            return new Point
            {
                X = Canvas.GetLeft(element) + element.ElementWidth / 2,
                Y = Canvas.GetTop(element) + element.ElementWidth / 2
            };
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

        public override Task PlaceGrid(Func<Grid> gridCreator, double topPadding, double leftPadding)
        {
            // A TaskCompletionSource allows us to create a Task
            // that we can complete manually later.
            var tcs = new TaskCompletionSource<bool>();

            // Clear any existing columns from the canvas and the list before generating new ones
            canvas.Children.Clear();

            _buttonsGrid = gridCreator(); // Create the new column Grid

            // Set left position on the Canvas (horizontally centered)
            //Output.TrialInfo(this, $"Placing single grid with size {grid.Width} in {this.Width}...");
            //double leftPosition = (this.Width - grid.Width) / 2;
            //Canvas.SetLeft(grid, leftPosition);

            // Set top position on the Canvas (from padding)
            Canvas.SetTop(_buttonsGrid, topPadding);

            // Add to the Canvas
            canvas.Children.Add(_buttonsGrid);

            //double leftPosition = (this.Width - _buttonsGrid.ActualWidth) / 2;
            //Canvas.SetLeft(_buttonsGrid, leftPosition);

            

            // Subscribe to the Loaded event to get the correct width.
            _buttonsGrid.Loaded += (sender, e) =>
            {
                try
                {
                    // Now ActualWidth has a valid value.
                    double leftPosition = (this.Width - _buttonsGrid.ActualWidth) / 2;
                    Canvas.SetLeft(_buttonsGrid, leftPosition);

                    RegisterAllButtons(_buttonsGrid);
                    LinkButtonNeighbors();

                    FindMiddleButton();

                    // Indicate that the task is successfully completed.
                    tcs.SetResult(true);

                    // Register buttons after the grid is loaded and positioned.
                    //Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
                    //{
                    //    RegisterAllButtons();
                    //    LinkButtonNeighbors();
                    //}));
                }
                catch (Exception ex)
                {
                    // If any error occurs, set the exception on the TaskCompletionSource
                    tcs.SetException(ex);
                }
            };

            return tcs.Task; // Return the Task to be awaited

        }

    }
}
