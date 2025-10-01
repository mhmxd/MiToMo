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

        private static Logger _gestureFileLog;
        private static Logger _blockFileLog;

        private static Dictionary<string, int> _trialLogs = new Dictionary<string, int>();

        private static int _ptcId = 0; // Participant ID
        private static Technique _technique = Technique.MOUSE; // Technique

        private static StreamWriter _trialLogWriter;
        private static bool _headerWritten = false;

        public static void Init(int participantId, Technique tech)
        {
            _ptcId = participantId;
            _technique = tech;

            bool fileExists = File.Exists(_sosfTrialLogFilePath);
            bool fileIsEmpty = !fileExists || new FileInfo(_sosfTrialLogFilePath).Length == 0;

            _trialLogWriter = new StreamWriter(_sosfTrialLogFilePath, append: true, Encoding.UTF8);

            if (fileIsEmpty)
            {
                WriteHeader<TrialLog>();
            }
        }

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
                            WriteHeader<SOSFTrialLog>();
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
                            WriteHeader<SOMFTrialLog>();
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
                            WriteHeader<SOMFTrialLog>();
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
                            WriteHeader<SOMFTrialLog>();
                        }
                    }
                    break;
            }

            
        }

        public static void StartBlockLog(int blockId, TaskType blockType, Complexity blockComplexity)
        {
            String blockFileName = $"Block-{blockId}-{blockType}.txt";
            string blockFilePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Multi.Cursor.Logs", $"{_ptcId}-{_technique}", blockFileName
            );

            //_blockFileLog = new LoggerConfiguration()
            //    .WriteTo.Async(a => a.File(blockFilePath, rollingInterval: RollingInterval.Day,
            //    outputTemplate: "{Timestamp:HH:mm:ss.fff} {Message:lj}{NewLine}"))
            //    .CreateLogger();

            _blockFileLog = new LoggerConfiguration()
                .WriteTo.Async(a => a.File(blockFilePath, rollingInterval: RollingInterval.Day,
                outputTemplate: "{Message:lj}{NewLine}"))
                .CreateLogger();

            _blockFileLog.Information($"--- Technique: {_technique} | Block#: {blockId} | Type: {blockType} | Complexity: {blockComplexity} ---");
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
                        _blockFileLog.Information($"Start Release   -> Obj Enter:   {trialRecord.GetDuration(Str.START_RELEASE, Str.OBJ_ENTER)}");
                        _blockFileLog.Information($"Obj Enter       -> Obj Press:   {trialRecord.GetDuration(Str.OBJ_ENTER, Str.OBJ_PRESS)}");
                        _blockFileLog.Information($"Obj Press       -> Obj Release: {trialRecord.GetDuration(Str.OBJ_PRESS, Str.OBJ_RELEASE)}");
                        _blockFileLog.Information($"Obj Release     -> Func Press:  {trialRecord.GetDuration(Str.OBJ_RELEASE, Str.FUN_PRESS)}");
                        _blockFileLog.Information($"Func Press      -> Func Release:{trialRecord.GetDuration(Str.FUN_PRESS, Str.FUN_RELEASE)}");
                        _blockFileLog.Information($"Func Release    -> Area Press:  {trialRecord.GetDuration(Str.FUN_RELEASE, Str.ARA_PRESS)}");
                        _blockFileLog.Information($"--------------------------------");
                        _blockFileLog.Information($"Total Time (Start Release -> Area Press) = {Utils.MStoSec(trialRecord.GetDuration(Str.START_RELEASE, Str.ARA_PRESS))}");
                        _blockFileLog.Information($"==============================================================================================================");
                        break;

                    case Technique.TOMO_TAP:
                        _blockFileLog.Information($"Start Release   -> Tap Down:    {trialRecord.GetDurationToFingerAction(Str.START_RELEASE, Str.TAP_DOWN)}");
                        _blockFileLog.Information($"Tap Down        -> Tap Up:      {trialRecord.GetGestureDuration(Technique.TOMO_TAP)}");
                        _blockFileLog.Information($"Tap Up          -> Obj Press:   {trialRecord.GetDurationFromFingerAction(Str.TAP_UP, Str.OBJ_PRESS)}");
                        _blockFileLog.Information($"Obj Press       -> Obj Release: {trialRecord.GetDuration(Str.OBJ_PRESS, Str.OBJ_RELEASE)}");
                        _blockFileLog.Information($"Obj Release     -> Obj Exit:    {trialRecord.GetDuration(Str.OBJ_RELEASE, Str.OBJ_EXIT)}");
                        _blockFileLog.Information($"Obj Exit        -> Area Press:  {trialRecord.GetDuration(Str.OBJ_EXIT, Str.ARA_PRESS)}");
                        _blockFileLog.Information($"--------------------------------");
                        _blockFileLog.Information($"Total Time (Start Release -> Area Press) = {Utils.MStoSec(trialRecord.GetDuration(Str.START_RELEASE, Str.ARA_PRESS))}");
                        _blockFileLog.Information($"==============================================================================================================");
                        break;

                    case Technique.TOMO_SWIPE:
                        _blockFileLog.Information($"Start Release   -> Swipe Start: {trialRecord.GetDurationToFingerAction(Str.START_RELEASE, Str.SWIPE_START)}");
                        _blockFileLog.Information($"Swipe Start     -> Swipe End:   {trialRecord.GetGestureDuration(Technique.TOMO_SWIPE)}");
                        _blockFileLog.Information($"Swipe End       -> Obj Press:   {trialRecord.GetDurationFromFingerAction(Str.SWIPE_END, Str.OBJ_PRESS)}");
                        _blockFileLog.Information($"Obj Press       -> Obj Release: {trialRecord.GetDuration(Str.OBJ_PRESS, Str.OBJ_RELEASE)}");
                        _blockFileLog.Information($"Obj Release     -> Obj Exit:    {trialRecord.GetDuration(Str.OBJ_RELEASE, Str.OBJ_EXIT)}");
                        _blockFileLog.Information($"Obj Exit        -> Area Press:  {trialRecord.GetDuration(Str.OBJ_EXIT, Str.ARA_PRESS)}");
                        _blockFileLog.Information($"--------------------------------");
                        _blockFileLog.Information($"Total Time (Start Release -> Area Press) = {Utils.MStoSec(trialRecord.GetDuration(Str.START_RELEASE, Str.ARA_PRESS))}");
                        _blockFileLog.Information($"==============================================================================================================");
                        break;
                }
            }
            else
            {
                // For now. Later we put detailed log
                _blockFileLog.Information($"Start Release   -> Area Press:   {trialRecord.GetDuration(Str.START_RELEASE, Str.ARA_PRESS)}");
            }
            
        }

        public static void LogMultipleObjTrialTimes(TrialRecord trialRecord)
        {
            // For now. Later we put detailed log
            _blockFileLog.Information($"Start Release   -> Area Press:   {trialRecord.GetDuration(Str.START_RELEASE, Str.ARA_PRESS)}");
        }

        public static void LogSOSFTrial(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            Output.Conlog<ExperiLogger>("Logging Trial");
            SOSFTrialLog log = new SOSFTrialLog();

            // Information
            LogTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Times
            log.trlsh_fstmv = trialRecord.GetDuration(Str.TRIAL_SHOW, Str.FIRST_MOVE);
            log.fstmv_strnt = trialRecord.GetDuration(Str.FIRST_MOVE, Str.START_ENTER);
            log.strnt_strpr = trialRecord.GetDuration(Str.START_ENTER, Str.START_PRESS);
            log.strpr_strrl = trialRecord.GetDuration(Str.START_PRESS, Str.START_RELEASE);

            switch (trial.Technique.GetDevice())
            {
                case Technique.MOUSE:
                    log.strrl_strxt = trialRecord.GetDuration(Str.START_RELEASE, Str.START_EXIT);
                    log.strxt_objnt = trialRecord.GetDuration(Str.START_EXIT, Str.OBJ_ENTER);
                    log.objxt_obaxt = trialRecord.GetDuration(Str.OBJ_EXIT, Str.ARA_EXIT);
                    log.obaxt_pnlnt = trialRecord.GetDuration(Str.ARA_EXIT, Str.PNL_ENTER);
                    log.pnlnt_funnt = trialRecord.GetDuration(Str.PNL_ENTER, Str.FUN_ENTER);
                    log.funnt_funpr = trialRecord.GetDuration(Str.FUN_ENTER, Str.FUN_PRESS);
                    log.funpr_funrl = trialRecord.GetDuration(Str.FUN_PRESS, Str.FUN_RELEASE);
                    log.funrl_funxt = trialRecord.GetDuration(Str.FUN_RELEASE, Str.FUN_EXIT);
                    log.funxt_pnlxt = trialRecord.GetDuration(Str.FUN_EXIT, Str.PNL_EXIT);
                    log.pnlxt_obant = trialRecord.GetDuration(Str.PNL_EXIT, Str.ARA_ENTER);
                    
                break;

                case Technique.TOMO:
                    log.strrl_gstst = trialRecord.GetDurationToGestureStart(Str.START_RELEASE, trial.Technique);
                    log.gstst_gstnd = trialRecord.GetGestureDuration(trial.Technique);
                    log.gstnd_fstfl = trialRecord.GetDurationFromGestureEnd(trial.Technique, Str.FLICK);
                    log.fstfl_funmk = trialRecord.GetDuration(Str.FLICK, Str.FUNCTION_MARKED);
                    log.funmk_obant = trialRecord.GetDuration(Str.FUNCTION_MARKED, Str.ARA_ENTER);
                    log.obant_objnt = trialRecord.GetDuration(Str.ARA_ENTER, Str.OBJ_ENTER);
                break;
            }

            log.objnt_objpr = trialRecord.GetDuration(Str.OBJ_ENTER, Str.OBJ_PRESS);
            log.objpr_objrl = trialRecord.GetDuration(Str.OBJ_PRESS, Str.OBJ_RELEASE);
            log.objrl_objxt = trialRecord.GetDuration(Str.OBJ_RELEASE, Str.OBJ_EXIT);

            log.obant_obapr = trialRecord.GetDuration(Str.ARA_ENTER, Str.ARA_PRESS);

            Output.Conlog<ExperiLogger>(log.tech);

            WriteTrialLog(log);
            //_trialLogWriter?.Dispose();
        }

        public static void LogMOSFTrial(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            Output.Conlog<ExperiLogger>("Logging Trial");
            MOSFTrialLog log = new MOSFTrialLog();

            // Information
            LogTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Times
            log.trlsh_fstmv = trialRecord.GetDuration(Str.TRIAL_SHOW, Str.FIRST_MOVE);
            log.fstmv_strnt = trialRecord.GetDuration(Str.FIRST_MOVE, Str.START_ENTER);
            log.strnt_strpr = trialRecord.GetDuration(Str.START_ENTER, Str.START_PRESS);
            log.strpr_strrl = trialRecord.GetDuration(Str.START_PRESS, Str.START_RELEASE);

            switch (trial.Technique.GetDevice())
            {
                case Technique.MOUSE:
                    log.strrl_strxt = trialRecord.GetDuration(Str.START_RELEASE, Str.START_EXIT);
                    log.strxt_obj1nt = trialRecord.GetDuration(Str.START_EXIT, Str.GetNumberedStr(Str.OBJ_ENTER, 1));

                    for (int  i = 1; i <= trial.NObjects; i++)
                    {
                        DynamiclySetFieldValue(
                            log, $"obj{i}nt_obj{i}pr", 
                            trialRecord.GetLastSeqDuration(Str.GetNumberedStr(Str.OBJ_ENTER, i), Str.GetNumberedStr(Str.OBJ_PRESS, i)));
                        DynamiclySetFieldValue(
                            log, $"obj{i}pr_obj{i}rl",
                            trialRecord.GetLastSeqDuration(Str.GetNumberedStr(Str.OBJ_PRESS, i), Str.GetNumberedStr(Str.OBJ_RELEASE, i)));
                        DynamiclySetFieldValue(
                            log, $"obj{i}rl_obj{i}xt",
                            trialRecord.GetLastSeqDuration(Str.GetNumberedStr(Str.OBJ_RELEASE, i), Str.GetNumberedStr(Str.OBJ_EXIT, i)));
                        DynamiclySetFieldValue(
                            log, $"obj{i}xt_ara{i}xt",
                            trialRecord.GetDurtionToFirstAfter(Str.GetNumberedStr(Str.OBJ_EXIT, i), Str.ARA_EXIT));
                        DynamiclySetFieldValue(
                            log, $"ara{i}xt_pnl{i}nt",
                            trialRecord.GetFirstSeqDuration(Str.ARA_EXIT, Str.PNL_ENTER));
                        DynamiclySetFieldValue(
                            log, $"pnl{i}nt_fun{i}nt",
                            trialRecord.GetFirstSeqDuration(Str.PNL_ENTER, Str.FUN_ENTER));         
                        DynamiclySetFieldValue(
                            log, $"fun{i}nt_fun{i}pr",
                            trialRecord.GetFirstSeqDuration(Str.FUN_ENTER, Str.FUN_PRESS));
                        DynamiclySetFieldValue(
                            log, $"fun{i}pr_fun{i}rl",
                            trialRecord.GetFirstSeqDuration(Str.FUN_PRESS, Str.FUN_RELEASE));
                        DynamiclySetFieldValue(
                            log, $"fun{i}rl_fun{i}xt",
                            trialRecord.GetFirstSeqDuration(Str.FUN_RELEASE, Str.FUN_EXIT));
                        DynamiclySetFieldValue(
                            log, $"fun{i}xt_pnl{i}xt",
                            trialRecord.GetFirstSeqDuration(Str.FUN_EXIT, Str.PNL_EXIT));
                        DynamiclySetFieldValue(
                            log, $"pnl{i}xt_ara{i}nt", // NOTE: Adjusted from pnl1xt_obant to pnl{i}xt_oba{i}nt for consistency
                            trialRecord.GetFirstSeqDuration(Str.PNL_EXIT, Str.ARA_ENTER));

                        // Transition to the Next Object (i + 1)
                        if (i < trial.NObjects)
                        {
                            DynamiclySetFieldValue(
                            log, $"ara{i}nt_obj{i + 1}nt",
                            trialRecord.GetLastSeqDuration(Str.ARA_ENTER, Str.GetNumberedStr(Str.OBJ_ENTER, i + 1)));
                        }
                        
                    }
                    

                    break;

                case Technique.TOMO:
                    log.strrl_gstst = trialRecord.GetDurationToGestureStart(Str.START_RELEASE, trial.Technique);
                    log.gstst_gstnd = trialRecord.GetGestureDuration(trial.Technique);
                    log.gstnd_fstfl = trialRecord.GetDurationFromGestureEnd(trial.Technique, Str.FLICK);
                    log.fstfl_funmk = trialRecord.GetDuration(Str.FLICK, Str.FUNCTION_MARKED);
                    log.funmk_obant = trialRecord.GetDuration(Str.FUNCTION_MARKED, Str.ARA_ENTER);
                    break;
            }

            // Testing
            Output.Conlog<ExperiLogger>(trialRecord.TimestampsToString());
            Output.Conlog<ExperiLogger>(log.ToString());

            WriteTrialLog(log);
            //_trialLogWriter?.Dispose();
        }

        public static void LogSOMFTrial(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            Output.Conlog<ExperiLogger>("Logging SOMF Trial");
            SOMFTrialLog log = new SOMFTrialLog();

            // Information
            LogTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Times
            log.trlsh_fstmv = trialRecord.GetDuration(Str.TRIAL_SHOW, Str.FIRST_MOVE);
            log.fstmv_strnt = trialRecord.GetDuration(Str.FIRST_MOVE, Str.START_ENTER);
            log.strnt_strpr = trialRecord.GetDuration(Str.START_ENTER, Str.START_PRESS);
            log.strpr_strrl = trialRecord.GetDuration(Str.START_PRESS, Str.START_RELEASE);

            switch (trial.Technique.GetDevice())
            {
                case Technique.MOUSE:
                    log.strrl_strxt = trialRecord.GetDuration(Str.START_RELEASE, Str.START_EXIT);
                    log.strxt_objnt = trialRecord.GetDuration(Str.START_EXIT, Str.OBJ_ENTER);
                    log.objnt_objpr = trialRecord.GetDuration(Str.OBJ_ENTER, Str.OBJ_PRESS);
                    log.objpr_objrl = trialRecord.GetDuration(Str.OBJ_PRESS, Str.OBJ_RELEASE);
                    log.objrl_objxt = trialRecord.GetDuration(Str.OBJ_RELEASE, Str.OBJ_EXIT);
                    log.objxt_araxt = trialRecord.GetDuration(Str.OBJ_EXIT, Str.ARA_EXIT);
                    log.araxt_pnlnt = trialRecord.GetDuration(Str.OBJ_EXIT, Str.PNL_ENTER);

                    log.pnlnt_fun1nt= trialRecord.GetLastSeqDuration(Str.PNL_ENTER, Str.GetNumberedStr(Str.FUN_ENTER, 1));

                    int i;
                    for (i = 1; i <= trial.GetNumFunctions(); i++)
                    {
                        DynamiclySetFieldValue(
                            log, $"fun{i}nt_fun{i}pr",
                            trialRecord.GetDuration(Str.GetNumberedStr(Str.FUN_ENTER, i), Str.GetNumberedStr(Str.FUN_PRESS, i)));
                        DynamiclySetFieldValue(
                            log, $"fun{i}pr_fun{i}rl",
                            trialRecord.GetDuration(Str.GetNumberedStr(Str.FUN_PRESS, i), Str.GetNumberedStr(Str.FUN_RELEASE, i)));
                        DynamiclySetFieldValue(
                            log, $"fun{i}rl_fun{i}xt",
                            trialRecord.GetDuration(Str.GetNumberedStr(Str.FUN_RELEASE, i), Str.GetNumberedStr(Str.FUN_EXIT, i)));

                        // Transition to the next function (i + 1)
                        if (i < trial.GetNumFunctions())
                        {
                            DynamiclySetFieldValue(
                            log, $"fun{i}xt_fun{i + 1}nt",
                            trialRecord.GetLastSeqDuration(Str.GetNumberedStr(Str.FUN_EXIT, i), Str.GetNumberedStr(Str.FUN_ENTER, i + 1)));
                        }
                        
                    }

                    log.funNxt_pnlxt = trialRecord.GetLastSeqDuration(Str.GetNumberedStr(Str.FUN_EXIT, trial.GetNumFunctions()), Str.PNL_EXIT);
                    log.pnlxt_arant = trialRecord.GetLastSeqDuration(Str.PNL_EXIT, Str.ARA_ENTER);
                    log.arant_arapr = trialRecord.GetLastSeqDuration(Str.ARA_ENTER, Str.ARA_PRESS);

                    break;

                case Technique.TOMO:
                    log.strrl_gstst = trialRecord.GetDurationToGestureStart(Str.START_RELEASE, trial.Technique);
                    log.gstst_gstnd = trialRecord.GetGestureDuration(trial.Technique);
                    log.gstnd_fstfl = trialRecord.GetDurationFromGestureEnd(trial.Technique, Str.FLICK);
                    log.fstfl_funmk = trialRecord.GetDuration(Str.FLICK, Str.FUNCTION_MARKED);
                    log.funmk_obant = trialRecord.GetDuration(Str.FUNCTION_MARKED, Str.ARA_ENTER);
                    break;
            }

            // Testing
            Output.Conlog<ExperiLogger>(trialRecord.TimestampsToString());
            Output.Conlog<ExperiLogger>(log.ToString());

        }

        public static void LogMOMFTrial(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            int nFun = trial.NObjects; // NFunc = NObjects

            Output.Conlog<ExperiLogger>("Logging Trial");
            MOMFTrialLong log = new MOMFTrialLong();

            // Information
            LogTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Times
            log.trlsh_fstmv = trialRecord.GetDuration(Str.TRIAL_SHOW, Str.FIRST_MOVE);
            log.fstmv_strnt = trialRecord.GetDuration(Str.FIRST_MOVE, Str.START_ENTER);
            log.strnt_strpr = trialRecord.GetDuration(Str.START_ENTER, Str.START_PRESS);
            log.strpr_strrl = trialRecord.GetDuration(Str.START_PRESS, Str.START_RELEASE);

            switch (trial.Technique.GetDevice())
            {
                case Technique.MOUSE:
                    log.strrl_strxt = trialRecord.GetDuration(Str.START_RELEASE, Str.START_EXIT);
                    log.strxt_obj1nt = trialRecord.GetDuration(Str.START_EXIT, Str.GetNumberedStr(Str.OBJ_ENTER, 1));

                    for (int i = 1; i <= nFun; i++)
                    {
                        DynamiclySetFieldValue(
                            log, $"obj{i}nt_obj{i}pr",
                            trialRecord.GetLastSeqDuration(Str.GetNumberedStr(Str.OBJ_ENTER, i), Str.GetNumberedStr(Str.OBJ_PRESS, i)));
                        DynamiclySetFieldValue(
                            log, $"obj{i}pr_obj{i}rl",
                            trialRecord.GetLastSeqDuration(Str.GetNumberedStr(Str.OBJ_PRESS, i), Str.GetNumberedStr(Str.OBJ_RELEASE, i)));
                        DynamiclySetFieldValue(
                            log, $"obj{i}rl_obj{i}xt",
                            trialRecord.GetLastSeqDuration(Str.GetNumberedStr(Str.OBJ_RELEASE, i), Str.GetNumberedStr(Str.OBJ_EXIT, i)));
                        DynamiclySetFieldValue(
                            log, $"obj{i}xt_ara{i}xt",
                            trialRecord.GetDurtionToFirstAfter(Str.GetNumberedStr(Str.OBJ_EXIT, i), Str.ARA_EXIT));
                        DynamiclySetFieldValue(
                            log, $"ara{i}xt_pnl{i}nt",
                            trialRecord.GetFirstSeqDuration(Str.ARA_EXIT, Str.PNL_ENTER));
                        DynamiclySetFieldValue(
                            log, $"pnl{i}nt_fun{i}nt",
                            trialRecord.GetFirstSeqDuration(Str.PNL_ENTER, Str.GetNumberedStr(Str.FUN_ENTER, i)));
                        DynamiclySetFieldValue(
                            log, $"fun{i}nt_fun{i}pr",
                            trialRecord.GetFirstSeqDuration(Str.GetNumberedStr(Str.FUN_ENTER, i), Str.GetNumberedStr(Str.FUN_PRESS, i)));
                        DynamiclySetFieldValue(
                            log, $"fun{i}pr_fun{i}rl",
                            trialRecord.GetFirstSeqDuration(Str.GetNumberedStr(Str.FUN_PRESS, i), Str.GetNumberedStr(Str.FUN_RELEASE, i)));
                        DynamiclySetFieldValue(
                            log, $"fun{i}rl_fun{i}xt",
                            trialRecord.GetFirstSeqDuration(Str.GetNumberedStr(Str.FUN_RELEASE, i), Str.GetNumberedStr(Str.FUN_EXIT, i)));
                        DynamiclySetFieldValue(
                            log, $"fun{i}xt_pnl{i}xt",
                            trialRecord.GetFirstSeqDuration(Str.GetNumberedStr(Str.FUN_EXIT, i), Str.PNL_EXIT));
                        DynamiclySetFieldValue(
                            log, $"pnl{i}xt_ara{i}nt", // NOTE: Adjusted from pnl1xt_obant to pnl{i}xt_oba{i}nt for consistency
                            trialRecord.GetFirstSeqDuration(Str.PNL_EXIT, Str.ARA_ENTER));

                        // Transition to the Next Object (i + 1)
                        if (i < trial.NObjects)
                        {
                            DynamiclySetFieldValue(
                            log, $"ara{i}nt_obj{i + 1}nt",
                            trialRecord.GetLastSeqDuration(Str.ARA_ENTER, Str.GetNumberedStr(Str.OBJ_ENTER, i + 1)));
                        }

                        log.funNxt_pnlNxt = trialRecord.GetLastSeqDuration(Str.GetNumberedStr(Str.FUN_EXIT, nFun), Str.PNL_EXIT);
                        log.pnlNxt_araNnt = trialRecord.GetLastSeqDuration(Str.PNL_EXIT, Str.ARA_ENTER);
                        log.araNnt_araNpr = trialRecord.GetLastSeqDuration(Str.ARA_ENTER, Str.ARA_PRESS);

                    }


                    break;

                case Technique.TOMO:
                    log.strrl_gstst = trialRecord.GetDurationToGestureStart(Str.START_RELEASE, trial.Technique);
                    log.gstst_gstnd = trialRecord.GetGestureDuration(trial.Technique);
                    log.gstnd_fstfl = trialRecord.GetDurationFromGestureEnd(trial.Technique, Str.FLICK);
                    log.fstfl_funmk = trialRecord.GetDuration(Str.FLICK, Str.FUNCTION_MARKED);
                    log.funmk_obant = trialRecord.GetDuration(Str.FUNCTION_MARKED, Str.ARA_ENTER);
                    break;
            }

            // Testing
            Output.Conlog<ExperiLogger>(trialRecord.TimestampsToString());
            Output.Conlog<ExperiLogger>(log.ToString());

            WriteTrialLog(log);
            //_trialLogWriter?.Dispose();
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

        private static void WriteHeader<T>()
        {
            var fields = typeof(T).GetFields();
            var headers = fields.Select(f => f.Name);
            _trialLogWriter.WriteLine(string.Join(";", headers));
        }

        private static void WriteTrialLog<T>(T trialLog)
        {
            var fields = typeof(T).GetFields();
            var values = fields.Select(f => f.GetValue(trialLog)?.ToString() ?? "");
            _trialLogWriter.WriteLine(string.Join(";", values));
            _trialLogWriter.Flush();
        }

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
