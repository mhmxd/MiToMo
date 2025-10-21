using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static Multi.Cursor.Experiment;
using static Multi.Cursor.TrialRecord;
using static Tensorflow.TensorShapeProto.Types;

namespace Multi.Cursor
{

    public abstract class BlockHandler : IGestureHandler
    {
        //protected class CachedTrialPositions
        //{
        //    public int TargetId { get; set; }
        //    public List<Point> StartPositions { get; set; } = new List<Point>();
        //}

        // Attributes
        protected Dictionary<int, TrialRecord> _trialRecords = new Dictionary<int, TrialRecord>(); // Trial id -> Record
        protected MainWindow _mainWindow;
        protected Block _activeBlock;
        protected int _activeBlockNum;
        protected Trial _activeTrial;
        protected int _activeTrialNum = 0;
        protected TrialRecord _activeTrialRecord;
        protected int _nSelectedObjects = 0; // Number of clicked objects in the current trial

        protected List<int> _functionsVisitMap = new List<int>();
        protected List<int> _objectsVisitMap = new List<int>();

        protected Random _random = new Random();

        public abstract bool FindPositionsForActiveBlock();
        public abstract bool FindPositionsForTrial(Trial trial);
        public void BeginActiveBlock()
        {
            this.TrialInfo("------------------- Beginning block ----------------------------");
            this.TrialInfo(Str.MINOR_LINE);

            _activeTrialNum = 1;
            _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
            _activeTrialRecord = _trialRecords[_activeTrial.Id];
            this.TrialInfo($"Active block id: {_activeBlock.Id}");
            // Start the log
            //ExperiLogger.StartBlockLog(_activeBlock.Id, _activeBlock.GetBlockType(), _activeBlock.GetComplexity());

            // Update the main window label
            //this.TrialInfo($"nTrials = {_activeBlock.GetNumTrials()}");
            //_mainWindow.UpdateInfoLabel(_activeTrialNum, _activeBlock.GetNumTrials());

            // Show the Start Trial button
            //_mainWindow.ShowStartTrialButton(OnStartButtonMouseUp);
            
            // Clear the main window canvas (to add shapes)
            _mainWindow.ClearCanvas();
            _mainWindow.ResetAllAuxWindows();

            // Show layout
            //_mainWindow.SetupLayout(_activeBlock.GetComplexity());

            // Show the first trial
            ShowActiveTrial();
        }

        public abstract void ShowActiveTrial();
        public virtual void EndActiveTrial(Result result)
        {
            this.TrialInfo($"Trial#{_activeTrial.Id} completed: {result}");
            this.TrialInfo(Str.MAJOR_LINE);
            _activeTrialRecord.Result = result;
            LogEvent(Str.TRIAL_END, _activeTrial.Id); // Log the trial end timestamp
            _mainWindow.DeactivateAuxWindow(); // Deactivate the aux window

            switch (result)
            {
                case Result.HIT:
                    Sounder.PlayHit();
                    double trialTime = GetDuration(Str.STR_RELEASE + "_1", Str.TRIAL_END);
                    _activeTrialRecord.AddTime(Str.TRIAL_TIME, trialTime);
                    
                    break;
                case Result.MISS:
                    Sounder.PlayTargetMiss();

                    _activeBlock.ShuffleBackTrial(_activeTrialNum);
                    _trialRecords[_activeTrial.Id].ClearTimestamps();
                    _trialRecords[_activeTrial.Id].ResetStates();
                    break;
            }

            //-- Log
            switch (Str.TASKTYPE_ABBR[_activeTrial.TaskType])
            {
                case "sosf":
                    ExperiLogger.LogSOSFTrial(_activeBlockNum, _activeTrialNum, _activeTrial, _activeTrialRecord);
                    break;
                case "somf":
                    ExperiLogger.LogSOMFTrial(_activeBlockNum, _activeTrialNum, _activeTrial, _activeTrialRecord);
                    break;
                case "mosf":
                    ExperiLogger.LogMOSFTrial(_activeBlockNum, _activeTrialNum, _activeTrial, _activeTrialRecord);
                    break;
                case "momf":
                    ExperiLogger.LogMOMFTrial(_activeBlockNum, _activeTrialNum, _activeTrial, _activeTrialRecord);
                    break;

            }

            GoToNextTrial();
        }
        public abstract void GoToNextTrial();

