using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SubTask.PanelNavigation
{
    internal class TouchFinger
    {
        public int MinCol, MaxCol;
        public bool IsDown;
        public bool IsUp => !IsDown;

        private int _downRow, _downCol;
        private Point _downPosition;
        private Point _lastPosition;

        private Stopwatch _timer = new Stopwatch();

        public TouchFinger(int minCol, int maxCol)
        {
            MinCol = minCol;
            MaxCol = maxCol;
            IsDown = false;
        }

        public (int, int) GetRange()
        {
            return (MinCol, MaxCol);
        }

        public void LiftUp()
        {
            IsDown = false;
        }

        public void TouchDown(int downRow, int downCol)
        {
            IsDown = true;
            this._downRow = downRow;
            this._downCol = downCol;
        }

        public void TouchDown(Point downPoint)
        {
            IsDown = true;
            this._downPosition = downPoint;
        }

        public void TouchMove(Point newPoint)
        {
            _lastPosition = newPoint;
        }

        public Point GetDownPoint()
        {
            return _downPosition;
        }

        public int GetDownRow()
        {
            return _downRow;
        }

        public int GetDownCol()
        {
            return _downCol;
        }

        public double GetTravelDist()
        {
            if (_lastPosition == null) return 0;
            return Utils.Dist(_lastPosition, _downPosition);
        }

        public double GetTravelDistX()
        {
            if (_lastPosition == null) return 0;
            return Math.Abs(_lastPosition.X - _downPosition.X);
        }

        public double GetTravelDistY()
        {
            if (_lastPosition == null) return 0;
            return Math.Abs(_lastPosition.Y - _downPosition.Y);
        }

        public void RestartTimer()
        {
            _timer.Restart();
        }

        public long GetDownTime()
        {
            return _timer.ElapsedMilliseconds;
        }
    }
}
