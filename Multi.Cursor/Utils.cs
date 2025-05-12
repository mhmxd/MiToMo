using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Math;
using static System.Windows.Rect;

namespace Multi.Cursor
{
    internal static class Utils
    {
        private static Random _random = new Random();

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

        //public static double DistX(Point p1, Point p2)
        //{
        //    return Abs(p1.X - p2.X);
        //}

        public static int MM2PX(double mm)
        {
            return (int)(mm / MM_IN_INCH * PPI);
        }

        public static double PX2MM(double px)
        {
            return px * MM_IN_INCH / PPI;
        }

        public static Point Relative(Point p, Point origin)
        {
            return new Point(p.X - origin.X, p.Y - origin.Y);
        }

        public static Point Relative(Point p, double originX, double originY)
        {
            return new Point((int)(p.X - originX), (int)(p.Y - originY));
        }

        public static Point Offset(Point p, double offsetX, double offsetY)
        {
            return new Point((int)(p.X + offsetX), (int)(p.Y + offsetY));
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

        public static Rect GetRect(this Window window)
        {
            return new Rect(window.Left, window.Top, window.Width, window.Height);
        }

        public static Rect GetRect(this Window window, int padding)
        {
            return new Rect(window.Left + padding, window.Top + padding, window.Width - 2*padding, window.Height - 2*padding);
        }

        public static bool ContainsNot(Rect rect, List<Point> points)
        {
            foreach (Point p in points)
            {
                if (rect.Contains(p))
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

        public static double NormalizeAngleRadian(double angle)
        {
            while (angle < 0) angle += 2 * Math.PI;
            while (angle >= 2 * Math.PI) angle -= 2 * Math.PI;
            return angle;
        }

        public static double RadToDeg(double rad)
        {
            return rad * 180 / Math.PI;
        }

        public static double DegToRad(double deg)
        {
            return deg * Math.PI / 180;
        }

        //public static double RandAngleClockwise(double angle1, double angle2)
        //{
        //    double range;
        //    if (angle1 >= angle2)
        //    {
        //        range = angle1 - angle2;
        //    }
        //    else
        //    {
        //        range = (2*PI - angle2) + angle1;
        //    }

        //    Random random = new Random();
        //    double randomOffset = random.NextDouble() * range;

        //    double randomAngle = angle2 + randomOffset;

        //    return NormalizeAngleRadian(randomAngle);
        //}

        public static double RandAngleClockwise(double angle1, double angle2)
        {
            double delta = NormalizeAngleRadian(angle2 - angle1);
            double range;

            if (delta <= PI)
            {
                // Clockwise arc is the shorter arc
                range = delta;
            }
            else
            {
                // Clockwise arc is the longer arc (go the other way around)
                range = 2*PI - delta;
            }

            double randomOffset = _random.NextDouble() * range;
            double randomAngle;

            if (delta <= PI)
            {
                // Add the offset to angle1 in the clockwise direction
                randomAngle = angle1 + randomOffset;
            }
            else
            {
                // Add the offset to angle2 in the clockwise direction (which is the shorter way from angle2 to angle1)
                randomAngle = angle2 + randomOffset;
            }

            return NormalizeAngleRadian(randomAngle);
        }


        public static void Shuffle<T>(this List<T> list)
        {
            Random random = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
                return true;
            }

            return false;
        }

    }

}
