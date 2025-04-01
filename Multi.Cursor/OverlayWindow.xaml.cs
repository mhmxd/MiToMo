﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using static Multi.Cursor.Output;
using static System.Math;
using static Multi.Cursor.Utils;
using System.Diagnostics;
using Tensorflow;

namespace Multi.Cursor
{
    /// <summary>
    /// Interaction logic for OverlayWindow.xaml
    /// </summary>
    public partial class OverlayWindow : Window
    {
        private double MAX_LINE_LENGTH = 5000; // Arbitrary large length to extend to window limits

        private const double LINE_PART_LENGTH = 4990;  // Adjusted length for part 1 (leaving room for plus sign)
        private const double PLUS_OFFSET = 5;  // Gap for the plus sign
        private const double GAP_BETWEEN_SEGMENTS = 10;  // Small gap between the line parts

        private double _sideWinSize;

        private Line _radiusLine;
        //private LineGeometry _radiusLine;
        private Point _lineStart;
        private double _lineA, _lineB;
        //private double _plusTopBound, _plusLeftBound, _plusRightBound;
        private Rect _outerRect = new Rect();
        private Rect _innerRect = new Rect();
        private bool _isVisible = false;

        private double _angle = 180; // Rotation angle in degrees
        private double _angleRad = 0; // Angle in Radians (updated when _angle updates)

        private double _plusDistance = 0;
        private Point _plusPos = new Point(-1, -1); // Relative to this window
        private Point _innerPoint = new Point();
        private Point _outerPoint = new Point();

        private Point _tlPoint, _trPoint, _blPoint, _brPoint;

        //--- Kalman Filters
        private KalmanVeloFilter _lineKalmanFilter, _plusKalmanFilter; 
        private List<TouchPoint> _lineTouchFrames, _plusTouchFrames;
        private Stopwatch _lineStopwatch, _plusStopwatch;
        private bool _lineTPInitMove, _plusTPInitMove;
        private Point _lineTPPrevPos, _plusTPPrevPos;

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        public double Angle
        {
            get { return _angle; } // Convert radians to degrees
            set { _angle = value; } // Convert degrees to radians
        }

