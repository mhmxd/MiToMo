using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Math;

namespace Multi.Cursor
{
    internal static class Utils
    {
        public struct Range
        {
            public double Min { get; set; }
            public double Max { get; set; }

            public Range(double min, double max)
            {
                Min = min;
                Max = max;
            }

            public override string ToString() => $"[{Min}, {Max}]";
        }

        public static readonly Random Random = new Random();

        private const double PPI = 89;
        private const double MM_IN_INCH = 25.4;

        public static Boolean AbsIn(double value, double min, double max)
        {
            return Math.Abs(value) > min && Math.Abs(value) < max;
        }

        public static Boolean AbsIn(double value, (double min, double max) range)
        {
            return Math.Abs(value) > range.min && Math.Abs(value) < range.max;
        }

        public static Boolean In(double value, double min, double max)
        {
            return value > min && value < max;
        }

        public static Boolean InInc(double value, double min, double max)
        {
            return value >= min && value <= max;
        }

        public static Boolean In(double value, (double min, double max) range)
        {
            return value > range.min && value < range.max;
        }

        public static Boolean AbsInOR(
            double value1, double value2, 
            (double min, double max) range)
        {
            return AbsIn(value1, range) || AbsIn(value2, range);
        }

        public static Boolean InOR(
            double value1, double value2,
            (double min, double max) range)
        {
            return In(value1, range) || In(value2, range);
        }

        public static bool IsInside(Point p, int Xmin, int Xmax, int Ymin, int Ymax)
        {
            return p.X >= Xmin && p.X <= Xmax && p.Y >= Ymin && p.Y <= Ymax;
        }

        public static double Dist(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        public static int MM2PX(double mm)
        {
            return (int)(mm / MM_IN_INCH * PPI);
        }

        public static Point Relative(Point p, Point origin)
        {
            return new Point(p.X - origin.X, p.Y - origin.Y);
        }

        public static Point Relative(Point p, double originX, double originY)
        {
            return new Point(p.X - originX, p.Y - originY);
        }

        public static Point Offset(Point p, double offsetX, double offsetY)
        {
            return new Point(p.X + offsetX, p.Y + offsetY);
        }

        public static bool IsBetween(double v, double v1, double v2)
        {
            if (v1 < v2) return (v1 < v && v < v2);
            else return (v1 > v && v > v2);
        }

        public static bool IsBetweenInc(double v, double v1, double v2)
        {
            return IsBetween(v, v1, v2) || (v == v1) || (v == v2);
        }

        public static bool IsEitherAbsMore(double v1, double v2, double threshold)
        {
            return Abs(v1) > threshold || Abs(v2) > threshold;
        }

        public static double RandDouble(double min, double max)
        {
            return min + (max - min) * Random.NextDouble();
        }

        public static int ThicknessInPX(double dips)
        {
            return (int)(dips * PPI / 96.0);
        }
        public static string ListToString<T>(List<T> list)
        {
            return "{" + string.Join(", ", list) + "}";
        }

        public static bool Contains(Rect rect, List<Point> points)
        {
            foreach (Point p in points)
            {
                if (!rect.Contains(p))
                {
                    return false;
                }
            }

            return true;
        }
        
    }
}
