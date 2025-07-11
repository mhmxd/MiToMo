﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MathNet.Numerics.Integration;
using Tensorflow.Operations;
using static System.Math;
using static System.Windows.Rect;
using static Multi.Cursor.Output;

namespace Multi.Cursor
{
    internal static class Utils
    {
        private const double MM_IN_INCH = 25.4;
        private const double DIPS_IN_INCH = 96.0;

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
            return (int)Math.Round(mm / MM_IN_INCH * Config.PPI);
        }

        //public static double MmToDips(double mm)
        //{
        //    return (mm / MM_IN_INCH) * DIPS_IN_INCH;
        //}

        //public static double DipsToMm(double dips)
        //{
        //    return (dips / DIPS_IN_INCH) * MM_IN_INCH;
        //}

        public static double PX2MM(double px)
        {
            return px * MM_IN_INCH / Config.PPI;
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

        public static Point OffsetPosition(this Point p, double offsetX, double offsetY)
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
            return min + (max - min) * _random.NextDouble();
        }

        public static int ThicknessInPX(double dips)
        {
            return (int)(dips * Config.PPI / 96.0);
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
            (new Point(rect.Right, rect.Bottom),new Point(rect.Left, rect.Bottom)),  // Down
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

        public static T GetRandomElement<T>(this IList<T> list) // Using IList<T> for broader compatibility
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list), "List cannot be null.");
            }

            if (list.Count == 0)
            {
                throw new ArgumentException("List cannot be empty.", nameof(list));
            }

            int randomIndex = _random.Next(list.Count);
            return list[randomIndex];
        }

        public static bool ContainsKeys<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, params TKey[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (!dictionary.ContainsKey(keys[i]))
                {
                    return false; // If any key is not found, return false
                }
            }

            return true;
        }

        public static bool ContainsAny<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, params TKey[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (dictionary.ContainsKey(keys[i]))
                {
                    return true; // If any key is found, return true
                }
            }
            return false; // No keys found
        }

        public static Point FindRandPointWithDist(this Rect rect, Point src, int dist, Range degreeRange)
        {
            // Define a maximum number of attempts to prevent infinite loops
            // Adjust this value based on expected density of valid points
            const int maxAttempts = 1000;

            // Convert the degree range to radians for trigonometric functions
            double minRad = DegToRad(degreeRange.Min);
            double maxRad = DegToRad(degreeRange.Max);

            for (int i = 0; i < maxAttempts; i++)
            {
                // 1. Generate a random angle within the specified range [minRad, maxRad]
                // Note: Rng.NextDouble() returns a value between 0.0 and 1.0 (exclusive of 1.0)
                double randomRad = minRad + (_random.NextDouble() * (maxRad - minRad));

                // 2. Calculate candidate point (startCenter) coordinates
                // s_x = t_x + d * cos(theta)
                // s_y = t_y + d * sin(theta)
                double s_x = src.X + dist * Math.Cos(randomRad);
                double s_y = src.Y + dist * Math.Sin(randomRad);

                // Convert to integer Point coordinates (rounding is typical for screen coordinates)
                Point startCenter = new Point((int)Math.Round(s_x), (int)Math.Round(s_y));
                
                // 3. Check if candidate is inside the Rect
                // The Contains method checks if the point is within or on the edge of the rectangle.
                if (rect.Contains(startCenter))
                {
                    return startCenter; // Found a valid point!
                }
            }

            // If after maxAttempts, no suitable point is found, return a default/empty Point
            // You might want to throw an exception or return a nullable Point in a real application
            return new Point(-1, -1);
        }

        public static Point FindRandPointWithDist(this Rect rect, Point src, double dist, Side side)
        {

            //rect.TrialInfo($"Finding position: Rect: {rect.ToString()}; Src: {src}; Dist: {dist:F2}; Side: {side}");

            const int maxAttempts = 1000;
            // A wider angle spread is often necessary to cover edge cases, especially for larger distances or wide/tall rects.
            // A 180-degree spread covers a full half-plane.
            const double angleSearchSpreadDeg = 180.0;

            double baseAngleRad;

            // Determine the base angle based on the side for Y-DOWN PIXEL COORDINATES
            switch (side)
            {
                case Side.Right:
                    baseAngleRad = DegToRad(0);   // Right is 0 degrees (positive X axis)
                    break;
                case Side.Down:
                    baseAngleRad = DegToRad(90);  // Down is 90 degrees (positive Y axis - Y increases downwards)
                    break;
                case Side.Left:
                    baseAngleRad = DegToRad(180); // Left is 180 degrees (negative X axis)
                    break;
                case Side.Top:
                    baseAngleRad = DegToRad(270); // Up is 270 degrees (or -90 degrees) (negative Y axis - Y increases upwards)
                    break;
                default:
                    throw new ArgumentException("Invalid Side specified.");
            }

            // Calculate the angular range for random point generation
            double halfSpreadRad = DegToRad(angleSearchSpreadDeg / 2.0);
            double minSearchRad = baseAngleRad - halfSpreadRad;
            double maxSearchRad = baseAngleRad + halfSpreadRad;

            // Normalize angles to be within a standard range (e.g., -PI to PI or 0 to 2PI)
            // This is good practice but not strictly necessary for Math.Cos/Sin if the range is continuous.
            // If minSearchRad < -PI, add 2PI. If maxSearchRad > PI, subtract 2PI.
            // For simplicity, we'll let Math.Cos/Sin handle angles outside 0-2PI.

            for (int i = 0; i < maxAttempts; i++)
            {
                // Generate a random angle within our defined search sector
                double randomRad = minSearchRad + _random.NextDouble() * (maxSearchRad - minSearchRad);

                // Calculate candidate point coordinates
                double candidateX = src.X + dist * Math.Cos(randomRad);
                double candidateY = src.Y + dist * Math.Sin(randomRad);

                Point candidate = new Point(candidateX, candidateY); // Keep as double for Contains if Rect/Point allow it
                                                                     // Or cast to int if your Rect.Contains expects int (common for graphics)
                                                                     // Point candidate = new Point((int)Math.Round(candidateX), (int)Math.Round(candidateY));


                // Check if the candidate point is within the target rectangle
                if (rect.Contains(candidate))
                {
                    return candidate;
                }
            }

            // No valid point found within maxAttempts
            //rect.TrialInfo($"No point found for Rect: {rect.ToString()}; Src: {src}; Dist: {dist:F2}; Side: {side}");
            return new Point(-1, -1); // Indicate failure
        }

        //public static Point FindRandPointWithDist(this Rect rect, Point src, double dist, Side side)
        //{
        //    rect.TrialInfo($"Finding position: Rect: {rect.ToString()}; Src: {src}; Dist: {dist:F2}; Side: {side}");

        //    const int maxAttempts = 1000;
        //    const double angleSpreadDeg = 90.0; // Spread in degrees

        //    // 1. Find the center of the target rect
        //    Point center = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);

        //    // 2. Calculate the direction vector and base angle in radians
        //    double dx = center.X - src.X;
        //    double dy = center.Y - src.Y;
        //    double angleToCenter = Math.Atan2(dy, dx); // This is in radians

        //    // 3. Compute the spread around that angle
        //    double spreadRad = DegToRad(angleSpreadDeg);
        //    double minRad = angleToCenter - spreadRad / 2;
        //    double maxRad = angleToCenter + spreadRad / 2;

        //    for (int i = 0; i < maxAttempts; i++)
        //    {
        //        double randomRad = minRad + _random.NextDouble() * (maxRad - minRad);
        //        double s_x = src.X + dist * Math.Cos(randomRad);
        //        double s_y = src.Y + dist * Math.Sin(randomRad);
        //        Point candidate = new Point((int)Math.Round(s_x), (int)Math.Round(s_y));

        //        if (rect.Contains(candidate))
        //        {
        //            return candidate;
        //        }
        //    }

        //    // No valid point found
        //    return new Point(-1, -1);
        //}

        public static Side GetOpposite(this Side side)
        {
            return side switch
            {
                Side.Left => Side.Right,
                Side.Right => Side.Left,
                Side.Top => Side.Down,
                Side.Down => Side.Top,
                _ => throw new ArgumentOutOfRangeException(nameof(side), "Unknown Side value")
            };
        }

        /// <summary>
        /// Shifts the elements of an array by a specified number of positions, with wrapping, in-place.
        /// A positive shiftAmount moves elements to the right; a negative shiftAmount moves elements to the left.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="array">The array to shift (will be modified).</param>
        /// <param name="shiftAmount">The number of positions to shift. Can be positive or negative.</param>
        public static void ShiftElementsInPlace<T>(this T[] array, int shiftAmount)
        {
            // No modification needed for null or empty arrays
            if (array == null || array.Length == 0)
            {
                return;
            }

            int length = array.Length;

            // Calculate the effective shift amount, handling cases where shiftAmount
            // is greater than array length or negative.
            int actualShift = shiftAmount % length;
            if (actualShift < 0)
            {
                actualShift += length; // Convert negative shifts to equivalent positive shifts
            }

            // If actualShift is 0, no shifting is needed
            if (actualShift == 0)
            {
                return;
            }

            // --- In-place Shifting Logic (using a temporary copy of the original for correct placement) ---
            // The most robust way for in-place with wrapping is to first copy the original
            // array, then place elements from the original into their new positions in the *same* array.

            // Create a temporary copy of the original array's elements
            // This is necessary because if you try to directly move elements,
            // you might overwrite an element before it's been moved to its new position.
            T[] tempArray = new T[length];
            Array.Copy(array, tempArray, length);

            for (int i = 0; i < length; i++)
            {
                // Calculate the new index for the element that was originally at 'i'
                // The element from tempArray[i] moves to array[(i + actualShift) % length]
                int newIndex = (i + actualShift) % length;
                array[newIndex] = tempArray[i];
            }
        }

        public static bool ContainsPartialKey<T>(this Dictionary<string, T> dictionary, string keyPart)
        {
            foreach (string key in dictionary.Keys)
            {
                if (key.Contains(keyPart)) return true;
            }

            return false;
        }

    }

}
