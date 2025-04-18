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

        public static List<double> GetIntersectionAngles(Point circleCenter, double radius, Rect rect)
        {
            List<double> angles = new List<double>();

            // Define the rectangle's sides as segments
            List<(Point A, Point B)> edges = new List<(Point, Point)>
        {
            (new Point(rect.Left, rect.Top),    new Point(rect.Right, rect.Top)),    // Top
            (new Point(rect.Right, rect.Top),   new Point(rect.Right, rect.Bottom)), // Right
            (new Point(rect.Right, rect.Bottom),new Point(rect.Left, rect.Bottom)),  // Bottom
            (new Point(rect.Left, rect.Bottom), new Point(rect.Left, rect.Top))      // Left
        };

            foreach (var (A, B) in edges)
            {
                foreach (var p in LineCircleIntersections(circleCenter, radius, A, B))
                {
                    double angle = Math.Atan2(p.Y - circleCenter.Y, p.X - circleCenter.X);
                    angles.Add(angle);
                }
            }

            return angles;
        }

        private static List<Point> LineCircleIntersections(Point center, double radius, Point p1, Point p2)
        {
            List<Point> intersections = new List<Point>();

            // Convert to vector form: P = p1 + t*(p2 - p1)
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;

            double fx = p1.X - center.X;
            double fy = p1.Y - center.Y;

            double a = dx * dx + dy * dy;
            double b = 2 * (fx * dx + fy * dy);
            double c = fx * fx + fy * fy - radius * radius;

            double discriminant = b * b - 4 * a * c;

            if (discriminant < 0)
            {
                // No intersection
                return intersections;
            }

            discriminant = Math.Sqrt(discriminant);

            double t1 = (-b - discriminant) / (2 * a);
            double t2 = (-b + discriminant) / (2 * a);

            if (t1 >= 0 && t1 <= 1)
            {
                intersections.Add(new Point(p1.X + t1 * dx, p1.Y + t1 * dy));
            }
            if (t2 >= 0 && t2 <= 1 && discriminant > 0)
            {
                intersections.Add(new Point(p1.X + t2 * dx, p1.Y + t2 * dy));
            }

            return intersections;
        }

        private static double NormalizeAngle(double angle)
        {
            while (angle < 0) angle += 2 * Math.PI;
            while (angle >= 2 * Math.PI) angle -= 2 * Math.PI;
            return angle;
        }

    }
}
