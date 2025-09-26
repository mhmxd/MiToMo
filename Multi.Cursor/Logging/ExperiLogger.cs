using MathNet.Numerics;
using Multi.Cursor.Logging;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.Internal;
using System.IO;
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
        // Set for each log (in constructor)
        private static string _trialLogFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Multi.Cursor.Logs", "trial_log.csv"
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

            bool fileExists = File.Exists(_trialLogFilePath);
            bool fileIsEmpty = !fileExists || new FileInfo(_trialLogFilePath).Length == 0;

            _trialLogWriter = new StreamWriter(_trialLogFilePath, append: true, Encoding.UTF8);

            if (fileIsEmpty)
            {
                WriteHeader<TrialLog>();
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
                        _blockFileLog.Information($"Obj Release     -> Func Press:  {trialRecord.GetDuration(Str.OBJ_RELEASE, Str.FUNCTION_PRESS)}");
                        _blockFileLog.Information($"Func Press      -> Func Release:{trialRecord.GetDuration(Str.FUNCTION_PRESS, Str.FUNCTION_RELEASE)}");
                        _blockFileLog.Information($"Func Release    -> Area Press:  {trialRecord.GetDuration(Str.FUNCTION_RELEASE, Str.OBJ_AREA_PRESS)}");
                        _blockFileLog.Information($"--------------------------------");
                        _blockFileLog.Information($"Total Time (Start Release -> Area Press) = {Utils.MStoSec(trialRecord.GetDuration(Str.START_RELEASE, Str.OBJ_AREA_PRESS))}");
                        _blockFileLog.Information($"==============================================================================================================");
                        break;

                    case Technique.TOMO_TAP:
                        _blockFileLog.Information($"Start Release   -> Tap Down:    {trialRecord.GetDurationToFingerAction(Str.START_RELEASE, Str.TAP_DOWN)}");
                        _blockFileLog.Information($"Tap Down        -> Tap Up:      {trialRecord.GetGestureDuration(Technique.TOMO_TAP)}");
                        _blockFileLog.Information($"Tap Up          -> Obj Press:   {trialRecord.GetDurationFromFingerAction(Str.TAP_UP, Str.OBJ_PRESS)}");
                        _blockFileLog.Information($"Obj Press       -> Obj Release: {trialRecord.GetDuration(Str.OBJ_PRESS, Str.OBJ_RELEASE)}");
                        _blockFileLog.Information($"Obj Release     -> Obj Exit:    {trialRecord.GetDuration(Str.OBJ_RELEASE, Str.OBJ_EXIT)}");
                        _blockFileLog.Information($"Obj Exit        -> Area Press:  {trialRecord.GetDuration(Str.OBJ_EXIT, Str.OBJ_AREA_PRESS)}");
                        _blockFileLog.Information($"--------------------------------");
                        _blockFileLog.Information($"Total Time (Start Release -> Area Press) = {Utils.MStoSec(trialRecord.GetDuration(Str.START_RELEASE, Str.OBJ_AREA_PRESS))}");
                        _blockFileLog.Information($"==============================================================================================================");
                        break;

                    case Technique.TOMO_SWIPE:
                        _blockFileLog.Information($"Start Release   -> Swipe Start: {trialRecord.GetDurationToFingerAction(Str.START_RELEASE, Str.SWIPE_START)}");
                        _blockFileLog.Information($"Swipe Start     -> Swipe End:   {trialRecord.GetGestureDuration(Technique.TOMO_SWIPE)}");
                        _blockFileLog.Information($"Swipe End       -> Obj Press:   {trialRecord.GetDurationFromFingerAction(Str.SWIPE_END, Str.OBJ_PRESS)}");
                        _blockFileLog.Information($"Obj Press       -> Obj Release: {trialRecord.GetDuration(Str.OBJ_PRESS, Str.OBJ_RELEASE)}");
                        _blockFileLog.Information($"Obj Release     -> Obj Exit:    {trialRecord.GetDuration(Str.OBJ_RELEASE, Str.OBJ_EXIT)}");
                        _blockFileLog.Information($"Obj Exit        -> Area Press:  {trialRecord.GetDuration(Str.OBJ_EXIT, Str.OBJ_AREA_PRESS)}");
                        _blockFileLog.Information($"--------------------------------");
                        _blockFileLog.Information($"Total Time (Start Release -> Area Press) = {Utils.MStoSec(trialRecord.GetDuration(Str.START_RELEASE, Str.OBJ_AREA_PRESS))}");
                        _blockFileLog.Information($"==============================================================================================================");
                        break;
                }
            }
            else
            {
                // For now. Later we put detailed log
                _blockFileLog.Information($"Start Release   -> Area Press:   {trialRecord.GetDuration(Str.START_RELEASE, Str.OBJ_AREA_PRESS)}");
            }
            
        }

        public static void LogMultipleObjTrialTimes(TrialRecord trialRecord)
        {
            // For now. Later we put detailed log
            _blockFileLog.Information($"Start Release   -> Area Press:   {trialRecord.GetDuration(Str.START_RELEASE, Str.OBJ_AREA_PRESS)}");
        }

        public static void LogTrial(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            Output.Conlog<ExperiLogger>("Logging Trial");
            TrialLog log = new TrialLog();

            // Information
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
                    log.objxt_obaxt = trialRecord.GetDuration(Str.OBJ_EXIT, Str.OBJ_AREA_EXIT);
                    log.obaxt_pnlnt = trialRecord.GetDuration(Str.OBJ_AREA_EXIT, Str.AUX_ENTER);
                    log.pnlnt_funnt = trialRecord.GetDuration(Str.AUX_ENTER, Str.FUNCTION_ENTER);
                    log.funnt_funpr = trialRecord.GetDuration(Str.FUNCTION_ENTER, Str.FUNCTION_PRESS);
                    log.funpr_funrl = trialRecord.GetDuration(Str.FUNCTION_PRESS, Str.FUNCTION_RELEASE);
                    log.funrl_funxt = trialRecord.GetDuration(Str.FUNCTION_RELEASE, Str.FUNCTION_EXIT);
                    log.funxt_pnlxt = trialRecord.GetDuration(Str.FUNCTION_EXIT, Str.AUX_EXIT);
                    log.pnlxt_obant = trialRecord.GetDuration(Str.AUX_EXIT, Str.OBJ_AREA_ENTER);
                    
                break;

                case Technique.TOMO:
                    log.strrl_gstst = trialRecord.GetDurationToGestureStart(Str.START_RELEASE, trial.Technique);
                    log.gstst_gstnd = trialRecord.GetGestureDuration(trial.Technique);
                    /// Flick logs
                    log.funmk_obant = trialRecord.GetDuration(Str.FUNCTION_MARKED, Str.OBJ_AREA_ENTER);
                    log.obant_objnt = trialRecord.GetDuration(Str.OBJ_AREA_ENTER, Str.OBJ_ENTER);
                break;
            }

            log.objnt_objpr = trialRecord.GetDuration(Str.OBJ_ENTER, Str.OBJ_PRESS);
            log.objpr_objrl = trialRecord.GetDuration(Str.OBJ_PRESS, Str.OBJ_RELEASE);
            log.objrl_objxt = trialRecord.GetDuration(Str.OBJ_RELEASE, Str.OBJ_EXIT);

            log.obant_obapr = trialRecord.GetDuration(Str.OBJ_AREA_ENTER, Str.OBJ_AREA_PRESS);

            Output.Conlog<ExperiLogger>(log.tech);

            WriteTrialLog(log);
            //_trialLogWriter?.Dispose();
        }

        private static void WriteHeader<T>()
        {
            var fields = typeof(T).GetFields();
            var headers = fields.Select(f => f.Name);
            _trialLogWriter.WriteLine(string.Join(";", headers));
        }

        private static void WriteTrialLog<T>(T trialLog)
        {
            //if (!_headerWritten)
            //{
            //    WriteHeader<T>();
            //    _headerWritten = true;
            //}

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

    }
}
