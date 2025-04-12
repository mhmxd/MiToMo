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

        public void TouchDown()
        {
            IsDown = true;
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
