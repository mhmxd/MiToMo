using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Object.Select
{
    public static class Timer
    {
        // Stopwatch.Frequency is the number of ticks per second from the high-resolution timer.
        // It's constant for the lifetime of the application.
        private static readonly long _frequency = Stopwatch.Frequency;

        /// <summary>
        /// Gets a high-resolution timestamp (raw tick count).
        /// This is the C# equivalent of Java's System.nanoTime().
        /// </summary>
        public static long GetCurrentTimestamp()
        {
            return Stopwatch.GetTimestamp();
        }

        /// <summary>
        /// Converts a duration in Stopwatch ticks to milliseconds.
        /// </summary>
        /// <param name="ticks">The number of ticks.</param>
        /// <returns>The duration in milliseconds.</returns>
        public static double TicksToMilliseconds(long ticks)
        {
            return (double)ticks / _frequency * 1000.0;
        }

        /// <summary>
        /// Gets the current timestamp in milliseconds since an arbitrary start point.
        /// This is functionally similar to Java's System.currentTimeMillis() for measuring durations,
        /// but its epoch is arbitrary (relative to the high-resolution timer's start).
        /// </summary>
        public static long GetCurrentMillis()
        {
            return (long)TicksToMilliseconds(Stopwatch.GetTimestamp());
        }

        /// <summary>
        /// Calculates the elapsed milliseconds between two timestamps.
        /// </summary>
        /// <param name="startTimestamp">The start timestamp (raw ticks).</param>
        /// <param name="endTimestamp">The end timestamp (raw ticks).</param>
        /// <returns>The duration in milliseconds.</returns>
        public static double GetElapsedMilliseconds(long startTimestamp, long endTimestamp)
        {
            return TicksToMilliseconds(endTimestamp - startTimestamp);
        }

        /// <summary>
        /// Calculates the elapsed TimeSpan between two timestamps.
        /// (Requires .NET 7+ for Stopwatch.GetElapsedTime(long startTimestamp, long endTimestamp))
        /// </summary>
        /// <param name="startTimestamp">The start timestamp (raw ticks).</param>
        /// <param name="endTimestamp">The end timestamp (raw ticks).</param>
        /// <returns>The TimeSpan duration.</returns>
        public static TimeSpan GetElapsedTimeSpan(long startTimestamp, long endTimestamp)
        {
            // For .NET 7+
#if NET7_0_OR_GREATER
            return Stopwatch.GetElapsedTime(startTimestamp, endTimestamp);
#else
            // Fallback for older .NET versions if Stopwatch.GetElapsedTime(long, long) is not available
            // You'd need a private Stopwatch instance or manual calculation.
            // A simple manual calculation:
            long elapsedTicks = endTimestamp - startTimestamp;
            return TimeSpan.FromSeconds((double)elapsedTicks / _frequency);
#endif
        }
    }
}
