using Common.Constants;
using Common.Logs;
using Common.Settings;
using MathNet.Numerics;
using Multi.Cursor.Logging;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.Internal;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static Common.Constants.ExpEnums;
using ILogger = Serilog.ILogger;

namespace Multi.Cursor
{
    internal class ExperiLogger
    {
        // Set for each log (in constructor)
        private static string _sosfTrialLogFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Multi.Cursor.Logs", "sosf_trial_log"
        );
        private static string _somfTrialLogFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Multi.Cursor.Logs", "somf_trial_log"
        );
        private static string _mosfTrialLogFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Multi.Cursor.Logs", "mosf_trial_log"
        );
        private static string _momfTrialLogFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Multi.Cursor.Logs", "momf_trial_log"
        );
        private static string _totalLogFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Multi.Cursor.Logs", "total_trial_log"
        );
        private static string _blockLogFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Multi.Cursor.Logs", "blocks_log"
        );

        private static string _cursorLogFilePath = ""; // Will be set when starting trial cursor log

        private static Logger _gestureFileLog;
        private static Logger _blockFileLog;

        private static StreamWriter _trialLogWriter;
        private static StreamWriter _totalTrialLogWriter;
        private static StreamWriter _cursorLogWriter;
        private static StreamWriter _blockLogWriter;

        private static Dictionary<string, int> _trialLogs = new Dictionary<string, int>();

        private static int _ptcId = 0; // Participant ID
        private static Technique _technique = Technique.MOUSE; // Technique

        private static bool _headerWritten = false;

        private static Dictionary<int, int> _trialTimes = new Dictionary<int, int>();

        private static Dictionary<int, List<PositionRecord>> _trialCursorRecords = new Dictionary<int, List<PositionRecord>>();
        private static int _activeTrialId = -1;

        //public static void Init(int participantId, Technique tech)
        //{
        //    _ptcId = participantId;
        //    _technique = tech;

        //    bool fileExists = File.Exists(_sosfTrialLogFilePath);
        //    bool fileIsEmpty = !fileExists || new FileInfo(_sosfTrialLogFilePath).Length == 0;

        //    _trialLogWriter = new StreamWriter(_sosfTrialLogFilePath, append: true, Encoding.UTF8);

        //    if (fileIsEmpty)
        //    {
        //        WriteHeader<TrialLog>();
        //    }
        //}

        public static void Init(Technique tech, TaskType taskType)
        {
            _ptcId = ExpPtc.PTC_NUM;
            _technique = tech;

            //string timestamp = DateTime.Now.ToString("dd-MM-yyyy_HH-mm");

            // Default (will set based on the task type)
            switch (taskType)
            {
                case TaskType.ONE_OBJ_ONE_FUNC:
                    {
                        _trialLogWriter = PrepareFile<SOSFTrialLog>(_sosfTrialLogFilePath);
                        //string logFilePath = $"{_sosfTrialLogFilePath}_{timestamp}.csv";
                        //bool fileExists = File.Exists(logFilePath);
                        //bool fileIsEmpty = !fileExists || new FileInfo(logFilePath).Length == 0;

                        //_trialLogWriter = new StreamWriter(logFilePath, append: true, Encoding.UTF8);

                        //if (fileIsEmpty)
                        //{
                        //    WriteHeader<SOSFTrialLog>(_trialLogWriter);
                        //}
                    }
                    break;
                case TaskType.ONE_OBJ_MULTI_FUNC:
                    {
                        _trialLogWriter = PrepareFile<SOMFTrialLog>(_somfTrialLogFilePath);
                        //string logFilePath = $"{_somfTrialLogFilePath}_{timestamp}.csv";
                        //bool fileExists = File.Exists(logFilePath);
                        //bool fileIsEmpty = !fileExists || new FileInfo(logFilePath).Length == 0;

                        //_trialLogWriter = new StreamWriter(logFilePath, append: true, Encoding.UTF8);

                        //if (fileIsEmpty)
                        //{
                        //    WriteHeader<SOMFTrialLog>(_trialLogWriter);
                        //}
                    }
                    break;
                case TaskType.MULTI_OBJ_ONE_FUNC:
                    {
                        _trialLogWriter = PrepareFile<MOSFTrialLog>(_mosfTrialLogFilePath);

                        //string logFilePath = $"{_mosfTrialLogFilePath}_{timestamp}.csv";
                        //bool fileExists = File.Exists(logFilePath);
                        //bool fileIsEmpty = !fileExists || new FileInfo(logFilePath).Length == 0;

                        //_trialLogWriter = new StreamWriter(logFilePath, append: true, Encoding.UTF8);

                        //if (fileIsEmpty)
                        //{
                        //    WriteHeader<MOSFTrialLog>(_trialLogWriter);
                        //}
                    }
                    break;
                case TaskType.MULTI_OBJ_MULTI_FUNC:
                    {
                        _trialLogWriter = PrepareFile<MOMFTrialLong>(_momfTrialLogFilePath);

                        //string logFilePath = $"{_momfTrialLogFilePath}_{timestamp}.csv";
                        //bool fileExists = File.Exists(logFilePath);
                        //bool fileIsEmpty = !fileExists || new FileInfo(logFilePath).Length == 0;

                        //_trialLogWriter = new StreamWriter(logFilePath, append: true, Encoding.UTF8);

                        //if (fileIsEmpty)
                        //{
                        //    WriteHeader<MOMFTrialLong>(_trialLogWriter);
                        //}
                    }
                    break;
            }


            //_trialLogWriter.AutoFlush = true;

            // Create total log if not exists
            _totalTrialLogWriter = PrepareFile<TotalTrialLog>(_totalLogFilePath);
            //string totalLogFilePath = $"{_totalLogFilePath}_{timestamp}.csv";
            //bool totalFileExists = File.Exists(totalLogFilePath);
            //bool totalFileIsEmpty = !totalFileExists || new FileInfo(totalLogFilePath).Length == 0;
            //_totalTrialLogWriter = new StreamWriter(totalLogFilePath, append: true, Encoding.UTF8);
            //_totalTrialLogWriter.AutoFlush = true;
            //if (totalFileIsEmpty)
            //{
            //    WriteHeader<TotalTrialLog>(_totalTrialLogWriter);
            //}

            // Create block log if not exists
            _blockLogWriter = PrepareFile<BlockLog>(_blockLogFilePath);
            //string blockLogFilePath = $"{_blockLogFilePath}_{timestamp}.csv";
            //bool blockFileExists = File.Exists(blockLogFilePath);
            //bool timedFileIsEmpty = !blockFileExists || new FileInfo(blockLogFilePath).Length == 0;
            //_blockLogWriter = new StreamWriter(blockLogFilePath, append: true, Encoding.UTF8);
            //_blockLogWriter.AutoFlush = true;
            //if (timedFileIsEmpty)
            //{
            //    WriteHeader<BlockLog>(_blockLogWriter);
            //}
        }

        //public static void StartBlockLog(int blockId, TaskType blockType, Complexity blockComplexity)
        //{
        //    String blockFileName = $"Block-{blockId}-{blockType}.txt";
        //    string blockFilePath = System.IO.Path.Combine(
        //        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        //        "Multi.Cursor.Logs", $"{_ptcId}-{_technique}", blockFileName
        //    );

        //    //_blockFileLog = new LoggerConfiguration()
        //    //    .WriteTo.Async(a => a.File(blockFilePath, rollingInterval: RollingInterval.Day,
        //    //    outputTemplate: "{TrialEvent:HH:mm:ss.fff} {Message:lj}{NewLine}"))
        //    //    .CreateLogger();

        //    _blockFileLog = new LoggerConfiguration()
        //        .WriteTo.Async(a => a.File(blockFilePath, rollingInterval: RollingInterval.Day,
        //        outputTemplate: "{Message:lj}{NewLine}"))
        //        .CreateLogger();

        //    _blockFileLog.Information($"--- Technique: {_technique} | Block#: {blockId} | Type: {blockType} | Complexity: {blockComplexity} ---");
        //}

        //private static void PrepareFile<T>(ref string filePath, StreamWriter writer)
        //{
        //    string timestamp = DateTime.Now.ToString("dd-MM-yyyy_HH-mm");
        //    filePath = $"{filePath}_{timestamp}.csv";
            
        //    string directoryPath = Path.GetDirectoryName(filePath);
        //    if (!Directory.Exists(directoryPath))
        //    {
        //        Directory.CreateDirectory(directoryPath);
        //    }
            
        //    bool timedFileExists = File.Exists(filePath);
        //    bool timedFileIsEmpty = !timedFileExists || new FileInfo(filePath).Length == 0;
        //    writer = new StreamWriter(filePath, append: true, Encoding.UTF8);
        //    writer.AutoFlush = true;
        //    if (timedFileIsEmpty)
        //    {
        //        WriteHeader<T>(writer);
        //    }
        //}

        private static void PrepareFileWithHeader<T>(ref string filePath, StreamWriter writer, string header)
        {
            string timestamp = DateTime.Now.ToString("dd-MM-yyyy_HH-mm");
            filePath = $"{filePath}_{timestamp}.csv";

            string directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            bool timedFileExists = File.Exists(filePath);
            bool timedFileIsEmpty = !timedFileExists || new FileInfo(filePath).Length == 0;
            writer = new StreamWriter(filePath, append: true, Encoding.UTF8);
            writer.AutoFlush = true;
            if (timedFileIsEmpty)
            {
                writer.WriteLine(header);
            }
        }

        private static StreamWriter PrepareFile<T>(string filePath)
        {
            string timestamp = DateTime.Now.ToString("dd-MM-yyyy_HH-mm");
            string timedFilePath = $"{filePath}_{timestamp}.csv";

            string directoryPath = Path.GetDirectoryName(timedFilePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            bool timedFileExists = File.Exists(timedFilePath);
            bool timedFileIsEmpty = !timedFileExists || new FileInfo(timedFilePath).Length == 0;
            StreamWriter writer = new StreamWriter(timedFilePath, append: true, Encoding.UTF8);
            writer.AutoFlush = true;
            if (timedFileIsEmpty)
            {
                WriteHeader<T>(writer);
            }

            return writer;
        }

        public static void StartTrialCursorLog(int trialId)
        {
            //string timestamp = DateTime.Now.ToString("dd-MM-yyyy_HH-mm");
            //String cursorFileName = $"trial-#{trialId}-cursor-{timestamp}.txt";
            //string cursorFilePath = System.IO.Path.Combine(
            //    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            //    "Multi.Cursor.Logs", $"{_ptcId}-{_technique}", "Cursor", cursorFileName
            //);

            _activeTrialId = trialId;
            _trialCursorRecords[_activeTrialId] = new List<PositionRecord>();

            _cursorLogFilePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Multi.Cursor.Logs", $"{_ptcId}-{_technique}", "Cursor", $"trial{trialId}-cursor-log"
            );
            PrepareFileWithHeader<PositionRecord>(ref _cursorLogFilePath, _cursorLogWriter, PositionRecord.GetHeader());
        }

        public static void StartTrialLog(Trial trial)
        {
            //_blockFileLog.Information($"{trial.ToString()}");
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

            

            // Enter log info
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

        public static void LogSingleObjTrialTimes(TrialRecord trialRecord)
        {
            int nFunctions = trialRecord.Functions.Count;

            if (nFunctions == 1)
            {
                switch (_technique)
                {
                    case Technique.MOUSE:
                        _blockFileLog.Information($"Start Release   -> Obj Enter:   {trialRecord.GetDuration(ExpStrs.STR_RELEASE, ExpStrs.OBJ_ENTER)}");
                        _blockFileLog.Information($"Obj Enter       -> Obj Press:   {trialRecord.GetDuration(ExpStrs.OBJ_ENTER, ExpStrs.OBJ_PRESS)}");
                        _blockFileLog.Information($"Obj Press       -> Obj Release: {trialRecord.GetDuration(ExpStrs.OBJ_PRESS, ExpStrs.OBJ_RELEASE)}");
                        _blockFileLog.Information($"Obj Release     -> Func Press:  {trialRecord.GetDuration(ExpStrs.OBJ_RELEASE, ExpStrs.FUN_PRESS)}");
                        _blockFileLog.Information($"Func Press      -> Func Release:{trialRecord.GetDuration(ExpStrs.FUN_PRESS, ExpStrs.FUN_RELEASE)}");
                        _blockFileLog.Information($"Func Release    -> Area Press:  {trialRecord.GetDuration(ExpStrs.FUN_RELEASE, ExpStrs.ARA_PRESS)}");
                        _blockFileLog.Information($"--------------------------------");
                        _blockFileLog.Information($"Total Time (Start Release -> Area Press) = {Utils.MStoSec(trialRecord.GetDuration(ExpStrs.STR_RELEASE, ExpStrs.ARA_PRESS))}");
                        _blockFileLog.Information($"==============================================================================================================");
                        break;

                    case Technique.TOMO_TAP:
                        _blockFileLog.Information($"Start Release   -> Tap Down:    {trialRecord.GetDurationToFingerAction(ExpStrs.STR_RELEASE, ExpStrs.TAP_DOWN)}");
                        _blockFileLog.Information($"Tap Down        -> Tap Up:      {trialRecord.GetGestureDuration(Technique.TOMO_TAP)}");
                        _blockFileLog.Information($"Tap Up          -> Obj Press:   {trialRecord.GetDurationFromFingerAction(ExpStrs.TAP_UP, ExpStrs.OBJ_PRESS)}");
                        _blockFileLog.Information($"Obj Press       -> Obj Release: {trialRecord.GetDuration(ExpStrs.OBJ_PRESS, ExpStrs.OBJ_RELEASE)}");
                        _blockFileLog.Information($"Obj Release     -> Obj Exit:    {trialRecord.GetDuration(ExpStrs.OBJ_RELEASE, ExpStrs.OBJ_EXIT)}");
                        _blockFileLog.Information($"Obj Exit        -> Area Press:  {trialRecord.GetDuration(ExpStrs.OBJ_EXIT, ExpStrs.ARA_PRESS)}");
                        _blockFileLog.Information($"--------------------------------");
                        _blockFileLog.Information($"Total Time (Start Release -> Area Press) = {Utils.MStoSec(trialRecord.GetDuration(ExpStrs.STR_RELEASE, ExpStrs.ARA_PRESS))}");
                        _blockFileLog.Information($"==============================================================================================================");
                        break;

                    case Technique.TOMO_SWIPE:
                        _blockFileLog.Information($"Start Release   -> Swipe Start: {trialRecord.GetDurationToFingerAction(ExpStrs.STR_RELEASE, ExpStrs.SWIPE_START)}");
                        _blockFileLog.Information($"Swipe Start     -> Swipe End:   {trialRecord.GetGestureDuration(Technique.TOMO_SWIPE)}");
                        _blockFileLog.Information($"Swipe End       -> Obj Press:   {trialRecord.GetDurationFromFingerAction(ExpStrs.SWIPE_END, ExpStrs.OBJ_PRESS)}");
                        _blockFileLog.Information($"Obj Press       -> Obj Release: {trialRecord.GetDuration(ExpStrs.OBJ_PRESS, ExpStrs.OBJ_RELEASE)}");
                        _blockFileLog.Information($"Obj Release     -> Obj Exit:    {trialRecord.GetDuration(ExpStrs.OBJ_RELEASE, ExpStrs.OBJ_EXIT)}");
                        _blockFileLog.Information($"Obj Exit        -> Area Press:  {trialRecord.GetDuration(ExpStrs.OBJ_EXIT, ExpStrs.ARA_PRESS)}");
                        _blockFileLog.Information($"--------------------------------");
                        _blockFileLog.Information($"Total Time (Start Release -> Area Press) = {Utils.MStoSec(trialRecord.GetDuration(ExpStrs.STR_RELEASE, ExpStrs.ARA_PRESS))}");
                        _blockFileLog.Information($"==============================================================================================================");
                        break;
                }
            }
            else
            {
                // For now. Later we put detailed log
                _blockFileLog.Information($"Start Release   -> Area Press:   {trialRecord.GetDuration(ExpStrs.STR_RELEASE, ExpStrs.ARA_PRESS)}");
            }
            
        }

        public static void LogMultipleObjTrialTimes(TrialRecord trialRecord)
        {
            // For now. Later we put detailed log
            _blockFileLog.Information($"Start Release   -> Area Press:   {trialRecord.GetDuration(ExpStrs.STR_RELEASE, ExpStrs.ARA_PRESS)}");
        }

        public static void LogSOSFTrial(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            string logFilePath = _sosfTrialLogFilePath; // Passed to the writer

            Output.Conlog<ExperiLogger>("Logging Trial");
            SOSFTrialLog log = new SOSFTrialLog();

            // Information
            LogTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Log start events
            log.trlsh_curmv = trialRecord.GetDuration(ExpStrs.TRIAL_SHOW, ExpStrs.FIRST_MOVE);
            log.curmv_strnt = trialRecord.GetLastSeqDuration(ExpStrs.FIRST_MOVE, ExpStrs.STR_ENTER);
            log.strnt_strpr = trialRecord.GetLastSeqDuration(ExpStrs.STR_ENTER, ExpStrs.STR_PRESS);
            log.strpr_strrl = trialRecord.GetLastSeqDuration(ExpStrs.STR_PRESS, ExpStrs.STR_RELEASE);

            // Log the rest of the times
            switch (trial.Technique.GetDevice())
            {
                case Technique.MOUSE:
                    log.strrl_objnt = trialRecord.GetLastSeqDuration(ExpStrs.STR_RELEASE, ExpStrs.OBJ_ENTER);

                    log.objrl_pnlnt = trialRecord.GetLastSeqDuration(ExpStrs.OBJ_RELEASE, ExpStrs.PNL_ENTER);
                    log.pnlnt_funnt = trialRecord.GetLastSeqDuration(ExpStrs.PNL_ENTER, ExpStrs.FUN_ENTER);
                    log.funnt_funpr = trialRecord.GetLastSeqDuration(ExpStrs.FUN_ENTER, ExpStrs.FUN_PRESS);
                    log.funpr_funrl = trialRecord.GetLastSeqDuration(ExpStrs.FUN_PRESS, ExpStrs.FUN_RELEASE);
                    log.funrl_arant = trialRecord.GetLastSeqDuration(ExpStrs.FUN_RELEASE, ExpStrs.ARA_ENTER);
                    
                break;

                case Technique.TOMO:
                    log.strrl_gstst = trialRecord.GetDurationToGestureStart(ExpStrs.STR_RELEASE, trial.Technique);
                    log.gstst_gstnd = trialRecord.GetGestureDuration(trial.Technique);
                    log.gstnd_fstfl = trialRecord.GetDurationFromGestureEnd(trial.Technique, ExpStrs.FLICK);
                    log.fstfl_funmk = trialRecord.GetDuration(ExpStrs.FLICK, ExpStrs.FUN_MARKED);
                    log.funmk_objnt = trialRecord.GetDuration(ExpStrs.ARA_ENTER, ExpStrs.OBJ_ENTER);
                break;
            }

            log.objnt_objpr = trialRecord.GetLastSeqDuration(ExpStrs.OBJ_ENTER, ExpStrs.OBJ_PRESS);
            log.objpr_objrl = trialRecord.GetLastSeqDuration(ExpStrs.OBJ_PRESS, ExpStrs.OBJ_RELEASE);

            log.arant_arapr = trialRecord.GetLastSeqDuration(ExpStrs.ARA_ENTER, ExpStrs.ARA_PRESS);

            // Testing
            Output.Conlog<ExperiLogger>(trialRecord.TrialEventsToString());
            Output.Conlog<ExperiLogger>(log.ToString());

            WriteTrialLog(log, logFilePath, _trialLogWriter);
            //_trialLogWriter?.Dispose();

            LogTotalTrialTime(blockNum, trialNum, trial, trialRecord);
        }

        public static void LogMOSFTrial(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            string logFilePath = _mosfTrialLogFilePath; // Passed to the writer

            Output.Conlog<ExperiLogger>("Logging Trial");
            MOSFTrialLog log = new MOSFTrialLog();

            // Information
            LogTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Log start events
            log.trlsh_curmv = trialRecord.GetDuration(ExpStrs.TRIAL_SHOW, ExpStrs.FIRST_MOVE);
            log.curmv_strnt = trialRecord.GetLastSeqDuration(ExpStrs.FIRST_MOVE, ExpStrs.STR_ENTER);
            log.strnt_strpr = trialRecord.GetLastSeqDuration(ExpStrs.STR_ENTER, ExpStrs.STR_PRESS);
            log.strpr_strrl = trialRecord.GetLastSeqDuration(ExpStrs.STR_PRESS, ExpStrs.STR_RELEASE);

            // Log the rest of the times
            switch (trial.Technique.GetDevice())
            {
                case Technique.MOUSE:
                    log.strrl_obj1pr = trialRecord.GetFirstSeqDuration(ExpStrs.STR_RELEASE, ExpStrs.OBJ_PRESS);

                    for (int  i = 1; i <= trial.NObjects; i++)
                    {
                        DynamiclySetFieldValue(
                            log, $"obj{i}pr_obj{i}rl",
                            trialRecord.GetNthSeqDuration(ExpStrs.OBJ_PRESS, ExpStrs.FUN_PRESS, i));
                        DynamiclySetFieldValue(
                            log, $"obj{i}rl_pnlnt{i}",
                            trialRecord.GetNthSeqDuration(ExpStrs.OBJ_RELEASE, ExpStrs.PNL_ENTER, i));
                        DynamiclySetFieldValue(
                            log, $"pnlnt{i}_funnt{i}",
                            trialRecord.GetNthSeqDuration(ExpStrs.PNL_ENTER, ExpStrs.FUN_ENTER, i));
                        DynamiclySetFieldValue(
                            log, $"funnt{i}_funpr{i}",
                            trialRecord.GetNthSeqDuration(ExpStrs.FUN_ENTER, ExpStrs.FUN_PRESS, i));
                        DynamiclySetFieldValue(
                            log, $"funpr{i}_funrl{i}",
                            trialRecord.GetNthSeqDuration(ExpStrs.FUN_PRESS, ExpStrs.FUN_RELEASE, i));

                        // Transition to the Next Object (i + 1)
                        if (i < trial.NObjects)
                        {
                            DynamiclySetFieldValue(
                            log, $"funrl{i}_obj{i + 1}pr",
                            trialRecord.GetNthSeqDuration(ExpStrs.FUN_RELEASE, ExpStrs.OBJ_PRESS, i));
                        }
                        
                    }
                    

                    break;

                case Technique.TOMO:
                    log.strrl_gstst = trialRecord.GetDurationToGestureStart(ExpStrs.STR_RELEASE, trial.Technique);
                    log.gstst_gstnd = trialRecord.GetGestureDuration(trial.Technique);
                    log.gstnd_fstfl = trialRecord.GetDurationFromGestureEnd(trial.Technique, ExpStrs.FLICK);
                    log.fstfl_funmk = trialRecord.GetDuration(ExpStrs.FLICK, ExpStrs.FUN_MARKED);
                    log.funmk_obj1pr = trialRecord.GetFirstSeqDuration(ExpStrs.FUN_MARKED, ExpStrs.OBJ_PRESS);

                    for (int i = 1; i < trial.NObjects; i++)
                    {
                        DynamiclySetFieldValue(
                            log, $"obj{i}rl_obj{i+1}pr",
                            trialRecord.GetNthSeqDuration(ExpStrs.OBJ_RELEASE, ExpStrs.OBJ_PRESS, i));

                        // Transition to the Next Object (i + 1)
                        if (i < trial.NObjects)
                        {
                            DynamiclySetFieldValue(
                            log, $"funrl{i}_obj{i + 1}pr",
                            trialRecord.GetNthSeqDuration(ExpStrs.FUN_RELEASE, ExpStrs.OBJ_PRESS, i));
                        }
                    }
                    break;
            }

            // Testing
            Output.Conlog<ExperiLogger>(trialRecord.TrialEventsToString());
            Output.Conlog<ExperiLogger>(log.ToString());

            WriteTrialLog(log, logFilePath, _trialLogWriter);
            //_trialLogWriter?.Dispose();

            LogTotalTrialTime(blockNum, trialNum, trial, trialRecord);
        }

        public static void LogSOMFTrial(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            string logFilePath = _somfTrialLogFilePath; // Passed to the writer

            Output.Conlog<ExperiLogger>("Logging SOMF Trial");
            SOMFTrialLog log = new SOMFTrialLog();

            // Information
            LogTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Log start events
            log.trlsh_curmv = trialRecord.GetDuration(ExpStrs.TRIAL_SHOW, ExpStrs.FIRST_MOVE);
            log.curmv_strnt = trialRecord.GetLastSeqDuration(ExpStrs.FIRST_MOVE, ExpStrs.STR_ENTER);
            log.strnt_strpr = trialRecord.GetLastSeqDuration(ExpStrs.STR_ENTER, ExpStrs.STR_PRESS);
            log.strpr_strrl = trialRecord.GetLastSeqDuration(ExpStrs.STR_PRESS, ExpStrs.STR_RELEASE);

            // Log the rest of the times
            switch (trial.Technique.GetDevice())
            {
                case Technique.MOUSE:
                    log.strrl_objnt = trialRecord.GetFirstSeqDuration(ExpStrs.STR_RELEASE, ExpStrs.OBJ_ENTER);
                    log.objnt_objpr = trialRecord.GetDuration(ExpStrs.OBJ_ENTER, ExpStrs.OBJ_PRESS);
                    log.objpr_objrl = trialRecord.GetDuration(ExpStrs.OBJ_PRESS, ExpStrs.OBJ_RELEASE);
                    log.objrl_pnlnt = trialRecord.GetFirstSeqDuration(ExpStrs.OBJ_RELEASE, ExpStrs.PNL_ENTER);

                    log.pnlnt_fun1nt= trialRecord.GetNthSeqDuration(ExpStrs.PNL_ENTER, ExpStrs.FUN_ENTER, 1);

                    int i;
                    for (i = 1; i <= trial.GetNumFunctions(); i++)
                    {
                        DynamiclySetFieldValue(
                            log, $"fun{i}nt_fun{i}pr",
                            trialRecord.GetNthSeqDuration(ExpStrs.FUN_ENTER, ExpStrs.FUN_PRESS, i));
                        DynamiclySetFieldValue(
                            log, $"fun{i}pr_fun{i}rl",
                            trialRecord.GetNthSeqDuration(ExpStrs.FUN_PRESS, ExpStrs.FUN_RELEASE, i));

                        // Transition to the next function (i + 1)
                        if (i < trial.GetNumFunctions())
                        {
                            DynamiclySetFieldValue(
                            log, $"fun{i}rl_fun{i + 1}nt",
                            trialRecord.GetNthSeqDuration(ExpStrs.FUN_RELEASE, ExpStrs.FUN_ENTER, i));
                        }
                        
                    }

                    log.funNrl_arapr = trialRecord.GetLastSeqDuration(ExpStrs.FUN_RELEASE, ExpStrs.ARA_PRESS);

                    break;

                case Technique.TOMO:
                    log.strrl_gstst = trialRecord.GetDurationToGestureStart(ExpStrs.STR_RELEASE, trial.Technique);
                    log.gstst_gstnd = trialRecord.GetGestureDuration(trial.Technique);
                    log.gstnd_fstfl = trialRecord.GetDurationFromGestureEnd(trial.Technique, ExpStrs.FLICK);

                    log.fstfl_fun1mk = trialRecord.GetFirstSeqDuration(ExpStrs.FLICK, ExpStrs.FUN_MARKED);

                    for (i = 1; i <= trial.GetNumFunctions(); i++)
                    {
                        DynamiclySetFieldValue(
                            log, $"fun{i}mk_objpr_{i}",
                            trialRecord.GetNthSeqDuration(ExpStrs.FUN_MARKED, ExpStrs.OBJ_PRESS, i));
                        DynamiclySetFieldValue(
                            log, $"objpr_objrl_{i}",
                            trialRecord.GetNthSeqDuration(ExpStrs.OBJ_PRESS, ExpStrs.OBJ_RELEASE, i));

                        // Transition to the next function (i + 1)
                        if (i < trial.GetNumFunctions())
                        {
                            DynamiclySetFieldValue(
                            log, $"objrl_fun{i + 1}mk",
                            trialRecord.GetNthSeqDuration(ExpStrs.OBJ_RELEASE, ExpStrs.FUN_MARKED, i));
                        }

                    }

                    log.objrl_arapr = trialRecord.GetLastSeqDuration(ExpStrs.OBJ_RELEASE, ExpStrs.ARA_PRESS);

                    break;
            }

            // Testing
            Output.Conlog<ExperiLogger>(trialRecord.TrialEventsToString());
            Output.Conlog<ExperiLogger>(log.ToString());

            WriteTrialLog(log, logFilePath, _trialLogWriter);

            LogTotalTrialTime(blockNum, trialNum, trial, trialRecord);
        }

        public static void LogMOMFTrial(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            string logFilePath = _momfTrialLogFilePath; // Passed to the writer

            int nFun = trial.NObjects; // NFunc = NObjects

            MOMFTrialLong log = new MOMFTrialLong();

            // Information
            LogTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Log start events
            log.trlsh_curmv = trialRecord.GetDuration(ExpStrs.TRIAL_SHOW, ExpStrs.FIRST_MOVE);
            log.curmv_strnt = trialRecord.GetLastSeqDuration(ExpStrs.FIRST_MOVE, ExpStrs.STR_ENTER);
            log.strnt_strpr = trialRecord.GetLastSeqDuration(ExpStrs.STR_ENTER, ExpStrs.STR_PRESS);
            log.strpr_strrl = trialRecord.GetLastSeqDuration(ExpStrs.STR_PRESS, ExpStrs.STR_RELEASE);

            switch (trial.Technique.GetDevice())
            {
                case Technique.MOUSE:
                    log.strrl_obj1pr = trialRecord.GetFirstSeqDuration(ExpStrs.STR_RELEASE, ExpStrs.OBJ_PRESS);

                    for (int i = 1; i <= nFun; i++)
                    {
                        DynamiclySetFieldValue(
                            log, $"obj{i}pr_obj{i}rl",
                            trialRecord.GetNthSeqDuration(ExpStrs.OBJ_PRESS, ExpStrs.OBJ_RELEASE, i));
                        DynamiclySetFieldValue(
                            log, $"obj{i}rl_pnlnt_{i}",
                            trialRecord.GetNthSeqDuration(ExpStrs.OBJ_RELEASE, ExpStrs.PNL_ENTER, i));
                        DynamiclySetFieldValue(
                            log, $"pnlnt_fun{i}nt",
                            trialRecord.GetNthSeqDuration(ExpStrs.PNL_ENTER, ExpStrs.FUN_ENTER, i));
                        DynamiclySetFieldValue(
                            log, $"fun{i}nt_fun{i}pr",
                            trialRecord.GetNthSeqDuration(ExpStrs.FUN_ENTER, ExpStrs.FUN_PRESS, i));
                        DynamiclySetFieldValue(
                            log, $"fun{i}pr_fun{i}rl",
                            trialRecord.GetNthSeqDuration(ExpStrs.FUN_PRESS, ExpStrs.FUN_RELEASE, i));

                        // Transition to the Next Object (i + 1)
                        if (i < trial.NObjects)
                        {
                            DynamiclySetFieldValue(
                            log, $"fun{i}rl_obj{i}pr",
                            trialRecord.GetNthSeqDuration(ExpStrs.FUN_RELEASE, ExpStrs.OBJ_PRESS, i));
                        }

                        log.funNrl_arapr = trialRecord.GetLastSeqDuration(ExpStrs.FUN_RELEASE, ExpStrs.ARA_PRESS);

                    }


                    break;

                case Technique.TOMO:
                    log.strrl_gstst = trialRecord.GetDurationToGestureStart(ExpStrs.STR_RELEASE, trial.Technique);
                    log.gstst_gstnd = trialRecord.GetGestureDuration(trial.Technique);
                    log.gstnd_fstfl = trialRecord.GetDurationFromGestureEnd(trial.Technique, ExpStrs.FLICK);

                    log.fstfl_fun1mk = trialRecord.GetFirstSeqDuration(ExpStrs.FLICK, ExpStrs.FUN_MARKED);

                    for (int i = 1; i <= trial.GetNumFunctions(); i++)
                    {
                        DynamiclySetFieldValue(
                            log, $"fun{i}mk_obj{i}pr",
                            trialRecord.GetNthSeqDuration(ExpStrs.FUN_MARKED, ExpStrs.OBJ_PRESS, i));
                        DynamiclySetFieldValue(
                            log, $"obj{i}pr_obj{i}rl",
                            trialRecord.GetNthSeqDuration(ExpStrs.OBJ_PRESS, ExpStrs.OBJ_RELEASE, i));

                        // Transition to the next function (i + 1)
                        if (i < trial.GetNumFunctions())
                        {
                            DynamiclySetFieldValue(
                            log, $"obj{i}rl_fun{i + 1}mk",
                            trialRecord.GetNthSeqDuration(ExpStrs.OBJ_RELEASE, ExpStrs.FUN_MARKED, i));
                        }

                    }

                    log.objNrl_arapr = trialRecord.GetLastSeqDuration(ExpStrs.OBJ_RELEASE, ExpStrs.ARA_PRESS);
                    break;
            }

            // Testing
            Output.Conlog<ExperiLogger>(trialRecord.TrialEventsToString());
            Output.Conlog<ExperiLogger>(log.ToString());

            WriteTrialLog(log, logFilePath, _trialLogWriter);
            //_trialLogWriter?.Dispose();

            LogTotalTrialTime(blockNum, trialNum, trial, trialRecord);
        }

        //private static void FillTrialInfo(TrialLog log, int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        //{
        //    // General info
        //    log.ptc = trial.PtcNum;
        //    log.block = blockNum;
        //    log.trial = trialNum;
        //    log.id = trial.Id;
        //    log.tech = trial.Technique.ToString().ToLower();
        //    log.cmplx = trial.Complexity.ToString().ToLower();
        //    log.tsk_type = ExpStrs.TASKTYPE_ABBR[trial.TaskType];
        //    log.fun_side = trial.FuncSide.ToString().ToLower();
        //    log.func_width = trial.GetFunctionWidthMM();
        //    log.n_obj = trial.NObjects;
        //    log.n_fun = trial.GetNumFunctions();
        //    log.dist_lvl = trial.DistRangeMM.Label.Split('-')[0].ToLower();
        //    log.dist = $"{Utils.PX2MM(trialRecord.AvgDistanceMM):F2}";
        //    log.result = (int)trialRecord.Result;
        //}

        private static void LogTotalTrialTime(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            TotalTrialLog log = new TotalTrialLog();

            // Information
            LogTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Total time
            log.trial_time = trialRecord.GetDuration(ExpStrs.STR_RELEASE, ExpStrs.ARA_PRESS);
            _trialTimes[trial.Id] = log.trial_time;

            log.funcs_sel_time = trialRecord.GetDuration(ExpStrs.PNL_ENTER, ExpStrs.FUN_RELEASE);
            log.objs_sel_time = trialRecord.GetDuration(ExpStrs.ARA_ENTER, ExpStrs.OBJ_RELEASE);
            log.func_po_sel_time = trialRecord.GetDuration(ExpStrs.OBJ_RELEASE, ExpStrs.FUN_RELEASE);
            log.panel_sel_time = trialRecord.GetDuration(ExpStrs.STR_RELEASE, ExpStrs.PNL_SELECT);
            log.panel_nav_time = trialRecord.GetDuration(ExpStrs.PNL_SELECT, ExpStrs.OBJ_PRESS);

            WriteTrialLog(log, _totalLogFilePath, _totalTrialLogWriter);

            // Write cursor records
            //using (StreamWriter writer = new StreamWriter(_cursorLogFilePath, append: false, Encoding.UTF8))
            //{
            //    writer.WriteLine("time_tick;x_px;y_px");
            //    foreach (var record in _trialCursorRecords[_activeTrialId])
            //    {
            //        writer.WriteLine($"{record.TimeMS};{record.Position.X};{record.Position.Y}");
            //    }
            //}
            StreamWriter writer = new StreamWriter(_cursorLogFilePath, append: true, Encoding.UTF8);
            writer.AutoFlush = true;
            foreach (var record in _trialCursorRecords[_activeTrialId])
            {
                writer.WriteLine($"{record.timestamp};{record.x};{record.y}");
            }
            writer.Dispose();
            // Clear records after writing
            //_trialCursorRecords[trialId].Clear();
        }

        private static void LogTrialInfo(TrialLog log, int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            log.ptc = trial.PtcNum;
            log.block = blockNum;
            log.trial = trialNum;
            log.id = trial.Id;
            log.tech = trial.Technique.ToString().ToLower();
            log.cmplx = trial.Complexity.ToString().ToLower();
            log.tsk_type = ExpStrs.TASKTYPE_ABBR[trial.TaskType];
            log.fun_side = trial.FuncSide.ToString().ToLower();
            log.func_width = trial.GetFunctionWidthMM();
            log.n_obj = trial.NObjects;
            log.n_fun = trial.GetNumFunctions();
            log.dist_lvl = trial.DistRangeMM.Label.Split('-')[0].ToLower();
            log.dist = $"{trialRecord.AvgDistanceMM:F2}";
            log.result = (int)trialRecord.Result;
        }

        public static void LogBlockTime(Block block)
        {
            BlockLog log = new BlockLog();

            log.ptc = block.PtcNum;
            log.id = block.Id;
            log.tech = block.Technique.ToString().ToLower();
            log.cmplx = block.Complexity.ToString().ToLower();
            log.n_trials = block.GetNumTrials();
            log.tsk_type = ExpStrs.TASKTYPE_ABBR[block.TaskType];
            log.n_fun = block.NFunctions;
            log.n_obj = block.NObjects;

            double avgTime = _trialTimes.Values.Average()/1000;
            log.block_time = $"{avgTime:F2}";

            WriteTrialLog(log, _blockLogFilePath, _blockLogWriter);

        }

        private static void WriteHeader<T>(StreamWriter streamWriter)
        {
            //var fields = typeof(T).GetFields();
            //var headers = fields.Select(f => f.Name);
            //_trialLogWriter.WriteLine(string.Join(";", headers));

            // Writing first the parent class fields, then the child class fields
            var type = typeof(T);
            var baseType = type.BaseType;

            // 1. Get fields from the base class (parent)
            // BindingFlags.DeclaredOnly ensures we only get fields directly defined in the base class,
            // not its own base classes, or the derived class's fields.
            var parentFields = baseType != null && baseType != typeof(object)
                ? baseType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                : Enumerable.Empty<FieldInfo>();

            // 2. Get fields from the derived class (child)
            // BindingFlags.DeclaredOnly ensures we only get fields directly defined in the derived class.
            var childFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            // 3. Combine them: Parent fields first, then Child fields.
            var allFields = parentFields.Concat(childFields);

            // 4. Extract names and write to the file.
            var headers = allFields.Select(f => f.Name);
            streamWriter.WriteLine(string.Join(";", headers));
        }

        private static void WriteTrialLog<T>(T log, string filePath, StreamWriter writer)
        {
            //var fields = typeof(T).GetFields();
            //var values = fields.Select(f => f.GetValue(trialLog)?.ToString() ?? "");
            //_trialLogWriter.WriteLine(string.Join(";", values));
            //_trialLogWriter.Flush();

            var type = typeof(T);
            var baseType = type.BaseType;

            // 1. Get fields from the base class (parent)
            // Use BindingFlags.Public and BindingFlags.Instance to match the default GetFields behavior.
            var parentFields = baseType != null && baseType != typeof(object)
                ? baseType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                : Enumerable.Empty<FieldInfo>();

            // 2. Get fields from the derived class (child)
            var childFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            // 3. Combine them: Parent fields first, then Child fields.
            var orderedFields = parentFields.Concat(childFields);

            // 4. Get values in the same order.
            var values = orderedFields
                .Select(f => f.GetValue(log)?.ToString() ?? "");

            // 5. Write the values.
            writer.WriteLine(string.Join(";", values));
            //streamWriter.Flush();
        }

        public static void LogCursorPosition(Point cursorPos)
        {
            _trialCursorRecords[_activeTrialId].Add(new PositionRecord(cursorPos.X, cursorPos.Y));
        }

        //private static void WriteTotalTrialLog<T>(T totalTrialLog)
        //{
        //    //var fields = typeof(T).GetFields();
        //    //var values = fields.Select(f => f.GetValue(trialLog)?.ToString() ?? "");
        //    //_trialLogWriter.WriteLine(string.Join(";", values));
        //    //_trialLogWriter.Flush();

        //    var type = typeof(T);
        //    var baseType = type.BaseType;

        //    // 1. Get fields from the base class (parent)
        //    // Use BindingFlags.Public and BindingFlags.Instance to match the default GetFields behavior.
        //    var parentFields = baseType != null && baseType != typeof(object)
        //        ? baseType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        //        : Enumerable.Empty<FieldInfo>();

        //    // 2. Get fields from the derived class (child)
        //    var childFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        //    // 3. Combine them: Parent fields first, then Child fields.
        //    var orderedFields = parentFields.Concat(childFields);

        //    // 4. Get values in the same order.
        //    var values = orderedFields
        //        .Select(f => f.GetValue(totalTrialLog)?.ToString() ?? "");

        //    // 5. Write the values.
        //    _totalTrialLogWriter.WriteLine(string.Join(";", values));
        //    _totalTrialLogWriter.Flush();
        //}

        private static void Dispose()
        {
            _trialLogWriter?.Dispose();
            _trialLogWriter = null;
        }

        public static void DynamiclySetFieldValue(TrialLog instance, string fieldName, int newValue)
        {
            // 2. Get the FieldInfo
            Type dataType = instance.GetType();
            FieldInfo field = dataType.GetField(fieldName);

            if (field != null)
            {
                // 3. Set the Value
                // Pass the object instance (dataInstance) and the new value
                field.SetValue(instance, newValue);
                Console.WriteLine($"Successfully set field '{fieldName}' to {newValue}.");
            }
            else
            {
                Console.WriteLine($"Error: Field '{fieldName}' not found.");
            }
        }

    }
}
