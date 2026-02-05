using CommunityToolkit.HighPerformance;
using Serilog;
using Serilog.Enrichers.WithCaller;
using System.Runtime.CompilerServices;
using System.Text;
using ILogger = Serilog.ILogger;

namespace Common.Helpers
{
    public static class MOuter
    {
        //public static ILogger FILOG;

        public static ILogger CONSOUT_WITHTIME = new LoggerConfiguration()
                .Enrich.WithCaller()
                .WriteTo.Console(outputTemplate: "[{Level:u3}] {MethodName} - " +
                "{Timestamp:HH:mm:ss.fff} " +
                "{Message:lj}{NewLine}")
                .MinimumLevel.Information() // Ignore Debug and Verbose
                .CreateLogger();

        public static ILogger CONSOUT_NOTIME = new LoggerConfiguration()
                .Enrich.WithCaller()
                .WriteTo.Console(outputTemplate: "[{Level:u3}] {ClassName}.{MethodName} - " +
                "{Message:lj}{NewLine}")
                .MinimumLevel.Information() // Ignore Debug and Verbose
                .CreateLogger();


        public static void Conlog<T>(string mssg, [CallerMemberName] string memberName = "")
        {
            var className = typeof(T).Name;
            CONSOUT_NOTIME?.ForContext("ClassName", className)
                .ForContext("MethodName", memberName)
                .Information(mssg);

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

        public static void PositionInfo(this object source, string mssg, [CallerMemberName] string memberName = "")
        {
            var className = source.GetType().Name;
            //NOTIME.ForContext("ClassName", className).ForContext("MethodName", memberName).Information(mssg);
        }

        public static void TrialInfo(this object source, string mssg, [CallerMemberName] string memberName = "")
        {
            // GetType() is called on the 'source' object at RUNTIME.
            var className = source.GetType().Name;
            CONSOUT_NOTIME.ForContext("ClassName", className).ForContext("MethodName", memberName).Information(mssg);
        }

        public static void LogsInfo(this object source, string mssg, [CallerMemberName] string memberName = "")
        {
            // GetType() is called on the 'source' object at RUNTIME.
            var className = source.GetType().Name;
            //CONSOUT_NOTIME.ForContext("ClassName", className).ForContext("MethodName", memberName).Information(mssg);
        }

        public static void LogsInfo<T>(string mssg, [CallerMemberName] string memberName = "")
        {
            //var className = typeof(T).Name;
            //CONSOUT_NOTIME.ForContext("ClassName", className).ForContext("MethodName", memberName).Information(mssg);
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
    }
}
