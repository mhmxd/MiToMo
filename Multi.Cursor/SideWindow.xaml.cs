using SkiaSharp;
using SkiaSharp.Views.WPF;
using Svg.Skia;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using WindowsInput;
using static Multi.Cursor.Output;
using Seril = Serilog.Log;

namespace Multi.Cursor
{
    /// <summary>
    /// Interaction logic for SideWindow.xaml
    /// </summary>
    public partial class SideWindow : Window
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


        private int windowWidth, windowHeight;
        private int _canvasWidth, _canvasHeight;

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

        private TranslateTransform _cursorTransform;

        private Dictionary<string, Rectangle> _gridElements = new Dictionary<string, Rectangle>(); // Key: "C{col}-R{row}", Value: Rectangle element
        private Dictionary<int, string> _elementWidths = new Dictionary<int, string>(); // Key: Width (px), Value: Element Key

        public SideWindow(string title, Point relPos)
        {
            InitializeComponent();
            WindowTitle = title;
            this.DataContext = this; // Set DataContext for data binding

            EnableMouseInPointer(true);
            SetForegroundWindow(new WindowInteropHelper(this).Handle);

            _relPos = relPos;

            _auxursor = new Auxursor(Config.FRAME_DUR_MS / 1000.0);

            _cursorTransform = (TranslateTransform)FindResource("CursorTransform");
        }

        /// <summary>
        /// Show the target
        /// </summary>
        /// <param name="targetWidth"> Width (diameter) of the target circle </param>
        /// <returns> Position of the target top-left (rel. to this window) </returns>
        //public Point ShowTarget(int targetWidth, Brush fill, 
        //    MouseEventHandler mouseEnterHandler, MouseEventHandler mouseLeaveHandler,
        //    MouseButtonEventHandler buttonDownHandler, MouseButtonEventHandler buttonUpHandler)
        //{
        //    // Get canvas dimensions
        //    _canvasWidth = (int)canvas.ActualWidth;
        //    _canvasHeight = (int)canvas.ActualHeight;

        //    // Ensure the Target stays fully within bounds (min/max for top-left)
        //    int marginPX = Utils.MM2PX(Config.WINDOW_PADDING_MM);
        //    int minX = marginPX;
        //    int maxX = _canvasWidth - marginPX - targetWidth;
        //    int minY = marginPX;
        //    int maxY = _canvasHeight - marginPX - targetWidth;

        //    // Generate random position
        //    double randomX = _random.Next(minX, maxX);
        //    double randomY = _random.Next(minY, maxY);

        //    // Create the target
        //    targetHalfW = targetWidth / 2;
        //    _target = new Rectangle
        //    {
        //        Width = targetWidth,
        //        Height = targetWidth,
        //        Fill = fill,
        //    };

        //    // Position the target on the Canvas
        //    Canvas.SetLeft(_target, randomX);
        //    Canvas.SetTop(_target, randomY);

        //    //--- TEMP (for measurement)
        //    // Longest dist
        //    //Canvas.SetLeft(_target, minX);
        //    //Canvas.SetTop(_target, minY);
        //    // Shortest dist
        //    //Canvas.SetLeft(_target, maxX - targetWidth);
        //    //Canvas.SetTop(_target, minY);


        //    // Add events
        //    _target.MouseEnter += mouseEnterHandler;
        //    _target.MouseLeave += mouseLeaveHandler;
        //    _target.MouseLeftButtonDown += buttonDownHandler;
        //    _target.MouseLeftButtonUp += buttonUpHandler;

        //    // Add the circle to the Canvas
        //    canvas.Children.Add(_target);

        //    // Set index
        //    Canvas.SetZIndex(_target, 0);
        //    Canvas.SetZIndex(activeCursor, 1);
        //    Canvas.SetZIndex(inactiveCursor, 1);

        //    return new Point(randomX, randomY);
        //}

        //public void ShowTarget(Point position, int targetW, Brush fill,
        //    MouseEventHandler mouseEnterHandler, MouseEventHandler mouseLeaveHandler,
        //    MouseButtonEventHandler buttonDownHandler, MouseButtonEventHandler buttonUpHandler)
        //{

