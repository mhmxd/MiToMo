using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using ILogger = Serilog.ILogger;
using Serilog.Core;
using System.Windows;

namespace Multi.Cursor
{
    internal class ToMoLogger
    {
        // Set for each trial (in constructor)
        //private static string gesturesFilePath = System.IO.Path.Combine(
        //    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        //    "Multi.Cursor.Logs", "trace_log.txt"
        //);

        private static Logger _gestureFileLog;
        private static Logger _blockFileLog;


        private static int ptcId = 0; // Participant ID
        private static Experiment.Technique technique = Experiment.Technique.Mouse; // Technique

        public static void Init(int participantId, Experiment.Technique tech)
        {
            ptcId = participantId;
            technique = tech;
        }

        public static void StartBlockLog(int blockId)
        {
            String blockFileName = $"block-{blockId}.txt";
            string blockFilePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Multi.Cursor.Logs", $"{ptcId}-{technique}", blockFileName
            );
            _blockFileLog = new LoggerConfiguration()
                .WriteTo.Async(a => a.File(blockFilePath, rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:HH:mm:ss.fff} {Message:lj}{NewLine}"))
                .CreateLogger();
        }

        public static void StartTrialLogs(int trialNum, int trialId, double targetWidthMM, double distanceMM, Point startPos, Point targetPos)
        {
            String gestureFileName = $"trial-{trialNum}-#{trialId}-gestures-.txt";
            

            string gesturesFilePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Multi.Cursor.Logs", $"{ptcId}-{technique}", gestureFileName
            );

            

            _gestureFileLog = new LoggerConfiguration()
                    .WriteTo.Async(a => a.File(gesturesFilePath, rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:HH:mm:ss.fff} {Message:lj}{NewLine}"))
                    .CreateLogger();

            

            // Enter trial info
            _gestureFileLog.Information($"TgtW: {targetWidthMM}, Dist: {distanceMM}, StPos: {startPos.ToStr()}, TgPos: {targetPos.ToStr()}");
        }

        public static void LogGestureEvent(string message)
        {
            //_gestureFileLog.Information(message);
        }

        public static void LogTrialEvent(string message)
        {
            _blockFileLog.Information(message);
        }

    }
}
