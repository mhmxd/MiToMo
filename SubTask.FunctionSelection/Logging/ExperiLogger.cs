using Common.Constants;
using Common.Helpers;
using Common.Logs;
using Common.Settings;
using Serilog.Core;
using SubTask.FunctionSelection.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using static Common.Constants.ExpEnums;

namespace SubTask.FunctionSelection
{
    internal class ExperiLogger
    {
        private static readonly Technique Technique = Technique.MOUSE; // Technique

        private static readonly string Namespace = typeof(ExperiLogger).Namespace;
        private static readonly string LogsFolderName = ExpStrs.JoinDot(Namespace, ExpStrs.Logs);
        private static readonly string MyDocumentsPath =
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        // Set for each log (in constructor)
        private static readonly string _detiledTrialLogPath = Path.Combine(
            MyDocumentsPath, LogsFolderName,
            $"P{ExpEnvironment.PTC_NUM}-{Technique}", ExpStrs.TRIALS_DETAIL_C);
        private static readonly string _totalTrialLogPath = Path.Combine(
            MyDocumentsPath, LogsFolderName,
            $"P{ExpEnvironment.PTC_NUM}-{Technique}", ExpStrs.TRIALS_TOTAL_C);
        private static readonly string _blockLogPath = Path.Combine(
            MyDocumentsPath, LogsFolderName,
            $"P{ExpEnvironment.PTC_NUM}-{Technique}", ExpStrs.BLOCKS_C);

        private static string _cursorLogFilePath = ""; // Will be set when starting trial cursor log
        private static string _eventsLogFilePath = ""; // Will be set when starting trial events log

        private static Logger _blockFileLog;

        private static StreamWriter _detailTrialLogWriter;
        private static StreamWriter _totalTrialLogWriter;
        private static StreamWriter _cursorLogWriter;
        private static StreamWriter _eventsLogWriter;
        private static StreamWriter _blockLogWriter;

        private static Dictionary<int, int> _trialTimes = new Dictionary<int, int>();

        private static Dictionary<int, List<PositionRecord>> _trialCursorRecords = new Dictionary<int, List<PositionRecord>>();
        private static int _activeTrialId = -1;

        public static void Init()
        {
            // Create detailed trial log if not exists
            _detailTrialLogWriter = MIO.PrepareFile<DetailTrialLog>(_detiledTrialLogPath, ExpStrs.TRIALS_DETAIL_S);

            // Create total log if not exists
            _totalTrialLogWriter = MIO.PrepareFile<TotalTrialLog>(_totalTrialLogPath, ExpStrs.TRIALS_TOTAL_S);

            // Create block log if not exists
            _blockLogWriter = MIO.PrepareFile<BlockLog>(_blockLogPath, ExpStrs.BLOCKS_S);
        }

        public static void StartTrialCursorLog(int trialId, int trialNum)
        {
            _activeTrialId = trialId;
            _trialCursorRecords[_activeTrialId] = new List<PositionRecord>();

            _cursorLogFilePath = Path.Combine(
                MyDocumentsPath, LogsFolderName,
                $"P{ExpEnvironment.PTC_NUM}-{Technique}", ExpStrs.CURSOR_C, $"trial-n{trialNum}-id{trialId}-{ExpStrs.CURSOR_S}"
            );

            _cursorLogWriter = MIO.PrepareFileWithHeader<PositionRecord>(_cursorLogFilePath, PositionRecord.GetHeader());

            _eventsLogFilePath = Path.Combine(
                MyDocumentsPath, LogsFolderName,
                $"P{ExpEnvironment.PTC_NUM}-{Technique}", ExpStrs.EventsCap, $"trial-n{trialNum}-id{trialId}-{ExpStrs.Events}"
            );

            _eventsLogWriter = MIO.PrepareFileWithHeader<TrialEvent>(_eventsLogFilePath, TrialEvent.GetHeader());
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
            log.func_width = trialRecord.GetFunctionWidthInUnits(0);
            log.n_obj = 0;
            log.n_fun = 1;
            log.dist_lvl = "-";
            log.dist = "-";
            log.result = (int)trialRecord.Result;
        }

