using Common.Constants;
using Common.Helpers;
using Common.Settings;
using CommonUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static Common.Constants.ExpEnums;

namespace SubTask.FunctionSelection
{
    public class BlockHandler
    {
        // Attributes
        protected List<TrialRecord> _trialRecords = new List<TrialRecord>(); // Trial id is inside the TrialRecord
        protected MainWindow _mainWindow;
        protected Block _activeBlock;
        protected int _activeBlockNum;
        protected Trial _activeTrial;
        protected int _activeTrialNum = 0;
        protected TrialRecord _activeTrialRecord;

        protected List<int> _functionsVisitMap = new List<int>();

        protected Random _random = new Random();

        public BlockHandler(MainWindow mainWindow, Block activeBlock, int activeBlockNum)
        {
            _mainWindow = mainWindow;
            _activeBlock = activeBlock;
            _activeBlockNum = activeBlockNum;
        }

        //public abstract bool FindPositionsForActiveBlock();
        //public abstract bool FindPositionsForTrial(Trial trial);
        public void BeginActiveBlock()
        {
            this.TrialInfo("------------------- Beginning block ----------------------------");
            this.TrialInfo(ExpStrs.MINOR_LINE);

            _activeTrialNum = 1;
            _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
            _trialRecords.Add(new TrialRecord(_activeTrial.Id));
            _activeTrialRecord = _trialRecords.Last();
            this.TrialInfo($"Active block id: {_activeBlock.Id}");

            // Clear the main window canvas (to add shapes)
            _mainWindow.ClearCanvas();
            _mainWindow.ResetAllAuxWindows();

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

            // Set the target window based on the trial's target side
            _mainWindow.SetTargetWindow(_activeTrial.FuncSide, OnAuxWindowMouseEnter, OnAuxWindowMouseExit, OnAuxWindowMouseDown, OnAuxWindowMouseUp);

            // Show Start Trial button
            MouseEvents startButtonEvents = new(
                OnStartButtonMouseEnter, OnStartButtonMouseDown, OnStartButtonMouseUp, OnStartButtonMouseExit);
            _mainWindow.ShowStart(
                Experiment.GetStartButtonWidth(),
                Experiment.GetStartButtonWidth(),
                UIColors.COLOR_START_INIT,
                startButtonEvents);

            // Color the target buttons and set the handlers
            List<TFunction> foundFunctions = _mainWindow.FindRandomFunctions(_activeTrial.FuncSide, _activeTrial.GetFunctionWidths());
            _activeTrialRecord.AddAllFunctions(foundFunctions);
            this.TrialInfo($"Function Id(s): {foundFunctions}");

            UpdateScene();

            _mainWindow.SetAuxButtonsHandlers(
                _activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds(),
                OnFunctionMouseEnter, this.OnFunctionMouseDown, this.OnFunctionMouseUp,
                OnFunctionMouseExit, this.OnNonTargetMouseDown);

            // Update info label
            _mainWindow.UpdateInfoLabel();
        }

        public virtual void EndActiveTrial(Result result)
        {
            this.TrialInfo($"Trial#{_activeTrial.Id} completed: {result}");
            this.TrialInfo(ExpStrs.MAJOR_LINE);
            _activeTrialRecord.Result = result;
            LogEvent(ExpStrs.TRIAL_END, _activeTrial.Id); // Log the trial end timestamp
            double trialTime = GetDuration(ExpStrs.STR_RELEASE + "_1", ExpStrs.TRIAL_END);

            switch (result)
            {
                case Result.HIT:
                    MSounder.PlayHit();
                    _activeTrialRecord.AddTime(ExpStrs.TRIAL_TIME, trialTime);
                    break;
                case Result.MISS:
                    MSounder.PlayTargetMiss();
                    _activeBlock.ShuffleBackTrial(_activeTrialNum);
                    _activeTrialRecord.AddTime(ExpStrs.TRIAL_TIME, trialTime);
                    break;
            }

            //-- Log
            ExperiLogger.LogDetailTrial(_activeBlockNum, _activeTrialNum, _activeTrial, _activeTrialRecord);
            ExperiLogger.LogTotalTrialTime(_activeBlockNum, _activeTrialNum, _activeTrial, _activeTrialRecord);
            ExperiLogger.LogCursorPositions();
            ExperiLogger.LogTrialEvents(_activeTrialRecord.GetTrialEvents());

            // Go to the next trial
            GoToNextTrial();
        }

