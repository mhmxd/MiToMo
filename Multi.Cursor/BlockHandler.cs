using Common.Constants;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static Common.Constants.ExpEnums;

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
            this.TrialInfo(ExpStrs.MINOR_LINE);

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

        private void TrialInfo(object mINOR_LINE)
        {
            throw new NotImplementedException();
        }

        public virtual void ShowActiveTrial()
        {
            this.TrialInfo(ExpStrs.MINOR_LINE);
            this.TrialInfo($"Showing " + _activeTrial.ToStr());

            LogEvent(ExpStrs.TRIAL_SHOW, _activeTrial.Id);

            // Start logging cursor positions
            ExperiLogger.StartTrialCursorLog(_activeTrial.Id);

            // Rest in overriding classes
        }

        public virtual void EndActiveTrial(Result result)
        {
            this.TrialInfo($"Trial#{_activeTrial.Id} completed: {result}");
            this.TrialInfo(ExpStrs.MAJOR_LINE);
            _activeTrialRecord.Result = result;
            LogEvent(ExpStrs.TRIAL_END, _activeTrial.Id); // Log the trial end timestamp
            _mainWindow.DeactivateAuxWindow(); // Deactivate the aux window

            switch (result)
            {
                case Result.HIT:
                    Sounder.PlayHit();
                    double trialTime = GetDuration(ExpStrs.STR_RELEASE + "_1", ExpStrs.TRIAL_END);
                    _activeTrialRecord.AddTime(ExpStrs.TRIAL_TIME, trialTime);
                    
                    break;
                case Result.MISS:
                    Sounder.PlayTargetMiss();

                    _activeBlock.ShuffleBackTrial(_activeTrialNum);
                    _trialRecords[_activeTrial.Id].ClearTimestamps();
                    _trialRecords[_activeTrial.Id].ResetStates();
                    break;
            }

            //-- Log
            switch (ExpStrs.TASKTYPE_ABBR[_activeTrial.TaskType])
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
            LogEvent(ExpStrs.MAIN_WIN_PRESS);

            if (!IsStartClicked()) // Start button not clicked yet
            {
                Sounder.PlayStartMiss();
            }
            else
            {
                EndActiveTrial(Result.MISS);
            }

            //e.Handled = true; // Mark the event as handled to prevent further processing
        }
        public virtual void OnMainWindowMouseMove(Object sender, MouseEventArgs e)
        {
            LogEventOnce(ExpStrs.FIRST_MOVE);

            // Log cursor movement
            ExperiLogger.LogCursorPosition(e.GetPosition(_mainWindow.Owner));
        }

        public virtual void OnMainWindowMouseUp(Object sender, MouseButtonEventArgs e)
        {
            if (IsStartPressed() && !IsStartClicked()) // Start button not clicked yet
            {
                Sounder.PlayStartMiss();
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public void OnAuxWindowMouseEnter(Side side, Object sender, MouseEventArgs e)
        {
            LogEvent(ExpStrs.PNL_ENTER, side.ToString().ToLower());
        }

        public void OnAuxWindowMouseDown(Side side, Object sender, MouseButtonEventArgs e)
        {
            LogEvent(ExpStrs.PNL_PRESS, side.ToString().ToLower());

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
            LogEvent(ExpStrs.PNL_RELEASE, side.ToString().ToLower());

            if (IsStartPressed()) // Pressed in Start, released in aux window
            {
                EndActiveTrial(Result.MISS);
            }

            e.Handled = true;
        }

        public void OnAuxWindowMouseExit(Side side, Object sender, MouseEventArgs e)
        {
            LogEvent(ExpStrs.PNL_EXIT, side.ToString().ToLower());
        }

        public virtual void OnObjectMouseEnter(Object sender, MouseEventArgs e)
        {
            // If the last timestamp was ARA_EXIT, remove that
            if (_activeTrialRecord.GetLastTrialEventType() == ExpStrs.ARA_EXIT) _activeTrialRecord.RemoveLastTimestamp();
            var objId = (int)((FrameworkElement)sender).Tag;

            // Add the id to the list of visited if not already there (will use the index for the order of visit)
            LogEvent(ExpStrs.OBJ_ENTER, objId);

            // Log the event
            LogEvent(ExpStrs.OBJ_ENTER, objId);
        }

        public virtual void OnObjectMouseLeave(Object sender, MouseEventArgs e)
        {
            var objId = (int)((FrameworkElement)sender).Tag;
            LogEvent(ExpStrs.OBJ_EXIT, objId);
        }

        public virtual void OnObjectMouseDown(Object sender, MouseButtonEventArgs e)
        {
            var objId = (int)((FrameworkElement)sender).Tag;
            LogEvent(ExpStrs.OBJ_PRESS, objId);

            // Rest of the handling is done in the derived classes
        }

        public virtual void OnObjectMouseUp(Object sender, MouseButtonEventArgs e)
        {
            var objId = (int)((FrameworkElement)sender).Tag;
            LogEvent(ExpStrs.OBJ_RELEASE, objId);

            // Rest of the handling is done in the derived classes
        }

        //---- Object area
        public virtual void OnObjectAreaMouseEnter(Object sender, MouseEventArgs e)
        {
            // Only log if entered from outside (NOT from the object)
            if (_activeTrialRecord.GetLastTrialEventType() != ExpStrs.OBJ_EXIT) LogEvent(ExpStrs.ARA_ENTER);
        }

        public virtual void OnObjectAreaMouseDown(Object sender, MouseButtonEventArgs e)
        {

            if (!IsStartClicked()) // Start button not clicked yet
            {
                Sounder.PlayStartMiss();
                e.Handled = true; // Mark the event as handled to prevent further processing
                return;
            }

            if (!_activeTrialRecord.AreAllObjectsApplied())
            {
                e.Handled = true; // Mark the event as handled to prevent further processing
                EndActiveTrial(Result.MISS);
            }

            LogEvent(ExpStrs.ARA_PRESS);

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public virtual void OnObjectAreaMouseUp(Object sender, MouseButtonEventArgs e)
        {
            LogEvent(ExpStrs.ARA_RELEASE);

            if (!IsStartClicked())
            {
                this.TrialInfo($"Start wasn't clicked");
                Sounder.PlayStartMiss();
                e.Handled = true; // Mark the event as handled to prevent further processing
                return; // Do nothing if start button was not clicked
            }

            if (_activeTrialRecord.AreAllObjectsApplied())
            {
                EndActiveTrial(Result.HIT);
            }
            else
            {
                this.TrialInfo($"Not all objects applied");
                EndActiveTrial(Result.MISS);
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public virtual void OnObjectAreaMouseExit(Object sender, MouseEventArgs e)
        {
            // Will be later removed if entered the object
            LogEvent(ExpStrs.ARA_EXIT);
        }

        //---- Function
        public virtual void OnFunctionMouseEnter(Object sender, MouseEventArgs e)
        {
            // Add the id to the list of visited if not already there (will use the index for the order of visit)
            int funId = (int)((FrameworkElement)sender).Tag;
            LogEvent(ExpStrs.FUN_ENTER, funId);
        }

        public virtual void OnFunctionMouseDown(Object sender, MouseButtonEventArgs e)
        {
            int funId = (int)((FrameworkElement)sender).Tag;
            LogEvent(ExpStrs.FUN_PRESS, funId);

            if (!IsStartClicked()) // Start button not clicked yet
            {
                Sounder.PlayStartMiss();
                e.Handled = true; // Mark the event as handled to prevent further processing
                return;
            }

            if (_activeTrial.Technique.GetDevice() == Technique.TOMO) // No function pressing in TOMO
            {
                EndActiveTrial(Result.MISS);
                e.Handled = true;
                return;
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }
        public virtual void OnFunctionMouseUp(Object sender, MouseButtonEventArgs e)
        {
            int funId = (int)((FrameworkElement)sender).Tag;
            LogEvent(ExpStrs.FUN_RELEASE, funId);

            // Rest of the handling is done in the derived classes
        }

        public virtual void OnFunctionMouseExit(Object sender, MouseEventArgs e)
        {
            int funId = (int)((FrameworkElement)sender).Tag;
            LogEvent(ExpStrs.FUN_EXIT, funId);
        }

        public abstract void OnNonTargetMouseDown(Object sender, MouseButtonEventArgs e);

        public void OnStartButtonMouseEnter(Object sender, MouseEventArgs e)
        {
            LogEvent(ExpStrs.STR_ENTER);
        }

        public void OnStartButtonMouseDown(Object sender, MouseButtonEventArgs e)
        {
            LogEvent(ExpStrs.STR_PRESS);
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TrialEventsToString()}");

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public void OnStartButtonMouseUp(Object sender, MouseButtonEventArgs e)
        {
            LogEvent(ExpStrs.STR_RELEASE);
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TrialEventsToString()}");

            var startButtonPressed = GetEventCount(ExpStrs.STR_PRESS) > 0;

            if (startButtonPressed)
            {
                _mainWindow.RemoveStartTrialButton();
                //UpdateScene(); // Temp (for measuring time)
            }
            else // Pressed outside the button => miss
            {
                EndActiveTrial(Result.MISS);
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public void OnStartButtonMouseExit(Object sender, MouseEventArgs e)
        {
            LogEvent(ExpStrs.STR_EXIT);
        }

        public virtual void OnFunctionMarked(int funId)
        {
            _activeTrialRecord.MarkFunction(funId);
            LogEvent(ExpStrs.FUN_MARKED, funId.ToString());
        }

        public virtual void OnFunctionUnmarked(int funId)
        {
            _activeTrialRecord.UnmarkFunction(funId);
            LogEvent(ExpStrs.FUN_DEMARKED, funId.ToString());
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
                    case ButtonState.SELECTED:
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
                    case ButtonState.SELECTED:
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

        public virtual void IndexTap()
        {
            if (_activeTrial.Technique == Technique.TOMO_SWIPE) // Wrong technique for thumb tap
            {
                EndActiveTrial(Result.MISS);
                return;
            }

            //-- TAP:

            if (!IsStartClicked())
            {
                return; // Do nothing if Start was not clicked
            }

            Side correspondingSide = Side.Top;
            var funcOnCorrespondingSide = _activeTrial.FuncSide == correspondingSide;

            if (funcOnCorrespondingSide)
            {
                LogEvent(ExpStrs.PNL_SELECT);
                _mainWindow.ActivateAuxWindowMarker(correspondingSide);
            }
            else
            {
                EndActiveTrial(Result.MISS);
            }
        }

        public void IndexMove(double dX, double dY)
        {

        }

        public void IndexMove(TouchPoint indPoint)
        {
            if (_mainWindow.IsAuxWindowActivated(_activeTrial.FuncSide))
            {
                LogEventOnce(ExpStrs.FLICK); // First flick after activation
                _mainWindow?.MoveMarker(indPoint, OnFunctionMarked, OnFunctionUnmarked);
            }
            
        }

        public void IndexUp()
        {
            _mainWindow.StopAuxNavigator();
            LogEvent(ExpStrs.JoinUs(ExpStrs.INDEX, ExpStrs.UP));
        }

        public virtual void ThumbSwipe(Direction dir)
        {
            if (_activeTrial.Technique != Technique.TOMO_SWIPE) // Wrong technique for swipe
            {
                EndActiveTrial(Result.MISS);
                return;
            }

            //-- SWIPE:

            if (!IsStartClicked())
            {
                return; // Do nothing if Start was not clicked
            }

            var allObjSelected = _nSelectedObjects == _activeTrialRecord.Objects.Count;
            var dirMatchesSide = dir switch
            {
                Direction.Left => _activeTrial.FuncSide == Side.Left,
                Direction.Right => _activeTrial.FuncSide == Side.Right,
                Direction.Up => _activeTrial.FuncSide == Side.Top,
                _ => false
            };

            var dirOppositeSide = dir switch
            {
                Direction.Left => _activeTrial.FuncSide == Side.Right,
                Direction.Right => _activeTrial.FuncSide == Side.Left,
                Direction.Down => _activeTrial.FuncSide == Side.Top,
                _ => false
            };

            if (dirMatchesSide)
            {
                LogEvent(ExpStrs.PNL_SELECT);
                _mainWindow.ActivateAuxWindowMarker(_activeTrial.FuncSide);
            }
            else
            {
                EndActiveTrial(Result.MISS);
            }
        }

        public virtual void ThumbTap(long downInstant, long upInstant)
        {
            if (_activeTrial.Technique == Technique.TOMO_SWIPE) // Wrong technique for thumb tap
            {
                EndActiveTrial(Result.MISS);
                return;
            }

            //-- TAP:

            if (!IsStartClicked())
            {
                return; // Do nothing if Start was not clicked
            }

            Side correspondingSide = Side.Left;
            var funcOnCorrespondingSide = _activeTrial.FuncSide == correspondingSide;

            if (funcOnCorrespondingSide)
            {
                LogEvent(ExpStrs.PNL_SELECT);
                _mainWindow.ActivateAuxWindowMarker(correspondingSide);
            }
            else
            {
                EndActiveTrial(Result.MISS);
            }
        }

        public void ThumbMove(TouchPoint thumbPoint)
        {
            // Nothing for now
        }

        public void ThumbUp()
        {
            // Nothing for now
        }

        public virtual void MiddleTap()
        {
            if (_activeTrial.Technique == Technique.TOMO_SWIPE) // Wrong technique for thumb tap
            {
                EndActiveTrial(Result.MISS);
                return;
            }

            //-- TAP:

            if (!IsStartClicked())
            {
                return; // Do nothing if Start was not clicked
            }

            Side correspondingSide = Side.Right;
            var funcOnCorrespondingSide = _activeTrial.FuncSide == correspondingSide;

            if (funcOnCorrespondingSide)
            {
                LogEvent(ExpStrs.PNL_SELECT);
                _mainWindow.ActivateAuxWindowMarker(correspondingSide);
            }
            else
            {
                EndActiveTrial(Result.MISS);
            }
        }

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
        //    if (what == ExpStrs.FUN)
        //    {
        //        // Check the Id in the visited list. If visited, log the event.
        //        if (!_functionsVisitMap.Contains(id)) // Function NOT visited before
        //        {
        //            _functionsVisitMap.Add(id);
        //        }

        //        int visitIndex = _functionsVisitMap.IndexOf(id);
        //        LogEvent(ExpStrs.GetIndexedStr(type, visitIndex + 1)); // Use 1-based indexing
        //    }

        //    if (what == ExpStrs.OBJ)
        //    {
        //        // Check the Id in the visited list. If visited, log the event.
        //        if (!_objectsVisitMap.Contains(id)) // Function NOT visited before
        //        {
        //            _objectsVisitMap.Add(id);
        //        }

        //        int visitIndex = _objectsVisitMap.IndexOf(id);
        //        LogEvent(ExpStrs.GetIndexedStr(type, visitIndex + 1)); // Use 1-based indexing
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

        //    string logStr = ExpStrs.GetCountedStr(type, _trialRecords[_activeTrial.Id].EventCounts[type]);
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
                if (trial != null && _trialRecords[trialId].HasTimestamp(ExpStrs.TRIAL_TIME))
                {
                    if (trial.DistRangeMM.Label == ExpStrs.SHORT_DIST)
                        shortDistTimes.Add(_trialRecords[trialId].GetTime(ExpStrs.TRIAL_TIME));
                    if (trial.DistRangeMM.Label == ExpStrs.MID_DIST)
                        midDistTimes.Add(_trialRecords[trialId].GetTime(ExpStrs.TRIAL_TIME));
                    if (trial.DistRangeMM.Label == ExpStrs.LONG_DIST)
                        longDistTimes.Add(_trialRecords[trialId].GetTime(ExpStrs.TRIAL_TIME));
                }
            }

            // Log the averages using ExperiLogger
            double shortDistAvg = shortDistTimes.Avg();
            double midDistAvg = midDistTimes.Avg();
            double lonDistAvg = longDistTimes.Avg();
            //ExperiLogger.LogTrialMessage($"Average Time per AvgDistanceMM --- " +
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
            return GetEventCount(ExpStrs.STR_PRESS) > 0;
        }

        protected bool IsStartClicked()
        {
            return GetEventCount(ExpStrs.STR_RELEASE) > 0;
        }

        protected bool WasObjectPressed(int objId)
        {
            this.TrialInfo($"Last event: {_activeTrialRecord.GetBeforeLastTrialEvent().ToString()}");
            return _activeTrialRecord.GetEventIndex(ExpStrs.OBJ_PRESS) != -1;
        }
    }

}