        public static void LogDetailTrial(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            DetailTrialLog log = new DetailTrialLog();

            // Information
            LogTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Log start events
            log.trlsh_curmv = trialRecord.GetDuration(ExpStrs.TRIAL_SHOW, ExpStrs.FIRST_MOVE);
            log.curmv_strnt = trialRecord.GetLastSeqDuration(ExpStrs.FIRST_MOVE, ExpStrs.STR_ENTER);
            log.strnt_strpr = trialRecord.GetFirstSeqDuration(ExpStrs.STR_ENTER, ExpStrs.STR_PRESS);
            log.strpr_strrl = trialRecord.GetFirstSeqDuration(ExpStrs.STR_PRESS, ExpStrs.STR_RELEASE);

            log.strrl_pnlnt = trialRecord.GetFirstSeqDuration(ExpStrs.STR_RELEASE, ExpStrs.PNL_ENTER);

            log.pnlnt_fun1nt = trialRecord.GetNthSeqDuration(ExpStrs.PNL_ENTER, ExpStrs.FUN_ENTER, 1);

            log.fun1nt_fun1pr = trialRecord.GetNthSeqDuration(ExpStrs.FUN_ENTER, ExpStrs.FUN_PRESS, 1);
            log.fun1pr_fun1rl = trialRecord.GetNthSeqDuration(ExpStrs.FUN_PRESS, ExpStrs.FUN_RELEASE, 1);
            log.fun1rl_fun2nt = trialRecord.GetNthSeqDuration(ExpStrs.FUN_RELEASE, ExpStrs.FUN_ENTER, 1);
            log.fun2nt_fun2pr = trialRecord.GetNthSeqDuration(ExpStrs.FUN_ENTER, ExpStrs.FUN_PRESS, 2);
            log.fun2pr_fun2rl = trialRecord.GetNthSeqDuration(ExpStrs.FUN_PRESS, ExpStrs.FUN_RELEASE, 2);
            log.fun2rl_fun3nt = trialRecord.GetNthSeqDuration(ExpStrs.FUN_RELEASE, ExpStrs.FUN_ENTER, 2);
            log.fun3nt_fun3pr = trialRecord.GetNthSeqDuration(ExpStrs.FUN_ENTER, ExpStrs.FUN_PRESS, 3);
            log.fun3pr_fun3rl = trialRecord.GetNthSeqDuration(ExpStrs.FUN_PRESS, ExpStrs.FUN_RELEASE, 3);

            log.fun3rl_pnlex = trialRecord.GetNthSeqDuration(ExpStrs.FUN_RELEASE, ExpStrs.PNL_EXIT, 3);
            log.pnlex_endnt = trialRecord.GetLastSeqDuration(ExpStrs.PNL_EXIT, ExpStrs.STR_ENTER);
            log.endnt_endpr = trialRecord.GetLastSeqDuration(ExpStrs.STR_ENTER, ExpStrs.STR_PRESS);
            log.endpr_endrl = trialRecord.GetLastSeqDuration(ExpStrs.STR_PRESS, ExpStrs.STR_RELEASE);

            MIO.WriteTrialLog(log, _detiledTrialLogPath, _detailTrialLogWriter);
        }

        public static void LogTotalTrialTime(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            TotalTrialLog log = new TotalTrialLog();

            // Information
            LogTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Total time
            log.trial_time = trialRecord.GetLastSeqDuration(ExpStrs.STR_RELEASE, ExpStrs.STR_PRESS);
            _trialTimes[trial.Id] = log.trial_time;

            log.funcs_sel_time = trialRecord.GetDurationFromFirstToLast(ExpStrs.FUN_ENTER, ExpStrs.FUN_RELEASE);

            MIO.WriteTrialLog(log, _totalTrialLogPath, _totalTrialLogWriter);
        }

        public static void LogCursorPositions()
        {
            foreach (var record in _trialCursorRecords[_activeTrialId])
            {
                _cursorLogWriter.WriteLine($"{record.timestamp};{record.x};{record.y}");
            }

            _cursorLogWriter.Dispose();
        }

        public static void LogTrialEvents(List<TrialEvent> events)
        {
            foreach (var e in events)
            {
                _eventsLogWriter.WriteLine(e.ToLogString());
            }

            _eventsLogWriter?.Dispose();
        }

        public static void LogBlockTime(Block block)
        {
            BlockLog log = new BlockLog();

            log.ptc = block.PtcNum;
            log.id = block.Id;
            log.cmplx = block.Complexity.ToString().ToLower();
            log.exptype = block.ExpType.ToString().ToLower();
            log.n_trials = block.GetNumTrials();
            log.n_fun = block.NFunctions;

            double avgTime = _trialTimes.Values.Average() / 1000;
            log.avg_time = $"{avgTime:F2}";

            MIO.WriteTrialLog(log, _blockLogPath, _blockLogWriter);

        }

        public static void RecordCursorPosition(Point cursorPos)
        {
            _trialCursorRecords[_activeTrialId].Add(new PositionRecord(cursorPos.X, cursorPos.Y));
        }

    }
}
