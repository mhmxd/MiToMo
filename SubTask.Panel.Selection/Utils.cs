using Common.Constants;
using MathNet.Numerics.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Tensorflow.Operations;
using static Common.Constants.ExpEnums;
using static SubTask.Panel.Selection.Output;
using static System.Math;
using static System.Windows.Rect;

namespace SubTask.Panel.Selection
{
    public static class Utils
    {
        private const double MM_IN_INCH = 25.4;
        private const double DIPS_IN_INCH = 96.0;

        private static Random _random = new Random();

        public class Pair
        {
            public int First { get; set; }
            public int Second { get; set; }
            public Pair(int first, int second)
            {
                First = first;
                Second = second;
            }
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

        public static Point OffsetPosition(this Point p, double offset)
        {
            return new Point(p.X + offset, p.Y + offset);
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
            return new Rect(window.Left + padding, window.Top + padding, window.Width - 2 * padding, window.Height - 2 * padding);
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
                range = 2 * PI - delta;
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

            // Convert the degree range to radians for trigonometric Functions
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

        public static Point FindRandPointWithDist(this Rect rect, Point src, double fixedDist, Side side)
        {
            const int maxAttempts = 1000;

            // Ensure src is outside rect for this method's logic to make sense
            if (rect.Contains(src))
            {
                // If src is inside, the concept of "distance from an outside point" is invalid.
                // You might want to handle this differently, e.g., return src itself or throw an exception.
                return new Point(-1, -1); // Indicate failure or invalid input for this scenario
            }

            // --- 1. Calculate Angular Range of the rectangle from src ---
            Point topLeft = new Point(rect.Left, rect.Top);
            Point topRight = new Point(rect.Right, rect.Top);
            Point bottomLeft = new Point(rect.Left, rect.Bottom);
            Point bottomRight = new Point(rect.Right, rect.Bottom);
            Point[] corners = { topLeft, topRight, bottomLeft, bottomRight };

            List<double> angles = new List<double>();
            foreach (Point corner in corners)
            {
                angles.Add(Math.Atan2(corner.Y - src.Y, corner.X - src.X));
            }

            List<double> normalizedAngles = angles.Select(a => a < 0 ? a + 2 * Math.PI : a).ToList();
            normalizedAngles.Sort();

            double minSearchRad;
            double maxSearchRad;
            double maxGap = 0;
            int maxGapIndex = -1;

            for (int i = 0; i < normalizedAngles.Count; i++)
            {
                double currentAngle = normalizedAngles[i];
                double nextAngle = normalizedAngles[(i + 1) % normalizedAngles.Count];

                double gap;
                if (nextAngle < currentAngle) // Wrap-around (e.g., 350 deg and 10 deg)
                {
                    gap = (nextAngle + 2 * Math.PI) - currentAngle;
                }
                else
                {
                    gap = nextAngle - currentAngle;
                }

                if (gap > maxGap)
                {
                    maxGap = gap;
                    maxGapIndex = i;
                }
            }

            minSearchRad = normalizedAngles[(maxGapIndex + 1) % normalizedAngles.Count];
            maxSearchRad = normalizedAngles[maxGapIndex];

            if (maxSearchRad < minSearchRad)
            {
                maxSearchRad += 2 * Math.PI; // Adjust for continuous range if it wrapped around
            }
            // --- End Angular Range Calculation ---


            // --- 2. Determine the actual minimum and maximum distances from src to rect boundaries ---
            // This is crucial to find a point within the rect if fixedDist is problematic.
            double minRectDist = double.MaxValue;
            double maxRectDist = 0;

            // Calculate distances to all 4 corners
            foreach (Point corner in corners)
            {
                double d = Dist(src, corner);
                minRectDist = Math.Min(minRectDist, d);
                maxRectDist = Math.Max(maxRectDist, d);
            }

            // Calculate distances to closest point on each side segment (if projection is on segment)
            // This refines minDist particularly if src is aligned with a side but not a corner.
            minRectDist = Math.Min(minRectDist, src.DistanceToLineSegment(topLeft, topRight));
            minRectDist = Math.Min(minRectDist, src.DistanceToLineSegment(topRight, bottomRight));
            minRectDist = Math.Min(minRectDist, src.DistanceToLineSegment(bottomRight, bottomLeft));
            minRectDist = Math.Min(minRectDist, src.DistanceToLineSegment(bottomLeft, topLeft));

            // Note: For maxRectDist, it will always be one of the corners. The lines do not extend the "farthest" reach beyond corners.


            // --- 3. Generate random points within the determined ranges ---
            for (int i = 0; i < maxAttempts; i++)
            {
                // Generate a random angle within the calculated angular range
                double randomRad = minSearchRad + _random.NextDouble() * (maxSearchRad - minSearchRad);

                // Determine the distance for this attempt:
                // Prioritize fixedDist if it's within the actual min/max distances to the rectangle.
                // Otherwise, pick a random distance within the actual min/max distances to ensure a point is found.
                double currentDist;
                if (fixedDist >= minRectDist && fixedDist <= maxRectDist)
                {
                    currentDist = fixedDist; // Use the exact fixed distance if it's valid
                }
                else
                {
                    // If fixedDist is outside the valid range, pick a random distance within the valid range
                    currentDist = minRectDist + _random.NextDouble() * (maxRectDist - minRectDist);
                }

                // Calculate candidate point coordinates
                double candidateX = src.X + currentDist * Math.Cos(randomRad);
                double candidateY = src.Y + currentDist * Math.Sin(randomRad);
                Point candidate = new Point(candidateX, candidateY);

                // Check if the candidate point is within the target rectangle
                if (rect.Contains(candidate))
                {
                    return candidate;
                }
            }

            // No valid point found within maxAttempts, even after adjusting distance strategy
            return new Point(-1, -1); // Indicate failure
        }

        public static Point FindPointWithinDistRangeFromMultipleSources(
            this Rect rect,
            List<Point> srcPoints,
            Range distRange)
        {
            const int maxAttempts = 5000; // Increased attempts as the search space is more constrained

            //if (srcPoints == null || srcPoints.Count == 0)
            //{
            //    throw new ArgumentException("Source points list cannot be null or empty.", nameof(srcPoints));
            //}
            //if (minAllowedDist < 0 || maxAllowedDist < minAllowedDist)
            //{
            //    throw new ArgumentOutOfRangeException("Distance range is invalid.");
            //}

            for (int i = 0; i < maxAttempts; i++)
            {
                // 1. Generate a random candidate point *inside* the rectangle
                double candidateX = rect.Left + _random.NextDouble() * rect.Width;
                double candidateY = rect.Top + _random.NextDouble() * rect.Height;
                Point candidate = new Point(candidateX, candidateY);

                // 2. Check if this candidate point satisfies the distance criteria for *all* source points
                bool allDistancesValid = true;
                foreach (Point src in srcPoints)
                {
                    double dist = candidate.DistanceTo(src);

                    if (!distRange.ContainsExc(dist))
                    {
                        allDistancesValid = false;
                        break; // No need to check other source points if one fails
                    }
                }

                if (allDistancesValid)
                {
                    return candidate; // Found a valid point!
                }
            }

            // No valid point found within maxAttempts
            return new Point(-1, -1); // Indicate failure
        }

        public static double DistanceTo(this Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

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

        public static Side DirToSide(this Direction dir)
        {
            return dir switch
            {
                Direction.Up => Side.Top,
                Direction.Down => Side.Down,
                Direction.Left => Side.Left,
                Direction.Right => Side.Right,
                _ => throw new ArgumentOutOfRangeException(nameof(dir), "Unknown Direction value")
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

        public static double MaxDistanceFromPoint(this Rect rect, Point src)
        {
            // If the source point is inside the rectangle, the concept of "largest distance from an outside point"
            // becomes ambiguous in the context of points *inside* the rectangle.
            // For this method, we assume src is strictly outside the rectangle.
            if (rect.Contains(src))
            {
                throw new ArgumentException("Source point must be outside the rectangle.");
            }

            // The largest distance from an outside point to any point within a rectangle
            // will always be the distance from the outside point to one of the rectangle's four corners.
            // This is because a rectangle is a convex shape, and for any convex shape,
            // the maximum distance from an external point to any point within the shape
            // will occur at one of its vertices.

            Point topLeft = new Point(rect.Left, rect.Top);
            Point topRight = new Point(rect.Right, rect.Top);
            Point bottomLeft = new Point(rect.Left, rect.Bottom);
            Point bottomRight = new Point(rect.Right, rect.Bottom);

            double maxDist = 0;

            // Calculate the distance from the source point to each corner and find the maximum.
            maxDist = Math.Max(maxDist, Dist(src, topLeft));
            maxDist = Math.Max(maxDist, Dist(src, topRight));
            maxDist = Math.Max(maxDist, Dist(src, bottomLeft));
            maxDist = Math.Max(maxDist, Dist(src, bottomRight));

            return maxDist;
        }

        public static double DistanceToLineSegment(this Point p, Point s1, Point s2)
        {
            double dx = s2.X - s1.X;
            double dy = s2.Y - s1.Y;

            // If the segment is a point (s1 == s2)
            if (dx == 0 && dy == 0)
            {
                return Dist(p, s1); // Distance from point p to point s1
            }

            // Calculate the parameter t that represents the projection of point p onto the line defined by s1 and s2
            // t = ((p.x - s1.x) * dx + (p.y - s1.y) * dy) / (dx*dx + dy*dy)
            double t = ((p.X - s1.X) * dx + (p.Y - s1.Y) * dy) / (dx * dx + dy * dy);

            // Clamp t to be between 0 and 1. This ensures the projected point lies on the segment.
            t = Math.Max(0, Math.Min(1, t));

            // Calculate the closest point on the segment to p
            Point closestPoint = new Point(s1.X + t * dx, s1.Y + t * dy);

            // Return the distance from p to this closest point
            return Dist(p, closestPoint);
        }

        public static double Avg(this List<double> numbers)
        {
            if (numbers == null || numbers.Count == 0) return 0;
            return numbers.Average();
        }

        public static Technique GetDevice(this Technique tech)
        {
            return tech == Technique.TOMO_SWIPE || tech == Technique.TOMO_TAP ? Technique.TOMO : Technique.MOUSE;
        }

        public static int GetDuration(long start, long end)
        {
            if (start != -1 && end != -1 && end >= start)
            {
                return (int)(end - start);
            }
            return -1; // Return -1 if timestamps are not found or invalid
        }

        public static string MStoSec(int ms)
        {
            // Convert milliseconds to seconds with 2 decimal places
            return (ms / 1000.0).ToString("F2");
        }

        public static string[] GetPropertyValues<T>(T obj)
        {
            return typeof(T).GetProperties()
                .Where(p => p.CanRead)
                .Select(p => p.GetValue(obj)?.ToString() ?? "")
                .ToArray();
        }

        public static string[] GetPropertyNames<T>(T obj)
        {
            return typeof(T).GetProperties()
                .Where(p => p.CanRead)
                .Select(p => p.Name)
                .ToArray();
        }

        public static bool HasDuplicates<T>(this IEnumerable<T> list)
        {
            HashSet<T> set = new HashSet<T>();
            foreach (T item in list)
            {
                if (!set.Add(item))
                {
                    return true; // Duplicate found
                }
            }
            return false; // No duplicates
        }

        public static KeyValuePair<TKey, TValue> GetRandomEntry<TKey, TValue>(this IDictionary<TKey, TValue> dict)
        {
            if (dict == null || dict.Count == 0)
                throw new ArgumentException("Dictionary is null or empty.", nameof(dict));

            int index = _random.Next(dict.Count);
            return dict.ElementAt(index);
        }

    }

}
