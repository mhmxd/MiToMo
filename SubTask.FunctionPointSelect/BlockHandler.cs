using Common.Constants;
using Common.Helpers;
using CommonUI;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static Common.Constants.ExpEnums;
using TouchPoint = CommonUI.TouchPoint;

namespace SubTask.FunctionPointSelect
{
    public class BlockHandler
    {
        //protected class CachedTrialPositions
        //{
        //    public int TargetId { get; set; }
        //    public List<Point> StartPositions { get; set; } = new List<Point>();
        //}

        // Attributes
        protected MainWindow _mainWindow;
        protected Block _activeBlock;
        protected int _activeBlockNum;
        protected Trial _activeTrial;
        protected int _activeTrialNum = 0;
        protected TrialRecord _activeTrialRecord;
        protected int _nSelectedObjects = 0; // Number of clicked objects in the current trial

        protected List<int> _functionsVisitMap = new List<int>();
        protected List<int> _objectsVisitMap = new List<int>();
        protected Dictionary<int, TrialRecord> _trialRecords = new Dictionary<int, TrialRecord>(); // Trial id -> Record

        protected Random _random = new Random();

        public BlockHandler(MainWindow mainWindow, Block activeBlock, int activeBlockNum)
        {
            _mainWindow = mainWindow;
            _activeBlock = activeBlock;
            _activeBlockNum = activeBlockNum;
        }

        public bool FindPositionsForActiveBlock()
        {
            foreach (Trial trial in _activeBlock.Trials)
            {
                if (!FindPositionsForTrial(trial))
                {
                    this.TrialInfo($"Failed to find positions for Trial#{trial.Id}");
                    return false; // If any trial fails, return false
                }
            }


            // If consecutive trials have the same function Ids or first trial's functions are over the middle button,
            // re-order them (so marker doesn't stay on the same function)
            int maxAttempts = 100;
            int attempt = 0;
            while (attempt < maxAttempts && AreFunctionsRepeated() && DoesFirstTrialsFunInclMidBtn())
            {
                _activeBlock.Trials.Shuffle();
                attempt++;
            }

            if (attempt == maxAttempts)
            {
                this.TrialInfo($"Warning: Could not eliminate repeated functions in consecutive trials after {maxAttempts} attempts.");
                return false;
            }

            return true;
        }

        public bool FindPositionsForTrial(Trial trial)
        {

            //this.TrialInfo(trial.ToStr());

            // Ensure TrialRecord exists for this trial
            if (!_trialRecords.ContainsKey(trial.Id))
            {
                _trialRecords[trial.Id] = new TrialRecord();
            }

            TFunction randFunc = _mainWindow.FindRandomFunction(trial.FuncSide, trial.GetFunctionWidth(0), trial.DistRangePX);
            this.TrialInfo($"Found functions: {randFunc}");
            _trialRecords[trial.Id].Functions.Add(randFunc);
            //_mainWindow.Dispatcher.Invoke(() =>
            //{
            //    int functionWidth = trial.GetFunctionWidth(0);
            //    _trialRecords[trial.Id].Functions.Add(
            //        _mainWindow.FindRandomFunction(trial.FuncSide, functionWidth, trial.DistRangePX)
            //    );
            //});



            // Find a position for the start button
            Rect StartBtnConstraintRect = _mainWindow.Dispatcher.Invoke(() =>
            {
                return _mainWindow.GetStartBtnConstraintRect();
            });

            (Point startCenter, double dist) = StartBtnConstraintRect.FindPointWithinDistRangeFromMultipleSources(
                _trialRecords[trial.Id].GetFunctionCenters(), trial.DistRangePX);


            if (startCenter.X == -1 && startCenter.Y == -1) // Failed to find a valid position 
            {
                this.TrialInfo($"No valid position found for object in Trial#{trial.Id}!");
                return false; // Return false to indicate failure
            }
            else
            {
                //this.TrialInfo($"Found object position: {startCenter.ToStr()}");

                // Get the top-left corner of the start button rectangle
                int startBtnW = UITools.MM2PX(_trialRecords[trial.Id].StartBtnRect.Width);
                int startBtnH = UITools.MM2PX(_trialRecords[trial.Id].StartBtnRect.Height);
                int startBtnHalfW = startBtnW / 2;
                Point startBtnPosition = startCenter.OffsetPosition(-startBtnHalfW);

                //this.TrialInfo($"Found object area position: {startBtnPosition.ToStr()}");

                _trialRecords[trial.Id].StartBtnRect = new Rect(
                        startBtnPosition.X,
                        startBtnPosition.Y,
                        startBtnW,
                        startBtnH);

                _trialRecords[trial.Id].DistanceMM = dist;

                return true;
            }
        }

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