        protected bool AreFunctionsRepeated()
        {
            List<int> functionIds = new List<int>();
            for (int i = 0; i < _activeBlock.Trials.Count - 1; i++)
            {
                functionIds = _trialRecords[_activeBlock.Trials[i].Id]?.GetFunctionIds();
                foreach (int id in _trialRecords[_activeBlock.Trials[i + 1].Id]?.GetFunctionIds())
                {
                    if (functionIds.Contains(id)) return true;
                }
            }

            return false;
        }

        protected bool DoesFirstTrialsFunInclMidBtn()
        {
            // Get the function Ids of first trial of each side
            bool leftChecked = false;
            bool rightChecked = false;
            bool topChecked = false;
            foreach (Trial trial in _activeBlock.Trials)
            {
                if (trial.FuncSide == Side.Left && !leftChecked)
                {
                    leftChecked = true;
                    List<int> functionIds = _trialRecords[trial.Id]?.GetFunctionIds();
                    int midBtnId = _mainWindow.GetMiddleButtonId(Side.Left);
                    if (functionIds.Contains(midBtnId)) return true;
                }

                if (trial.FuncSide == Side.Right && !rightChecked)
                {
                    rightChecked = true;
                    List<int> functionIds = _trialRecords[trial.Id]?.GetFunctionIds();
                    int midBtnId = _mainWindow.GetMiddleButtonId(Side.Right);
                    if (functionIds.Contains(midBtnId)) return true;
                }

                if (trial.FuncSide == Side.Top && !topChecked)
                {
                    topChecked = true;
                    List<int> functionIds = _trialRecords[trial.Id]?.GetFunctionIds();
                    int midBtnId = _mainWindow.GetMiddleButtonId(Side.Top);
                    if (functionIds.Contains(midBtnId)) return true;
                }
            }

            return false;

        }

