using Common.Constants;
using Common.Helpers;
using Common.Logs;
using Common.Settings;
using CommonUI;
using SubTask.PanelNavigation.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using static Common.Constants.ExpEnums;

namespace SubTask.PanelNavigation
{
    internal class ExperiLogger
    {
        private static readonly Technique Technique = Technique.TOMO; // Technique

        private static readonly string Namespace = typeof(ExperiLogger).Namespace;
        private static readonly string LogsFolderName = ExpStrs.JoinDot(Namespace, ExpStrs.Logs);
        private static readonly string MyDocumentsPath =
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        // Set for each log (in constructor)
        private static readonly string _detailTrialLogPath = Path.Combine(
            MyDocumentsPath, LogsFolderName,
            $"P{ExpEnvironment.PTC_NUM}-{Technique}", ExpStrs.TRIALS_DETAIL_C);
        private static readonly string _totalTrialLogPath = Path.Combine(
            MyDocumentsPath, LogsFolderName,
            $"P{ExpEnvironment.PTC_NUM}-{Technique}", ExpStrs.TRIALS_TOTAL_C);
        private static readonly string _blockLogPath = Path.Combine(
            MyDocumentsPath, LogsFolderName,
            $"P{ExpEnvironment.PTC_NUM}-{Technique}", ExpStrs.BLOCKS_C);

        private static string _cursorLogFilePath = ""; // Will be set when starting trial cursor log
        private static string _gestureLogFilePath = ""; // Will be set when starting trial cursor log

        private static StreamWriter _detailTrialLogWriter;
        private static StreamWriter _totalTrialLogWriter;
        private static StreamWriter _cursorLogWriter;
        private static StreamWriter _gestureLogWriter;
        private static StreamWriter _blockLogWriter;

        private static Dictionary<int, int> _trialTimes = new Dictionary<int, int>();

        private static Dictionary<int, List<PositionRecord>> _trialCursorRecords = new();
        private static Dictionary<int, List<PositionRecord>> _trialMarkerRecords = new();
        private static List<GestureLog> _trialGestureRecords = new();
        private static int _activeTrialId = -1;

        public static void Init()
        {
            // Create detailed trial log if not exists
            _detailTrialLogWriter = MIO.PrepareFile<DetailTrialLog>(_detailTrialLogPath, ExpStrs.TRIALS_DETAIL_S);

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
                $"P{ExpEnvironment.PTC_NUM}-{Technique}", ExpStrs.CURSOR_C, $"trial-id{trialId}-n{trialNum}-{ExpStrs.CURSOR_S}"
            );

            _cursorLogWriter = MIO.PrepareFileWithHeader<PositionRecord>(_cursorLogFilePath, PositionRecord.GetHeader());

            _gestureLogFilePath = Path.Combine(
                MyDocumentsPath, LogsFolderName,
                $"P{ExpEnvironment.PTC_NUM}-{Technique}", ExpStrs.GestureCap, $"trial-id{trialId}-n{trialNum}-{ExpStrs.Gesture}"
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

            DetailTrialLog log = new DetailTrialLog();

            // Information
            LogTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Log start events
            log.trlsh_curmv = trialRecord.GetDuration(ExpStrs.TRIAL_SHOW, ExpStrs.FIRST_MOVE);
            log.curmv_strnt = trialRecord.GetLastSeqDuration(ExpStrs.FIRST_MOVE, ExpStrs.STR_ENTER);
            log.strnt_strpr = trialRecord.GetLastSeqDuration(ExpStrs.STR_ENTER, ExpStrs.STR_PRESS);
            log.strpr_strrl = trialRecord.GetLastSeqDuration(ExpStrs.STR_PRESS, ExpStrs.STR_RELEASE);

            // Log the rest of the times
            log.strrl_fngmv = trialRecord.GetLastSeqDuration(ExpStrs.STR_RELEASE, ExpStrs.FLICK);
            log.fngmv_mrkmv = trialRecord.GetFirstSeqDuration(ExpStrs.FLICK, ExpStrs.BTN_MARKED);
            log.mrkmv_mrksp = trialRecord.GetSequenceDuration(ExpStrs.BTN_MARKED);
            log.mrksp_endpr = trialRecord.GetDuration(ExpStrs.BTN_MARKED, ExpStrs.END_PRESS);
            log.endpr_endrl = trialRecord.GetDuration(ExpStrs.END_PRESS, ExpStrs.END_RELEASE);

            MIO.WriteTrialLog(log, _detailTrialLogPath, _detailTrialLogWriter);
            //_detailTrialLogWriter?.Dispose();
        }

        public static void LogTotalTrialTime(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            TotalTrialLog log = new();

            // Information
            LogTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Total time
            log.trial_time = trialRecord.GetLastSeqDuration(ExpStrs.STR_RELEASE, ExpStrs.BTN_MARKED);
            log.marker_move_time = trialRecord.GetSequenceDuration(ExpStrs.BTN_MARKED);

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
            BlockLog log = new BlockLog();

            log.ptc = block.PtcNum;
            log.id = block.Id;
            log.tech = block.Technique.ToString().ToLower();
            log.cmplx = block.Complexity.ToString().ToLower();
            log.exptype = block.ExpType.ToString().ToLower();
            log.n_fun = 1;
            log.n_trials = block.GetNumTrials();

            //double avgTime = _trialTimes.Values.Average() / 1000;
            //log.block_time = $"{avgTime:F2}";

            MIO.WriteTrialLog(log, _blockLogPath, _blockLogWriter);

        }

        public static void RecordCursorPosition(Point cursorPos)
        {
            _trialCursorRecords[_activeTrialId].Add(new PositionRecord(cursorPos.X, cursorPos.Y));
        }

        public static void RecordMarkerPosition(int row, int column)
        {
            _trialMarkerRecords[_activeTrialId].Add(new PositionRecord(row, column));
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

    }
}