        public virtual void ShowActiveTrial()
        {
            this.TrialInfo(ExpStrs.MINOR_LINE);
            this.TrialInfo($"Showing " + _activeTrial.ToStr());

            LogEvent(ExpStrs.TRIAL_SHOW, _activeTrial.Id);

            // Start logging cursor positions
            ExperiLogger.StartTrialCursorLog(_activeTrial.Id, _activeTrialNum);

            // Update the main window label
            //_mainWindow.UpdateInfoLabel(_activeTrialNum, _activeBlock.GetNumTrials());

            // Set the target window based on the trial's target side
            _mainWindow.SetTargetWindow(_activeTrial.FuncSide, OnAuxWindowMouseEnter, OnAuxWindowMouseExit, OnAuxWindowMouseDown, OnAuxWindowMouseUp);

            // Color the target button and set the handlers
            this.TrialInfo($"Function Id(s): {_activeTrialRecord.GetFunctionIds().ToStr()}");
            Brush funcDefaultColor = UIColors.COLOR_FUNCTION_DEFAULT;
            UpdateScene(); // (comment for measuring panel selection time)
            //_mainWindow.FillButtonInTargetWindow(
            //    _activeTrial.FuncSide, 
            //    _activeTrialRecord.FunctionId, 
            //    funcDefaultColor);

            _mainWindow.SetAuxButtonsHandlers(
                _activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds(),
                OnFunctionMouseEnter, this.OnFunctionMouseDown, this.OnFunctionMouseUp,
                OnFunctionMouseExit, this.OnNonTargetMouseDown);

            // Clear the main window canvas (to add shapes)
            _mainWindow.ClearCanvas();

            // Show the area
            //MouseEvents objAreaEvents = new MouseEvents(OnObjectAreaMouseEnter, OnObjectAreaMouseDown, OnObjectAreaMouseUp, OnObjectAreaMouseExit);
            //_mainWindow.ShowStartBtn(
            //    _activeTrialRecord.StartBtnRect,
            //    UIColors.COLOR_START_AVAILABLE,
            //    objAreaEvents);

            // Show objects
            //Brush objDefaultColor = UIColors.COLOR_OBJ_DEFAULT;
            //MouseEvents objectEvents = new MouseEvents(
            //    OnObjectMouseEnter, OnObjectMouseDown, OnObjectMouseUp, OnObjectMouseLeave);
            //_mainWindow.ShowObjects(
            //    _activeTrialRecord.Objects, objDefaultColor, objectEvents);

            // Show Start Trial button
            MouseEvents startButtonEvents = new MouseEvents(
                OnStartButtonMouseEnter, OnStartButtonMouseDown, OnStartButtonMouseUp, OnStartButtonMouseExit);
            _mainWindow.ShowStartBtn(
                _activeTrialRecord.StartBtnRect,
                UIColors.COLOR_START_INIT,
                startButtonEvents);
            //_mainWindow.ShowStartTrialButton(_activeTrialRecord.ObjectAreaRect, startButtonEvents);

            // Update info label
            _mainWindow.UpdateInfoLabel();
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
                    MSounder.PlayHit();

                    double trialTime = GetDuration(ExpStrs.STR_RELEASE + "_1", ExpStrs.TRIAL_END);
                    _activeTrialRecord.AddTime(ExpStrs.TRIAL_TIME, trialTime);
                    break;

                case Result.MISS:
                    MSounder.PlayTargetMiss();

                    _activeBlock.ShuffleBackTrial(_activeTrialNum);
                    _trialRecords[_activeTrial.Id].ClearTimestamps();
                    _trialRecords[_activeTrial.Id].ResetStates();
                    break;
            }

            //-- Log
            ExperiLogger.LogDetailTrial(_activeBlockNum, _activeTrialNum, _activeTrial, _activeTrialRecord);
            ExperiLogger.LogTotalTrialTime(_activeBlockNum, _activeTrialNum, _activeTrial, _activeTrialRecord);
            ExperiLogger.LogCursorRecords();

