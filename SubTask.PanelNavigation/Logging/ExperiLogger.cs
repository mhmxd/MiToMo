using Common.Settings;
using MathNet.Numerics;
using Serilog;
using Serilog.Core;
using SubTask.PanelNavigation.Logging;
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

        public static void LogDetailTrial(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            string logFilePath = _sosfTrialLogFilePath; // Passed to the writer

            Output.Conlog<ExperiLogger>("Logging Trial");
            DetailTrialLog log = new DetailTrialLog(blockNum, trialNum, trial, trialRecord);

            // Information
            //FillTrialInfo(log, blockNum, trialNum, trial, trialRecord);

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
            //Output.Conlog<ExperiLogger>(trialRecord.TrialEventsToString());
            //Output.Conlog<ExperiLogger>(log.ToString());

            WriteTrialLog(log, logFilePath, _detailTrialLogWriter);
            //_detailTrialLogWriter?.Dispose();

            LogTotalTrialTime(blockNum, trialNum, trial, trialRecord);
        }

        private static void LogTotalTrialTime(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            TotalTrialLog log = new TotalTrialLog(blockNum, trialNum, trial, trialRecord);

            // Information
            //FillTrialInfo(log, blockNum, trialNum, trial, trialRecord);

            // Total time
            log.trial_time = trialRecord.GetDuration(Str.STR_RELEASE, Str.ARA_PRESS);
            _trialTimes[trial.Id] = log.trial_time;

            log.funcs_sel_time = trialRecord.GetDuration(Str.PNL_ENTER, Str.FUN_RELEASE);
            log.objs_sel_time = trialRecord.GetDuration(Str.ARA_ENTER, Str.OBJ_RELEASE);
            log.func_po_sel_time = trialRecord.GetDuration(Str.OBJ_RELEASE, Str.FUN_RELEASE);
            log.panel_sel_time = trialRecord.GetDuration(Str.STR_RELEASE, Str.PNL_SELECT);
            log.panel_nav_time = trialRecord.GetDuration(Str.PNL_SELECT, Str.OBJ_PRESS);

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
            _trialCursorRecords[_activeTrialId].Add(new CursorRecord(cursorPos));
        }

        //private static void WriteTotalTrialLog<T>(T totalTrialLog)
        //{
        //    //var fields = typeof(T).GetFields();
        //    //var values = fields.Select(f => f.GetValue(trialLog)?.ToString() ?? "");
        //    //_detailTrialLogWriter.WriteLine(string.Join(";", values));
        //    //_detailTrialLogWriter.Flush();

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
