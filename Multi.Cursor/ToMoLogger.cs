using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using ILogger = Serilog.ILogger;
using Serilog.Core;

namespace Multi.Cursor
{
    internal class ToMoLogger
    {
        // Set for each trial (in constructor)
        private static string _trialGesturesLogPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Multi.Cursor.Logs", "trace_log.txt"
        );

        private static Logger _fileLog;


        public static void StartTrialGesturesLog(int trialId, double targetWidthMM, double distanceMM)
        {
            String fileName = $"trial-{trialId}-gestures-.txt";
            _trialGesturesLogPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Multi.Cursor.Logs", fileName
            );

            _fileLog = new LoggerConfiguration()
                    .WriteTo.Async(a => a.File(_trialGesturesLogPath, rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:HH:mm:ss.fff} {Message:lj}{NewLine}"))
                    .CreateLogger();
        }

        public static void LogGestureEvent(string message)
        {
            _fileLog.Information(message);
        }

    }
}
