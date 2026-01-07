using Common.Constants;
using Common.Logs;
using Common.Settings;
using Serilog;
using Serilog.Core;
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
        // Set for each log (in constructor)
        private static string _sosfTrialLogFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "SubTask.PanelNavigation.Logs", "sosf_trial_log"
        );
        private static string _totalLogFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "SubTask.PanelNavigation.Logs", "total_trial_log"
        );
        private static string _blockLogFilePath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "SubTask.PanelNavigation.Logs", "blocks_log"
        );

        private static string _cursorLogFilePath = ""; // Will be set when starting trial cursor log

        private static Logger _gestureFileLog;
        private static Logger _blockFileLog;

        private static StreamWriter _detailTrialLogWriter;
        private static StreamWriter _totalTrialLogWriter;
        private static StreamWriter _cursorLogWriter;
        private static StreamWriter _blockLogWriter;

        private static Dictionary<string, int> _trialLogs = new Dictionary<string, int>();

        private static int _ptcId = 0; // Participant ID
        private static Technique _technique = Technique.MOUSE; // Technique

        private static bool _headerWritten = false;

        private static Dictionary<int, int> _trialTimes = new Dictionary<int, int>();

        private static Dictionary<int, List<CursorRecord>> _trialCursorRecords = new Dictionary<int, List<CursorRecord>>();
        private static int _activeTrialId = -1;

        //public static void Init(int participantId, Technique tech)
        //{
        //    _ptcId = participantId;
        //    _technique = tech;

        //    bool fileExists = File.Exists(_sosfTrialLogFilePath);
        //    bool fileIsEmpty = !fileExists || new FileInfo(_sosfTrialLogFilePath).Length == 0;

        //    _detailTrialLogWriter = new StreamWriter(_sosfTrialLogFilePath, append: true, Encoding.UTF8);

        //    if (fileIsEmpty)
        //    {
        //        WriteHeader<TrialLog>();
        //    }
        //}

        public static void Init(Technique tech)
        {
            _ptcId = ExpPtc.PTC_NUM;
            _technique = tech;

            _detailTrialLogWriter = PrepareFile<DetailTrialLog>(_sosfTrialLogFilePath);


            //_detailTrialLogWriter.AutoFlush = true;

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
        //        "SubTask.PanelNavigation.Logs", $"{_ptcId}-{_technique}", blockFileName
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

        public static void StartTrialCursorLog(int trialId, int trialNum)
        {
            //string timestamp = DateTime.Now.ToString("dd-MM-yyyy_HH-mm");
            //String cursorFileName = $"trial-#{trialId}-cursor-{timestamp}.txt";
            //string cursorFilePath = System.IO.Path.Combine(
            //    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            //    "SubTask.PanelNavigation.Logs", $"{_ptcId}-{_technique}", "Cursor", cursorFileName
            //);

            _activeTrialId = trialId;
            _trialCursorRecords[_activeTrialId] = new List<CursorRecord>();

            _cursorLogFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "SubTask.PanelNavigation.Logs", $"{_ptcId}-{_technique}", "Cursor", $"trial{trialId}-{trialNum}-cursor-log"
            );

            PrepareFileWithHeader<CursorRecord>(ref _cursorLogFilePath, _cursorLogWriter, CursorRecord.GetHeader());
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
                "SubTask.PanelNavigation.Logs", $"{_ptcId}-{_technique}", gestureFileName
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
            //log.func_width = trial.GetFunctionWidthMM();
            //log.n_obj = trial.NObjects;
            log.n_fun = trial.GetNumFunctions();
            //log.dist_lvl = trial.DistRangeMM.Label.Split('-')[0].ToLower();
            //log.dist = $"{trialRecord.AvgDistanceMM:F2}";
            log.result = (int)trialRecord.Result;
        }

        public static void LogDetailTrial(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            string logFilePath = _sosfTrialLogFilePath; // Passed to the writer

            Output.Conlog<ExperiLogger>("Logging Trial");
            DetailTrialLog log = new DetailTrialLog();

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

                case Technique.TOMO_TAP:
                    log.strrl_fngup = trialRecord.GetLastSeqDuration(ExpStrs.STR_RELEASE, ExpStrs.UP);
                    log.fngup_fngdn = trialRecord.GetLastSeqDuration(ExpStrs.UP, ExpStrs.TAP_DOWN);
                    log.fngdn_fngup = trialRecord.GetLastSeqDuration(ExpStrs.TAP_DOWN, ExpStrs.TAP_UP);
                    log.tap_xlen = trialRecord.GetLength(ExpStrs.TAP_XLEN).ToString("F2");
                    log.tap_ylen = trialRecord.GetLength(ExpStrs.TAP_YLEN).ToString("F2");
                    break;

                case Technique.TOMO_SWIPE:
                    log.strrl_fngmv = trialRecord.GetLastSeqDuration(ExpStrs.STR_RELEASE, ExpStrs.SWIPE_START);
                    log.fngmv_swpthr = trialRecord.GetLastSeqDuration(ExpStrs.SWIPE_START, ExpStrs.SWIPE_END);
                    log.swipe_xlen = trialRecord.GetLength(ExpStrs.SWIPE_XLEN).ToString("F2");
                    log.swipe_ylen = trialRecord.GetLength(ExpStrs.SWIPE_YLEN).ToString("F2");
                    break;
            }

            // Testing
            //Output.Conlog<ExperiLogger>(trialRecord.TrialEventsToString());
            //Output.Conlog<ExperiLogger>(log.ToString());

            WriteTrialLog(log, logFilePath, _detailTrialLogWriter);
            //_detailTrialLogWriter?.Dispose();

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

            log.gesture_start_time = trialRecord.GetDurationToGestureStart(ExpStrs.STR_RELEASE, trial.Technique);
            log.gesture_duration = trialRecord.GetGestureDuration(trial.Technique);

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


        public static void LogBlockTime(Block block)
        {
            BlockLog log = new BlockLog();

            log.ptc = block.PtcNum;
            log.id = block.Id;
            log.tech = block.Technique.ToString().ToLower();
            log.cmplx = block.Complexity.ToString().ToLower();
            log.n_fun = 1;
            log.n_trials = block.GetNumTrials();

            double avgTime = _trialTimes.Values.Average() / 1000;
            log.block_time = $"{avgTime:F2}";

            WriteTrialLog(log, _blockLogFilePath, _blockLogWriter);

        }

        private static void WriteHeader<T>(StreamWriter streamWriter)
        {
            //var fields = typeof(T).GetFields();
            //var headers = fields.Select(f => f.Name);
            //_detailTrialLogWriter.WriteLine(string.Join(";", headers));

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

        public static void LogCursorPosition(Point cursorPos)
        {
            _trialCursorRecords[_activeTrialId].Add(new CursorRecord(cursorPos.X, cursorPos.Y));
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
                Console.WriteLine($"Successfully set field '{fieldName}' to {newValue}.");
            }
            else
            {
                Console.WriteLine($"Error: Field '{fieldName}' not found.");
            }
        }

    }
}
