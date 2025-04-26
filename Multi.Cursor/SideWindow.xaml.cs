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

        private bool isCursorVisible;
        private Point lastSimCursorPos = new Point(0, 0);

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

            //this.MouseMove += this.Window_MouseMove;

            EnableMouseInPointer(true);
            SetForegroundWindow(new WindowInteropHelper(this).Handle);

            _relPos = relPos;

            _auxursor = new Auxursor(Config.FRAME_DUR_MS / 1000.0);

            _cursorTransform = (TranslateTransform)FindResource("CursorTransform");
        }

        //private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        //{
        //    if (_svg == null)
        //        return;

        //    var canvas = e.Surface.Canvas;
        //    canvas.Clear(SKColors.Black);

        //    // Draw SVG
        //    canvas.DrawPicture(_svg.Picture);
        //}

        /// <summary>
        /// Show the target
        /// </summary>
        /// <param name="widthMM"> Width (diameter) of the target circle </param>
        /// <returns> Position of the target top-left (rel. to this window) </returns>
        public Point ShowTarget(double widthMM, Brush fill, 
            MouseEventHandler mouseEnterHandler, MouseEventHandler mouseLeaveHandler,
            MouseButtonEventHandler buttonDownHandler, MouseButtonEventHandler buttonUpHandler)
        {
            
            // Radius in pixels
            //const double PPI = 109;
            //const double MM_IN_INCH = 25.4;
            int targetWidth = Utils.MM2PX(widthMM);
            
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
            Canvas.SetZIndex(cursor_active, 1);
            Canvas.SetZIndex(cursor_inactive, 1);

            return new Point(randomX, randomY);
        }

        public void ShowTarget(Point position, double widthMM, Brush fill,
            MouseEventHandler mouseEnterHandler, MouseEventHandler mouseLeaveHandler,
            MouseButtonEventHandler buttonDownHandler, MouseButtonEventHandler buttonUpHandler)
        {

            // Radius in pixels
            //const double PPI = 109;
            //const double MM_IN_INCH = 25.4;
            int targetWidth = Utils.MM2PX(widthMM);

            // Create the target
            targetHalfW = targetWidth / 2;
            _target = new Rectangle
            {
                Width = targetWidth,
                Height = targetWidth,
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
            Canvas.SetZIndex(cursor_active, 1);
            Canvas.SetZIndex(cursor_inactive, 1);
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
            Canvas.SetZIndex(cursor_active, 1);
            Canvas.SetZIndex(cursor_inactive, 1);
        }

        public void ColorTarget(Brush color)
        {
            _target.Fill = color;
        }

        public bool IsAuxCursorInsideTarget()
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

        public void ShowAuxCursor(int x, int y)
        {
            //Mouse.OverrideCursor = null;
            //isCursorVisible = true;

            // Still not sure where to show the cursor
            //PositionCursor(x, y);

            // Show the simulated cursor
            cursor_inactive.Visibility = Visibility.Hidden;
            cursor_active.Visibility = Visibility.Visible;
            _cursorTransform.X = x;
            _cursorTransform.Y = y;

            // Get window's actual width and height including borders
            //windowWidth = (int)this.ActualWidth;
            //windowHeight = (int)this.ActualHeight;
        }

        public void ActivateCursor(Location dir)
        {
            Point position = new Point();

            switch (dir)
            {
                case Location.Left:
                    position.X = canvas.ActualWidth / 4; // Middle of the left
                    position.Y = canvas.ActualHeight / 2; // Middle of height
                    break;
                case Location.Top:
                    position.X = canvas.ActualWidth / 2; // Middle of width
                    position.Y = canvas.ActualHeight / 4; // Middle of the top
                    break;
                case Location.Bottom:
                    position.X = canvas.ActualWidth / 2; // Middle of width
                    position.Y = canvas.ActualHeight * 3/4; // Middle of the top
                    break;
                case Location.Right:
                    position.X = canvas.ActualWidth * 3 / 4; // Middle of the right
                    position.Y = canvas.ActualHeight / 2; // Middle of height
                    break;
                case Location.Middle:
                    position.X = canvas.ActualWidth / 2; // Middle of width
                    position.Y = canvas.ActualHeight / 2; // Middle of height
                break;
            }

            cursor_inactive.Visibility = Visibility.Hidden;
            cursor_active.Visibility = Visibility.Visible;
            _cursorTransform.X = position.X;
            _cursorTransform.Y = position.Y;
            _auxursor.Activate();
        }

        public void DeactivateCursor()
        {
            cursor_inactive.Visibility = Visibility.Visible;
            cursor_active.Visibility = Visibility.Hidden;
            _auxursor.Deactivate();
        }

        public void ShowAuxCursor(Point p)
        {
            ShowAuxCursor((int)p.X, (int)p.Y);
        }

        public void MoveAuxPointer(TouchPoint tp)
        {
            (double dX, double dY) = _auxursor.Move(tp);
            MoveAuxCursor(dX, dY);
        }

        public void ShowSimCursorInMiddle()
        {
            // Get window's actual width and height including borders
            windowWidth = (int)this.ActualWidth;
            windowHeight = (int)this.ActualHeight;

            // Show the inactive cursor (default)
            cursor_inactive.Visibility = Visibility.Visible;
            cursor_active.Visibility = Visibility.Hidden;
            _cursorTransform.X = windowWidth / 2;
            _cursorTransform.Y = windowHeight / 2;
            TRACK_LOG.Debug($"Show the cursor at {_cursorTransform.ToString()}");
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //Console.WriteLine("MouseDown event triggered.");
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            var cursorPoint = e.GetPosition(this);
            TRACK_LOG.Debug($"Cursor Position: {cursorPoint.ToString()}");
            _cursorTransform.X = cursorPoint.X;
            _cursorTransform.Y = cursorPoint.Y;
        }

        private void Window_MouseUp(object sender, MouseEventArgs e)
        {

        }

        private void PositionCursor(int x, int y)
        {
            var windowPosition = PointToScreen(new Point(0, 0));
            TRACK_LOG.Debug($"Set pos X = {(int)(windowPosition.X + x)}, Y = {(int)(windowPosition.Y + y)}");
            SetCursorPos((int)(windowPosition.X + x), (int)(windowPosition.Y + y));
        }

        public void MoveAuxCursor(double dX, double dY)
        {
            // Potential new position
            double potentialX = _cursorTransform.X + dX;
            double potentialY = _cursorTransform.Y + dY; 
            //Console.WriteLine($"Current Cursor Pos: {cursorTransform.X} {cursorTransform.Y}");
            //Console.WriteLine("Window WxH = {0}x{1}", windowWidth, windowHeight);
            // X: Within boundaries
            if (potentialX < 0)
            {
                dX = -_cursorTransform.X + 3;
            }
            else if (potentialX > windowWidth)
            {
                dX = windowWidth - 10 - _cursorTransform.X;
            }

            // Y: Within boundaries
            if (potentialY < 0)
            {
                dY = -_cursorTransform.Y + 3;
            }
            else if (potentialY > windowHeight)
            {
                dY = windowHeight - 10 - _cursorTransform.Y;
            }

            // Move the cursor
            _cursorTransform.X += dX;
            _cursorTransform.Y += dY;
            TRACK_LOG.Debug($"Sim cursor moved ({dX:F3}, {dY:F3})");
            lastSimCursorPos.X = _cursorTransform.X;
            lastSimCursorPos.Y = _cursorTransform.Y;

            // Check if entered the target
            if (IsAuxCursorInsideTarget())
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

            TRACK_LOG.Debug($"Pos: ({currentX}, {currentY}); Delta: ({dX}, {dY})");

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
                TRACK_LOG.Debug($"Moved by: ({dX}, {dY})");
            }

            TRACK_LOG.Debug("--------------------------------------------");
        }

        public void HideSimCursor()
        {
            //cursor.Visibility = Visibility.Hidden;
            //Mouse.OverrideCursor = Cursors.None;
            //isCursorVisible = false;
        }

        public bool HasCursor()
        {
            return isCursorVisible;
        }

        public void ClearCanvas()
        {
            canvas.Children.Remove(_target);
        }
    }
}