            GoToNextTrial();
        }
        public void GoToNextTrial()
        {
            if (_activeTrialNum < _activeBlock.Trials.Count)
            {
                //_mainWindow.ShowStartTrialButton(OnStartButtonMouseUp);
                _mainWindow.ResetAuxWindow(_activeTrial.FuncSide);
                _mainWindow.ClearCanvas();
                _activeTrialRecord.ClearTimestamps();
                _nSelectedObjects = 0; // Reset the number of selected objects
                _functionsVisitMap.Clear();
                _objectsVisitMap.Clear();

                _activeTrialNum++;
                _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
                _activeTrialRecord = _trialRecords[_activeTrial.Id];

                ShowActiveTrial();
            }
            else // Last trial of the block
            {
                // Log the avg times
                LogAverageTimeOnDistances();

                // Log block time
                ExperiLogger.LogBlockTime(_activeBlock);

                // Show end of block window
                BlockEndWindow blockEndWindow = new BlockEndWindow(_mainWindow.GoToNextBlock);
                blockEndWindow.Owner = _mainWindow;
                blockEndWindow.ShowDialog();
            }
        }

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
                MSounder.PlayStartMiss();
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
            ExperiLogger.RecordCursorPosition(e.GetPosition(_mainWindow.Owner));
        }

        public virtual void OnMainWindowMouseUp(Object sender, MouseButtonEventArgs e)
        {
            LogEvent(ExpStrs.MAIN_WIN_RELEASE);

            if (IsStartPressed() && !IsStartClicked()) // Start button not clicked yet
            {
                MSounder.PlayStartMiss();
            }

            if (IsFuncPressed())
            {
                EndActiveTrial(Result.MISS);
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
                MSounder.PlayStartMiss();
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

            if (!IsStartClicked()) // Start button not clicked yet
            {
                MSounder.PlayStartMiss();
                e.Handled = true; // Mark the event as handled to prevent further processing
                return;
            }

            //if (_activeTrial.Technique.GetDevice() == Technique.TOMO) // No function pressing in TOMO
            //{
            //    EndActiveTrial(Result.MISS);
            //    e.Handled = true;
            //    return;
            //}

            LogEvent(ExpStrs.FUN_PRESS, funId);

            // Change state to APPLIED
            _activeTrialRecord.SetFunctionAsApplied(funId);
            UpdateScene();

            Mouse.Capture(null);  // Release any active capture (so other windows/elements can get the MouseUp)

            e.Handled = true; // Mark the event as handled to prevent further processing
        }
        public virtual void OnFunctionMouseUp(Object sender, MouseButtonEventArgs e)
        {
            int funcId = (int)((FrameworkElement)sender).Tag;
            LogEvent(ExpStrs.FUN_RELEASE, funcId);

            // If the trial has already ended, ignore further events
            if (_activeTrialRecord.GetLastTrialEventType() == ExpStrs.TRIAL_END)
            {
                e.Handled = true;
                return;
            }

            //if (!IsStartClicked())
            //{
            //    Sounder.PlayStartMiss();
            //    e.Handled = true; // Mark the event as handled to prevent further processing
            //    return; // Do nothing if start button was not clicked
            //}

            _activeTrialRecord.ApplyFunction(funcId, 1);
            UpdateScene();

            EndActiveTrial(Result.HIT);

            // Change obj's color only if all functions are selected
            //if (_activeTrialRecord.AreAllFunctionsApplied())
            //{
            //    _activeTrialRecord.ChangeObjectState(1, ButtonState.APPLIED);
            //}



            // Function id is sender's tag as int
            //var functionId = (int)((FrameworkElement)sender).Tag;
            //var device = Utils.GetDevice(_activeBlock.Technique);
            //var objectMarked = GetEventCount(ExpStrs.OBJ_RELEASE) > 0;

            //if (!objectMarked) // Technique doesn't matter here
            //{
            //    EndActiveTrial(Result.MISS);
            //    e.Handled = true; // Mark the event as handled to prevent further processing
            //    return; // Do nothing if object is not marked
            //}

            //-- Object is marked:
            //this.TrialInfo($"Events: {_activeTrialRecord.TrialEventsToString()}");
            //if (_activeTrial.Technique == Technique.MOUSE)
            //{
            //    _activeTrialRecord.ApplyFunction(functionId, 1);
            //    // Change obj's color only if all functions are selected
            //    if (_activeTrialRecord.AreAllFunctionsApplied())
            //    {
            //        _activeTrialRecord.ChangeObjectState(1, ButtonState.APPLIED);
            //    }

            //    UpdateScene();
            //}


            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public virtual void OnFunctionMouseExit(Object sender, MouseEventArgs e)
        {
            int funId = (int)((FrameworkElement)sender).Tag;
            LogEvent(ExpStrs.FUN_EXIT, funId);
        }

        public void OnNonTargetMouseDown(Object sender, MouseButtonEventArgs e)
        {
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TrialEventsToString()}");
            SButton clickedButton = sender as SButton;
            var functionId = (int)((FrameworkElement)sender).Tag;
            this.TrialInfo($"Non-function id: {functionId}");
            this.TrialInfo($"Trial Id: {_activeTrial.Id} | Obj: {this.GetHashCode()}");
            if (clickedButton != null && clickedButton.Tag is Dictionary<string, int>)
            {
                // Show neighbor IDs
                Dictionary<string, int> tag = clickedButton.Tag as Dictionary<string, int>;
                this.TrialInfo($"Clicked button ID: {clickedButton.Id}, Left: {tag["LeftId"]}, Right: {tag["RightId"]}, Top: {tag["TopId"]}, Bottom: {tag["BottomId"]}");
            }

            // It's always a miss
            EndActiveTrial(Result.MISS);

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

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
                // Change START to END and unavailable
                _mainWindow.ChangeStartButtonText(ExpStrs.END_CAP);
                _mainWindow.ChangeStartButtonColor(UIColors.DARK_ORANGE);

                // Enable function
                _activeTrialRecord.EnableFunction();

                // Update scene
                UpdateScene();
            }
            else // Press was outside the button => miss
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

            _activeTrialRecord.MarkObject(1);
            UpdateScene();
        }

        public virtual void OnFunctionUnmarked(int funId)
        {
            _activeTrialRecord.UnmarkFunction(funId);
            LogEvent(ExpStrs.FUN_DEMARKED, funId.ToString());

            _activeTrialRecord.UnmarkObject(1);
            UpdateScene();
        }

        public void SetFunctionAsApplied(int funcId)
        {
            _activeTrialRecord.SetFunctionAsApplied(funcId);
        }

        public void UpdateScene()
        {
            foreach (var func in _activeTrialRecord.Functions)
            {
                Brush funcColor = UIColors.COLOR_FUNCTION_DEFAULT;
                //this.TrialInfo($"Function#{func.Id} state: {func.State}");
                switch (func.State)
                {
                    case ButtonState.ENABLED:
                        funcColor = UIColors.COLOR_FUNCTION_ENABLED;
                        break;
                    case ButtonState.MARKED:
                        funcColor = UIColors.COLOR_FUNCTION_ENABLED;
                        break;
                    case ButtonState.SELECTED:
                        funcColor = UIColors.COLOR_FUNCTION_APPLIED;
                        break;
                }

                _mainWindow.FillButtonInAuxWindow(_activeTrial.FuncSide, func.Id, funcColor);
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

            //-- TAP:

            if (!IsStartClicked())
            {
                return; // Do nothing if Start was not clicked
            }

            // Show error?

        }

        public void IndexMove(double dX, double dY)
        {

        }

        public void IndexMove(TouchPoint indPoint)
        {

        }

        public void IndexUp()
        {
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

            // Show error?
        }

        public virtual void ThumbTap(long downInstant, long upInstant)
        {

            //-- TAP:

            if (!IsStartClicked())
            {
                return; // Do nothing if Start was not clicked
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

            //-- TAP:
            if (!IsStartClicked())
            {
                return; // Do nothing if Start was not clicked
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
            if (_activeTrialRecord == null)
            {
                this.LogsInfo($"Warning: Trial record is null when logging event '{type}' with id '{id}'. Event not logged.");
                return;
            }

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

            if (_activeTrialRecord == null)
            {
                this.LogsInfo($"Warning: Trial record is null when getting event count for type '{type}'. Returning 0.");
                return 0;
            }

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

        public int GetActiveTrialNum()
        {
            return _activeTrialNum;
        }

        public int GetNumTrialsInBlock()
        {
            return _activeBlock.GetNumTrials();
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
                if (trial != null && _trialRecords[trialId].HasTime(ExpStrs.TRIAL_TIME))
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
            //ExperiLogger.LogTrialMessage($"Average Time per DistanceMM --- " +
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

        protected bool IsFuncPressed()
        {
            return GetEventCount(ExpStrs.FUN_PRESS) > 0;
        }

        public Complexity GetComplexity()
        {
            return _activeBlock.GetComplexity();
        }
    }

}