        //    // Create the target
        //    targetHalfW = targetW / 2;
        //    _target = new Rectangle
        //    {
        //        Width = targetW,
        //        Height = targetW,
        //        Fill = fill,
        //    };

        //    // Position the target on the Canvas
        //    Canvas.SetLeft(_target, position.X);
        //    Canvas.SetTop(_target, position.Y);
            
        //    // Add events
        //    _target.MouseEnter += mouseEnterHandler;
        //    _target.MouseLeave += mouseLeaveHandler;
        //    _target.MouseLeftButtonDown += buttonDownHandler;
        //    _target.MouseLeftButtonUp += buttonUpHandler;

        //    // Add the circle to the Canvas
        //    canvas.Children.Add(_target);

        //    // Set index
        //    Canvas.SetZIndex(_target, 0);
        //    Canvas.SetZIndex(activeCursor, 1);
        //    Canvas.SetZIndex(inactiveCursor, 1);
        //}

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
            Canvas.SetZIndex(activeCursor, 1);
            Canvas.SetZIndex(inactiveCursor, 1);
        }

        public void ColorTarget(Brush color)
        {
            _target.Fill = color;
        }

        public bool IsCursorInsideTarget()
        {
            // Get circle's center
            double centerX = Canvas.GetLeft(_target) + targetHalfW;
            double centerY = Canvas.GetTop(_target) + targetHalfW;

            // Calculate distance from the point to the circle's center
            double distance = Math.Sqrt(
                Math.Pow(_cursorTransform.X - centerX, 2) + Math.Pow(_cursorTransform.Y - centerY, 2)
                );

            // Check if the distance is less than or equal to the radius
            return distance <= targetHalfW;
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
            //    Math.Pow(p.X - centerX, 2) + Math.Pow(p.Y - centerY, 2)
            //    );

            // Check if the distance is less than or equal to the radius
            return targetRect.Contains(p); 
        }

        public void ShowCursor(int x, int y)
        {

            // Show the simulated cursor
            inactiveCursor.Visibility = Visibility.Hidden;
            activeCursor.Visibility = Visibility.Visible;
            _cursorTransform.X = x;
            _cursorTransform.Y = y;
        }

        public void ShowCursor(Point p)
        {
            ShowCursor((int)p.X, (int)p.Y);
        }

        public void ShowCursor(Location location)
        {
            Point position = new Point();

            switch (location)
            {
                case Location.Left:
                    position.X = canvas.ActualWidth / 4.0; // Middle of the left
                    position.Y = canvas.ActualHeight / 2.0; // Middle of height
                    break;
                case Location.Top:
                    position.X = canvas.ActualWidth / 2.0; // Middle of width
                    position.Y = canvas.ActualHeight / 4.0; // Middle of the top
                    break;
                case Location.Bottom:
                    position.X = canvas.ActualWidth / 2.0; // Middle of width
                    position.Y = canvas.ActualHeight * 3 / 4.0; // Middle of the top
                    break;
                case Location.Right:
                    position.X = canvas.ActualWidth * 3 / 4.0; // Middle of the right
                    position.Y = canvas.ActualHeight / 2.0; // Middle of height
                    break;
                case Location.Middle:
                    position.X = canvas.ActualWidth / 2.0; // Middle of width
                    position.Y = canvas.ActualHeight / 2.0; // Middle of height
                    break;
            }
            
            //inactiveCursor.Visibility = Visibility.Visible;
            activeCursor.Visibility = Visibility.Visible;

            _cursorTransform.X = position.X;
            _cursorTransform.Y = position.Y;

            //_cursorTransform.X = position.X;
            //_cursorTransform.Y = 10;
        }


        public void ActivateCursor()
        {
            inactiveCursor.Visibility = Visibility.Hidden;
            activeCursor.Visibility = Visibility.Visible;
            _auxursor.Activate();
        }

        public void DeactivateCursor()
        {
            activeCursor.Visibility = Visibility.Hidden;
            inactiveCursor.Visibility = Visibility.Visible;
            _auxursor.Deactivate();
        }

        public void UpdateCursor(TouchPoint tp)
        {
            (double dX, double dY) = _auxursor.Update(tp);
            // Only move if above the threshold
            double moveMagnitude = Math.Sqrt(dX * dX + dY * dY);
            if (moveMagnitude >= Config.MIN_MOVEMENT_THRESHOLD)
            {
                MoveCursor(dX, dY);
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

            // X: Within boundaries
            if (potentialX < 0)
            {
                dX = -_cursorTransform.X + 3;
            }
            else if (potentialX > ActualWidth) 
            {
                dX = windowWidth - 10 - _cursorTransform.X;
            }

            // Y: Within boundaries
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
                // X: Within boundaries
                if (potentialX < 0) 
                {
                    dX = -currentX; // Don't stick it all the way
                } else if (potentialX > windowWidth)
                {
                    dX = windowWidth - currentX;
                }

                // Y: Within boundaries
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
            inactiveCursor.Visibility = Visibility.Hidden;
            activeCursor.Visibility = Visibility.Hidden;
            _auxursor.Deactivate();
            //Mouse.OverrideCursor = Cursors.None;
        }

        public bool HasCursor()
        {
            return _isCursorVisible;
        }

        public void ClearTarget()
        {
            canvas.Children.Remove(_target);
        }

        public void KnollHorizontal(int minNumCols, int maxNumCols, 
            MouseEventHandler mouseEnterHandler, MouseEventHandler mouseLeaveHandler,
            MouseButtonEventHandler buttonDownHandler, MouseButtonEventHandler buttonUpHandler)
        {
            // Choose a random number of columns with set widths
            //int maxNumCols = 10; // Max W = 10*45mm + 9*1mm = 459mm
            //int minNumCols = 5; // Min W = 5*3mm + 4*1mm = 19mm
            //int numCols = _random.Next(minNumCols, maxNumCols + 1);
            //List<double> colWidths = new List<double>();
            //for (int i = 0; i < numCols; i++)
            //{
            //    //double colWidth = Utils.RandDouble(Config.GRID_MIN_ELEMENT_WIDTH_MM, Config.GRID_MAX_ELEMENT_WIDTH_MM);
            //    colWidths.Add(colWidth);
            //}

            // Choose a random number of columns (multiples of widths) within the specified range
            List<int> possibleCols = new List<int>();
            for (int i = minNumCols; i <= maxNumCols; i += Experiment.GetNumGridTargetWidths())
            {
                possibleCols.Add(i);
            }
            int numCols = possibleCols[_random.Next(possibleCols.Count)];

            // Calculate how many times each width should appear
            int nRepetitionsPerWidth = numCols / 3;

            // Create the Base List with equal repetitions
            List<double> colWidths = new List<double>();
            foreach (double width in Experiment.GetGridTargetWidthsMM())
            {
                for (int i = 0; i < nRepetitionsPerWidth; i++)
                {
                    colWidths.Add(width);
                }
            }

            // Shuffle the widths to randomize their order
            colWidths.Shuffle();

            // For each column, randomly choose a height formation (1 to 4)
            double minW = Experiment.GetGridMinTargetWidthMM();
            List<int> colFormations = new List<int>();
            for (int i = 0; i < numCols; i++)
            {
                int formation = _random.Next(1, 5); // 1 to 4
                if (colWidths[i] == minW) formation = 3; // Don't go full H with small targets

                colFormations.Add(formation);
            }

            // Create the grid
            Brush defaultElementColor = Config.ELEMENT_DEFAULT_COLOR;
            int gutter = Utils.MM2PX(Config.GRID_GUTTER_MM);
            int padding = Utils.MM2PX(Config.WINDOW_PADDING_MM); // Assuming this is the top/bottom window padding
            int colX = padding; // Start from the left (increased inside the loop)

            // This represents the total height available for the *grid content* within the window padding.
            // This is the height we want all columns to span from top-most content edge to bottom-most content edge.
            int totalGridContentHeight = (int)ActualHeight - 2 * padding;

            // Fractions of height


            for (int i = 0; i < numCols; i++)
            {
                // Create elements based on the formation
                int colW = Utils.MM2PX(colWidths[i]);
                Conlog<SideWindow>($"Column {i}: W = {colW}, Form = {colFormations[i]}");

                // All elements in all columns start at the same Canvas.Top position (after the top window padding)
                int currentY = padding;

                switch (colFormations[i])
                {
                    case 1: // Single element (1/1 H)
                            // This column has 1 element. Its total height should be totalGridContentHeight.
                            // The single element takes up all of this space.

                        Rectangle topElement_case1 = CreateElement(
                            colW, totalGridContentHeight,
                            mouseEnterHandler, mouseLeaveHandler, buttonDownHandler, buttonUpHandler);

                        AddElementToCanvas(topElement_case1, colX, currentY);

                        string elementId = $"C{i}-R0"; // Column i, Row 0
                        _gridElements.Add(elementId, topElement_case1);
                        _elementWidths[colW] = elementId; // Store the width and element key

                        break;

                    case 2: // 2/3H top, 1/3H bottom
                            // This column has 2 elements and 1 internal gutter.
                            // The available space for elements + internal gutter is totalGridContentHeight.
                            // We want fixed element sizes, so we calculate the 2/3 and 1/3 heights first.
                            // totalGridContentHeight = (height2_3) + (gutter) + (height1_3)
                            // So, (height2_3 + height1_3) = totalGridContentHeight - gutter

                        double effectiveHeightForElements_case2 = (double)totalGridContentHeight - gutter; // Remaining height after 1 internal gutter

                        // Calculate target element heights
                        int targetHeight2_3 = (int)Math.Round(2.0 * effectiveHeightForElements_case2 / 3.0);
                        int targetHeight1_3 = (int)Math.Round(effectiveHeightForElements_case2 / 3.0);

                        // Calculate the actual sum of these rounded heights
                        int sumOfTargetHeights_case2 = targetHeight2_3 + targetHeight1_3;
                        // The difference between the desired total element height and the actual sum is distributed to the gutter
                        int extraHeightForGutter_case2 = (int)effectiveHeightForElements_case2 - sumOfTargetHeights_case2;

                        Rectangle topElement_case2 = CreateElement(
                            colW, targetHeight2_3,
                            mouseEnterHandler, mouseLeaveHandler, buttonDownHandler, buttonUpHandler);

                        AddElementToCanvas(topElement_case2, colX, currentY);

                        elementId = $"C{i}-R0"; // Column i, Row 0
                        _gridElements.Add(elementId, topElement_case2);
                        _elementWidths[colW] = elementId; // Store the width and element key

                        // Adjust the gutter to absorb the rounding difference
                        currentY += (int)topElement_case2.Height + gutter + extraHeightForGutter_case2;

                        Rectangle bottomElement_case2 = CreateElement(
                            colW, targetHeight1_3,
                            mouseEnterHandler, mouseLeaveHandler, buttonDownHandler, buttonUpHandler);

                        AddElementToCanvas(bottomElement_case2, colX, currentY);

                        elementId = $"C{i}-R1"; // Column i, Row 1
                        _gridElements.Add(elementId, bottomElement_case2);
                        _elementWidths[colW] = elementId; // Store the width and element key
                        break;

                    case 3: // 1/3H top, 1/3H middle, 1/3H bottom
                            // This column has 3 elements and 2 internal gutters.
                            // (height1_3 + height1_3 + height1_3) = totalGridContentHeight - (2 * gutter)
                        double effectiveHeightForElements_case3 = (double)totalGridContentHeight - (2 * gutter);

                        int targetHeight1_3_seg = (int)Math.Round(effectiveHeightForElements_case3 / 3.0);

                        // Calculate the actual sum of these rounded heights
                        int sumOfTargetHeights_case3 = 3 * targetHeight1_3_seg;
                        // The difference between the desired total element height and the actual sum is distributed to the gutter
                        int extraHeightForGutter_case3 = (int)effectiveHeightForElements_case3 - sumOfTargetHeights_case3;

                        Rectangle topElement_case3 = CreateElement(
                            colW, targetHeight1_3_seg,
                            mouseEnterHandler, mouseLeaveHandler, buttonDownHandler, buttonUpHandler);

                        AddElementToCanvas(topElement_case3, colX, currentY);

                        elementId = $"C{i}-R0"; // Column i, Row 0
                        _gridElements.Add(elementId, topElement_case3);
                        _elementWidths[colW] = elementId; // Store the width and element key

                        // Distribute the extra height from rounding
                        int gutter1_height = gutter + (extraHeightForGutter_case3 / 2); // Split extra height if two gutters
                        int gutter2_height = gutter + (extraHeightForGutter_case3 - (extraHeightForGutter_case3 / 2));

                        currentY += (int)topElement_case3.Height + gutter1_height;

                        Rectangle middleElement_case3 = CreateElement(
                            colW, targetHeight1_3_seg,
                            mouseEnterHandler, mouseLeaveHandler, buttonDownHandler, buttonUpHandler);

                        AddElementToCanvas(middleElement_case3, colX, currentY);

                        elementId = $"C{i}-R1"; // Column i, Row 1
                        _gridElements.Add(elementId, middleElement_case3);
                        _elementWidths[colW] = elementId; // Store the width and element key

                        currentY += (int)middleElement_case3.Height + gutter2_height;

                        Rectangle bottomElement_case3 = CreateElement(
                            colW, targetHeight1_3_seg,
                            mouseEnterHandler, mouseLeaveHandler, buttonDownHandler, buttonUpHandler);

                        AddElementToCanvas(bottomElement_case3, colX, currentY);

                        elementId = $"C{i}-R2"; // Column i, Row 2
                        _gridElements.Add(elementId, bottomElement_case3);
                        _elementWidths[colW] = elementId; // Store the width and element key
                        break;

                    case 4: // 1/3H top, 2/3H bottom
                            // This column has 2 elements and 1 internal gutter.
                            // (height1_3 + height2_3) = totalGridContentHeight - gutter
                        double effectiveHeightForElements_case4 = (double)totalGridContentHeight - gutter;

                        // Calculate target element heights
                        targetHeight1_3 = (int)Math.Round(effectiveHeightForElements_case4 / 3.0);
                        targetHeight2_3 = (int)Math.Round(2.0 * effectiveHeightForElements_case4 / 3.0);

                        // Calculate the actual sum of these rounded heights
                        int sumOfTargetHeights_case4 = targetHeight1_3 + targetHeight2_3;
                        // The difference is distributed to the gutter
                        int extraHeightForGutter_case4 = (int)effectiveHeightForElements_case4 - sumOfTargetHeights_case4;

                        Rectangle topElement_case4 = CreateElement(
                            colW, targetHeight1_3,
                            mouseEnterHandler, mouseLeaveHandler, buttonDownHandler, buttonUpHandler);

                        AddElementToCanvas(topElement_case4, colX, currentY);

                        elementId = $"C{i}-R0";
                        _gridElements.Add(elementId, topElement_case4);
                        _elementWidths[colW] = elementId; // Store the width and element key

                        // Adjust the gutter to absorb the rounding difference
                        currentY += (int)topElement_case4.Height + gutter + extraHeightForGutter_case4;

                        Rectangle bottomElement_case4 = CreateElement(
                            colW, targetHeight2_3,
                            mouseEnterHandler, mouseLeaveHandler, buttonDownHandler, buttonUpHandler);

                        AddElementToCanvas(bottomElement_case4, colX, currentY);

                        elementId = $"C{i}-R1"; // Column i, Row 1
                        _gridElements.Add(elementId, bottomElement_case4);
                        _elementWidths[colW] = elementId; // Store the width and element key
                        break;
                }

                // Move forward
                colX += colW + gutter;
            }

        }

        public void KnollVertical(int minNumRows, int maxNumRows)
        {
            // Choose a random number of rows with random heights
            int numRows = _random.Next(minNumRows, maxNumRows + 1);
            List<double> rowHeights = new List<double>();
            for (int i = 0; i < numRows; i++)
            {
                double rowHeight = Utils.RandDouble(Config.GRID_MIN_ELEMENT_WIDTH_MM, Config.GRID_MAX_ELEMENT_WIDTH_MM); // Reusing width config for height
                rowHeights.Add(rowHeight);
            }

            // For each row, randomly choose a horizontal formation (1 to 4)
            // Cases will now represent horizontal divisions:
            // Case 1: 1/1 W (full width)
            // Case 2: 2/3 W left, 1/3 W right
            // Case 3: 1/3 W left, 1/3 W middle, 1/3 W right
            // Case 4: 1/3 W left, 2/3 W right
            List<int> rowFormations = new List<int>();
            for (int i = 0; i < numRows; i++)
            {
                int formation = _random.Next(1, 5); // 1 to 4
                rowFormations.Add(formation);
            }

            // Create the grid
            int gutter = Utils.MM2PX(Config.GRID_GUTTER_MM);
            int padding = Utils.MM2PX(Config.WINDOW_PADDING_MM); // Assuming this is the left/right window padding
            int rowY = padding; // Start from the top (increased inside the loop)

            // This represents the total width available for the *grid content* within the window padding.
            // This is the width we want all rows to span from left-most content edge to right-most content edge.
            int totalGridContentWidth = (int)ActualWidth - 2 * padding; // Changed to ActualWidth

            for (int i = 0; i < numRows; i++)
            {
                // Create elements based on the formation
                int rowH = Utils.MM2PX(rowHeights[i]); // This is the height of the current row
                Conlog<SideWindow>($"Row {i}: H = {rowH}, Form = {rowFormations[i]}");

                // All elements in all rows start at the same Canvas.Left position (after the left window padding)
                int currentX = padding; // Changed from currentY to currentX

                switch (rowFormations[i])
                {
                    case 1: // Single element (1/1 W)
                            // This row has 1 element. Its total width should be totalGridContentWidth.
                            // The single element takes up all of this space.
                        Rectangle leftElement = new Rectangle // Renamed topElement to leftElement
                        {
                            Width = totalGridContentWidth, // Changed from Height to Width
                            Height = rowH,
                            Fill = Brushes.Blue
                        };
                        Canvas.SetLeft(leftElement, currentX); // Position relative to the Canvas's left edge
                        Canvas.SetTop(leftElement, rowY);
                        canvas.Children.Add(leftElement);
                        Conlog<SideWindow>($"Case 1: Element Width = {leftElement.Width}, Left = {Canvas.GetLeft(leftElement)}");
                        break;

                    case 2: // 2/3 W left, 1/3 W right
                            // This row has 2 elements and 1 internal gutter.
                            // The available space for elements + internal gutter is totalGridContentWidth.
                            // We want fixed element sizes, so we calculate the 2/3 and 1/3 widths first.
                            // totalGridContentWidth = (width2_3) + (gutter) + (width1_3)
                            // So, (width2_3 + width1_3) = totalGridContentWidth - gutter

                        double effectiveWidthForElements_case2 = (double)totalGridContentWidth - gutter; // Remaining width after 1 internal gutter

                        // Calculate target element widths
                        int targetWidth2_3 = (int)Math.Round(2.0 * effectiveWidthForElements_case2 / 3.0);
                        int targetWidth1_3 = (int)Math.Round(effectiveWidthForElements_case2 / 3.0);

                        // Calculate the actual sum of these rounded widths
                        int sumOfTargetWidths_case2 = targetWidth2_3 + targetWidth1_3;
                        // The difference between the desired total element width and the actual sum is distributed to the gutter
                        int extraWidthForGutter_case2 = (int)effectiveWidthForElements_case2 - sumOfTargetWidths_case2;

                        leftElement = new Rectangle // Renamed topElement to leftElement
                        {
                            Width = targetWidth2_3, // Changed from Height to Width
                            Height = rowH,
                            Fill = Brushes.Blue
                        };
                        Canvas.SetLeft(leftElement, currentX);
                        Canvas.SetTop(leftElement, rowY);
                        canvas.Children.Add(leftElement);
                        Conlog<SideWindow>($"Case 2: Left Element Width = {leftElement.Width}, Left = {Canvas.GetLeft(leftElement)}");

                        // Adjust the gutter to absorb the rounding difference
                        currentX += (int)leftElement.Width + gutter + extraWidthForGutter_case2; // Changed from currentY to currentX

                        Rectangle rightElement_case2 = new Rectangle // Renamed bottomElement to rightElement
                        {
                            Width = targetWidth1_3, // Changed from Height to Width
                            Height = rowH,
                            Fill = Brushes.Blue
                        };
                        Canvas.SetLeft(rightElement_case2, currentX);
                        Canvas.SetTop(rightElement_case2, rowY);
                        canvas.Children.Add(rightElement_case2);
                        Conlog<SideWindow>($"Case 2: Right Element Width = {rightElement_case2.Width}, Left = {Canvas.GetLeft(rightElement_case2)}");
                        break;

                    case 3: // 1/3 W left, 1/3 W middle, 1/3 W right
                            // This row has 3 elements and 2 internal gutters.
                            // (width1_3 + width1_3 + width1_3) = totalGridContentWidth - (2 * gutter)
                        double effectiveWidthForElements_case3 = (double)totalGridContentWidth - (2 * gutter);

                        int targetWidth1_3_seg = (int)Math.Round(effectiveWidthForElements_case3 / 3.0);

                        // Calculate the actual sum of these rounded widths
                        int sumOfTargetWidths_case3 = 3 * targetWidth1_3_seg;
                        // The difference between the desired total element width and the actual sum is distributed to the gutter
                        int extraWidthForGutter_case3 = (int)effectiveWidthForElements_case3 - sumOfTargetWidths_case3;

                        leftElement = new Rectangle // Renamed topElement to leftElement
                        {
                            Width = targetWidth1_3_seg, // Changed from Height to Width
                            Height = rowH,
                            Fill = Brushes.Blue
                        };
                        Canvas.SetLeft(leftElement, currentX);
                        Canvas.SetTop(leftElement, rowY);
                        canvas.Children.Add(leftElement);
                        Conlog<SideWindow>($"Case 3: Left Element Width = {leftElement.Width}, Left = {Canvas.GetLeft(leftElement)}");

                        // Distribute the extra width from rounding
                        int gutter1_width = gutter + (extraWidthForGutter_case3 / 2); // Split extra width if two gutters
                        int gutter2_width = gutter + (extraWidthForGutter_case3 - (extraWidthForGutter_case3 / 2));

                        currentX += (int)leftElement.Width + gutter1_width; // Changed from currentY to currentX

                        Rectangle middleElement_case3 = new Rectangle
                        {
                            Width = targetWidth1_3_seg, // Changed from Height to Width
                            Height = rowH,
                            Fill = Brushes.Blue
                        };
                        Canvas.SetLeft(middleElement_case3, currentX);
                        Canvas.SetTop(middleElement_case3, rowY);
                        canvas.Children.Add(middleElement_case3);
                        Conlog<SideWindow>($"Case 3: Middle Element Width = {middleElement_case3.Width}, Left = {Canvas.GetLeft(middleElement_case3)}");

                        currentX += (int)middleElement_case3.Width + gutter2_width; // Changed from currentY to currentX

                        Rectangle rightElement_case3 = new Rectangle // Renamed bottomElement to rightElement
                        {
                            Width = targetWidth1_3_seg, // Changed from Height to Width
                            Height = rowH,
                            Fill = Brushes.Blue
                        };
                        Canvas.SetLeft(rightElement_case3, currentX);
                        Canvas.SetTop(rightElement_case3, rowY);
                        canvas.Children.Add(rightElement_case3);
                        Conlog<SideWindow>($"Case 3: Right Element Width = {rightElement_case3.Width}, Left = {Canvas.GetLeft(rightElement_case3)}");
                        break;

                    case 4: // 1/3 W left, 2/3 W right
                            // This row has 2 elements and 1 internal gutter.
                            // (width1_3 + width2_3) = totalGridContentWidth - gutter
                        double effectiveWidthForElements_case4 = (double)totalGridContentWidth - gutter;

                        // Calculate target element widths
                        targetWidth1_3 = (int)Math.Round(effectiveWidthForElements_case4 / 3.0);
                        targetWidth2_3 = (int)Math.Round(2.0 * effectiveWidthForElements_case4 / 3.0);

                        // Calculate the actual sum of these rounded widths
                        int sumOfTargetWidths_case4 = targetWidth1_3 + targetWidth2_3;
                        // The difference is distributed to the gutter
                        int extraWidthForGutter_case4 = (int)effectiveWidthForElements_case4 - sumOfTargetWidths_case4;

                        leftElement = new Rectangle // Renamed topElement to leftElement
                        {
                            Width = targetWidth1_3, // Changed from Height to Width
                            Height = rowH,
                            Fill = Brushes.Blue
                        };
                        Canvas.SetLeft(leftElement, currentX);
                        Canvas.SetTop(leftElement, rowY);
                        canvas.Children.Add(leftElement);
                        Conlog<SideWindow>($"Case 4: Left Element Width = {leftElement.Width}, Left = {Canvas.GetLeft(leftElement)}");

                        // Adjust the gutter to absorb the rounding difference
                        currentX += (int)leftElement.Width + gutter + extraWidthForGutter_case4; // Changed from currentY to currentX

                        Rectangle rightElement_case4 = new Rectangle // Renamed bottomElement to rightElement
                        {
                            Width = targetWidth2_3, // Changed from Height to Width
                            Height = rowH,
                            Fill = Brushes.Blue
                        };
                        Canvas.SetLeft(rightElement_case4, currentX);
                        Canvas.SetTop(rightElement_case4, rowY);
                        canvas.Children.Add(rightElement_case4);
                        Conlog<SideWindow>($"Case 4: Right Element Width = {rightElement_case4.Width}, Left = {Canvas.GetLeft(rightElement_case4)}");
                        break;
                }

                // Move forward for the next row
                rowY += rowH + gutter; // Changed from colX to rowY, colW to rowH
            }
        }

        private Rectangle CreateElement(double w, double h,
            MouseEventHandler mouseEnterHandler, MouseEventHandler mouseLeaveHandler,
            MouseButtonEventHandler mouseDownHandler, MouseButtonEventHandler mouseUpHandler
            )
        {
            Rectangle rectangle = new Rectangle // Renamed topElement to leftElement
            {
                Width = w,
                Height = h,
                Fill = Config.ELEMENT_DEFAULT_COLOR
            };

            rectangle.MouseEnter += mouseEnterHandler;
            rectangle.MouseLeave += mouseLeaveHandler;
            rectangle.MouseDown += mouseDownHandler;
            rectangle.MouseUp += mouseUpHandler;

            return rectangle;
        }

        private void AddElementToCanvas(UIElement element, double left, double top)
        {
            Canvas.SetLeft(element, left);
            Canvas.SetTop(element, top);
            canvas.Children.Add(element);
        }

        public void HighlightElement(string elementKey)
        {
            if (_gridElements.ContainsKey(elementKey))
            {
                Rectangle element = _gridElements[elementKey];
                element.Stroke = Config.GRID_HIGHLIGHT_COLOR;
                element.StrokeThickness = Config.GRID_HIGHLIGHT_STROKE_THICKNESS;
            }
            else
            {
                Console.WriteLine($"Element {elementKey} not found.");
            }
        }

        public void TargetElement(string elementKey)
        {
            if (_gridElements.ContainsKey(elementKey))
            {
                Rectangle element = _gridElements[elementKey];
                element.Fill = Config.GRID_TARGET_COLOR;
            }
            else
            {
                Console.WriteLine($"Element {elementKey} not found.");
            }
        }

        public void ResetElements()
        {
            foreach (Rectangle element in _gridElements.Values)
            {
                element.Fill = Config.GRAY_A0A0A0; // Reset to default color
            }
        }

        public Point TargetRandomElementWithWidth(double widthMM)
        {
            int widthPX = Utils.MM2PX(widthMM);
            List<Rectangle> candidates = new List<Rectangle>();
            foreach (Rectangle element in _gridElements.Values)
            {
                if (element.Width == widthPX)
                {
                    candidates.Add(element);
                }
            }

            Rectangle randomElement = candidates[_random.Next(candidates.Count)];
            TrialInfo<SideWindow>($"Key = ");

            // Color the target element
            randomElement.Fill = Config.GRID_TARGET_COLOR;

            return new Point
            {
                X = Canvas.GetLeft(randomElement) + randomElement.Width / 2,
                Y = Canvas.GetTop(randomElement) + randomElement.Height / 2
            };
        }

    }
}