        public OverlayWindow()
        {
            InitializeComponent();
            this.DataContext = this; // Set the DataContext to the Window
            //MakeWindowTransparent();
            Topmost = true;
            Loaded += OverlayWindow_Loaded;

            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                this.Topmost = false;
                this.Topmost = true;
            }));
        }

        private void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Set up the line in the overlay
            //_radiusLine = RadiusLine; // From XML
            //_radiusLine = RadiusLine;
            //HideLine(); // Start hidden

        }

        private void MakeWindowTransparent()
        {
            // Allows the window to be transparent and click-through
            int extendedStyle = GetWindowLong(new System.Windows.Interop.WindowInteropHelper(this).Handle, GWL_EXSTYLE);
            SetWindowLong(new System.Windows.Interop.WindowInteropHelper(this).Handle, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
        }

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int nIndex, int dwNewLong);
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;

        public void ShowLine(Point cursorPos)
        {
            _lineStart = cursorPos;
            _isVisible = true;

            //GESTURE_LOG.Information(Output.GetString(_lineStart));

            // Find the four corners of the main region
            _sideWinSize = Utils.MM2PX(Config.SIDE_WINDOW_SIZE_MM);
            _tlPoint = new Point(_sideWinSize, _sideWinSize);
            _trPoint = new Point(this.Width - _sideWinSize, _sideWinSize);
            _blPoint = new Point(_tlPoint.X, this.Height);
            _brPoint = new Point(_trPoint.X, _blPoint.Y);

            // Set the bound the plus position
            //_plusTopBound = _sideWinSize;
            //_plusLeftBound = _sideWinSize;
            //_plusRightBound = this.Width - _sideWinSize;

            _innerRect = new Rect(
                _sideWinSize, _sideWinSize,
                this.Width - 2 * _sideWinSize, this.Width - 2 * _sideWinSize);

            _outerRect = new Rect(
                0, 0,
                this.Width, this.Height);
            //GESTURE_LOG.Information(Output.GetString(_trPoint));

            //UpdatePlusPosition();
            UpdateLine();

            // Init Kalman
            double dT = Config.FRAME_DUR_MS / 1000.0; // Seconds

            _lineKalmanFilter = new KalmanVeloFilter(dT, 0.1, 30);
            _plusKalmanFilter = new KalmanVeloFilter(dT, 0.3, 40);
            _lineStopwatch = new Stopwatch();
            _plusStopwatch = new Stopwatch();
            _lineTPInitMove = true;
            _plusTPInitMove = true;
            _lineTPPrevPos = new Point(-1, -1);
            _plusTPPrevPos = new Point(-1, -1);
            _lineTouchFrames = new List<TouchPoint>();
            _plusTouchFrames = new List<TouchPoint>();

            _lineStopwatch.Start();
            _plusStopwatch.Start();

            this.Show();
        }

        public void HideLine()
        {
            _isVisible = false;
            this.Hide();
        }

        private void UpdateAngleRad()
        {
            _angleRad = _angle * Math.PI / 180.0;
            GESTURE_LOG.Debug($"RadAngle = {_angleRad}");
        }

        private double CalculateAngle(Point p1, Point p2)
        {
            double angle = Math.Atan2(p1.Y - p2.Y, p2.X - p1.X) * (180.0 / Math.PI); // Degrees
            if (angle < 0) angle += 360;

            return angle;
        }

        public double GetAngle()
        {
            return _angle;
        }

        // 🌀 Rotate the main line
        public void RotateLine(double deltaAngle)
        {
            _angle += deltaAngle;

            // Normalize to [0, 360] range correctly
            //while (_angle >= 360) _angle -= 360;
            //while (_angle < 0) _angle += 360;

            // Normalize to [0, 360] range correctly
            while (_angle >= 360) _angle -= 360;
            while (_angle < 0) _angle += 360;

            UpdateLine();
        }

        public void RotateLine(TouchPoint tp)
        {
            if (_lineStopwatch.ElapsedMilliseconds < Config.FRAME_DUR_MS) // Still collecting frames
            {
                _lineTouchFrames.Add(tp);
            }
            else // Time for action!
            {
                GESTURE_LOG.Information($"Count = {_lineTouchFrames.Count}");

                if (_lineTPInitMove)
                {
                    // First touch: Don't move the cursor, just initialize
                    _lineTPPrevPos = tp.GetCenterOfMass();
                    _lineKalmanFilter.Initialize(0, 0); // Start with zero velocity
                    _lineTPInitMove = false;
                    _lineTouchFrames.Clear();
                    _lineStopwatch.Restart();
                    return;
                }

                if (_lineTouchFrames.Count <= 1)
                {
                    _lineTouchFrames.Add(tp); //add the current touch point.
                    _lineTouchFrames.Clear();
                    _lineStopwatch.Restart();
                    return; // No frames to work with
                }

                // Compute delta from oldest to newest frame
                TouchPoint first = _lineTouchFrames.First();
                TouchPoint last = _lineTouchFrames.Last();

                double dXRaw = last.GetCenterOfMass().X - first.GetCenterOfMass().X;
                double dYRaw = last.GetCenterOfMass().Y - first.GetCenterOfMass().Y;

                // Compute velocity
                _lineStopwatch.Stop();
                double dT = _lineStopwatch.ElapsedMilliseconds / 1000.0; // seconds
                double vXRaw = dXRaw / dT;
                double vYRaw = dYRaw / dT;

                // Kalman filter for velocity
                _lineKalmanFilter.Predict();
                _lineKalmanFilter.Update(vXRaw, vYRaw);

                (double fvX, double fvY) filteredV = _lineKalmanFilter.GetEstVelocity();

                // Compute speed and apply dynamic gain
                double speed = Sqrt(Pow(filteredV.fvX, 2) + Pow(filteredV.fvY, 2));
                double gain = Config.BASE_GAIN +
                    Config.SCALE_FACTOR * Tanh(speed * Config.SENSITIVITY);

                double kdX = filteredV.fvX * dT * (gain / 2);
                double kdY = filteredV.fvY * dT * (gain / 2);

                GESTURE_LOG.Information($"<Line> KF vX = {filteredV.fvX:F3}, vY = {filteredV.fvY:F3}");
                GESTURE_LOG.Information($"<Line> KF dX = {kdX:F3}, dY = {kdY:F3}");
                GESTURE_LOG.Information(Str.MINOR_LINE);

                // Update previous state
                _lineTPPrevPos = tp.GetCenterOfMass();
                _lineTouchFrames.Clear();
                _lineTouchFrames.Add(tp); //Add the current touch point for the next calculation.
                _lineStopwatch.Restart();

                // Make the move!
                RotateLine(kdX, kdY);


            }
        }

        /// <summary>
        /// Rotate line using the touch pointer movement
        /// </summary>
        /// <param name="dX"></param>
        /// <param name="dY"></param>        
        public void RotateLine(double dX, double dY)
        {
            UpdateAngleRad();

            double weightX = Abs(Sin(_angleRad));
            double weightY = Abs(Cos(_angleRad));

            // Configure weights according to four regions
            if (_angle >= 0 && _angle <= 90)
            {
                weightX = -weightX;
            }

            if (_angle > 90 && _angle <= 180)
            {
                weightX = -weightX;
            }

            //if (_angle > 180 && _angle <= 270)
            //{
            //    weightX = -weightX;
            //    weightY = -weightY;
            //}

            if (_angle > 270)
            {
                //weightX = -weightX;
                weightY = -weightY;
            }

            GESTURE_LOG.Debug($"Weights = {weightX}, {weightY}");
            double dAngle = (dX * weightX + dY * weightY) * Config.RAD_GAIN;
            _angle += dAngle;

            if (_angle >= 360) _angle -= 360;
            if (_angle < 0) _angle += 360;

            GESTURE_LOG.Debug($"dX = {dX:F3} | dY = {dY:F3} | dAn = {dAngle:F3} | angle = {_angle:F3}");
            UpdateLine();
        }

        // 🔼🔽 Move cross along the visible part of the line
        public void MovePlus(double dX, double dY)
        {
            // Project movement onto the rotated X-axis
            double movementAlongLine = dX * Math.Cos(_angleRad) + dY * Math.Sin(_angleRad);

            // Move the cross along the line
            _plusDistance += movementAlongLine;
            
            UpdatePlusPosition();
            DrawLineParts();
        }

        public void MovePlus(TouchPoint tp)
        {
            if (_plusStopwatch.ElapsedMilliseconds < Config.FRAME_DUR_MS) // Still collecting frames
            {
                _plusTouchFrames.Add(tp);
            }
            else // Time for action!
            {
                if (_plusTPInitMove)
                {
                    // First touch: Don't move the cursor, just initialize
                    _plusTPPrevPos = tp.GetCenterOfMass();
                    _plusKalmanFilter.Initialize(0, 0); // Start with zero velocity
                    _plusTPInitMove = false;
                    _plusTouchFrames.Clear();
                    _plusStopwatch.Restart();
                    return;
                }

                if (_plusTouchFrames.Count <= 1)
                {
                    _plusTouchFrames.Add(tp); //add the current touch point.
                    _plusTouchFrames.Clear();
                    _plusStopwatch.Restart();
                    return; // No frames to work with
                }

                // Compute delta from oldest to newest frame
                TouchPoint first = _plusTouchFrames.First();
                TouchPoint last = _plusTouchFrames.Last();

                double dXRaw = last.GetCenterOfMass().X - first.GetCenterOfMass().X;
                double dYRaw = last.GetCenterOfMass().Y - first.GetCenterOfMass().Y;

                // Compute velocity
                _plusStopwatch.Stop();
                double dT = _plusStopwatch.ElapsedMilliseconds / 1000.0; // seconds
                double vXRaw = dXRaw / dT;
                double vYRaw = dYRaw / dT;

                // Kalman filter for velocity
                _plusKalmanFilter.Predict();
                _plusKalmanFilter.Update(vXRaw, vYRaw);

                (double fvX, double fvY) filteredV = _plusKalmanFilter.GetEstVelocity();

                // Compute speed and apply dynamic gain
                double speed = Sqrt(Pow(filteredV.fvX, 2) + Pow(filteredV.fvY, 2));
                double gain = Config.BASE_GAIN +
                    Config.SCALE_FACTOR * Tanh(speed * Config.SENSITIVITY);

                double kdX = filteredV.fvX * dT * gain * 500;
                double kdY = filteredV.fvY * dT * gain * 500;

                GESTURE_LOG.Information($"<Plus> KF vX = {filteredV.fvX:F3}, vY = {filteredV.fvY:F3}");
                GESTURE_LOG.Information($"<Plus> KF dX = {kdX:F3}, dY = {kdY:F3}");
                GESTURE_LOG.Information(Str.MINOR_LINE);

                // Update previous state
                _plusTPPrevPos = tp.GetCenterOfMass();
                _plusTouchFrames.Clear();
                _plusTouchFrames.Add(tp); //Add the current touch point for the next calculation.
                _plusStopwatch.Restart();

                // Make the move!
                MovePlus(kdX, kdY);

            }
        }

        //public void RotateLine(double deltaAngle)
        //{
        //    if (!_isVisible) return;

        //    _angle += deltaAngle;

        //    // Set the starting position of the line at the cursor
        //    _radiusLine.X1 = _cursorPosition.X;
        //    _radiusLine.Y1 = _cursorPosition.Y;

        //    // Extend the line outward based on the angle
        //    double radians = _angle * Math.PI / 180.0;

        //    _radiusLine.X2 = _cursorPosition.X - LINE_LENGTH * Math.Cos(radians);
        //    _radiusLine.Y2 = _cursorPosition.Y - LINE_LENGTH * Math.Sin(radians);

        //    // Move the cross to its new position after rotation
        //    UpdateCrossPosition();

        //    Output.Info(_radiusLine);
        //}

        // 📍 Update the line and cross, ensuring they stay within the screen
        private void UpdateLine()
        {
            // First update the radian angle
            UpdateAngleRad();
            
            // Get the max visible length from cursor position
            double maxLength = GetMaxVisibleLineLength(_lineStart, _angle);
            GESTURE_LOG.Verbose($"Sin = {Sin(_angleRad)}, Cos = {Cos(_angleRad)}");

            Point lineEnd = new Point(
                _lineStart.X + MAX_LINE_LENGTH * Cos(_angleRad),
                _lineStart.Y - MAX_LINE_LENGTH * Sin(_angleRad));

            //Canvas.SetLeft(PlusCanvas, x1 - linePartLength * Math.Cos(_angleRad) - plusOffset);  // Center of the first part
            //Canvas.SetTop(PlusCanvas, y1 - linePartLength * Math.Sin(_angleRad) - plusOffset);  // Center of the first part

            // Find the line formula (needed for finding the interception point)
            _lineA = Tan(_angleRad);
            _lineB = _lineStart.Y - _lineA * _lineStart.X;

            // Find the interception points (of the inner and outer rects)
            Point? innerPoint = GetLineRectangleIntersection(_lineStart, lineEnd, _innerRect);
            Point? outerPoint = GetLineRectangleIntersection(_lineStart, lineEnd, _outerRect);

            if (innerPoint != null && outerPoint != null)
            {
                GESTURE_LOG.Debug($"Inner: {Output.GetString((Point)innerPoint)}");
                GESTURE_LOG.Debug($"Outer: {Output.GetString((Point)outerPoint)}");

                _innerPoint = (Point)innerPoint;
                _outerPoint = (Point)outerPoint;

                // If first placement, place it in the middle point
                if (_plusPos.X == -1)
                {
                    // Get the middle point
                    Point midPoint = new Point();
                    midPoint.X = (double)(innerPoint?.X + outerPoint?.X) / 2;
                    midPoint.Y = (double)(innerPoint?.Y + outerPoint?.Y) / 2;

                    // Set Plus dist to (start -> mid)
                    double distToMid = Sqrt(
                        Pow(_lineStart.X - midPoint.X, 2) + 
                        Pow(_lineStart.Y - midPoint.Y, 2));

                    GESTURE_LOG.Information($"Dist = {distToMid}");

                    _plusDistance = distToMid;
                }
                
            }

            
            UpdatePlusPosition();
            DrawLineParts();
        }

        private void DrawLineParts()
        {
            //--- Draw line parts
            // Part 1
            double lineP1Dist = _plusDistance - 15;
            LinePart1.X1 = _lineStart.X;
            LinePart1.Y1 = _lineStart.Y;
            LinePart1.X2 = _lineStart.X + lineP1Dist * Cos(_angleRad);
            LinePart1.Y2 = _lineStart.Y - lineP1Dist * Sin(_angleRad);

            // Part 2
            double lineP2StartDist = lineP1Dist + 30; // gap = 30

            LinePart2.X1 = _lineStart.X + lineP2StartDist * Cos(_angleRad);
            LinePart2.Y1 = _lineStart.Y - lineP2StartDist * Sin(_angleRad);
            LinePart2.X2 = _outerPoint.X;
            LinePart2.Y2 = _outerPoint.Y;
        }

        private Point? GetLineRectangleIntersection(Point lineStart, Point lineEnd, Rect rect)
        {
            // LineStart is assumed to be inside the rectangle

            // Calculate the four lines of the rectangle
            Point rectTopLeft = new Point(rect.Left, rect.Top);
            Point rectTopRight = new Point(rect.Right, rect.Top);
            Point rectBottomLeft = new Point(rect.Left, rect.Bottom);
            Point rectBottomRight = new Point(rect.Right, rect.Bottom);

            // Check for intersection with each of the rectangle's lines
            Point? intersection;

            intersection = GetLineIntersection(lineStart, lineEnd, rectTopLeft, rectTopRight);
            if (intersection.HasValue) return intersection;

            intersection = GetLineIntersection(lineStart, lineEnd, rectTopRight, rectBottomRight);
            if (intersection.HasValue) return intersection;

            intersection = GetLineIntersection(lineStart, lineEnd, rectBottomRight, rectBottomLeft);
            if (intersection.HasValue) return intersection;

            intersection = GetLineIntersection(lineStart, lineEnd, rectBottomLeft, rectTopLeft);
            if (intersection.HasValue) return intersection;

            return null; // No intersection found (shouldn't happen with the condition)
        }

        private Point? GetLineIntersection(Point line1Start, Point line1End, Point line2Start, Point line2End)
        {
            double x1 = line1Start.X;
            double y1 = line1Start.Y;
            double x2 = line1End.X;
            double y2 = line1End.Y;

            double x3 = line2Start.X;
            double y3 = line2Start.Y;
            double x4 = line2End.X;
            double y4 = line2End.Y;

            // Calculate the denominator
            double denominator = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);

            // Check if lines are parallel
            if (denominator == 0)
            {
                return null; // Lines are parallel or coincident
            }

            // Calculate the numerators
            double ua = (x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3);
            double ub = (x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3);

            // Calculate the intersection parameters
            double uaDivDenom = ua / denominator;
            double ubDivDenom = ub / denominator;

            // Check if the intersection point is within the line segments
            if (uaDivDenom >= 0 && uaDivDenom <= 1 && ubDivDenom >= 0 && ubDivDenom <= 1)
            {
                // Calculate the intersection point
                double intersectionX = x1 + uaDivDenom * (x2 - x1);
                double intersectionY = y1 + uaDivDenom * (y2 - y1);

                return new Point(intersectionX, intersectionY);
            }

            return null; // Intersection point is outside the line segments
        }

        // ✂️ Compute the maximum visible length within screen bounds
        private double GetMaxVisibleLineLength(Point origin, double angle)
        {
            double radians = angle * Math.PI / 180.0;
            Screen secondMonitor = Screen.AllScreens[1];

            double maxX = secondMonitor.Bounds.Width;
            double maxY = secondMonitor.Bounds.Height;

            // Compute how far we can extend in both directions
            double limitX = (radians > 0 ? maxX - origin.X : origin.X) / Math.Cos(radians);
            double limitY = (radians > 0 ? maxY - origin.Y : origin.Y) / Math.Sin(radians);

            return Math.Min(MAX_LINE_LENGTH, Math.Min(limitX, limitY));
        }

        // 📍 Position the cross correctly within visible screen
        private void UpdatePlusPosition()
        {

            UpdateAngleRad();

            // Length of Plus
            double length = 10;

            // Compute new plus position along the line
            Point potentialPos = new Point();
            potentialPos.X = _lineStart.X + _plusDistance * Math.Cos(_angleRad);
            potentialPos.Y = _lineStart.Y - _plusDistance * Math.Sin(_angleRad);

            // If Plus is inside side window, update its position along with the line
            double distToOuterPoint = Utils.Dist(_lineStart, _outerPoint);
            double distToInnerPoint = Utils.Dist(_lineStart, _innerPoint);
            //Utils.IsBetweenInc(potentialPos.X, _innerPoint.X, _outerPoint.X) &&
            //    Utils.IsBetweenInc(potentialPos.Y, _innerPoint.Y, _outerPoint.Y)

            if (_plusDistance > distToInnerPoint && _plusDistance < distToOuterPoint)
            {
                // Move the plus to the potentional position
                _plusPos = potentialPos;

            } else // Stick the Plus to the inner intersection point
            {
                
                if (_plusDistance > distToOuterPoint)
                {
                    _plusPos = _outerPoint;
                } else
                {
                    _plusPos = _innerPoint;
                }
                
            }

            // Update distance
            _plusDistance = Utils.Dist(_lineStart, _plusPos);

            // Draw the Plus
            Canvas.SetLeft(PlusCanvas, _plusPos.X);
            Canvas.SetTop(PlusCanvas, _plusPos.Y);

            PlusHorizontal.X1 = -length * Math.Cos(_angleRad);
            PlusHorizontal.Y1 = length * Math.Sin(_angleRad); // Corrected Y1
            PlusHorizontal.X2 = length * Math.Cos(_angleRad);
            PlusHorizontal.Y2 = -length * Math.Sin(_angleRad); // Corrected Y2

            // Vertical line (perpendicular)
            PlusVertical.X1 = -length * Math.Cos(_angleRad + Math.PI / 2);
            PlusVertical.Y1 = length * Math.Sin(_angleRad + Math.PI / 2); // Corrected Y1
            PlusVertical.X2 = length * Math.Cos(_angleRad + Math.PI / 2);
            PlusVertical.Y2 = -length * Math.Sin(_angleRad + Math.PI / 2); // Corrected Y2

        }

        public Point GetCrossPosition()
        {
            return _plusPos;
        }
    }
}
