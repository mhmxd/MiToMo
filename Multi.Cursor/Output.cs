
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using SeriLog = Serilog.Log;
using Serilog;
using System.Windows.Shapes;
using CommunityToolkit.HighPerformance; // Alias Serilog's Log class

namespace Multi.Cursor
{
    internal class Output
    {
        public static ILogger TRACK_LOG;
        public static ILogger TRIAL_LOG;
        public static ILogger GESTURE_LOG;
        static Output()
        {
            // Configure Serilog
            SeriLog.Logger = new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: "[{Level:u3}] {Tag} {Message:lj}{NewLine}")
                .Enrich.WithProperty("Tag", "Default") // Default tag
                .MinimumLevel.Information() // Ignore Debug and Verbose
                .Filter.ByIncludingOnly(logEvent =>
                    logEvent.Properties.ContainsKey("Tag") &&
                    (logEvent.Properties["Tag"].ToString() == "\"{GESTURE}\"" ||
                    logEvent.Properties["Tag"].ToString() == "\"{TRIAL}\"")) // Only include logs with Tag = "App"
                .CreateLogger();

            TRACK_LOG = SeriLog.ForContext("Tag", "{TRACK}");
            TRIAL_LOG = SeriLog.ForContext("Tag", "{TRIAL}");
            GESTURE_LOG = SeriLog.ForContext("Tag", "{GESTURE}");
        }

        public static void Print(string output)
        {
            Console.WriteLine(output);
        }

        public static void Print(string name, Point point)
        {
            Console.WriteLine(name + $": ({point.X}, {point.Y})");
        }

        public static void Print(string timeName, long start, long end)
        {
            Console.WriteLine(timeName + $" = {(end - start)/1000.0}");
        }

        private void PrintSpan(Span2D<Byte> span)
        {
            for (int i = 0; i < span.Height; i++)
            {
                for (int j = 0; j < span.Width; j++)
                {
                    Console.Write(span[i, j] + "\t");
                }
                Console.WriteLine();
            }
            Console.WriteLine("---------------------------------");
        }

        public static string GetString(Dictionary<int, TouchPoint> touchPoints)
        {
            if (touchPoints == null)
            {
                return "null";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            bool firstPair = true;

            foreach (var pair in touchPoints)
            {
                if (!firstPair)
                {
                    sb.Append(",\n");
                }
                sb.Append(pair.Key).Append(": ").Append(pair.Value);
                firstPair = false;
            }

            sb.Append("}");
            return sb.ToString();
        }

        public static string GetString(Point p)
        {
            return $"(X = {p.X:F3}, Y = {p.Y:F3})";
        }

        public static void Info(Line l)
        {
            TRACK_LOG.Information($"Line: ({l.X1:F3}, {l.Y1:F3}) -> ({l.X2:F3}, {l.Y2:F3})");
        }
    }
}
