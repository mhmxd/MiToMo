using Common.Constants;
using Common.Helpers;
using Common.Logs;
using Common.Settings;
using Serilog.Core;
using SubTask.FunctionPointSelect.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using static Common.Constants.ExpEnums;

namespace SubTask.FunctionPointSelect
{
    internal class ExperiLogger
    {
        private static readonly Technique Technique = Technique.MOUSE; // Technique

        private static readonly string Namespace = typeof(ExperiLogger).Namespace;
        private static readonly string LogsFolderName = ExpStrs.JoinDot(Namespace, ExpStrs.Logs);
        private static readonly string MyDocumentsPath =
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        // Set for each log (in constructor)
        private static readonly string _detilTrialLogPath = Path.Combine(
            MyDocumentsPath, LogsFolderName,
            $"P{ExpEnvironment.PTC_NUM}-{Technique}", ExpStrs.TRIALS_DETAIL_C);
        private static readonly string _totalTrialLogPath = Path.Combine(
            MyDocumentsPath, LogsFolderName,
            $"P{ExpEnvironment.PTC_NUM}-{Technique}", ExpStrs.TRIALS_TOTAL_C);
        private static readonly string _blockLogPath = Path.Combine(
            MyDocumentsPath, LogsFolderName,
            $"P{ExpEnvironment.PTC_NUM}-{Technique}", ExpStrs.BLOCKS_C);

        private static string _cursorLogFilePath = ""; // Will be set when starting trial cursor log

        private static Logger _gestureFileLog;
        private static Logger _blockFileLog;

        private static StreamWriter _detailTrialLogWriter;
        private static StreamWriter _totalTrialLogWriter;
        private static StreamWriter _cursorLogWriter;
        private static StreamWriter _blockLogWriter;

        private static Dictionary<string, int> _trialLogs = new Dictionary<string, int>();

        private static bool _headerWritten = false;

        private static Dictionary<int, int> _trialTimes = new Dictionary<int, int>();

        private static Dictionary<int, List<PositionRecord>> _trialCursorRecords = new Dictionary<int, List<PositionRecord>>();
        private static int _activeTrialId = -1;

        public static void Init()
        {
            // Create detailed trial log if not exists
            _detailTrialLogWriter = MIO.PrepareFile<DetailTrialLog>(_detilTrialLogPath, ExpStrs.TRIALS_DETAIL_S);

            // Create total log if not exists
            _totalTrialLogWriter = MIO.PrepareFile<TotalTrialLog>(_totalTrialLogPath, ExpStrs.TRIALS_TOTAL_S);

            // Create block log if not exists
            _blockLogWriter = MIO.PrepareFile<BlockLog>(_blockLogPath, ExpStrs.BLOCKS_S);
        }

        public static void StartTrialCursorLog(int trialId)
        {
            _activeTrialId = trialId;
            _trialCursorRecords[_activeTrialId] = new List<PositionRecord>();

            _cursorLogFilePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "SubTask.FunctionPointSelect.Logs", $"P{ExpEnvironment.PTC_NUM}-{Technique}", "Cursor", $"trial{trialId}-cursor-log"
            );

            _cursorLogWriter = MIO.PrepareFileWithHeader<PositionRecord>(_cursorLogFilePath, PositionRecord.GetHeader());
        }

        public static void StartTrialLog(Trial trial)
        {
            //_blockFileLog.Information($"{trial.ToString()}");
            //_blockFileLog.Information($"----------------------------------------------------------------------------------");
        }

        public static void LogGestureEvent(string message)
        {
            //_gestureFileLog.Information(message);
        }

        public static void LogTrialMessage(string message)
        {
            _blockFileLog.Information(message);
        }

