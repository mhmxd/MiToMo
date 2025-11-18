using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Function.Point.Logging
{
    internal struct CursorRecord
    {
        public long timestamp { get; set; } // Ticks
        public int x { get; set; }
        public int y { get; set; }

        public CursorRecord(int x, int y)
        {
            this.x = x;
            this.y = y;
            // Get current time in ticks
            timestamp = DateTime.UtcNow.Ticks;
        }

        public CursorRecord(Point point)
        {
            x = (int)point.X;
            y = (int)point.Y;
            // Get current time in ticks
            timestamp = DateTime.UtcNow.Ticks;
        }

        public static string GetHeader()
        {
            return "timestamp;x_pos;y_pos";
        }
    }
}
