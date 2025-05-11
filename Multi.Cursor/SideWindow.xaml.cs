using SkiaSharp;
using SkiaSharp.Views.WPF;
using Svg.Skia;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows;
using System.Windows.Controls;
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
        public Point ShowTarget(int targetWidth, Brush fill, 
            MouseEventHandler mouseEnterHandler, MouseEventHandler mouseLeaveHandler,
            MouseButtonEventHandler buttonDownHandler, MouseButtonEventHandler buttonUpHandler)
        {
            // Get canvas dimensions
            _canvasWidth = (int)canvas.ActualWidth;
            _canvasHeight = (int)canvas.ActualHeight;

            // Ensure the Target stays fully within bounds (min/max for top-left)
            int marginPX = Utils.MM2PX(Config.WINDOW_PADDING_MM);
            int minX = marginPX;
            int maxX = _canvasWidth - marginPX - targetWidth;
            int minY = marginPX;
            int maxY = _canvasHeight - marginPX - targetWidth;

            // Generate random position
            Random random = new Random();
            double randomX = random.Next(minX, maxX);
            double randomY = random.Next(minY, maxY);

            // Create the target
            targetHalfW = targetWidth / 2;
            _target = new Rectangle
            {
                Width = targetWidth,
                Height = targetWidth,
                Fill = fill,
            };

            // Position the target on the Canvas
            Canvas.SetLeft(_target, randomX);
            Canvas.SetTop(_target, randomY);

            //--- TEMP (for measurement)
            // Longest dist
            //Canvas.SetLeft(_target, minX);
            //Canvas.SetTop(_target, minY);
            // Shortest dist
            //Canvas.SetLeft(_target, maxX - targetWidth);
            //Canvas.SetTop(_target, minY);


            // Add events
            _target.MouseEnter += mouseEnterHandler;
            _target.MouseLeave += mouseLeaveHandler;
            _target.MouseLeftButtonDown += buttonDownHandler;
            _target.MouseLeftButtonUp += buttonUpHandler;

            // Add the circle to the Canvas
            canvas.Children.Add(_target);

            // Set index
            Canvas.SetZIndex(_target, 0);
            Canvas.SetZIndex(activeCursor, 1);
            Canvas.SetZIndex(inactiveCursor, 1);

            return new Point(randomX, randomY);
        }

        public void ShowTarget(Point position, int targetW, Brush fill,
            MouseEventHandler mouseEnterHandler, MouseEventHandler mouseLeaveHandler,
            MouseButtonEventHandler buttonDownHandler, MouseButtonEventHandler buttonUpHandler)
        {

            // Create the target
            targetHalfW = targetW / 2;
            _target = new Rectangle
            {
                Width = targetW,
                Height = targetW,
                Fill = fill,
            };

            // Position the target on the Canvas
            Canvas.SetLeft(_target, position.X);
            Canvas.SetTop(_target, position.Y);
            
            // Add events
            _target.MouseEnter += mouseEnterHandler;
            _target.MouseLeave += mouseLeaveHandler;
            _target.MouseLeftButtonDown += buttonDownHandler;
            _target.MouseLeftButtonUp += buttonUpHandler;

            // Add the circle to the Canvas
            canvas.Children.Add(_target);

            // Set index
            Canvas.SetZIndex(_target, 0);
            Canvas.SetZIndex(activeCursor, 1);
            Canvas.SetZIndex(inactiveCursor, 1);
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
            
            inactiveCursor.Visibility = Visibility.Visible;
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
            MoveCursor(dX, dY);
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

        public void HideSimCursor()
        {
            //cursor.Visibility = Visibility.Hidden;
            //Mouse.OverrideCursor = Cursors.None;
            //isCursorVisible = false;
        }

        public bool HasCursor()
        {
            return _isCursorVisible;
        }

        public void ClearCanvas()
        {
            canvas.Children.Remove(_target);
        }
    }
}
