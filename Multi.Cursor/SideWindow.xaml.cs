﻿using SkiaSharp;
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
        
        private double InterGroupGutter = Utils.MM2PX(Config.GRID_INTERGROUP_GUTTER_MM);
        private double WithinGroupGutter = Utils.MM2PX(Config.GRID_WITHINGROUP_GUTTER_MM);

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

        public SideWindow(string title, Point relPos)
        {
            InitializeComponent();
            WindowTitle = title;
            this.DataContext = this; // Set DataContext for data binding

            EnableMouseInPointer(true);
            SetForegroundWindow(new WindowInteropHelper(this).Handle);

            _relPos = relPos;

            _auxursor = new Auxursor(Config.FRAME_DUR_MS / 1000.0);
            _gridNavigator = new GridNavigator(Config.FRAME_DUR_MS / 1000.0);

            foreach (int wm in Experiment.BUTTON_MULTIPLES.Values)
            {
                _widthButtons.TryAdd(wm, new List<SButton>());
            }

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
            //    Math.Pow(_cursorTransform.X - centerX, 2) + Math.Pow(_cursorTransform.Y - centerY, 2)
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
            //    Math.Pow(p.X - centerX, 2) + Math.Pow(p.Y - centerY, 2)
            //    );

            // Check if the distance is less than or equal to the radius
            return targetRect.Contains(p); 
        }

        public void ShowCursor(int x, int y)
        {

            // Show the simulated cursor
            //inactiveCursor.Visibility = Visibility.Hidden;
            //activeCursor.Visibility = Visibility.Visible;
            //_cursorTransform.X = x;
            //_cursorTransform.Y = y;
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

            //_cursorTransform.X = position.X;
            //_cursorTransform.Y = 10;
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
            //inactiveCursor.Visibility = Visibility.Hidden;
            //activeCursor.Visibility = Visibility.Hidden;
            //_auxursor.Deactivate();
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

        //public void KnollHorizontal(int minNumCols, int maxNumCols, 
        //    MouseEventHandler mouseEnterHandler, MouseEventHandler mouseLeaveHandler,
        //    MouseButtonEventHandler buttonDownHandler, MouseButtonEventHandler buttonUpHandler)
        //{
        //    // Choose a random number of columns with set widths
        //    //int maxNumCols = 10; // Max W = 10*45mm + 9*1mm = 459mm
        //    //int minNumCols = 5; // Min W = 5*3mm + 4*1mm = 19mm
        //    //int numCols = _random.Next(minNumCols, maxNumCols + 1);
        //    //List<double> colWidths = new List<double>();
        //    //for (int i = 0; i < numCols; i++)
        //    //{
        //    //    //double colWidth = Utils.RandDouble(Config.GRID_MIN_ELEMENT_WIDTH_MM, Config.GRID_MAX_ELEMENT_WIDTH_MM);
        //    //    colWidths.Add(colWidth);
        //    //}

        //    // Choose a random number of columns (multiples of widths) within the specified range
        //    List<int> possibleCols = new List<int>();
        //    for (int i = minNumCols; i <= maxNumCols; i += Experiment.GetNumGridTargetWidths())
        //    {
        //        possibleCols.Add(i);
        //    }
        //    int numCols = possibleCols[_random.Next(possibleCols.Count)];

        //    // Calculate how many times each width should appear
        //    int nRepetitionsPerWidth = numCols / 3;

        //    // Create the Base List with equal repetitions
        //    List<int> colWidths = new List<int>();
        //    foreach (double width in Experiment.GetGridTargetWidthsMM())
        //    {
        //        for (int i = 0; i < nRepetitionsPerWidth; i++)
        //        {
        //            colWidths.Add(Utils.MM2PX(width));
        //        }
        //    }

        //    // Shuffle the widths to randomize their order
        //    colWidths.Shuffle();

        //    // For each column, randomly choose a height formation (1 to 4)
        //    double minW = Experiment.GetGridMinTargetWidthMM();
        //    List<int> colFormations = new List<int>();
        //    for (int i = 0; i < numCols; i++)
        //    {
        //        int formation = _random.Next(1, 5); // 1 to 4
        //        if (colWidths[i] == minW) formation = 3; // Don't go full H with small targets

        //        colFormations.Add(formation);
        //    }

        //    // Create the grid
        //    Brush defaultElementColor = Config.BUTTON_DEFAULT_FILL_COLOR;
        //    int padding = Utils.MM2PX(Config.WINDOW_PADDING_MM);
        //    int colX = padding;

        //    int totalGridContentHeight = (int)ActualHeight - 2 * padding;

        //    for (int i = 0; i < numCols; i++)
        //    {
        //        int currentY = padding; // Reset Y for each new column

        //        int colW = colWidths[i];
        //        Conlog<SideWindow>($"Column {i}: W = {colW}, Form = {colFormations[i]}");

        //        switch (colFormations[i])
        //        {
        //            case 1: // Single element (1/1 H)
        //                string elementId_case1 = $"C{i}-R0";
        //                Element topElement_case1 = CreateElement(
        //                    elementId_case1, colW, totalGridContentHeight,
        //                    mouseEnterHandler, mouseLeaveHandler, buttonDownHandler, buttonUpHandler);
        //                AddElementToCanvas(topElement_case1, colX, currentY);
        //                break;

        //            case 2: // 2/3H top, 1/3H bottom
        //                    // The available height for elements is totalGridContentHeight - gutter
        //                    // We calculate these as percentages of the *total* available height, then round.
        //                    // It's often safer to calculate the absolute heights, then adjust for rounding difference.
        //                    // Let's try to calculate heights relative to totalGridContentHeight, then place them.

        //                // Calculate the fractional heights *including* the gutter as a fraction of the total height.
        //                // Or, more simply:
        //                // H_total = H_top + Gutter + H_bottom
        //                // H_top = (2/3) * (H_total - Gutter)
        //                // H_bottom = (1/3) * (H_total - Gutter)

        //                // Height available for the two elements combined
        //                double effectiveHeightForElements_case2_calc = (double)totalGridContentHeight - gutter;

        //                int topElementHeight_case2 = (int)Math.Round(2.0 * effectiveHeightForElements_case2_calc / 3.0);
        //                int bottomElementHeight_case2 = (int)Math.Round(effectiveHeightForElements_case2_calc / 3.0);

        //                // If due to rounding, the sum is not exactly effectiveHeightForElements_case2_calc,
        //                // we can adjust one of the heights or spread the difference.
        //                // For simplicity, let's ensure the sum adds up by adjusting the bottom one slightly
        //                // if there's a small rounding error.
        //                int currentTotalElementHeight_case2 = topElementHeight_case2 + bottomElementHeight_case2;
        //                if (currentTotalElementHeight_case2 != (int)effectiveHeightForElements_case2_calc)
        //                {
        //                    bottomElementHeight_case2 += ((int)effectiveHeightForElements_case2_calc - currentTotalElementHeight_case2);
        //                }


        //                string elementId_case2_R0 = $"C{i}-R0";
        //                Element topElement_case2 = CreateElement(
        //                    elementId_case2_R0, colW, topElementHeight_case2, // Use calculated height
        //                    mouseEnterHandler, mouseLeaveHandler, buttonDownHandler, buttonUpHandler);
        //                AddElementToCanvas(topElement_case2, colX, currentY);

        //                // Position the next element directly after the previous one, plus the gutter
        //                currentY += (int)topElement_case2.ElementHeight + gutter; // <--- SIMPLIFIED THIS LINE

        //                string elementId_case2_R1 = $"C{i}-R1";
        //                Element bottomElement_case2 = CreateElement(
        //                    elementId_case2_R1, colW, bottomElementHeight_case2, // Use calculated height
        //                    mouseEnterHandler, mouseLeaveHandler, buttonDownHandler, buttonUpHandler);
        //                AddElementToCanvas(bottomElement_case2, colX, currentY);
        //                break;

        //            case 3: // 1/3H top, 1/3H middle, 1/3H bottom
        //                double effectiveHeightForElements_case3_calc = (double)totalGridContentHeight - (2 * gutter);

        //                int segmentHeight_case3 = (int)Math.Round(effectiveHeightForElements_case3_calc / 3.0);

        //                // Adjust for rounding: make sure the three segments sum to effectiveHeightForElements_case3_calc
        //                int currentTotalElementHeight_case3 = 3 * segmentHeight_case3;
        //                int roundingDiff_case3 = (int)effectiveHeightForElements_case3_calc - currentTotalElementHeight_case3;

        //                // Distribute rounding error among segments or to the last one.
        //                // For simplicity, let's just make the last segment absorb any remaining difference.
        //                int topHeight_case3 = segmentHeight_case3;
        //                int middleHeight_case3 = segmentHeight_case3;
        //                int bottomHeight_case3 = segmentHeight_case3 + roundingDiff_case3;


        //                string elementId_case3_R0 = $"C{i}-R0";
        //                Element topElement_case3 = CreateElement(
        //                    elementId_case3_R0, colW, topHeight_case3,
        //                    mouseEnterHandler, mouseLeaveHandler, buttonDownHandler, buttonUpHandler);
        //                AddElementToCanvas(topElement_case3, colX, currentY);

        //                currentY += (int)topElement_case3.ElementHeight + gutter; // <--- SIMPLIFIED

        //                string elementId_case3_R1 = $"C{i}-R1";
        //                Element middleElement_case3 = CreateElement(
        //                    elementId_case3_R1, colW, middleHeight_case3,
        //                    mouseEnterHandler, mouseLeaveHandler, buttonDownHandler, buttonUpHandler);
        //                AddElementToCanvas(middleElement_case3, colX, currentY);

        //                currentY += (int)middleElement_case3.ElementHeight + gutter; // <--- SIMPLIFIED

        //                string elementId_case3_R2 = $"C{i}-R2";
        //                Element bottomElement_case3 = CreateElement(
        //                    elementId_case3_R2, colW, bottomHeight_case3,
        //                    mouseEnterHandler, mouseLeaveHandler, buttonDownHandler, buttonUpHandler);
        //                AddElementToCanvas(bottomElement_case3, colX, currentY);
        //                break;

        //            case 4: // 1/3H top, 2/3H bottom
        //                double effectiveHeightForElements_case4_calc = (double)totalGridContentHeight - gutter;

        //                int topElementHeight_case4 = (int)Math.Round(effectiveHeightForElements_case4_calc / 3.0);
        //                int bottomElementHeight_case4 = (int)Math.Round(2.0 * effectiveHeightForElements_case4_calc / 3.0);

        //                // Adjust for rounding
        //                int currentTotalElementHeight_case4 = topElementHeight_case4 + bottomElementHeight_case4;
        //                if (currentTotalElementHeight_case4 != (int)effectiveHeightForElements_case4_calc)
        //                {
        //                    bottomElementHeight_case4 += ((int)effectiveHeightForElements_case4_calc - currentTotalElementHeight_case4);
        //                }

        //                string elementId_case4_R0 = $"C{i}-R0";
        //                Element topElement_case4 = CreateElement(
        //                    elementId_case4_R0, colW, topElementHeight_case4,
        //                    mouseEnterHandler, mouseLeaveHandler, buttonDownHandler, buttonUpHandler);
        //                AddElementToCanvas(topElement_case4, colX, currentY);

        //                currentY += (int)topElement_case4.ElementHeight + gutter; // <--- SIMPLIFIED

        //                string elementId_case4_R1 = $"C{i}-R1";
        //                Element bottomElement_case4 = CreateElement(
        //                    elementId_case4_R1, colW, bottomElementHeight_case4,
        //                    mouseEnterHandler, mouseLeaveHandler, buttonDownHandler, buttonUpHandler);
        //                AddElementToCanvas(bottomElement_case4, colX, currentY);
        //                break;
        //        }

        //        colX += colW + gutter;
        //    }

        //}

        //public void KnollVertical(int minNumRows, int maxNumRows)
        //{
        //    // Choose a random number of rows with random heights
        //    int numRows = _random.Next(minNumRows, maxNumRows + 1);
        //    List<double> rowHeights = new List<double>();
        //    for (int i = 0; i < numRows; i++)
        //    {
        //        double rowHeight = Utils.RandDouble(Config.GRID_MIN_ELEMENT_WIDTH_MM, Config.GRID_MAX_ELEMENT_WIDTH_MM); // Reusing width config for height
        //        rowHeights.Add(rowHeight);
        //    }

        //    // For each row, randomly choose a horizontal formation (1 to 4)
        //    // Cases will now represent horizontal divisions:
        //    // Case 1: 1/1 W (full width)
        //    // Case 2: 2/3 W left, 1/3 W right
        //    // Case 3: 1/3 W left, 1/3 W middle, 1/3 W right
        //    // Case 4: 1/3 W left, 2/3 W right
        //    List<int> rowFormations = new List<int>();
        //    for (int i = 0; i < numRows; i++)
        //    {
        //        int formation = _random.Next(1, 5); // 1 to 4
        //        rowFormations.Add(formation);
        //    }

        //    // Create the grid
        //    int gutter = Utils.MM2PX(Config.GRID_GUTTER_MM);
        //    int padding = Utils.MM2PX(Config.WINDOW_PADDING_MM); // Assuming this is the left/right window padding
        //    int rowY = padding; // Start from the top (increased inside the loop)

        //    // This represents the total width available for the *grid content* within the window padding.
        //    // This is the width we want all rows to span from left-most content edge to right-most content edge.
        //    int totalGridContentWidth = (int)ActualWidth - 2 * padding; // Changed to ActualWidth

        //    for (int i = 0; i < numRows; i++)
        //    {
        //        // Create elements based on the formation
        //        int rowH = Utils.MM2PX(rowHeights[i]); // This is the height of the current row
        //        Conlog<SideWindow>($"Row {i}: H = {rowH}, Form = {rowFormations[i]}");

        //        // All elements in all rows start at the same Canvas.Left position (after the left window padding)
        //        int currentX = padding; // Changed from currentY to currentX

        //        switch (rowFormations[i])
        //        {
        //            case 1: // Single element (1/1 W)
        //                    // This row has 1 element. Its total width should be totalGridContentWidth.
        //                    // The single element takes up all of this space.
        //                Rectangle leftElement = new Rectangle // Renamed topElement to leftElement
        //                {
        //                    Width = totalGridContentWidth, // Changed from Height to Width
        //                    Height = rowH,
        //                    Fill = Brushes.Blue
        //                };
        //                Canvas.SetLeft(leftElement, currentX); // Position relative to the Canvas's left edge
        //                Canvas.SetTop(leftElement, rowY);
        //                canvas.Children.Add(leftElement);
        //                Conlog<SideWindow>($"Case 1: Element Width = {leftElement.Width}, Left = {Canvas.GetLeft(leftElement)}");
        //                break;

        //            case 2: // 2/3 W left, 1/3 W right
        //                    // This row has 2 elements and 1 internal gutter.
        //                    // The available space for elements + internal gutter is totalGridContentWidth.
        //                    // We want fixed element sizes, so we calculate the 2/3 and 1/3 widths first.
        //                    // totalGridContentWidth = (width2_3) + (gutter) + (width1_3)
        //                    // So, (width2_3 + width1_3) = totalGridContentWidth - gutter

        //                double effectiveWidthForElements_case2 = (double)totalGridContentWidth - gutter; // Remaining width after 1 internal gutter

        //                // Calculate target element widths
        //                int targetWidth2_3 = (int)Math.Round(2.0 * effectiveWidthForElements_case2 / 3.0);
        //                int targetWidth1_3 = (int)Math.Round(effectiveWidthForElements_case2 / 3.0);

        //                // Calculate the actual sum of these rounded widths
        //                int sumOfTargetWidths_case2 = targetWidth2_3 + targetWidth1_3;
        //                // The difference between the desired total element width and the actual sum is distributed to the gutter
        //                int extraWidthForGutter_case2 = (int)effectiveWidthForElements_case2 - sumOfTargetWidths_case2;

        //                leftElement = new Rectangle // Renamed topElement to leftElement
        //                {
        //                    Width = targetWidth2_3, // Changed from Height to Width
        //                    Height = rowH,
        //                    Fill = Brushes.Blue
        //                };
        //                Canvas.SetLeft(leftElement, currentX);
        //                Canvas.SetTop(leftElement, rowY);
        //                canvas.Children.Add(leftElement);
        //                Conlog<SideWindow>($"Case 2: Left Element Width = {leftElement.Width}, Left = {Canvas.GetLeft(leftElement)}");

        //                // Adjust the gutter to absorb the rounding difference
        //                currentX += (int)leftElement.Width + gutter + extraWidthForGutter_case2; // Changed from currentY to currentX

        //                Rectangle rightElement_case2 = new Rectangle // Renamed bottomElement to rightElement
        //                {
        //                    Width = targetWidth1_3, // Changed from Height to Width
        //                    Height = rowH,
        //                    Fill = Brushes.Blue
        //                };
        //                Canvas.SetLeft(rightElement_case2, currentX);
        //                Canvas.SetTop(rightElement_case2, rowY);
        //                canvas.Children.Add(rightElement_case2);
        //                Conlog<SideWindow>($"Case 2: Right Element Width = {rightElement_case2.Width}, Left = {Canvas.GetLeft(rightElement_case2)}");
        //                break;

        //            case 3: // 1/3 W left, 1/3 W middle, 1/3 W right
        //                    // This row has 3 elements and 2 internal gutters.
        //                    // (width1_3 + width1_3 + width1_3) = totalGridContentWidth - (2 * gutter)
        //                double effectiveWidthForElements_case3 = (double)totalGridContentWidth - (2 * gutter);

        //                int targetWidth1_3_seg = (int)Math.Round(effectiveWidthForElements_case3 / 3.0);

        //                // Calculate the actual sum of these rounded widths
        //                int sumOfTargetWidths_case3 = 3 * targetWidth1_3_seg;
        //                // The difference between the desired total element width and the actual sum is distributed to the gutter
        //                int extraWidthForGutter_case3 = (int)effectiveWidthForElements_case3 - sumOfTargetWidths_case3;

        //                leftElement = new Rectangle // Renamed topElement to leftElement
        //                {
        //                    Width = targetWidth1_3_seg, // Changed from Height to Width
        //                    Height = rowH,
        //                    Fill = Brushes.Blue
        //                };
        //                Canvas.SetLeft(leftElement, currentX);
        //                Canvas.SetTop(leftElement, rowY);
        //                canvas.Children.Add(leftElement);
        //                Conlog<SideWindow>($"Case 3: Left Element Width = {leftElement.Width}, Left = {Canvas.GetLeft(leftElement)}");

        //                // Distribute the extra width from rounding
        //                int gutter1_width = gutter + (extraWidthForGutter_case3 / 2); // Split extra width if two gutters
        //                int gutter2_width = gutter + (extraWidthForGutter_case3 - (extraWidthForGutter_case3 / 2));

        //                currentX += (int)leftElement.Width + gutter1_width; // Changed from currentY to currentX

        //                Rectangle middleElement_case3 = new Rectangle
        //                {
        //                    Width = targetWidth1_3_seg, // Changed from Height to Width
        //                    Height = rowH,
        //                    Fill = Brushes.Blue
        //                };
        //                Canvas.SetLeft(middleElement_case3, currentX);
        //                Canvas.SetTop(middleElement_case3, rowY);
        //                canvas.Children.Add(middleElement_case3);
        //                Conlog<SideWindow>($"Case 3: Middle Element Width = {middleElement_case3.Width}, Left = {Canvas.GetLeft(middleElement_case3)}");

        //                currentX += (int)middleElement_case3.Width + gutter2_width; // Changed from currentY to currentX

        //                Rectangle rightElement_case3 = new Rectangle // Renamed bottomElement to rightElement
        //                {
        //                    Width = targetWidth1_3_seg, // Changed from Height to Width
        //                    Height = rowH,
        //                    Fill = Brushes.Blue
        //                };
        //                Canvas.SetLeft(rightElement_case3, currentX);
        //                Canvas.SetTop(rightElement_case3, rowY);
        //                canvas.Children.Add(rightElement_case3);
        //                Conlog<SideWindow>($"Case 3: Right Element Width = {rightElement_case3.Width}, Left = {Canvas.GetLeft(rightElement_case3)}");
        //                break;

        //            case 4: // 1/3 W left, 2/3 W right
        //                    // This row has 2 elements and 1 internal gutter.
        //                    // (width1_3 + width2_3) = totalGridContentWidth - gutter
        //                double effectiveWidthForElements_case4 = (double)totalGridContentWidth - gutter;

        //                // Calculate target element widths
        //                targetWidth1_3 = (int)Math.Round(effectiveWidthForElements_case4 / 3.0);
        //                targetWidth2_3 = (int)Math.Round(2.0 * effectiveWidthForElements_case4 / 3.0);

        //                // Calculate the actual sum of these rounded widths
        //                int sumOfTargetWidths_case4 = targetWidth1_3 + targetWidth2_3;
        //                // The difference is distributed to the gutter
        //                int extraWidthForGutter_case4 = (int)effectiveWidthForElements_case4 - sumOfTargetWidths_case4;

        //                leftElement = new Rectangle // Renamed topElement to leftElement
        //                {
        //                    Width = targetWidth1_3, // Changed from Height to Width
        //                    Height = rowH,
        //                    Fill = Brushes.Blue
        //                };
        //                Canvas.SetLeft(leftElement, currentX);
        //                Canvas.SetTop(leftElement, rowY);
        //                canvas.Children.Add(leftElement);
        //                Conlog<SideWindow>($"Case 4: Left Element Width = {leftElement.Width}, Left = {Canvas.GetLeft(leftElement)}");

        //                // Adjust the gutter to absorb the rounding difference
        //                currentX += (int)leftElement.Width + gutter + extraWidthForGutter_case4; // Changed from currentY to currentX

        //                Rectangle rightElement_case4 = new Rectangle // Renamed bottomElement to rightElement
        //                {
        //                    Width = targetWidth2_3, // Changed from Height to Width
        //                    Height = rowH,
        //                    Fill = Brushes.Blue
        //                };
        //                Canvas.SetLeft(rightElement_case4, currentX);
        //                Canvas.SetTop(rightElement_case4, rowY);
        //                canvas.Children.Add(rightElement_case4);
        //                Conlog<SideWindow>($"Case 4: Right Element Width = {rightElement_case4.Width}, Left = {Canvas.GetLeft(rightElement_case4)}");
        //                break;
        //        }

        //        // Move forward for the next row
        //        rowY += rowH + gutter; // Changed from colX to rowY, colW to rowH
        //    }
        //}

        private Element CreateElement(string id, int w, int h,
            MouseEventHandler mouseEnterHandler, MouseEventHandler mouseLeaveHandler,
            MouseButtonEventHandler mouseDownHandler, MouseButtonEventHandler mouseUpHandler
            )
        {

            Element element = new Element
            {
                Id = id,
                ElementWidth = w,
                ElementHeight = h,
            };

            element.MouseEnter += mouseEnterHandler;
            element.MouseLeave += mouseLeaveHandler;
            element.MouseDown += mouseDownHandler;
            element.MouseUp += mouseUpHandler;

            return element;
        }

        public override void GenerateGrid(Rect startConstraintsRectAbsolute, params Func<Grid>[] groupCreators)
        {
            _startConstraintsRectAbsolute = startConstraintsRectAbsolute;

            // Clear any existing columns from the canvas and the list before generating new ones
            canvas.Children.Clear();
            _gridGroups.Clear();
            _buttonInfos.Clear();
            this.TrialInfo($"Generating grid");
            double currentTopPosition = VerticalPadding; // Start with the initial padding
            double leftGroupLeft = HorizontalPadding;
            double rightGroupLeft = HorizontalPadding + ColumnFactory.MAX_GROUP_WITH + InterGroupGutter;

            // Left column
            foreach (var group in groupCreators)
            {
                // Create the row
                Grid newGroup = group();
                // Set its position on the Canvas
                Canvas.SetTop(newGroup, currentTopPosition);
                Canvas.SetLeft(newGroup, leftGroupLeft);
                // Add to the Canvas
                canvas.Children.Add(newGroup);
                // Add to our internal list for tracking/future reference
                _gridGroups.Add(newGroup);
                // Force a layout pass on the newly added column to get its ActualWidth
                newGroup.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                newGroup.Arrange(new Rect(newGroup.DesiredSize));
                // Register buttons in this row
                //RegisterButtons(newGroup);
                // Update the top position for the next row
                currentTopPosition += ColumnFactory.COLUMN_HEIGHT + InterGroupGutter;
            }

            // Right column
            currentTopPosition = VerticalPadding; // Reset the top position for the right column
            groupCreators.ShiftElementsInPlace(2);
            foreach (var group in groupCreators)
            {
                // Create the row
                Grid newGroup = group();
                // Set its position on the Canvas
                Canvas.SetTop(newGroup, currentTopPosition);
                Canvas.SetLeft(newGroup, rightGroupLeft);
                // Add to the Canvas
                canvas.Children.Add(newGroup);
                // Add to our internal list for tracking/future reference
                _gridGroups.Add(newGroup);
                // Force a layout pass on the newly added column to get its ActualWidth
                newGroup.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                newGroup.Arrange(new Rect(newGroup.DesiredSize));
                // Register buttons in this row
                //RegisterButtons(newGroup);
                // Update the top position for the next row
                currentTopPosition += ColumnFactory.COLUMN_HEIGHT + InterGroupGutter;
            }


            //for (int i = 0; i < groupCreators.Count() - 1; i++)
            //{
            //    // Create the row
            //    Grid leftGroup = groupCreators[i]();
            //    Grid rightGroup = groupCreators[i + 1]();

            //    // Set its position on the Canvas
            //    Canvas.SetTop(leftGroup, currentTopPosition);
            //    Canvas.SetTop(rightGroup, currentTopPosition);

            //    Canvas.SetLeft(leftGroup, leftGroupLeft);
            //    Canvas.SetLeft(rightGroup, rightGroupLeft);

            //    // Add to the Canvas
            //    canvas.Children.Add(leftGroup);
            //    canvas.Children.Add(rightGroup);

            //    // Add to our internal list for tracking/future reference
            //    _gridGroups.Add(leftGroup);
            //    _gridGroups.Add(rightGroup);

            //    // Update the top position for the next row
            //    currentTopPosition += ColumnFactory.COLUMN_HEIGHT + InterGroupGutter; 

            //}

            //foreach (var createGroupFunc in groupCreators)
            //{
            //    Grid newGroup = createGroupFunc(); // Create the new group

            //    // Set its position on the Canvas
            //    Canvas.SetTop(newGroup, currentTopPosition);
            //    Canvas.SetLeft(newGroup, HorizontalPadding); // Assuming all columns start at the same left padding

            //    // Add to the Canvas
            //    canvas.Children.Add(newGroup);

            //    // Add to our internal list for tracking/future reference
            //    _gridGroups.Add(newGroup);

            //    // Force a layout pass on the newly added column to get its ActualWidth
            //    // This is crucial because the next column's position depends on this one's actual size.
            //    newGroup.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            //    newGroup.Arrange(new Rect(newGroup.DesiredSize));

            //    // Register buttons in this row
            //    //RegisterButtons(newGroup);

            //    // Update the currentLeftPosition for the next column, adding the current column's width and the 2*gutter
            //    currentTopPosition += newGroup.ActualHeight + VerticalPadding;

            //    //Debug.WriteLine($"Added column. Current left: {currentLeftPosition} DIPs. Column width: {newColumnGrid.ActualWidth}");
            //}

            Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
            {
                RegisterAllButtons(); // Register all buttons after all groups are created
                LinkButtonNeighbors(); // Link neighbors after all buttons are registered
            }));
        }

        private void RegisterAllButtons()
        {
            // Iterate through all groups (Grids) in the window
            foreach (Grid group in _gridGroups)
            {
                RegisterButtons(group); // Register buttons for each group
            }
            //this.TrialInfo($"Total buttons registered: {_allButtons.Count}");

            int middleId = FindMiddleButtonId();
            if (middleId != -1)
            {
                _lastHighlightedButtonId = middleId; // Set the last highlighted button to the middle button
            }
            else
            {
                this.TrialInfo("No middle button found in the grid.");
            }
        }

        private void RegisterButtons(Grid group)
        {
            //this.TrialInfo($"Registering buttons in group with {group.Children.Count} children...");

            // Iterate through all direct children of the Grid column
            foreach (UIElement childOfGroup in group.Children)
            {
                // We know our rows are StackPanels
                if (childOfGroup is StackPanel rowStackPanel)
                {
                    // Iterate through all children of the StackPanel (which should be buttons or in-row gutters)
                    foreach (UIElement childOfRow in rowStackPanel.Children)
                    {
                        // Check if the child is an SButton
                        if (childOfRow is SButton button)
                        {
                            _widthButtons[button.WidthMultiple].Add(button); // Add the button to the dictionary with its width as the key
                            _buttonInfos[button.Id] = new ButtonInfo(button);
                            //_allButtons.Add(button.Id, button); // Add to the list of all buttons

                            //foreach (int wm in _widthButtons.Keys)
                            //{
                            //    string ids = string.Join(", ", _widthButtons[wm].Select(b => b.Id.ToString()));
                            //    this.TrialInfo($"WM {wm} -> {ids}");
                            //}
                            // Add button position to the dictionary
                            // Get the transform from the button to the Window (or the root visual)
                            GeneralTransform transformToWindow = button.TransformToVisual(Window.GetWindow(button));
                            // Get the point representing the top-left corner of the button relative to the Window
                            Point positionInWindow = transformToWindow.Transform(new Point(0, 0));
                            _buttonInfos[button.Id].Position = positionInWindow;
                            //_buttonPositions.Add(button.Id, positionInWindow); // Store the position of the button
                            //this.TrialInfo($"Button Position: {positionInWindow}");
                            if (positionInWindow.X <= _topLeftButtonPosition.X && positionInWindow.Y <= _topLeftButtonPosition.Y)
                            {
                                //this.TrialInfo($"Top-left button position updated: {positionInWindow} for button ID#{button.Id}");
                                _topLeftButtonPosition = positionInWindow; // Update the top-left button position
                                //_lastHighlightedButtonId = button.Id; // Set the last highlighted button to this one
                            }

                            Rect buttonRect = new Rect(positionInWindow.X, positionInWindow.Y, button.ActualWidth, button.ActualHeight);
                            _buttonInfos[button.Id].Rect = buttonRect;
                            //_buttonRects.Add(button.Id, buttonRect); // Store the rect for later

                            // Set possible distance range to the Start positions
                            Point buttonCenterAbsolute =
                                positionInWindow
                                .OffsetPosition(button.ActualWidth / 2, button.ActualHeight / 2)
                                .OffsetPosition(this.Left, this.Top);

                            //double distToStartTL = Utils.Dist(buttonCenterAbsolute, _startConstraintsRectAbsolute.TopLeft);
                            //double distToStartTR = Utils.Dist(buttonCenterAbsolute, _startConstraintsRectAbsolute.TopRight);
                            //double distToStartLL = Utils.Dist(buttonCenterAbsolute, _startConstraintsRectAbsolute.BottomLeft);
                            //double distToStartLR = Utils.Dist(buttonCenterAbsolute, _startConstraintsRectAbsolute.BottomRight);

                            //double[] dists = { distToStartTL, distToStartTR, distToStartLL, distToStartLR };
                            //_buttonInfos[button.Id].DistToStartRange = new Range(dists.Min(), dists.Max());

                            // Correct way of finding min and max dist
                            _buttonInfos[button.Id].DistToStartRange = GetMinMaxDistances(buttonCenterAbsolute, _startConstraintsRectAbsolute);

                            // Update min/max X and Y for grid bounds
                            _gridMinX = Math.Min(_gridMinX, buttonRect.Left);
                            _gridMinY = Math.Min(_gridMinY, buttonRect.Top);
                            _gridMaxX = Math.Max(_gridMaxX, buttonRect.Right);
                            _gridMaxY = Math.Max(_gridMaxY, buttonRect.Bottom);

                            if (positionInWindow.X <= _topLeftButtonPosition.X && positionInWindow.Y <= _topLeftButtonPosition.Y)
                            {
                                //this.TrialInfo($"Top-left button position updated: {positionInWindow} for button ID#{button.Id}");
                                _topLeftButtonPosition = positionInWindow; // Update the top-left button position
                                //_lastHighlightedButtonId = button.Id; // Set the last highlighted button to this one
                            }

                            //this.TrialInfo($"Registered button ID#{button.Id}, Wx{button.WidthMultiple} | Position: {positionInWindow}");
                        }
                    }
                }
            }

            //this.TrialInfo($"Finished registering buttons in group. Current allButtons count: {_allButtons.Count}");
            // Set the first button as the highlighted button
            //_lastHighlightedButtonId = _widthButtons.FirstOrDefault().Value.FirstOrDefault()?.Id ?? -1; // Get the first button ID or -1 if no buttons are present
        }

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

    }
}
