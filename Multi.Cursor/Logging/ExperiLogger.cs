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
using ILogger = Serilog.ILogger;

namespace Multi.Cursor
{
    internal class ExperiLogger
    {
        // Set for each log (in constructor)
        private static string _sosfTrialLogFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Multi.Cursor.Logs", "sosf_trial_log.csv"
        );
        private static string _somfTrialLogFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Multi.Cursor.Logs", "somf_trial_log.csv"
        );
        private static string _mosfTrialLogFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Multi.Cursor.Logs", "mosf_trial_log.csv"
        );
        private static string _momfTrialLogFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Multi.Cursor.Logs", "momf_trial_log.csv"
        );
        private static string _totalLogFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Multi.Cursor.Logs", "total_trial_log.csv"
        );
        private static string _blockLogFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Multi.Cursor.Logs", "block_log.csv"
        );

        private static Logger _gestureFileLog;
        private static Logger _blockFileLog;

        private static StreamWriter _trialLogWriter;
        private static StreamWriter _totalTrialLogWriter;
        private static StreamWriter _blockLogWriter;

        private static Dictionary<string, int> _trialLogs = new Dictionary<string, int>();

        private static int _ptcId = 0; // Participant ID
        private static Technique _technique = Technique.MOUSE; // Technique

        private static bool _headerWritten = false;

        private static Dictionary<int, int> _trialTimes = new Dictionary<int, int>();

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

        public static void Init(int participantId, Technique tech, TaskType taskType)
        {
            _ptcId = participantId;
            _technique = tech;

            // Default (will set based on the task type)
            switch (taskType)
            {
                case TaskType.ONE_OBJ_ONE_FUNC:
                    {
                        string logFilePath = _sosfTrialLogFilePath;
                        bool fileExists = File.Exists(logFilePath);
                        bool fileIsEmpty = !fileExists || new FileInfo(logFilePath).Length == 0;

                        _trialLogWriter = new StreamWriter(logFilePath, append: true, Encoding.UTF8);

                        if (fileIsEmpty)
                        {
                            WriteHeader<SOSFTrialLog>(_trialLogWriter);
                        }
                    }
                    break;
                case TaskType.ONE_OBJ_MULTI_FUNC:
                    {
                        string logFilePath = _somfTrialLogFilePath;
                        bool fileExists = File.Exists(logFilePath);
                        bool fileIsEmpty = !fileExists || new FileInfo(logFilePath).Length == 0;

                        _trialLogWriter = new StreamWriter(logFilePath, append: true, Encoding.UTF8);
                        
                        if (fileIsEmpty)
                        {
                            WriteHeader<SOMFTrialLog>(_trialLogWriter);
                        }
                    }
                    break;
                case TaskType.MULTI_OBJ_ONE_FUNC:
                    {
                        string logFilePath = _mosfTrialLogFilePath;
                        bool fileExists = File.Exists(logFilePath);
                        bool fileIsEmpty = !fileExists || new FileInfo(logFilePath).Length == 0;

                        _trialLogWriter = new StreamWriter(logFilePath, append: true, Encoding.UTF8);

                        if (fileIsEmpty)
                        {
                            WriteHeader<MOSFTrialLog>(_trialLogWriter);
                        }
                    }
                    break;
                case TaskType.MULTI_OBJ_MULTI_FUNC:
                    {
                        string logFilePath = _momfTrialLogFilePath;
                        bool fileExists = File.Exists(logFilePath);
                        bool fileIsEmpty = !fileExists || new FileInfo(logFilePath).Length == 0;

                        _trialLogWriter = new StreamWriter(logFilePath, append: true, Encoding.UTF8);

                        if (fileIsEmpty)
                        {
                            WriteHeader<MOMFTrialLong>(_trialLogWriter);
                        }
                    }
                    break;
            }

            _trialLogWriter.AutoFlush = true;

            // Create total log if not exists
            bool totalFileExists = File.Exists(_totalLogFilePath);
            bool totalFileIsEmpty = !totalFileExists || new FileInfo(_totalLogFilePath).Length == 0;
            _totalTrialLogWriter = new StreamWriter(_totalLogFilePath, append: true, Encoding.UTF8);
            _totalTrialLogWriter.AutoFlush = true;
            if (totalFileIsEmpty)
            {
                WriteHeader<TotalTrialLog>(_totalTrialLogWriter);
            }

            // Create block log if not exists
            bool blockFileExists = File.Exists(_blockLogFilePath);
            bool blockFileIsEmpty = !blockFileExists || new FileInfo(_blockLogFilePath).Length == 0;
            _blockLogWriter = new StreamWriter(_blockLogFilePath, append: true, Encoding.UTF8);
            _blockLogWriter.AutoFlush = true;
            if (blockFileIsEmpty)
            {
                WriteHeader<BlockLog>(_blockLogWriter);
            }
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

        public static void LogGestureDuration(string gesture, int duration)
        {

        }

        public static void LogSingleObjTrialTimes(TrialRecord trialRecord)
        {
            int nFunctions = trialRecord.Functions.Count;

            if (nFunctions == 1)
            {
                switch (_technique)
                {
                    case Technique.MOUSE:
                        _blockFileLog.Information($"Start Release   -> Obj Enter:   {trialRecord.GetDuration(Str.STR_RELEASE, Str.OBJ_ENTER)}");
                        _blockFileLog.Information($"Obj Enter       -> Obj Press:   {trialRecord.GetDuration(Str.OBJ_ENTER, Str.OBJ_PRESS)}");
                        _blockFileLog.Information($"Obj Press       -> Obj Release: {trialRecord.GetDuration(Str.OBJ_PRESS, Str.OBJ_RELEASE)}");
                        _blockFileLog.Information($"Obj Release     -> Func Press:  {trialRecord.GetDuration(Str.OBJ_RELEASE, Str.FUN_PRESS)}");
                        _blockFileLog.Information($"Func Press      -> Func Release:{trialRecord.GetDuration(Str.FUN_PRESS, Str.FUN_RELEASE)}");
                        _blockFileLog.Information($"Func Release    -> Area Press:  {trialRecord.GetDuration(Str.FUN_RELEASE, Str.ARA_PRESS)}");
                        _blockFileLog.Information($"--------------------------------");
                        _blockFileLog.Information($"Total Time (Start Release -> Area Press) = {Utils.MStoSec(trialRecord.GetDuration(Str.STR_RELEASE, Str.ARA_PRESS))}");
                        _blockFileLog.Information($"==============================================================================================================");
                        break;

                    case Technique.TOMO_TAP:
                        _blockFileLog.Information($"Start Release   -> Tap Down:    {trialRecord.GetDurationToFingerAction(Str.STR_RELEASE, Str.TAP_DOWN)}");
                        _blockFileLog.Information($"Tap Down        -> Tap Up:      {trialRecord.GetGestureDuration(Technique.TOMO_TAP)}");
                        _blockFileLog.Information($"Tap Up          -> Obj Press:   {trialRecord.GetDurationFromFingerAction(Str.TAP_UP, Str.OBJ_PRESS)}");
                        _blockFileLog.Information($"Obj Press       -> Obj Release: {trialRecord.GetDuration(Str.OBJ_PRESS, Str.OBJ_RELEASE)}");
                        _blockFileLog.Information($"Obj Release     -> Obj Exit:    {trialRecord.GetDuration(Str.OBJ_RELEASE, Str.OBJ_EXIT)}");
                        _blockFileLog.Information($"Obj Exit        -> Area Press:  {trialRecord.GetDuration(Str.OBJ_EXIT, Str.ARA_PRESS)}");
                        _blockFileLog.Information($"--------------------------------");
                        _blockFileLog.Information($"Total Time (Start Release -> Area Press) = {Utils.MStoSec(trialRecord.GetDuration(Str.STR_RELEASE, Str.ARA_PRESS))}");
                        _blockFileLog.Information($"==============================================================================================================");
                        break;

                    case Technique.TOMO_SWIPE:
                        _blockFileLog.Information($"Start Release   -> Swipe Start: {trialRecord.GetDurationToFingerAction(Str.STR_RELEASE, Str.SWIPE_START)}");
                        _blockFileLog.Information($"Swipe Start     -> Swipe End:   {trialRecord.GetGestureDuration(Technique.TOMO_SWIPE)}");
                        _blockFileLog.Information($"Swipe End       -> Obj Press:   {trialRecord.GetDurationFromFingerAction(Str.SWIPE_END, Str.OBJ_PRESS)}");
                        _blockFileLog.Information($"Obj Press       -> Obj Release: {trialRecord.GetDuration(Str.OBJ_PRESS, Str.OBJ_RELEASE)}");
                        _blockFileLog.Information($"Obj Release     -> Obj Exit:    {trialRecord.GetDuration(Str.OBJ_RELEASE, Str.OBJ_EXIT)}");
                        _blockFileLog.Information($"Obj Exit        -> Area Press:  {trialRecord.GetDuration(Str.OBJ_EXIT, Str.ARA_PRESS)}");
                        _blockFileLog.Information($"--------------------------------");
                        _blockFileLog.Information($"Total Time (Start Release -> Area Press) = {Utils.MStoSec(trialRecord.GetDuration(Str.STR_RELEASE, Str.ARA_PRESS))}");
                        _blockFileLog.Information($"==============================================================================================================");
                        break;
                }
            }
            else
            {
                // For now. Later we put detailed log
                _blockFileLog.Information($"Start Release   -> Area Press:   {trialRecord.GetDuration(Str.STR_RELEASE, Str.ARA_PRESS)}");
            }
            
        }

        public static void LogMultipleObjTrialTimes(TrialRecord trialRecord)
        {
            // For now. Later we put detailed log
            _blockFileLog.Information($"Start Release   -> Area Press:   {trialRecord.GetDuration(Str.STR_RELEASE, Str.ARA_PRESS)}");
        }

        public static void LogSOSFTrial(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            LogTotalTrialTime(blockNum, trialNum, trial, trialRecord);

            Output.Conlog<ExperiLogger>("Logging Trial");
            SOSFTrialLog log = new SOSFTrialLog();

            // Information
            LogTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Log start events
            log.trlsh_curmv = trialRecord.GetDuration(Str.TRIAL_SHOW, Str.FIRST_MOVE);
            log.curmv_strnt = trialRecord.GetLastSeqDuration(Str.FIRST_MOVE, Str.STR_ENTER);
            log.strnt_strpr = trialRecord.GetLastSeqDuration(Str.STR_ENTER, Str.STR_PRESS);
            log.strpr_strrl = trialRecord.GetLastSeqDuration(Str.STR_PRESS, Str.STR_RELEASE);

            // Log the rest of the times
            switch (trial.Technique.GetDevice())
            {
                case Technique.MOUSE:
                    log.strrl_objnt = trialRecord.GetLastSeqDuration(Str.STR_RELEASE, Str.OBJ_ENTER);

                    log.objrl_pnlnt = trialRecord.GetLastSeqDuration(Str.OBJ_RELEASE, Str.PNL_ENTER);
                    log.pnlnt_funnt = trialRecord.GetLastSeqDuration(Str.PNL_ENTER, Str.FUN_ENTER);
                    log.funnt_funpr = trialRecord.GetLastSeqDuration(Str.FUN_ENTER, Str.FUN_PRESS);
                    log.funpr_funrl = trialRecord.GetLastSeqDuration(Str.FUN_PRESS, Str.FUN_RELEASE);
                    log.funrl_arant = trialRecord.GetLastSeqDuration(Str.FUN_RELEASE, Str.ARA_ENTER);
                    
                break;

                case Technique.TOMO:
                    log.strrl_gstst = trialRecord.GetDurationToGestureStart(Str.STR_RELEASE, trial.Technique);
                    log.gstst_gstnd = trialRecord.GetGestureDuration(trial.Technique);
                    log.gstnd_fstfl = trialRecord.GetDurationFromGestureEnd(trial.Technique, Str.FLICK);
                    log.fstfl_funmk = trialRecord.GetDuration(Str.FLICK, Str.FUN_MARKED);
                    log.funmk_objnt = trialRecord.GetDuration(Str.ARA_ENTER, Str.OBJ_ENTER);
                break;
            }

            log.objnt_objpr = trialRecord.GetLastSeqDuration(Str.OBJ_ENTER, Str.OBJ_PRESS);
            log.objpr_objrl = trialRecord.GetLastSeqDuration(Str.OBJ_PRESS, Str.OBJ_RELEASE);

            log.arant_arapr = trialRecord.GetLastSeqDuration(Str.ARA_ENTER, Str.ARA_PRESS);

            // Testing
            Output.Conlog<ExperiLogger>(trialRecord.TrialEventsToString());
            Output.Conlog<ExperiLogger>(log.ToString());

            WriteTrialLog(log, _trialLogWriter);
            //_trialLogWriter?.Dispose();


        }

        public static void LogMOSFTrial(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            Output.Conlog<ExperiLogger>("Logging Trial");
            MOSFTrialLog log = new MOSFTrialLog();

            // Information
            LogTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Log start events
            log.trlsh_curmv = trialRecord.GetDuration(Str.TRIAL_SHOW, Str.FIRST_MOVE);
            log.curmv_strnt = trialRecord.GetLastSeqDuration(Str.FIRST_MOVE, Str.STR_ENTER);
            log.strnt_strpr = trialRecord.GetLastSeqDuration(Str.STR_ENTER, Str.STR_PRESS);
            log.strpr_strrl = trialRecord.GetLastSeqDuration(Str.STR_PRESS, Str.STR_RELEASE);

            // Log the rest of the times
            switch (trial.Technique.GetDevice())
            {
                case Technique.MOUSE:
                    log.strrl_obj1pr = trialRecord.GetFirstSeqDuration(Str.STR_RELEASE, Str.OBJ_PRESS);

                    for (int  i = 1; i <= trial.NObjects; i++)
                    {
                        DynamiclySetFieldValue(
                            log, $"obj{i}pr_obj{i}rl",
                            trialRecord.GetNthSeqDuration(Str.OBJ_PRESS, Str.FUN_PRESS, i));
                        DynamiclySetFieldValue(
                            log, $"obj{i}rl_pnlnt{i}",
                            trialRecord.GetNthSeqDuration(Str.OBJ_RELEASE, Str.PNL_ENTER, i));
                        DynamiclySetFieldValue(
                            log, $"pnlnt{i}_funnt{i}",
                            trialRecord.GetNthSeqDuration(Str.PNL_ENTER, Str.FUN_ENTER, i));
                        DynamiclySetFieldValue(
                            log, $"funnt{i}_funpr{i}",
                            trialRecord.GetNthSeqDuration(Str.FUN_ENTER, Str.FUN_PRESS, i));
                        DynamiclySetFieldValue(
                            log, $"funpr{i}_funrl{i}",
                            trialRecord.GetNthSeqDuration(Str.FUN_PRESS, Str.FUN_RELEASE, i));

                        // Transition to the Next Object (i + 1)
                        if (i < trial.NObjects)
                        {
                            DynamiclySetFieldValue(
                            log, $"funrl{i}_obj{i + 1}pr",
                            trialRecord.GetNthSeqDuration(Str.FUN_RELEASE, Str.OBJ_PRESS, i));
                        }
                        
                    }
                    

                    break;

                case Technique.TOMO:
                    log.strrl_gstst = trialRecord.GetDurationToGestureStart(Str.STR_RELEASE, trial.Technique);
                    log.gstst_gstnd = trialRecord.GetGestureDuration(trial.Technique);
                    log.gstnd_fstfl = trialRecord.GetDurationFromGestureEnd(trial.Technique, Str.FLICK);
                    log.fstfl_funmk = trialRecord.GetDuration(Str.FLICK, Str.FUN_MARKED);
                    log.funmk_obj1pr = trialRecord.GetFirstSeqDuration(Str.FUN_MARKED, Str.OBJ_PRESS);

                    for (int i = 1; i < trial.NObjects; i++)
                    {
                        DynamiclySetFieldValue(
                            log, $"obj{i}rl_obj{i+1}pr",
                            trialRecord.GetNthSeqDuration(Str.OBJ_RELEASE, Str.OBJ_PRESS, i));

                        // Transition to the Next Object (i + 1)
                        if (i < trial.NObjects)
                        {
                            DynamiclySetFieldValue(
                            log, $"funrl{i}_obj{i + 1}pr",
                            trialRecord.GetNthSeqDuration(Str.FUN_RELEASE, Str.OBJ_PRESS, i));
                        }
                    }
                    break;
            }

            // Testing
            Output.Conlog<ExperiLogger>(trialRecord.TrialEventsToString());
            Output.Conlog<ExperiLogger>(log.ToString());

            WriteTrialLog(log, _trialLogWriter);
            //_trialLogWriter?.Dispose();

            LogTotalTrialTime(blockNum, trialNum, trial, trialRecord);
        }

        public static void LogSOMFTrial(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            Output.Conlog<ExperiLogger>("Logging SOMF Trial");
            SOMFTrialLog log = new SOMFTrialLog();

            // Information
            LogTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Log start events
            log.trlsh_curmv = trialRecord.GetDuration(Str.TRIAL_SHOW, Str.FIRST_MOVE);
            log.curmv_strnt = trialRecord.GetLastSeqDuration(Str.FIRST_MOVE, Str.STR_ENTER);
            log.strnt_strpr = trialRecord.GetLastSeqDuration(Str.STR_ENTER, Str.STR_PRESS);
            log.strpr_strrl = trialRecord.GetLastSeqDuration(Str.STR_PRESS, Str.STR_RELEASE);

            // Log the rest of the times
            switch (trial.Technique.GetDevice())
            {
                case Technique.MOUSE:
                    log.strrl_objnt = trialRecord.GetFirstSeqDuration(Str.STR_RELEASE, Str.OBJ_ENTER);
                    log.objnt_objpr = trialRecord.GetDuration(Str.OBJ_ENTER, Str.OBJ_PRESS);
                    log.objpr_objrl = trialRecord.GetDuration(Str.OBJ_PRESS, Str.OBJ_RELEASE);
                    log.objrl_pnlnt = trialRecord.GetFirstSeqDuration(Str.OBJ_RELEASE, Str.PNL_ENTER);

                    log.pnlnt_fun1nt= trialRecord.GetNthSeqDuration(Str.PNL_ENTER, Str.FUN_ENTER, 1);

                    int i;
                    for (i = 1; i <= trial.GetNumFunctions(); i++)
                    {
                        DynamiclySetFieldValue(
                            log, $"fun{i}nt_fun{i}pr",
                            trialRecord.GetNthSeqDuration(Str.FUN_ENTER, Str.FUN_PRESS, i));
                        DynamiclySetFieldValue(
                            log, $"fun{i}pr_fun{i}rl",
                            trialRecord.GetNthSeqDuration(Str.FUN_PRESS, Str.FUN_RELEASE, i));

                        // Transition to the next function (i + 1)
                        if (i < trial.GetNumFunctions())
                        {
                            DynamiclySetFieldValue(
                            log, $"fun{i}rl_fun{i + 1}nt",
                            trialRecord.GetNthSeqDuration(Str.FUN_RELEASE, Str.FUN_ENTER, i));
                        }
                        
                    }

                    log.funNrl_arapr = trialRecord.GetLastSeqDuration(Str.FUN_RELEASE, Str.ARA_PRESS);

                    break;

                case Technique.TOMO:
                    log.strrl_gstst = trialRecord.GetDurationToGestureStart(Str.STR_RELEASE, trial.Technique);
                    log.gstst_gstnd = trialRecord.GetGestureDuration(trial.Technique);
                    log.gstnd_fstfl = trialRecord.GetDurationFromGestureEnd(trial.Technique, Str.FLICK);

                    log.fstfl_fun1mk = trialRecord.GetFirstSeqDuration(Str.FLICK, Str.FUN_MARKED);

                    for (i = 1; i <= trial.GetNumFunctions(); i++)
                    {
                        DynamiclySetFieldValue(
                            log, $"fun{i}mk_objpr_{i}",
                            trialRecord.GetNthSeqDuration(Str.FUN_MARKED, Str.OBJ_PRESS, i));
                        DynamiclySetFieldValue(
                            log, $"objpr_objrl_{i}",
                            trialRecord.GetNthSeqDuration(Str.OBJ_PRESS, Str.OBJ_RELEASE, i));

                        // Transition to the next function (i + 1)
                        if (i < trial.GetNumFunctions())
                        {
                            DynamiclySetFieldValue(
                            log, $"objrl_fun{i + 1}mk",
                            trialRecord.GetNthSeqDuration(Str.OBJ_RELEASE, Str.FUN_MARKED, i));
                        }

                    }

                    log.objrl_arapr = trialRecord.GetLastSeqDuration(Str.OBJ_RELEASE, Str.ARA_PRESS);

                    break;
            }

            // Testing
            Output.Conlog<ExperiLogger>(trialRecord.TrialEventsToString());
            Output.Conlog<ExperiLogger>(log.ToString());

            WriteTrialLog(log, _trialLogWriter);

            LogTotalTrialTime(blockNum, trialNum, trial, trialRecord);
        }

        public static void LogMOMFTrial(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            int nFun = trial.NObjects; // NFunc = NObjects

            MOMFTrialLong log = new MOMFTrialLong();

            // Information
            LogTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Log start events
            log.trlsh_curmv = trialRecord.GetDuration(Str.TRIAL_SHOW, Str.FIRST_MOVE);
            log.curmv_strnt = trialRecord.GetLastSeqDuration(Str.FIRST_MOVE, Str.STR_ENTER);
            log.strnt_strpr = trialRecord.GetLastSeqDuration(Str.STR_ENTER, Str.STR_PRESS);
            log.strpr_strrl = trialRecord.GetLastSeqDuration(Str.STR_PRESS, Str.STR_RELEASE);

            switch (trial.Technique.GetDevice())
            {
                case Technique.MOUSE:
                    log.strrl_obj1pr = trialRecord.GetFirstSeqDuration(Str.STR_RELEASE, Str.OBJ_PRESS);

                    for (int i = 1; i <= nFun; i++)
                    {
                        DynamiclySetFieldValue(
                            log, $"obj{i}pr_obj{i}rl",
                            trialRecord.GetNthSeqDuration(Str.OBJ_PRESS, Str.OBJ_RELEASE, i));
                        DynamiclySetFieldValue(
                            log, $"obj{i}rl_pnlnt_{i}",
                            trialRecord.GetNthSeqDuration(Str.OBJ_RELEASE, Str.PNL_ENTER, i));
                        DynamiclySetFieldValue(
                            log, $"pnlnt_fun{i}nt",
                            trialRecord.GetNthSeqDuration(Str.PNL_ENTER, Str.FUN_ENTER, i));
                        DynamiclySetFieldValue(
                            log, $"fun{i}nt_fun{i}pr",
                            trialRecord.GetNthSeqDuration(Str.FUN_ENTER, Str.FUN_PRESS, i));
                        DynamiclySetFieldValue(
                            log, $"fun{i}pr_fun{i}rl",
                            trialRecord.GetNthSeqDuration(Str.FUN_PRESS, Str.FUN_RELEASE, i));

                        // Transition to the Next Object (i + 1)
                        if (i < trial.NObjects)
                        {
                            DynamiclySetFieldValue(
                            log, $"fun{i}rl_obj{i}pr",
                            trialRecord.GetNthSeqDuration(Str.FUN_RELEASE, Str.OBJ_PRESS, i));
                        }

                        log.funNrl_arapr = trialRecord.GetLastSeqDuration(Str.FUN_RELEASE, Str.ARA_PRESS);

                    }


                    break;

                case Technique.TOMO:
                    log.strrl_gstst = trialRecord.GetDurationToGestureStart(Str.STR_RELEASE, trial.Technique);
                    log.gstst_gstnd = trialRecord.GetGestureDuration(trial.Technique);
                    log.gstnd_fstfl = trialRecord.GetDurationFromGestureEnd(trial.Technique, Str.FLICK);

                    log.fstfl_fun1mk = trialRecord.GetFirstSeqDuration(Str.FLICK, Str.FUN_MARKED);

                    for (int i = 1; i <= trial.GetNumFunctions(); i++)
                    {
                        DynamiclySetFieldValue(
                            log, $"fun{i}mk_obj{i}pr",
                            trialRecord.GetNthSeqDuration(Str.FUN_MARKED, Str.OBJ_PRESS, i));
                        DynamiclySetFieldValue(
                            log, $"obj{i}pr_obj{i}rl",
                            trialRecord.GetNthSeqDuration(Str.OBJ_PRESS, Str.OBJ_RELEASE, i));

                        // Transition to the next function (i + 1)
                        if (i < trial.GetNumFunctions())
                        {
                            DynamiclySetFieldValue(
                            log, $"obj{i}rl_fun{i + 1}mk",
                            trialRecord.GetNthSeqDuration(Str.OBJ_RELEASE, Str.FUN_MARKED, i));
                        }

                    }

                    log.objNrl_arapr = trialRecord.GetLastSeqDuration(Str.OBJ_RELEASE, Str.ARA_PRESS);
                    break;
            }

            // Testing
            Output.Conlog<ExperiLogger>(trialRecord.TrialEventsToString());
            Output.Conlog<ExperiLogger>(log.ToString());

            WriteTrialLog(log, _trialLogWriter);
            //_trialLogWriter?.Dispose();

            LogTotalTrialTime(blockNum, trialNum, trial, trialRecord);
        }

        private static void LogTrialInfo(TrialLog log, int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            log.ptc = trial.PtcNum;
            log.block = blockNum;
            log.trial = trialNum;
            log.id = trial.Id;
            log.tech = trial.Technique.ToString().ToLower();
            log.cmplx = trial.Complexity.ToString().ToLower();
            log.tsk_type = Str.TASKTYPE_ABBR[trial.TaskType];
            log.fun_side = trial.FuncSide.ToString().ToLower();
            log.func_width = trial.GetFunctionWidthMM();
            log.n_obj = trial.NObjects;
            log.n_fun = trial.GetNumFunctions();
            log.dist_lvl = trial.DistRangeMM.Label.Split('-')[0].ToLower();
            log.dist = $"{trialRecord.GetDistMM():F2}";
        }

        private static void LogTotalTrialTime(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            TotalTrialLog log = new TotalTrialLog();

            // Information
            LogTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Total time
            log.trial_time = trialRecord.GetDuration(Str.STR_RELEASE, Str.ARA_PRESS);
            _trialTimes[trial.Id] = log.trial_time;

            WriteTrialLog(log, _totalTrialLogWriter);
        }

        public static void LogBlockTime(Block block)
        {
            BlockLog log = new BlockLog();

            log.ptc = block.PtcNum;
            log.id = block.Id;
            log.tech = block.Technique.ToString().ToLower();
            log.cmplx = block.Complexity.ToString().ToLower();
            log.n_trials = block.GetNumTrials();
            log.tsk_type = block.TaskType.ToString().ToLower();
            log.n_fun = block.NFunctions;
            log.n_obj = block.NObjects;

            double avgTime = _trialTimes.Values.Average()/1000;
            log.block_time = $"{avgTime:F2}";

            WriteTrialLog(log, _blockLogWriter);

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

        private static void WriteTrialLog<T>(T log, StreamWriter streamWriter)
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
            streamWriter.WriteLine(string.Join(";", values));
            //streamWriter.Flush();
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
