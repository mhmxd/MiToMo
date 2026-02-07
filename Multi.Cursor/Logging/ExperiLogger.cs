using Common.Constants;
using Common.Helpers;
using Common.Logs;
using Common.Settings;
using CommonUI;
using Multi.Cursor.Logging;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using static Common.Constants.ExpEnums;

namespace Multi.Cursor
{
    internal class ExperiLogger
    {
        private static Technique _technique = Technique.MOUSE; // Technique

        private static readonly string Namespace = typeof(ExperiLogger).Namespace;
        private static readonly string LogsFolderName = ExpStrs.JoinDot(Namespace, ExpStrs.Logs);
        private static readonly string MyDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        // Set for each log (in constructor)
        private static string _detailTrialLogPath;
        private static string _totalTrialLogPath;
        private static string _blockLogPath;

        private static string _cursorLogFilePath = ""; // Will be set when starting trial cursor log

        private static Logger _gestureFileLog;
        private static Logger _blockFileLog;

        private static StreamWriter _detailTrialLogWriter;
        private static StreamWriter _totalTrialLogWriter;
        private static StreamWriter _cursorLogWriter;
        private static StreamWriter _blockLogWriter;

        private static Dictionary<string, int> _trialLogs = new Dictionary<string, int>();


        private static Dictionary<int, int> _trialTimes = new Dictionary<int, int>();

        private static Dictionary<int, List<PositionRecord>> _trialCursorRecords = new Dictionary<int, List<PositionRecord>>();
        private static int _activeTrialId = -1;

        public static void Init(Technique tech, TaskType taskType)
        {
            _technique = tech;

            _detailTrialLogPath = Path.Combine(MyDocumentsPath, LogsFolderName,
            $"P{ExpEnvironment.PTC_NUM}-{_technique}", ExpStrs.TRIALS_DETAIL_C);

            _totalTrialLogPath = Path.Combine(MyDocumentsPath, LogsFolderName,
            $"P{ExpEnvironment.PTC_NUM}-{_technique}", ExpStrs.TRIALS_TOTAL_C);

            _blockLogPath = Path.Combine(MyDocumentsPath, LogsFolderName,
            $"P{ExpEnvironment.PTC_NUM}-{_technique}", ExpStrs.BLOCKS_C);

            // Default (will set based on the task type)
            //Output.Conlog<ExperiLogger>(taskType.ToString());
            switch (taskType)
            {
                case TaskType.ONE_OBJ_ONE_FUNC:
                    {
                        _detailTrialLogWriter = MIO.PrepareFile<SOSFTrialLog>(_detailTrialLogPath, ExpStrs.SOSF);
                    }
                    break;
                case TaskType.ONE_OBJ_MULTI_FUNC:
                    {
                        _detailTrialLogWriter = MIO.PrepareFile<SOMFTrialLog>(_detailTrialLogPath, ExpStrs.SOMF);
                    }
                    break;
                case TaskType.MULTI_OBJ_ONE_FUNC:
                    {
                        _detailTrialLogWriter = MIO.PrepareFile<MOSFTrialLog>(_detailTrialLogPath, ExpStrs.MOSF);
                    }
                    break;
                case TaskType.MULTI_OBJ_MULTI_FUNC:
                    {
                        _detailTrialLogWriter = MIO.PrepareFile<MOMFTrialLong>(_detailTrialLogPath, ExpStrs.MOMF);
                    }
                    break;
            }

            // Create total log if not exists
            _totalTrialLogWriter = MIO.PrepareFile<TotalTrialLog>(_totalTrialLogPath, ExpStrs.TRIALS_TOTAL_S);

            // Create block log if not exists
            _blockLogWriter = MIO.PrepareFile<BlockLog>(_blockLogPath, ExpStrs.BLOCKS_S);

        }

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

        public static void StartTrialCursorLog(int trialId, int trialNum)
        {
            _activeTrialId = trialId;
            _trialCursorRecords[_activeTrialId] = new List<PositionRecord>();

            _cursorLogFilePath = Path.Combine(
                MyDocumentsPath, LogsFolderName,
                $"P{ExpEnvironment.PTC_NUM}-{_technique}", ExpStrs.CURSOR_C, $"trial-id{trialId}-n{trialNum}-{ExpStrs.CURSOR_S}"
            );

            _cursorLogWriter = MIO.PrepareFileWithHeader<PositionRecord>(_cursorLogFilePath, PositionRecord.GetHeader());
        }