        private static void LogTrialInfo(TrialLog log, int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            log.ptc = trial.PtcNum;
            log.block = blockNum;
            log.trial = trialNum;
            log.id = trial.Id;
            log.tech = trial.Technique.ToString().ToLower();
            log.cmplx = trial.Complexity.ToString().ToLower();
            log.exptype = trial.ExpType.ToString().ToLower();
            log.tsk_type = ExpStrs.TASKTYPE_ABBR[trial.TaskType];
            log.fun_side = trial.FuncSide.ToString().ToLower();
            log.func_width = trial.GetFunctionWidthMM();
            log.n_obj = trial.NObjects;
            log.n_fun = trial.GetNumFunctions();
            log.dist_lvl = trial.DistRangeMM.Label.Split('-')[0].ToLower();
            log.dist = trialRecord.DistanceMM.ToString("F2");
            log.result = (int)trialRecord.Result;
        }

        public static void LogDetailTrial(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            DetailTrialLog log = new DetailTrialLog();

            // Information
            LogTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Information
            LogTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Log events
            log.trlsh_curmv = trialRecord.GetDuration(ExpStrs.TRIAL_SHOW, ExpStrs.FIRST_MOVE);
            log.curmv_strnt = trialRecord.GetLastSeqDuration(ExpStrs.FIRST_MOVE, ExpStrs.STR_ENTER);
            log.strnt_strpr = trialRecord.GetLastSeqDuration(ExpStrs.STR_ENTER, ExpStrs.STR_PRESS);
            log.strpr_strrl = trialRecord.GetLastSeqDuration(ExpStrs.STR_PRESS, ExpStrs.STR_RELEASE);

            log.strrl_strxt = trialRecord.GetLastSeqDuration(ExpStrs.STR_RELEASE, ExpStrs.STR_EXIT);
            log.strxt_pnlnt = trialRecord.GetLastSeqDuration(ExpStrs.STR_EXIT, ExpStrs.PNL_ENTER);

            log.pnlnt_funnt = trialRecord.GetLastSeqDuration(ExpStrs.PNL_ENTER, ExpStrs.FUN_ENTER);
            log.funnt_funpr = trialRecord.GetLastSeqDuration(ExpStrs.FUN_ENTER, ExpStrs.FUN_PRESS);
            log.funpr_funrl = trialRecord.GetLastSeqDuration(ExpStrs.FUN_PRESS, ExpStrs.FUN_RELEASE);

            WriteTrialLog(log, _detilTrialLogPath, _detailTrialLogWriter);
            //_detailTrialLogWriter?.Dispose();
        }

        public static void LogTotalTrialTime(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            TotalTrialLog log = new TotalTrialLog();

            // Information
            LogTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Total time
            log.trial_time = trialRecord.GetDuration(ExpStrs.STR_RELEASE, ExpStrs.FUN_PRESS);
            _trialTimes[trial.Id] = log.trial_time;

            WriteTrialLog(log, _totalTrialLogPath, _totalTrialLogWriter);
        }

        public static void LogCursorRecords()
        {
            foreach (var record in _trialCursorRecords[_activeTrialId])
            {
                _cursorLogWriter.WriteLine($"{record.x};{record.y}");
            }

            _cursorLogWriter?.Dispose();
        }


        public static void LogBlockTime(Block block)
        {
            BlockLog log = new BlockLog();

            log.ptc = block.PtcNum;
            log.id = block.Id;
            log.cmplx = block.Complexity.ToString().ToLower();
            log.exptype = block.ExpType.ToString().ToLower();
            log.n_trials = block.GetNumTrials();

            double avgTime = _trialTimes.Values.Average() / 1000;
            log.block_time = $"{avgTime:F2}";

            WriteTrialLog(log, _blockLogPath, _blockLogWriter);

        }

        private static void WriteTrialLog<T>(T log, string filePath, StreamWriter writer)
        {
            //var fields = typeof(T).GetFields();
            //var values = fields.Select(f => f.GetValue(trialLog)?.ToString() ?? "");
            //_detailTrialLogWriter.WriteLine(string.Join(";", values));
            //_detailTrialLogWriter.Flush();

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

        public static void RecordCursorPosition(Point cursorPos)
        {
            if (_trialCursorRecords.ContainsKey(_activeTrialId))
            {
                _trialCursorRecords[_activeTrialId]
                    .Add(new PositionRecord(cursorPos.X, cursorPos.Y));
            }

        }

    }
}