        public virtual void OnMainWindowMouseDown(Object sender, MouseButtonEventArgs e)
        {
            LogEvent(Str.MAIN_WIN_PRESS);

            if (!IsStartClicked()) // Start button not clicked yet
            {
                Sounder.PlayStartMiss();
            } else
            {
                EndActiveTrial(Result.MISS);
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }
        public virtual void OnMainWindowMouseMove(Object sender, MouseEventArgs e)
        {
            LogEventOnce(Str.FIRST_MOVE);
        }

        public virtual void OnMainWindowMouseUp(Object sender, MouseButtonEventArgs e)
        {
            if (IsStartPressed()) // Pressed in Start, released in main window
            {
                EndActiveTrial(Result.MISS);
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public void OnAuxWindowMouseEnter(Side side, Object sender, MouseEventArgs e)
        {
            LogEvent(Str.PNL_ENTER, side.ToString().ToLower());
        }

        public void OnAuxWindowMouseDown(Side side, Object sender, MouseButtonEventArgs e)
        {
            LogEvent(Str.PNL_PRESS, side.ToString().ToLower());

            if (!IsStartClicked())
            {
                Sounder.PlayStartMiss();
            }
            else
            {
                EndActiveTrial(Result.MISS);
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public void OnAuxWindowMouseMove(Object sender, MouseEventArgs e)
        {
            // Nothing for now
        }
        public void OnAuxWindowMouseUp(Side side, Object sender, MouseButtonEventArgs e)
        {
            LogEvent(Str.PNL_PRESS, side.ToString().ToLower());

            if (IsStartPressed()) // Pressed in Start, released in aux window
            {
                EndActiveTrial(Result.MISS);
            }

            e.Handled = true;
        }

        public void OnAuxWindowMouseExit(Side side, Object sender, MouseEventArgs e)
        {
            LogEvent(Str.PNL_EXIT, side.ToString().ToLower());
        }

        public virtual void OnObjectMouseEnter(Object sender, MouseEventArgs e)
        {
            // If the last timestamp was ARA_EXIT, remove that
            if (_activeTrialRecord.GetLastTrialEventType() == Str.ARA_EXIT) _activeTrialRecord.RemoveLastTimestamp();
            var objId = (int)((FrameworkElement)sender).Tag;

            // Add the id to the list of visited if not already there (will use the index for the order of visit)
            LogEvent(Str.OBJ_ENTER, objId);

            // Log the event
            LogEvent(Str.OBJ_ENTER, objId);
        }

        public virtual void OnObjectMouseLeave(Object sender, MouseEventArgs e)
        {
            var objId = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.OBJ_EXIT, objId);
        }

        public virtual void OnObjectMouseDown(Object sender, MouseButtonEventArgs e)
        {
            var objId = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.OBJ_PRESS, objId);

            // Rest of the handling is done in the derived classes
        }

        public virtual void OnObjectMouseUp(Object sender, MouseButtonEventArgs e)
        {
            // Rest of the handling is done in the derived classes

            var objId = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.OBJ_RELEASE, objId);

            e.Handled = true;
        }

        //---- Object area
        public abstract void OnObjectAreaMouseEnter(Object sender, MouseEventArgs e);

        public abstract void OnObjectAreaMouseDown(Object sender, MouseButtonEventArgs e);

        public abstract void OnObjectAreaMouseUp(Object sender, MouseButtonEventArgs e);

        public abstract void OnObjectAreaMouseExit(Object sender, MouseEventArgs e);

        //---- Function
        public abstract void OnFunctionMouseEnter(Object sender, MouseEventArgs e);

        public virtual void OnFunctionMouseDown(Object sender, MouseButtonEventArgs e)
        {
            int funId = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.FUN_PRESS, funId);

            e.Handled = true; // Mark the event as handled to prevent further processing
        }
        public abstract void OnFunctionMouseUp(Object sender, MouseButtonEventArgs e);

        public abstract void OnFunctionMouseExit(Object sender, MouseEventArgs e);

        public abstract void OnNonTargetMouseDown(Object sender, MouseButtonEventArgs e);

        public void OnStartButtonMouseEnter(Object sender, MouseEventArgs e)
        {
            LogEvent(Str.STR_ENTER);
        }

        public void OnStartButtonMouseDown(Object sender, MouseButtonEventArgs e)
        {
            LogEvent(Str.STR_PRESS);
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TrialEventsToString()}");

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public void OnStartButtonMouseUp(Object sender, MouseButtonEventArgs e)
        {
            LogEvent(Str.STR_RELEASE);
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TrialEventsToString()}");

            var startButtonPressed = GetEventCount(Str.STR_PRESS) > 0;

            if (startButtonPressed)
            {
                _mainWindow.RemoveStartTrialButton();
            }
            else // Pressed outside the button => miss
            {
                EndActiveTrial(Result.MISS);
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public void OnStartButtonMouseExit(Object sender, MouseEventArgs e)
        {
            LogEvent(Str.STR_EXIT);
        }

        public virtual void OnFunctionMarked(int funId)
        {
            _activeTrialRecord.MarkFunction(funId);
            LogEvent(Str.FUN_MARKED, funId.ToString());
        }

        public virtual void OnFunctionUnmarked(int funId)
        {
            _activeTrialRecord.UnmarkFunction(funId);
            LogEvent(Str.FUN_DEMARKED, funId.ToString());
        }

        public void SetFunctionAsEnabled(int funcId)
        {
            _mainWindow.FillButtonInAuxWindow(
                _activeTrial.FuncSide,
                funcId, 
                Config.FUNCTION_ENABLED_COLOR);
        }

        public void SetFunctionAsDisabled(int funcId)
        {
            _mainWindow.FillButtonInAuxWindow(
                _activeTrial.FuncSide, 
                funcId, 
                Config.FUNCTION_DEFAULT_COLOR);
        }

        public void SetFunctionAsApplied(int funcId)
        {
            _activeTrialRecord.SetFunctionAsApplied(funcId);
        }

        protected void SetObjectAsDisabled(int objId)
        {
            _mainWindow.FillObject(objId, Config.OBJ_DEFAULT_COLOR);
        }

        public void UpdateScene()
        {
            foreach (var func in _activeTrialRecord.Functions)
            {
                Brush funcColor = Config.FUNCTION_DEFAULT_COLOR;
                //this.TrialInfo($"Function#{func.Id} state: {func.State}");
                switch (func.State)
                {
                    case ButtonState.MARKED:
                        funcColor = Config.FUNCTION_ENABLED_COLOR;
                        break;
                    case ButtonState.APPLIED:
                        funcColor = Config.FUNCTION_APPLIED_COLOR;
                        break;
                }

                _mainWindow.FillButtonInAuxWindow(_activeTrial.FuncSide, func.Id, funcColor);
            }

            foreach (var obj in _activeTrialRecord.Objects)
            {
                Brush objColor = Config.OBJ_DEFAULT_COLOR;
                switch (obj.State)
                {
                    case ButtonState.MARKED:
                        objColor = Config.OBJ_MARKED_COLOR;
                        break;
                    case ButtonState.APPLIED:
                        objColor = Config.OBJ_APPLIED_COLOR;
                        break;
                }

                _mainWindow.FillObject(obj.Id, objColor);
            }
        }

        public void LeftPress()
        {
            
        }

        public void RightPress()
        {
            
        }

        public void TopPress()
        {
            
        }

        public void LeftMove(double dX, double dY)
        {
            
        }

        public void IndexDown(TouchPoint indPoint)
        {
            
        }

        public abstract void IndexTap();

        public void IndexMove(double dX, double dY)
        {

        }

        public void IndexMove(TouchPoint indPoint)
        {
            if (_mainWindow.IsAuxWindowActivated(_activeTrial.FuncSide))
            {
                LogEventOnce(Str.FLICK); // First flick after activation
                _mainWindow?.MoveMarker(indPoint, OnFunctionMarked, OnFunctionUnmarked);
            }
            
        }

        public void IndexUp()
        {
            _mainWindow.StopAuxNavigator();
            LogEvent(Str.Join(Str.INDEX, Str.UP));
        }

        public abstract void ThumbSwipe(Direction dir);

        public abstract void ThumbTap(long downInstant, long upInstant);

        public void ThumbMove(TouchPoint thumbPoint)
        {
            // Nothing for now
        }

        public void ThumbUp()
        {
            // Nothing for now
        }

        public abstract void MiddleTap();

        public void RingTap()
        {
            
        }

        public void PinkyTap(Side loc)
        {
            
        }

        protected void LogEvent(string type, string id)
        {
            //if (_trialRecords[_activeTrial.Id].EventCounts.ContainsKey(type))
            //{
            //    _trialRecords[_activeTrial.Id].EventCounts[type]++;
            //}
            //else
            //{
            //    _trialRecords[_activeTrial.Id].EventCounts[type] = 1;
            //}

            //string timeKey = type + "_" + _trialRecords[_activeTrial.Id].EventCounts[type];
            _activeTrialRecord.RecordEvent(type, id); // Let them have the same name. We know the count from EventCounts

        }

        protected void LogEvent(string type, int id)
        {
            LogEvent(type, id.ToString());
        }

        protected void LogEvent(string type)
        {
            LogEvent(type, "");
        }

        //protected void LogEvent(string type, long eventTime)
        //{
        //    //if (_trialRecords[_activeTrial.Id].EventCounts.ContainsKey(type))
        //    //{
        //    //    _trialRecords[_activeTrial.Id].EventCounts[type]++;
        //    //}
        //    //else
        //    //{
        //    //    _trialRecords[_activeTrial.Id].EventCounts[type] = 1;
        //    //}

        //    //string timeKey = type + "_" + _trialRecords[_activeTrial.Id].EventCounts[type];
        //    _activeTrialRecord.RecordEvent(type);

        //}

        //protected void LogEventWithIndex(string type, int id)
        //{
        //    string what = type.Split('_')[0];
        //    this.TrialInfo($"What: {what}");
        //    if (what == Str.FUN)
        //    {
        //        // Check the Id in the visited list. If visited, log the event.
        //        if (!_functionsVisitMap.Contains(id)) // Function NOT visited before
        //        {
        //            _functionsVisitMap.Add(id);
        //        }

        //        int visitIndex = _functionsVisitMap.IndexOf(id);
        //        LogEvent(Str.GetIndexedStr(type, visitIndex + 1)); // Use 1-based indexing
        //    }

        //    if (what == Str.OBJ)
        //    {
        //        // Check the Id in the visited list. If visited, log the event.
        //        if (!_objectsVisitMap.Contains(id)) // Function NOT visited before
        //        {
        //            _objectsVisitMap.Add(id);
        //        }

        //        int visitIndex = _objectsVisitMap.IndexOf(id);
        //        LogEvent(Str.GetIndexedStr(type, visitIndex + 1)); // Use 1-based indexing
        //    }
        //}

        //protected void LogEventWithCount(string type)
        //{
        //    if (_trialRecords[_activeTrial.Id].EventCounts.ContainsKey(type))
        //    {
        //        _trialRecords[_activeTrial.Id].EventCounts[type]++;
        //    }
        //    else
        //    {
        //        _trialRecords[_activeTrial.Id].EventCounts[type] = 1;
        //    }

        //    string logStr = Str.GetCountedStr(type, _trialRecords[_activeTrial.Id].EventCounts[type]);
        //    _activeTrialRecord.RecordEvent(logStr); // Let them have the same name. We know the count from EventCounts
        //}

        protected void LogEventOnce(string type)
        {
            if (GetEventCount(type) == 0) // Not yet logged
            {
                LogEvent(type);
            }
        }

        protected int GetEventCount(string type)
        {
            //if (_trialRecords[_activeTrial.Id].EventCounts.ContainsKey(type))
            //{
            //    return _trialRecords[_activeTrial.Id].EventCounts[type];
            //}
            //return 0; // TrialEvent has not occurred

            return _activeTrialRecord.CountEvent(type);
            
        }

        protected double GetDuration(string begin, string end)
        {
            if (_activeTrialRecord.HasTimestamp(begin) && _activeTrialRecord.HasTimestamp(end))
            {
                return (_activeTrialRecord.GetFirstTime(end) - _activeTrialRecord.GetFirstTime(begin)) / 1000.0; // Convert to seconds
            }

            return 0;
        }


        public abstract void ThumbTap(Side side);

        public int GetMappedObjId(int funcId)
        {
            return _activeTrialRecord.FindMappedObjectId(funcId);
        }

        public void MarkAllObjects()
        {
            _activeTrialRecord.MarkAllObjects();
        }

        public void MarkMappedObject(int funcId)
        {
            switch (_activeBlock.GetFunctionType())
            {
                case TaskType.ONE_FUNCTION: // One function => mark all objects
                    MarkAllObjects();
                    break;
                case TaskType.MULTI_FUNCTION: // Multi function => mark the mapped object 
                    _activeTrialRecord.MarkMappedObject(funcId);
                    break;
            }
        }

        public int GetActiveTrialNum()
        {
            return _activeTrialNum;
        }

        public int GetNumTrialsInBlock()
        {
            return _activeBlock.GetNumTrials();
        }

        public TaskType GetBlockType()
        {
            return _activeBlock.GetBlockType();
        }

        public void LogAverageTimeOnDistances()
        {
            // Find the average trial Time from TrialRecord.Times
            List<double> shortDistTimes = new List<double>();
            List<double> midDistTimes = new List<double>();
            List<double> longDistTimes = new List<double>();

            foreach (int trialId in _trialRecords.Keys)
            {
                Trial trial = _activeBlock.GetTrialById(trialId);
                if (trial != null && _trialRecords[trialId].HasTime(Str.TRIAL_TIME))
                {
                    if (trial.DistRangeMM.Label == Str.SHORT_DIST)
                        shortDistTimes.Add(_trialRecords[trialId].GetTime(Str.TRIAL_TIME));
                    if (trial.DistRangeMM.Label == Str.MID_DIST)
                        midDistTimes.Add(_trialRecords[trialId].GetTime(Str.TRIAL_TIME));
                    if (trial.DistRangeMM.Label == Str.LONG_DIST)
                        longDistTimes.Add(_trialRecords[trialId].GetTime(Str.TRIAL_TIME));
                }
            }

            // Log the averages using ExperiLogger
            double shortDistAvg = shortDistTimes.Avg();
            double midDistAvg = midDistTimes.Avg();
            double lonDistAvg = longDistTimes.Avg();
            //ExperiLogger.LogTrialMessage($"Average Time per Distance --- " +
            //    $"Short({shortDistTimes.Count}): {shortDistAvg:F2}; " +
            //    $"Mid({midDistTimes.Count}): {midDistAvg:F2}; " +
            //    $"Long({longDistTimes.Count}): {lonDistAvg:F2}");
        }

        public void RecordToMoAction(Finger finger, string action)
        {
            LogEvent(action, finger.ToString().ToLower());
        }

        //public void RecordToMoAction(string action)
        //{
        //    LogEvent(action);
        //}

        protected bool IsStartPressed()
        {
            return GetEventCount(Str.STR_PRESS) > 0;
        }

        protected bool IsStartClicked()
        {
            return GetEventCount(Str.STR_RELEASE) > 0;
        }
    }

}
