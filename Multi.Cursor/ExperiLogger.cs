using MathNet.Numerics;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ILogger = Serilog.ILogger;

namespace Multi.Cursor
{
    internal class ExperiLogger
    {
        // Set for each trial (in constructor)
        //private static string gesturesFilePath = System.IO.Path.Combine(
        //    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        //    "Multi.Cursor.Logs", "trace_log.txt"
        //);

        private static Logger _gestureFileLog;
        private static Logger _blockFileLog;

        private static Dictionary<string, int> _trialLogs = new Dictionary<string, int>();

        private static int _ptcId = 0; // Participant ID
        private static Technique _technique = Technique.MOUSE; // Technique

        public static void Init(int participantId, Technique tech)
        {
            _ptcId = participantId;
            _technique = tech;
        }

        public static void StartBlockLog(int blockId, TaskType blockType)
        {
            String blockFileName = $"Block-{blockId}-{blockType}.txt";
            string blockFilePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Multi.Cursor.Logs", $"{_ptcId}-{_technique}", blockFileName
            );

            //_blockFileLog = new LoggerConfiguration()
            //    .WriteTo.Async(a => a.File(blockFilePath, rollingInterval: RollingInterval.Day,
            //    outputTemplate: "{TimeStamp:HH:mm:ss.fff} {Message:lj}{NewLine}"))
            //    .CreateLogger();

            _blockFileLog = new LoggerConfiguration()
                .WriteTo.Async(a => a.File(blockFilePath, rollingInterval: RollingInterval.Day,
                outputTemplate: "{Message:lj}{NewLine}"))
                .CreateLogger();

            _blockFileLog.Information($"--- Technique: {_technique} - Block#{blockId} - Type: {blockType} ---");
        }

        public static void StartTrialLog(Trial trial)
        {
            _blockFileLog.Information($"{trial.ToString()}");
            //_blockFileLog.Information($"----------------------------------------------------------------------------------");
        }

        public static void StartTrialLogs(int trialNum, int trialId, double targetWidthMM, double distanceMM, Point startPos, Point targetPos)
        {
            String gestureFileName = $"trial-{trialNum}-#{trialId}-gestures-.txt";
            

            string gesturesFilePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Multi.Cursor.Logs", $"{_ptcId}-{_technique}", gestureFileName
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

        public static void LogTrialMessage(string message)
        {
            _blockFileLog.Information(message);
        }

        public static void LogGestureDuration(string gesture, int duration)
        {

        }

        public static void LogTrialTimes(TrialRecord trialRecord)
        {
            switch (_technique)
            {
                case Technique.MOUSE:
                    _blockFileLog.Information($"Start Release   -> Obj Enter:   {trialRecord.GetDuration(Str.START_RELEASE, Str.OBJ_ENTER)}");
                    _blockFileLog.Information($"Obj Enter       -> Obj Press:   {trialRecord.GetDuration(Str.OBJ_ENTER, Str.OBJ_PRESS)}");
                    _blockFileLog.Information($"Obj Press       -> Obj Release: {trialRecord.GetDuration(Str.OBJ_PRESS, Str.OBJ_RELEASE)}");
                    _blockFileLog.Information($"Obj Release     -> Func Press:  {trialRecord.GetDuration(Str.OBJ_RELEASE, Str.FUNCTION_PRESS)}");
                    _blockFileLog.Information($"Func Press      -> Func Release:{trialRecord.GetDuration(Str.FUNCTION_PRESS, Str.FUNCTION_RELEASE)}");
                    _blockFileLog.Information($"Func Release    -> Area Press:  {trialRecord.GetDuration(Str.FUNCTION_RELEASE, Str.OBJ_AREA_PRESS)}");
                    break;

                case Technique.TOMO_TAP:
                    _blockFileLog.Information($"Start Release   -> Tap Down:    {trialRecord.GetDuration(Str.START_RELEASE, Str.DOWN)}");
                    _blockFileLog.Information($"Tap Down        -> Tap Up:      {trialRecord.GetDuration(Str.DOWN, Str.UP)}");
                    break;
            }
        }



    }
}
