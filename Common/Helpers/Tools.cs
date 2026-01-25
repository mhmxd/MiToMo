using Common.Settings;
using CommunityToolkit.HighPerformance;
using System;
using System.Text;
using static System.Math;

namespace Common.Helpers
{
    public static class Tools
    {
        private static readonly Random _random = new Random();

        public static double RadToDeg(double rad)
        {
            return rad * 180 / PI;
        }

        public static double DegToRad(double deg)
        {
            return deg * PI / 180;
        }

        public static double NormalizeAngleRadian(double angle)
        {
            while (angle < 0) angle += 2 * PI;
            while (angle >= 2 * PI) angle -= 2 * PI;
            return angle;
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
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _random.Next(n + 1);
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

        public static int GetDuration(long start, long end)
        {
            if (start != -1 && end != -1 && end >= start)
            {
                return (int)(end - start);
            }
            return -1; // Return -1 if timestamps are not found or invalid
        }

        public static double Avg(this List<double> numbers)
        {
            if (numbers == null || numbers.Count == 0) return 0;
            return numbers.Average();
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

        public static string[] GetPropertyNames<T>(T obj)
        {
            return typeof(T).GetProperties()
                .Where(p => p.CanRead)
                .Select(p => p.Name)
                .ToArray();
        }

        public static string[] GetPropertyValues<T>(T obj)
        {
            return typeof(T).GetProperties()
                .Where(p => p.CanRead)
                .Select(p => p.GetValue(obj)?.ToString() ?? "")
                .ToArray();
        }

        public static double RandDouble(double min, double max)
        {
            return min + (max - min) * _random.NextDouble();
        }

        public static Boolean AbsIn(double value, double min, double max)
        {
            return Math.Abs(value) > min && Math.Abs(value) < max;
        }

        public static Boolean AbsIn(double value, (double min, double max) range)
        {
            return Math.Abs(value) > range.min && Math.Abs(value) < range.max;
        }

        public static Boolean AbsInOR(
            double value1, double value2,
            (double min, double max) range)
        {
            return AbsIn(value1, range) || AbsIn(value2, range);
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

        public static Boolean InOR(
            double value1, double value2,
            (double min, double max) range)
        {
            return In(value1, range) || In(value2, range);
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

        public static string Str<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
            {
                return "null";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("{ ");
            bool first = true;
            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            {
                if (!first)
                {
                    sb.Append(", ");
                }
                sb.Append($"{pair.Key}: {pair.Value}");
                first = false;
            }
            sb.Append(" }");
            return sb.ToString();
        }

        public static string Str<T>(this List<T> list)
        {
            return "{" + string.Join(", ", list) + "}";
        }

        public static string Str(this Span2D<Byte> span)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("---------------------------------");
            for (int i = 0; i < span.Height; i++)
            {
                for (int j = 0; j < span.Width; j++)
                {
                    sb.Append(span[i, j]).Append("\t");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public static string MStoSecStr(int ms)
        {
            // Convert milliseconds to seconds with 2 decimal places
            return (ms / 1000.0).ToString("F2");
        }
    }
}
