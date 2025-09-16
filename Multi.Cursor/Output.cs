﻿
using CommunityToolkit.HighPerformance;
using Serilog;
using Serilog.Enrichers.CallerInfo; // Alias Serilog's Log class
using Serilog.Enrichers.WithCaller;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using System.Xml.Linq;
using static Multi.Cursor.MainWindow;
using ILogger = Serilog.ILogger;
using Seril = Serilog.Log;

namespace Multi.Cursor
{
    internal static class Output
    {
        private static readonly string LOG_PATH = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Multi.Cursor.Logs", "trace_log.txt"
        );

        public static ILogger FILOG;
        public static ILogger CONSOUT_WITHTIME;
        public static ILogger CONSOUT_NOTIME;

        public static void Init()
        {
            CONSOUT_WITHTIME = new LoggerConfiguration()
                .Enrich.WithCaller()
                .WriteTo.Console(outputTemplate: "[{Level:u3}] {MethodName} - " +
                "{Timestamp:HH:mm:ss.fff} " +
                "{Message:lj}{NewLine}")
                .MinimumLevel.Information() // Ignore Debug and Verbose
                .CreateLogger();

            CONSOUT_NOTIME = new LoggerConfiguration()
                .Enrich.WithCaller()
                .WriteTo.Console(outputTemplate: "[{Level:u3}] {ClassName}.{MethodName} - " +
                "{Message:lj}{NewLine}")
                .MinimumLevel.Information() // Ignore Debug and Verbose
                .CreateLogger();

            FILOG = new LoggerConfiguration()
                .WriteTo.Async(a => a.File(LOG_PATH,
                outputTemplate: "[{Level:u3}] [{Timestamp:HH:mm:ss.fff}] {Message:lj}{NewLine}"))
                .MinimumLevel.Information()
                .CreateLogger();
        }

        public static void Conlog<T>(string mssg, [CallerMemberName] string memberName = "")
        {
            var className = typeof(T).Name;
            //CONSOUT_NOTIME.ForContext("ClassName", className)
            //    .ForContext("MethodName", memberName)
            //    .Information(mssg);

        }

        public static ILogger Outlog<T>([CallerMemberName] string memberName = "")
        {
            var className = typeof(T).Name;
            return CONSOUT_NOTIME.ForContext("ClassName", className).ForContext("MethodName", memberName);
        }

        public static void GestInfo<T>(string mssg, [CallerMemberName] string memberName = "")
        {
            var className = typeof(T).Name;
            CONSOUT_WITHTIME.ForContext("ClassName", className).ForContext("MethodName", memberName).Information(mssg);
            //FILOG.Information(mssg);
        }

        public static void PositionInfo<T>(string mssg, [CallerMemberName] string memberName = "")
        {
            var className = typeof(T).Name;
            //NOTIME.ForContext("ClassName", className).ForContext("MethodName", memberName).Information(mssg);
        }

        public static void TrialInfo(this object source, string mssg, [CallerMemberName] string memberName = "")
        {
            // GetType() is called on the 'source' object at RUNTIME.
            var className = source.GetType().Name;
            CONSOUT_NOTIME.ForContext("ClassName", className).ForContext("MethodName", memberName).Warning(mssg);
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

        public static string ToStr(this Span2D<Byte> span)
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

        public static string ToStr(this Dictionary<int, TouchPoint> touchPoints)
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

        public static string ToStr(this List<double> numbers)
        {
            if (numbers == null || !numbers.Any())
            {
                return string.Empty;
            }

            return string.Join(", ", numbers.Select(n => n.ToString("F2")));
        }

        public static string ToStr(this List<int> list)
        {
            if (list == null || !list.Any())
            {
                return string.Empty;
            }

            return string.Join(", ", list.Select(n => n.ToString()));
        }

        public static string ToStr(Rect rect)
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

        public static string ToStr(this Point point)
        {
            return $"({point.X:F2}, {point.Y:F2})";
        }
    }
}
