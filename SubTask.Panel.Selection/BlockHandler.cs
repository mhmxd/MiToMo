using Common.Constants;
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
using static SubTask.Panel.Selection.Experiment;
using static SubTask.Panel.Selection.TrialRecord;
using static Tensorflow.TensorShapeProto.Types;

namespace SubTask.Panel.Selection
{
    public class BlockHandler : IGestureHandler
    {
        // Attributes
        protected List<TrialRecord> _trialRecords = new List<TrialRecord>(); // Trial id is stored in TrialRecord
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

        public BlockHandler(MainWindow mainWindow, Block activeBlock, int activeBlockNum)
        {
            _mainWindow = mainWindow;
            _activeBlock = activeBlock;
            _activeBlockNum = activeBlockNum;
        }

        public void BeginActiveBlock()
        {
            this.TrialInfo("------------------- Beginning block ----------------------------");
            this.TrialInfo(Str.MINOR_LINE);

            _activeTrialNum = 1;
            _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
            TrialRecord trialRecord = new TrialRecord(_activeTrial.Id);
            _trialRecords.Add(trialRecord);
            _activeTrialRecord = _trialRecords.Last();
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

        public void ShowActiveTrial()
        {
            this.TrialInfo(Str.MINOR_LINE);
            this.TrialInfo($"Showing " + _activeTrial.ToStr());

            LogEvent(Str.TRIAL_SHOW, _activeTrial.Id);

            // Start logging cursor positions
            ExperiLogger.StartTrialCursorLog(_activeTrial.Id);

            // Clear the main window canvas (to add shapes)
            _mainWindow.ClearCanvas();

            // Set the target window based on the trial's target side
            _mainWindow.SetTargetWindow(_activeTrial.FuncSide, OnAuxWindowMouseEnter, OnAuxWindowMouseExit, OnAuxWindowMouseDown, OnAuxWindowMouseUp);

            // Show Start Trial button
            MouseEvents startButtonEvents = new MouseEvents(
                OnStartButtonMouseEnter, OnStartButtonMouseDown, OnStartButtonMouseUp, OnStartButtonMouseExit);
            _mainWindow.ShowStartBtn(
                Utils.MM2PX(ExpSizes.START_BUTTON_DIM_MM.W),
                Utils.MM2PX(ExpSizes.START_BUTTON_DIM_MM.H),    
                Experiment.START_INIT_COLOR,
                startButtonEvents);

            // Update info label
            _mainWindow.UpdateInfoLabel();
        }

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
                    break;
            }

            //-- Log
            ExperiLogger.LogDetailTrial(_activeBlockNum, _activeTrialNum, _activeTrial, _activeTrialRecord);

            GoToNextTrial();
        }
        public void GoToNextTrial()
        {
            if (_activeTrialNum < _activeBlock.Trials.Count)
            {
                //_mainWindow.ShowStartTrialButton(OnStartButtonMouseUp);
                _mainWindow.ResetTargetWindow(_activeTrial.FuncSide);
                _mainWindow.ClearCanvas();
                _activeTrialRecord.ClearTimestamps();
                _nSelectedObjects = 0; // Reset the number of selected objects
                _functionsVisitMap.Clear();
                _objectsVisitMap.Clear();

                _activeTrialNum++;
                _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
                TrialRecord trialRecord = new TrialRecord(_activeTrial.Id);
                _trialRecords.Add(trialRecord);
                _activeTrialRecord = _trialRecords.Last();

                ShowActiveTrial();
            }
            else // Last trial of the block
            {
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
            LogEvent(Str.MAIN_WIN_PRESS);

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
            LogEventOnce(Str.FIRST_MOVE);

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

        //---- Function
        public virtual void OnFunctionMouseEnter(Object sender, MouseEventArgs e)
        {
            // Add the id to the list of visited if not already there (will use the index for the order of visit)
            int funId = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.FUN_ENTER, funId);
        }

        public virtual void OnFunctionMouseDown(Object sender, MouseButtonEventArgs e)
        {
            int funId = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.FUN_PRESS, funId);

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
            LogEvent(Str.FUN_RELEASE, funId);

            // Rest of the handling is done in the derived classes
        }

        public virtual void OnFunctionMouseExit(Object sender, MouseEventArgs e)
        {
            int funId = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.FUN_EXIT, funId);
        }

        public void OnNonTargetMouseDown(Object sender, MouseButtonEventArgs e)
        {

        }

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

                // Color a random function button in the aux window and set the width in trialRecord
                TFunction selectedFunc = _mainWindow.ColorRandomFunction(_activeTrial.FuncSide, Config.FUNCTION_DEFAULT_COLOR);
                _activeTrialRecord.Functions.Add(selectedFunc);
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
                LogEvent(Str.PNL_SELECT);
                //_mainWindow.ActivateAuxWindowMarker(correspondingSide);

                EndActiveTrial(Result.HIT);
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
                LogEventOnce(Str.FLICK); // First flick after activation
                _mainWindow?.MoveMarker(indPoint, OnFunctionMarked, OnFunctionUnmarked);
            }

        }

        public void IndexUp()
        {
            _mainWindow.StopAuxNavigator();
            LogEvent(Str.Join(Str.INDEX, Str.UP));
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

            var dirMatchesSide = dir switch
            {
                Direction.Left => _activeTrial.FuncSide == Side.Left,
                Direction.Right => _activeTrial.FuncSide == Side.Right,
                Direction.Up => _activeTrial.FuncSide == Side.Top,
                _ => false
            };

            //var dirOppositeSide = dir switch
            //{
            //    Direction.Left => _activeTrial.FuncSide == Side.Right,
            //    Direction.Right => _activeTrial.FuncSide == Side.Left,
            //    Direction.Down => _activeTrial.FuncSide == Side.Top,
            //    _ => false
            //};

            if (dirMatchesSide)
            {
                LogEvent(Str.PNL_SELECT);
                //_mainWindow.ActivateAuxWindowMarker(_activeTrial.FuncSide);

                // End trial
                EndActiveTrial(Result.HIT);
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
                LogEvent(Str.PNL_SELECT);
                //_mainWindow.ActivateAuxWindowMarker(correspondingSide);

                EndActiveTrial(Result.HIT);
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
                LogEvent(Str.PNL_SELECT);
                //_mainWindow.ActivateAuxWindowMarker(correspondingSide);

                EndActiveTrial(Result.HIT);
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

        public int GetActiveTrialNum()
        {
            return _activeTrialNum;
        }

        public int GetNumTrialsInBlock()
        {
            return _activeBlock.GetNumTrials();
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

        protected bool WasObjectPressed(int objId)
        {
            this.TrialInfo($"Last event: {_activeTrialRecord.GetBeforeLastTrialEvent().ToString()}");
            return _activeTrialRecord.GetEventIndex(Str.OBJ_PRESS) != -1;
        }
    }

}
