﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Seril = Serilog.Log;
using Serilog;
using CommunityToolkit.HighPerformance;
using System.IO;
using Serilog.Enrichers.WithCaller;
using Serilog.Enrichers.CallerInfo; // Alias Serilog's Log class
using ILogger = Serilog.ILogger;
using System.Runtime.CompilerServices;
using static Multi.Cursor.MainWindow;
using System.Windows;
using System.Windows.Shapes;

namespace Multi.Cursor
{
    internal static class Output
    {
        private static readonly string LOG_PATH = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Multi.Cursor.Logs", "trace_log.txt"
        );

        //public static ILogger TRACK_LOG;
        //public static ILogger TRIAL_LOG;
        //public static ILogger GESTURE_LOG;

        //public static ILogger Seril;

        public static ILogger FILOG;
        public static ILogger WITHTIME;
        public static ILogger NOTIME;

        static Output()
        {
            // Configure Serilog
            //SeriLog.Logger = new LoggerConfiguration()
            //    .Enrich.WithCaller()
            //    .WriteTo.Console(outputTemplate: "[{Level:u3}] {Timestamp:HH:mm:ss.fff} " +
            //    "{Tag} {Message:lj}{NewLine}")
            //    .Enrich.WithProperty("Tag", "Default") // Default tag
            //    .MinimumLevel.Information() // Ignore Debug and Verbose
            //    .Filter.ByIncludingOnly(logEvent =>
            //        logEvent.Properties.ContainsKey("Tag") &&
            //        (logEvent.Properties["Tag"].ToString() == "\"{GESTURE}\"" ||
            //        logEvent.Properties["Tag"].ToString() == "\"{TRIAL}\"")) // Only include logs with Tag = "App"
            //    .CreateLogger();

            

            //Seril = SeriLog.ForContext("Tag", "{DEF}");

            //TRACK_LOG = SeriLog.ForContext("Tag", "{TRACK}");
            //TRIAL_LOG = SeriLog.ForContext("Tag", "{TRIAL}");
            //GESTURE_LOG = SeriLog.ForContext("Tag", "{GESTURE}");

            
        }

        public static void Init()
        {
            WITHTIME = new LoggerConfiguration()
                .Enrich.WithCaller()
                .WriteTo.Console(outputTemplate: "[{Level:u3}] {MethodName} - " +
                "{Timestamp:HH:mm:ss.fff} " +
                "{Message:lj}{NewLine}")
                .MinimumLevel.Information() // Ignore Debug and Verbose
                .CreateLogger();

            NOTIME = new LoggerConfiguration()
                .Enrich.WithCaller()
                .WriteTo.Console(outputTemplate: "[{Level:u3}] {ClassName}.{MethodName} - " +
                "{Message:lj}{NewLine}")
                .MinimumLevel.Information() // Ignore Debug and Verbose
                .CreateLogger();

            FILOG = new LoggerConfiguration()
                .WriteTo.File(LOG_PATH, outputTemplate: "[{Level:u3}] [{Timestamp:HH:mm:ss.fff}]" +
                " {Message:lj}{NewLine}")
                .MinimumLevel.Information() // Ignore Debug and Verbose
                .CreateLogger();


        }

        public static ILogger Outlog<T>([CallerMemberName] string memberName = "")
        {
            var className = typeof(T).Name;
            return NOTIME.ForContext("ClassName", className).ForContext("MethodName", memberName);
        }

        public static void GestInfo<T>(string mssg, [CallerMemberName] string memberName = "")
        {
            var className = typeof(T).Name;
            //WITHTIME.ForContext("ClassName", className).ForContext("MethodName", memberName).Information(mssg);
        }

        public static void PositionInfo<T>(string mssg, [CallerMemberName] string memberName = "")
        {
            var className = typeof(T).Name;
            NOTIME.ForContext("ClassName", className).ForContext("MethodName", memberName).Information(mssg);
        }

        public static void TrialInfo<T>(string mssg, [CallerMemberName] string memberName = "")
        {
            var className = typeof(T).Name;
            NOTIME.ForContext("ClassName", className).ForContext("MethodName", memberName).Information(mssg);
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

        public static void PrintSpan(Span2D<Byte> span)
        {
            Console.WriteLine("---------------------------------");
            for (int i = 0; i < span.Height; i++)
            {
                for (int j = 0; j < span.Width; j++)
                {
                    Console.Write(span[i, j] + "\t");
                }
                Console.WriteLine();
            }
            
        }

        public static string GetString(Span2D<Byte> span)
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

        public static string GetKeys(Dictionary<int, TouchPoint> touchPoints)
        {
            if (touchPoints == null)
            {
                return "null";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("{");

            foreach (var pair in touchPoints)
            {
                sb.Append(pair.Key).Append(", ");
            }
            if (sb.Length >= 2) sb.Remove(sb.Length - 2, 2);
            sb.Append("}");
            return sb.ToString();
        }

        public static string GetString(Point p)
        {
            return $"(X = {p.X:F2}, Y = {p.Y:F2})";
        }

        public static string GetString(List<double> numbers)
        {
            if (numbers == null || !numbers.Any())
            {
                return string.Empty;
            }

            return string.Join(", ", numbers.Select(n => n.ToString("F3")));
        }

        public static string GetString(Rect rect)
        {
            return $"{{X={rect.X},Y={rect.Y},Width={rect.Width},Height={rect.Height}}}";
        }

        public static string Stringify<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
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

        public static string GetCorners(this Rect rect)
        {
            return $"TL: ({rect.TopLeft.X:F0} | {rect.TopLeft.Y:F0}) | " +
                   $"TR: ({rect.TopRight.X:F0} | {rect.TopRight.Y:F0}) | " +
                   $"BR: ({rect.BottomRight.X:F0} | {rect.BottomRight.Y:F0}) | " +
                   $"BL: ({rect.BottomLeft.X:F0} | {rect.BottomLeft.Y:F0})";
        }

        public static string GetCorners(this Window window)
        {
            var windowRect = window.GetRect();
            return $"TL: ({windowRect.TopLeft.X:F0} | {windowRect.TopLeft.Y:F0}) | " +
                    $"TR: ({windowRect.TopRight.X:F0} | {windowRect.TopRight.Y:F0}) | " +
                    $"BR: ({windowRect.BottomRight.X:F0} | {windowRect.BottomRight.Y:F0}) | " +
                    $"BL: ({windowRect.BottomLeft.X:F0} | {windowRect.BottomLeft.Y:F0})";
        }

        public static string GetCorners(this Window window, int padding)
        {
            var windowRect = window.GetRect();
            return $"TL: ({windowRect.TopLeft.X:F0} | {windowRect.TopLeft.Y:F0}) | " +
                    $"TR: ({windowRect.TopRight.X:F0} | {windowRect.TopRight.Y:F0}) | " +
                    $"BR: ({windowRect.BottomRight.X:F0} | {windowRect.BottomRight.Y:F0}) | " +
                    $"BL: ({windowRect.BottomLeft.X:F0} | {windowRect.BottomLeft.Y:F0})";
        }


        public static void Info(Line l)
        {
            Seril.Information($"Line: ({l.X1:F3}, {l.Y1:F3}) -> ({l.X2:F3}, {l.Y2:F3})");
        }
    }
}