        public static void StartTrialLogs(int trialNum, int trialId, double targetWidthMM, double distanceMM, Point startPos, Point targetPos)
        {
            String gestureFileName = $"trial-{trialNum}-#{trialId}-gestures-.txt";


            string gesturesFilePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Multi.Cursor.Logs", $"P{ExpEnvironment.PTC_NUM}-{_technique}", gestureFileName
            );


            _gestureFileLog = new LoggerConfiguration()
                    .WriteTo.Async(a => a.File(gesturesFilePath, rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:HH:mm:ss.fff} {Message:lj}{NewLine}"))
                    .CreateLogger();



            // Enter log info
            _gestureFileLog.Information($"TgtW: {targetWidthMM}, Dist: {distanceMM}, StPos: {startPos.Str()}, TgPos: {targetPos.Str()}");
        }

        public static void LogGestureEvent(string message)
        {
            //_gestureFileLog.Information(message);
        }

        public static void LogTrialMessage(string message)
        {
            _blockFileLog.Information(message);
        }

        public static void LogMultipleObjTrialTimes(TrialRecord trialRecord)
        {
            // For now. Later we put detailed log
            _blockFileLog.Information($"Start Release   -> Area Press:   {trialRecord.GetDuration(ExpStrs.STR_RELEASE, ExpStrs.ARA_PRESS)}");
        }

        public static void LogSOSFTrial(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
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

            MIO.WriteTrialLog(log, _detailTrialLogPath, _detailTrialLogWriter);
            //_trialLogWriter?.Dispose();

            LogTotalTrialTime(blockNum, trialNum, trial, trialRecord);
        }

        public static void LogMOSFTrial(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
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

                    for (int i = 1; i <= trial.NObjects; i++)
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
                            log, $"obj{i}rl_obj{i + 1}pr",
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


            MIO.WriteTrialLog(log, _detailTrialLogPath, _detailTrialLogWriter);
            //_trialLogWriter?.Dispose();

            LogTotalTrialTime(blockNum, trialNum, trial, trialRecord);
        }

        public static void LogSOMFTrial(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
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

                    log.pnlnt_fun1nt = trialRecord.GetNthSeqDuration(ExpStrs.PNL_ENTER, ExpStrs.FUN_ENTER, 1);

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

            MIO.WriteTrialLog(log, _detailTrialLogPath, _detailTrialLogWriter);

            LogTotalTrialTime(blockNum, trialNum, trial, trialRecord);
        }

        public static void LogMOMFTrial(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
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

            MIO.WriteTrialLog(log, _detailTrialLogPath, _detailTrialLogWriter);

            //_trialLogWriter?.Dispose();

            LogTotalTrialTime(blockNum, trialNum, trial, trialRecord);
        }

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

            MIO.WriteTrialLog(log, _totalTrialLogPath, _totalTrialLogWriter);

            StreamWriter writer = new StreamWriter(_cursorLogFilePath, append: true, Encoding.UTF8);
            writer.AutoFlush = true;
            foreach (var record in _trialCursorRecords[_activeTrialId])
            {
                writer.WriteLine($"{record.timestamp};{record.x};{record.y}");
            }
            writer.Dispose();
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
            log.exptype = block.ExpType.ToString().ToLower();
            log.n_trials = block.GetNumTrials();
            log.tsk_type = ExpStrs.TASKTYPE_ABBR[block.TaskType];
            log.n_fun = block.NFunctions;
            log.n_obj = block.NObjects;

            double avgTime = _trialTimes.Values.Average() / 1000;
            log.block_time = $"{avgTime:F2}";

            MIO.WriteTrialLog(log, _blockLogPath, _blockLogWriter);

        }

        public static void LogCursorPosition(Point cursorPos)
        {
            _trialCursorRecords[_activeTrialId].Add(new PositionRecord(cursorPos.X, cursorPos.Y));
        }

        private static void Dispose()
        {
            _detailTrialLogWriter?.Dispose();
            _detailTrialLogWriter = null;
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
            }
            else
            {
                //Output.Conlog<ExperiLogger>($"Error: Field '{fieldName}' not found.");
            }
        }

    }
}
