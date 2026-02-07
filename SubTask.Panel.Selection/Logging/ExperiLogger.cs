using Common.Constants;
using Common.Helpers;
using Common.Logs;
using Common.Settings;
using SubTask.Panel.Selection.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using static Common.Constants.ExpEnums;

namespace SubTask.Panel.Selection
{
    internal class ExperiLogger
    {
        private static Technique _technique = Technique.TOMO; // Technique

        private static readonly string Namespace = typeof(ExperiLogger).Namespace;
        private static readonly string LogsFolderName = ExpStrs.JoinDot(Namespace, ExpStrs.Logs);
        private static readonly string MyDocumentsPath =
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        // Set for each log (in constructor)
        private static string _detiledTrialLogPath = Path.Combine(
            MyDocumentsPath, LogsFolderName,
            $"P{ExpEnvironment.PTC_NUM}-{_technique}", ExpStrs.TRIALS_DETAIL_C);
        private static string _totalTrialLogPath = Path.Combine(
            MyDocumentsPath, LogsFolderName,
            $"P{ExpEnvironment.PTC_NUM}-{_technique}", ExpStrs.TRIALS_TOTAL_C);
        private static string _blockLogPath = Path.Combine(
            MyDocumentsPath, LogsFolderName,
            $"P{ExpEnvironment.PTC_NUM}-{_technique}", ExpStrs.BLOCKS_C);

        private static string _cursorLogFilePath = ""; // Will be set when starting trial cursor log
        private static string _gestureLogFilePath = ""; // Will be set when starting trial cursor log

        private static StreamWriter _detailTrialLogWriter;
        private static StreamWriter _totalTrialLogWriter;
        private static StreamWriter _cursorLogWriter;
        private static StreamWriter _gestureLogWriter;
        private static StreamWriter _blockLogWriter;

        private static Dictionary<string, int> _trialLogs = new Dictionary<string, int>();

        private static Dictionary<int, int> _trialTimes = new Dictionary<int, int>();

        private static Dictionary<int, List<PositionRecord>> _trialCursorRecords = new Dictionary<int, List<PositionRecord>>();
        private static List<GestureLog> _trialGestureRecords = new();
        private static int _activeTrialId = -1;

        public static void Init(Technique tech)
        {
            _technique = tech;

            _detiledTrialLogPath = Path.Combine(
                MyDocumentsPath, LogsFolderName,
                $"P{ExpEnvironment.PTC_NUM}-{_technique}", ExpStrs.TRIALS_DETAIL_C);
            // Create detailed trial log if not exists
            _detailTrialLogWriter = MIO.PrepareFile<DetailTrialLog>(_detiledTrialLogPath, ExpStrs.TRIALS_DETAIL_S);

            _totalTrialLogPath = Path.Combine(
                MyDocumentsPath, LogsFolderName,
                $"P{ExpEnvironment.PTC_NUM}-{_technique}", ExpStrs.TRIALS_TOTAL_C);
            // Create total log if not exists
            _totalTrialLogWriter = MIO.PrepareFile<TotalTrialLog>(_totalTrialLogPath, ExpStrs.TRIALS_TOTAL_S);

            _blockLogPath = Path.Combine(
                MyDocumentsPath, LogsFolderName,
                $"P{ExpEnvironment.PTC_NUM}-{_technique}", ExpStrs.BLOCKS_C);
            // Create block log if not exists
            _blockLogWriter = MIO.PrepareFile<BlockLog>(_blockLogPath, ExpStrs.BLOCKS_S);

        }

        public static void StartTrialCursorLog(int trialId, int trialNum)
        {
            _activeTrialId = trialId;
            _trialCursorRecords[_activeTrialId] = new List<PositionRecord>();

            _cursorLogFilePath = Path.Combine(
                MyDocumentsPath, LogsFolderName,
                $"P{ExpEnvironment.PTC_NUM}-{_technique}", ExpStrs.CURSOR_C, $"trial-n{trialNum}-id{trialId}-{ExpStrs.CURSOR_S}"
            );

            _cursorLogWriter = MIO.PrepareFileWithHeader<PositionRecord>(_cursorLogFilePath, PositionRecord.GetHeader());

            _gestureLogFilePath = Path.Combine(
                MyDocumentsPath, LogsFolderName,
                $"P{ExpEnvironment.PTC_NUM}-{_technique}", ExpStrs.GestureCap, $"trial-n{trialNum}-id{trialId}-{ExpStrs.Gesture}"
            );

            _gestureLogWriter = MIO.PrepareFileWithHeader<GestureLog>(_gestureLogFilePath);
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

            DetailTrialLog log = new();

            // Information
            LogTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Log start events
            log.trlsh_curmv = trialRecord.GetDuration(ExpStrs.TRIAL_SHOW, ExpStrs.FIRST_MOVE);
            log.curmv_strnt = trialRecord.GetLastSeqDuration(ExpStrs.FIRST_MOVE, ExpStrs.STR_ENTER);
            log.strnt_strpr = trialRecord.GetLastSeqDuration(ExpStrs.STR_ENTER, ExpStrs.STR_PRESS);
            log.strpr_strrl = trialRecord.GetLastSeqDuration(ExpStrs.STR_PRESS, ExpStrs.STR_RELEASE);

            // Log the rest of the times

            MIO.WriteTrialLog(log, _detiledTrialLogPath, _detailTrialLogWriter);
            //_detailTrialLogWriter?.Dispose();            
        }

        public static void LogTotalTrialTime(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            TotalTrialLog log = new();

            // Information
            LogTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Total time
            log.trial_time = trialRecord.GetDuration(ExpStrs.STR_RELEASE, ExpStrs.PNL_SELECT);

            _trialTimes[trial.Id] = log.trial_time;

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

        public static void LogGestures()
        {
            foreach (var log in _trialGestureRecords)
            {
                _gestureLogWriter.WriteLine($"{log.timestamp};{log.finger};{log.action};{log.x};{log.y}");
            }
            _gestureLogWriter.Dispose();
        }


        public static void LogBlockTime(Block block)
        {
            BlockLog log = new()
            {
                ptc = block.PtcNum,
                id = block.Id,
                tech = block.Technique.ToString().ToLower(),
                cmplx = block.Complexity.ToString().ToLower(),
                exptype = block.ExpType.ToString().ToLower(),
                n_fun = 1,
                n_trials = block.GetNumTrials()
            };

            double avgTime = _trialTimes.Values.Average() / 1000;
            log.block_time = $"{avgTime:F2}";

            MIO.WriteTrialLog(log, _blockLogPath, _blockLogWriter);

        }

        public static void RecordCursorPosition(Point cursorPos)
        {
            _trialCursorRecords[_activeTrialId].Add(new PositionRecord(cursorPos.X, cursorPos.Y));
        }

        public static void RecordGesture(long timestamp, Finger finger, string action, Point point)
        {
            _trialGestureRecords.Add(new GestureLog
            {
                timestamp = timestamp,
                finger = finger.ToString().ToLower(),
                action = action,
                x = point.X.ToString("F2"),
                y = point.Y.ToString("F2")
            });
        }

        private static void Dispose()
        {
            _detailTrialLogWriter?.Dispose();
            _detailTrialLogWriter = null;
        }

    }
}
