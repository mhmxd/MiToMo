using Common.Constants;
using Common.Helpers;
using Common.Settings;
using CommonUI;
using SubTask.PanelNavigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static Common.Constants.ExpEnums;
using TouchPoint = CommonUI.TouchPoint;

namespace SubTask.Panel.Selection
{
    public class BlockHandler : IGestureHandler
    {
        // Attributes
        protected List<TrialRecord> _trialRecords = new(); // Trial id is stored in TrialRecord
        protected MainWindow _mainWindow;
        protected Block _activeBlock;
        protected int _activeBlockNum;
        protected Trial _activeTrial;
        protected int _activeTrialNum = 0;
        protected TrialRecord _activeTrialRecord;
        protected int _nSelectedObjects = 0; // Number of clicked objects in the current trial

        protected List<int> _functionsVisitMap = new();
        protected List<int> _objectsVisitMap = new();

        protected Random _random = new();

        public BlockHandler(MainWindow mainWindow, Block activeBlock, int activeBlockNum)
        {
            _mainWindow = mainWindow;
            _activeBlock = activeBlock;
            _activeBlockNum = activeBlockNum;
        }

        public void BeginActiveBlock()
        {
            this.TrialInfo("------------------- Beginning block -------------------------------------------");
            this.TrialInfo(ExpStrs.MINOR_LINE);

            _activeTrialNum = 1;
            _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
            TrialRecord trialRecord = new(_activeTrial.Id);
            _trialRecords.Add(trialRecord);
            _activeTrialRecord = _trialRecords.Last();
            this.TrialInfo($"Block #{_activeBlock.Id} active");

            // Clear the main window canvas (to add shapes)
            _mainWindow.ClearCanvas();
            _mainWindow.ResetAllAuxWindows();

            // Show the first trial
            ShowActiveTrial();
        }

        public void ShowActiveTrial()
        {
            this.TrialInfo(ExpStrs.MINOR_LINE);
            this.TrialInfo($"Showing " + _activeTrial.Str());

            LogEvent(ExpStrs.TRIAL_SHOW, _activeTrial.Id);

            // Start logging cursor positions
            ExperiLogger.StartTrialCursorLog(_activeTrial.Id);

            // Clear the main window canvas (to add shapes)
            _mainWindow.ClearCanvas();

            // Set the target window based on the trial's target side
            _mainWindow.SetTargetWindow(_activeTrial.FuncSide, OnAuxWindowMouseEnter, OnAuxWindowMouseExit, OnAuxWindowMouseDown, OnAuxWindowMouseUp);

            // Show Start Trial button
            MouseEvents startButtonEvents = new(
                OnStartButtonMouseEnter, OnStartButtonMouseDown, OnStartButtonMouseUp, OnStartButtonMouseExit);
            _mainWindow.ShowStartBtn(
                UITools.MM2PX(ExpLayouts.START_BUTTON_LARGE_SIDE_MM),
                UITools.MM2PX(ExpLayouts.START_BUTTON_LARGE_SIDE_MM),
                UIColors.COLOR_START_INIT,
                startButtonEvents);

            // Update info label
            _mainWindow.UpdateInfoLabel();
        }

        public virtual async Task EndActiveTrialAsync(Result result)
        {
            this.TrialInfo($"Trial#{_activeTrial.Id} completed: {result}");
            this.TrialInfo(ExpStrs.MAJOR_LINE);
            _activeTrialRecord.Result = result;
            LogEvent(ExpStrs.TRIAL_END, _activeTrial.Id); // Log the trial end timestamp

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
                    break;
            }

            //-- Log
            ExperiLogger.LogDetailTrial(_activeBlockNum, _activeTrialNum, _activeTrial, _activeTrialRecord);
            ExperiLogger.LogTotalTrialTime(_activeBlockNum, _activeTrialNum, _activeTrial, _activeTrialRecord);
            ExperiLogger.LogCursorPositions();

            // Next trial
            await GoToNextTrial();
        }

