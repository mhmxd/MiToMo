using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multi.Cursor
{
    internal class Finger
    {
        public int MinCol, MaxCol;
        public bool IsDown;
        public bool IsUp => !IsDown;
        
        private int _downRow, _downCol;

        private Stopwatch _timer = new Stopwatch();

        public Finger(int minCol, int maxCol)
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

        public int GetDownRow()
        {
            return _downRow;
        }

        public int GetDownCol()
        {
            return _downCol;
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
