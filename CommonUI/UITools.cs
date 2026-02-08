using Common.Helpers;
using Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using static Common.Constants.ExpEnums;

namespace CommonUI
{

    public static class UITools
    {

        private const double MM_IN_INCH = 25.4;

        private static readonly Random _random = new Random();

        public static double PX2MM(double px)
        {
            return px * MM_IN_INCH / ExpEnvironment.PPI;
        }

        public static int MM2PX(double mm)
        {
            return (int)Math.Round(mm / MM_IN_INCH * ExpEnvironment.PPI);
        }

        public static double Dist(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        public static double DistInMM(Point p1, Point p2)
        {
            // Convert pixel distance to mm
            return PX2MM(Dist(p1, p2));
        }

        public static double DistanceTo(this Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        public static double DistanceToLineSegment(this Point p, Point s1, Point s2)
        {
            double dx = s2.X - s1.X;
            double dy = s2.Y - s1.Y;

            // If the segment is a point (s1 == s2)
            if (dx == 0 && dy == 0)
            {
                return Dist(p, s1); // AvgDistanceMM from point p to point s1
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

        public static (Point, double) FindPointWithinDistRangeFromMultipleSources(
            this Rect rect,
            List<Point> srcPoints,
            MRange distRange)
        {
            if (srcPoints.Count == 0) return (new Point(-1, -1), -1);

            const int maxAttempts = 1000; // Can be lower now because search is smarter
            Point firstSrc = srcPoints[0];

            for (int i = 0; i < maxAttempts; i++)
            {
                // Smarter Sampling: Pick a point in the valid ring around the first source
                double angle = _random.NextDouble() * Math.PI * 2;
                double r = distRange.Min + _random.NextDouble() * (distRange.Max - distRange.Min);

                double candidateX = firstSrc.X + r * Math.Cos(angle);
                double candidateY = firstSrc.Y + r * Math.Sin(angle);
                Point candidate = new Point(candidateX, candidateY);

                // 1. Check if inside bounds first (fastest check)
                if (!rect.Contains(candidate)) continue;

                // 2. Check other sources
                bool allValid = true;
                double sumDistMM = 0;

                for (int j = 0; j < srcPoints.Count; j++)
                {
                    double d = candidate.DistanceTo(srcPoints[j]);
                    if (!distRange.ContainsExc(d))
                    {
                        allValid = false;
                        break;
                    }
                    sumDistMM += PX2MM(d);
                }

                if (allValid)
                {
                    return (candidate, sumDistMM / srcPoints.Count);
                }
            }
            return (new Point(-1, -1), -1);
        }

        //public static (Point, double) FindPointWithinDistRangeFromMultipleSources(
        //    this Rect rect,
        //    List<Point> srcPoints,
        //    MRange distRange)
        //{
        //    const int maxAttempts = 5000; // Increased attempts as the search space is more constrained

        //    for (int i = 0; i < maxAttempts; i++)
        //    {
        //        // 1. Generate a random candidate point *inside* the rectangle
        //        double candidateX = rect.Left + _random.NextDouble() * rect.Width;
        //        double candidateY = rect.Top + _random.NextDouble() * rect.Height;
        //        Point candidate = new Point(candidateX, candidateY);

        //        // 2. Check if this candidate point satisfies the distance criteria for *all* source points
        //        bool allDistancesValid = true;
        //        List<double> distMMList = new List<double>();
        //        foreach (Point src in srcPoints)
        //        {
        //            double dist = candidate.DistanceTo(src);

        //            if (!distRange.ContainsExc(dist))
        //            {
        //                allDistancesValid = false;
        //                break; // No need to check other source points if one fails
        //            }

        //            // Add the valid dist to the list
        //            distMMList.Add(PX2MM(dist));
        //        }

        //        // Found a valid point
        //        if (allDistancesValid)
        //        {
        //            double avgDist = distMMList.Average();
        //            return (candidate, avgDist);
        //        }
        //    }

        //    // No valid point found within maxAttempts
        //    return (new Point(-1, -1), -1); // Indicate failure
        //}

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

        public static Point FindRandPointWithDist(this Rect rect, Point src, int dist, MRange degreeRange)
        {
            // Define a maximum number of attempts to prevent infinite loops
            // Adjust this value based on expected density of valid points
            const int maxAttempts = 1000;

            // Convert the degree range to radians for trigonometric Functions
            double minRad = MTools.DegToRad(degreeRange.Min);
            double maxRad = MTools.DegToRad(degreeRange.Max);

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

        public static Rect GetRect(this Window window)
        {
            return new Rect(window.Left, window.Top, window.Width, window.Height);
        }

        public static Rect GetRect(this Window window, int padding)
        {
            return new Rect(window.Left + padding, window.Top + padding, window.Width - 2 * padding, window.Height - 2 * padding);
        }

        public static int ThicknessInPX(double dips)
        {
            return (int)(dips * ExpEnvironment.PPI / 96.0);
        }

        public static bool IsInside(Point p, int Xmin, int Xmax, int Ymin, int Ymax)
        {
            return p.X >= Xmin && p.X <= Xmax && p.Y >= Ymin && p.Y <= Ymax;
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

        // --- String Representations ---
        public static string Str(this Point point)
        {
            return $"({point.X:F2}, {point.Y:F2})";
        }

        public static string Str(this Rect rect)
        {
            return $"{{X={rect.X},Y={rect.Y},Width={rect.Width},Height={rect.Height}}}";
        }
    }
}