        public async Task GoToNextTrial()
        {
            if (_activeTrialNum < _activeBlock.Trials.Count)
            {
                _mainWindow.ResetAllAuxWindows();
                _mainWindow.ClearCanvas();
                _activeTrialRecord.ClearTimestamps();
                _nSelectedObjects = 0; // Reset the number of selected objects
                _functionsVisitMap.Clear();
                _objectsVisitMap.Clear();

                _activeTrialNum++;
                _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
                TrialRecord trialRecord = new(_activeTrial.Id);
                _trialRecords.Add(trialRecord);
                _activeTrialRecord = _trialRecords.Last();

                ShowActiveTrial();
            }
            else // Last trial of the block
            {
                // Log block time
                ExperiLogger.LogBlockTime(_activeBlock);

                // Show pause pop-up if reached the number of defined blocks
                if (_activeBlockNum == ExpDesign.PaneNavBreakAfterBlocks)
                {
                    // Clear MainWindow
                    _mainWindow.ClearAll();

                    // Show the popup
                    PausePopUp pausePopUp = new();
                    pausePopUp.Owner = _mainWindow;
                    pausePopUp.ShowDialog();
                }

                // Go to the next block
                await _mainWindow.GoToNextBlock();
            }
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
                EndActiveTrialAsync(Result.MISS);
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
                EndActiveTrialAsync(Result.MISS);
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public void OnAuxWindowMouseMove(Object sender, MouseEventArgs e)
        {
            // Nothing for now
        }
        public void OnAuxWindowMouseUp(Side side, Object sender, MouseButtonEventArgs e)
        {
            LogEvent(ExpStrs.PNL_PRESS, side.ToString().ToLower());

            if (IsStartPressed()) // Pressed in Start, released in aux window
            {
                EndActiveTrialAsync(Result.MISS);
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
            LogEvent(ExpStrs.FUN_PRESS, funId);

            if (!IsStartClicked()) // Start button not clicked yet
            {
                MSounder.PlayStartMiss();
                e.Handled = true; // Mark the event as handled to prevent further processing
                return;
            }

            if (_activeTrial.Technique.GetDevice() == Technique.TOMO) // No function pressing in TOMO
            {
                EndActiveTrialAsync(Result.MISS);
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

        public void OnNonTargetMouseDown(Object sender, MouseButtonEventArgs e)
        {

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
                _mainWindow.RemoveStartTrialButton();

                // Find a random function button in the aux window and set it in TrialRecord
                TFunction selectedFunc = _mainWindow.FindRandomFunction(_activeTrial.GetFunctionWidth(0));
                _activeTrialRecord.Functions.Add(selectedFunc);

                // Color the found function as enabled
                _mainWindow.SetFunctionDefault(selectedFunc.Id);
            }
            else // Pressed outside the button => miss
            {
                EndActiveTrialAsync(Result.MISS);
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
                UIColors.COLOR_FUNCTION_ENABLED);
        }

        public void SetFunctionAsDisabled(int funcId)
        {
            _mainWindow.FillButtonInAuxWindow(
                _activeTrial.FuncSide,
                funcId,
                UIColors.COLOR_FUNCTION_DEFAULT);
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


        public override void IndexTap()
        {
            if (_activeTrial.Technique == Technique.TOMO_SWIPE) // Wrong technique for thumb tap
            {
                EndActiveTrialAsync(Result.MISS);
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
                //_mainWindow.ActivateAuxWindowMarker(correspondingSide);

                EndActiveTrialAsync(Result.HIT);
            }
            else
            {
                EndActiveTrialAsync(Result.MISS);
            }
        }

        public override void IndexMove(TouchPoint indPoint)
        {
            LogEventOnce(ExpStrs.FLICK); // First flick after activation
            // Maybe show error? (Since there is no flick here)
        }

        public override void IndexUp()
        {
            LogEvent(ExpStrs.JoinUs(ExpStrs.INDEX, ExpStrs.UP));
        }

        public override void ThumbSwipe(Direction dir)
        {
            if (_activeTrial == null) return;

            if (_activeTrial.Technique != Technique.TOMO_SWIPE) // Wrong technique for swipe
            {
                EndActiveTrialAsync(Result.MISS);
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

            if (dirMatchesSide)
            {
                LogEvent(ExpStrs.PNL_SELECT);
                //_mainWindow.ActivateAuxWindowMarker(_activeTrial.FuncSide);

                // End trial
                EndActiveTrialAsync(Result.HIT);
            }
            else
            {
                EndActiveTrialAsync(Result.MISS);
            }
        }

        public override void ThumbTap(long downInstant, long upInstant)
        {
            if (_activeTrial.Technique == Technique.TOMO_SWIPE) // Wrong technique for thumb tap
            {
                EndActiveTrialAsync(Result.MISS);
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
                //_mainWindow.ActivateAuxWindowMarker(correspondingSide);

                EndActiveTrialAsync(Result.HIT);
            }
            else
            {
                EndActiveTrialAsync(Result.MISS);
            }
        }

        public override void MiddleTap()
        {
            if (_activeTrial.Technique == Technique.TOMO_SWIPE) // Wrong technique for thumb tap
            {
                EndActiveTrialAsync(Result.MISS);
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
                //_mainWindow.ActivateAuxWindowMarker(correspondingSide);

                EndActiveTrialAsync(Result.HIT);
            }
            else
            {
                EndActiveTrialAsync(Result.MISS);
            }
        }

        protected void LogEvent(string type, string id)
        {
            // Only log if there is an _activeTrialRecord (Trial is actually started)
            _activeTrialRecord?.RecordEvent(type, id);
            // Note: Let them have the same name. We know the count from EventCounts
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
            if (_activeTrialRecord != null) return _activeTrialRecord.CountEvent(type);
            return 0;
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

        public Technique GetTechnique()
        {
            return _activeBlock.Technique;
        }

        public Complexity GetComplexity()
        {
            return _activeBlock.Complexity;
        }

        public int GetNumTrialsInBlock()
        {
            return _activeBlock.GetNumTrials();
        }

        public override void RecordToMoAction(Finger finger, string action)
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
