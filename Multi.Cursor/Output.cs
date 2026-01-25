using Serilog;
using Serilog.Enrichers.WithCaller;
using System;
using System.Runtime.CompilerServices;
using ILogger = Serilog.ILogger;

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

            //FILOG = new LoggerConfiguration()
            //    .WriteTo.Async(a => a.File(LOG_PATH,
            //    outputTemplate: "[{Level:u3}] [{TrialEvent:HH:mm:ss.fff}] {Message:lj}{NewLine}"))
            //    .MinimumLevel.Information()
            //    .CreateLogger();
        }

        public static void Conlog<T>(string mssg, [CallerMemberName] string memberName = "")
        {
            var className = typeof(T).Name;
            CONSOUT_NOTIME.ForContext("ClassName", className)
                .ForContext("MethodName", memberName)
                .Information(mssg);

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

        public static void TimeInfo(this object source, string mssg, [CallerMemberName] string memberName = "")
        {
            var className = source.GetType().Name;
            //CONSOUT_NOTIME.ForContext("ClassName", className).ForContext("MethodName", memberName).Information(mssg);
        }
    }
}
