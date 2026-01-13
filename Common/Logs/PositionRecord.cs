using System.Drawing;

namespace Common.Logs
{
    public class PositionRecord
    {
        public long timestamp { get; set; } // Ticks
        public int x { get; set; }
        public int y { get; set; }

        public PositionRecord(int x, int y)
        {
            this.x = x;
            this.y = y;
            // Get current time in ticks
            timestamp = DateTime.UtcNow.Ticks;
        }

        public PositionRecord(double x, double y)
        {
            this.x = (int)x;
            this.y = (int)y;
            // Get current time in ticks
            timestamp = DateTime.UtcNow.Ticks;
        }

        public PositionRecord(Point point)
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