        public void GoToNextTrial()
        {
            if (_activeTrialNum < _activeBlock.Trials.Count)
            {
                //_mainWindow.ShowStartTrialButton(OnStartButtonMouseUp);
                _mainWindow.ResetAllAuxWindows();
                _mainWindow.ClearCanvas();
                _functionsVisitMap.Clear();

                _activeTrialNum++;
                _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
                _trialRecords.Add(new TrialRecord(_activeTrial.Id));
                _activeTrialRecord = _trialRecords.Last();

                ShowActiveTrial();
            }
            else // Last trial of the block
            {
                // Log block time
                ExperiLogger.LogBlockTime(_activeBlock);

                // Go to next block
                _mainWindow.GoToNextBlock();
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
            if (IsStartPressed() && !IsStartClicked()) // Start button not clicked yet
            {
                MSounder.PlayStartMiss();
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

        ////---- Function
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
                MSounder.PlayStartMiss();
                e.Handled = true; // Mark the event as handled to prevent further processing
                return;
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }
        public virtual void OnFunctionMouseUp(Object sender, MouseButtonEventArgs e)
        {
            int funId = (int)((FrameworkElement)sender).Tag;
            LogEvent(ExpStrs.FUN_RELEASE, funId);

            // If the trial has already ended, ignore further events
            //if (_activeTrialRecord.GetLastTrialEventType() == ExpStrs.TRIAL_END)
            //{
            //    e.Handled = true;
            //    return;
            //}

            if (!IsStartClicked())
            {
                MSounder.PlayStartMiss();
                e.Handled = true; // Mark the event as handled to prevent further processing
                return; // Do nothing if start button was not clicked
            }

            // Apply the function
            SetFunctionAsApplied(funId);

            // If all functions are applied => end trial with HIT
            if (_activeTrialRecord.AreAllFunctionsSelected())
            {
                //EndActiveTrial(Result.HIT);
                _mainWindow.ChangeStartButtonColor(UIColors.COLOR_FUNCTION_ENABLED);
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public virtual void OnFunctionMouseExit(Object sender, MouseEventArgs e)
        {
            int funId = (int)((FrameworkElement)sender).Tag;
            LogEvent(ExpStrs.FUN_EXIT, funId);
        }

        public void OnNonTargetMouseDown(Object sender, MouseButtonEventArgs e)
        {
            if (!IsStartClicked()) // Start button not clicked yet
            {
                MSounder.PlayStartMiss();
            }
            else
            {
                EndActiveTrial(Result.MISS);
            }
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
                // Change Start to End in the main window

                // Remove the Start
                //_mainWindow.RemoveStartTrialButton();
                UpdateScene();
            }
            else // Press was outside the button => miss
            {
                EndActiveTrial(Result.MISS);
            }

            //--- Correctly pressed
            if (_activeTrialRecord.AreAllFunctionsSelected())
            {
                EndActiveTrial(Result.HIT);
            }
            else
            {
                // Make functions available
                _mainWindow.EnableFunctions(_activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds());

                // Change START to END and unavailable (until all functions are applied)
                _mainWindow.ChangeStartButtonText(ExpStrs.END_CAP);
                _mainWindow.ChangeStartButtonColor(UIColors.DARK_ORANGE);
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

            UpdateScene();
        }

        public virtual void OnFunctionUnmarked(int funId)
        {
            _activeTrialRecord.UnmarkFunction(funId);
            LogEvent(ExpStrs.FUN_DEMARKED, funId.ToString());

            UpdateScene();
        }

        public void SetFunctionAsEnabled(int funcId)
        {
            _mainWindow.FillButtonInAuxWindow(
                _activeTrial.FuncSide,
                funcId,
                UIColors.COLOR_FUNCTION_ENABLED);
        }

        public void SetFunctionAsApplied(int funcId)
        {
            _activeTrialRecord.SetFunctionAsSelected(funcId);
            _mainWindow.FillButtonInAuxWindow(
                _activeTrial.FuncSide,
                funcId,
                UIColors.COLOR_FUNCTION_APPLIED);
        }

        public void UpdateScene()
        {
            foreach (var func in _activeTrialRecord.Functions)
            {
                Brush funcColor = UIColors.COLOR_FUNCTION_DEFAULT;
                //this.TrialInfo($"Function#{func.Id} state: {func.State}");
                switch (func.State)
                {
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

        protected bool IsStartPressed()
        {
            return GetEventCount(ExpStrs.STR_PRESS) > 0;
        }

        protected bool IsStartClicked()
        {
            return GetEventCount(ExpStrs.STR_RELEASE) > 0;
        }

        public Complexity GetComplexity()
        {
            return _activeBlock.Complexity;
        }
    }

}
