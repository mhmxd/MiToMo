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
using static SubTask.FunctionSelection.Experiment;
using static SubTask.FunctionSelection.TrialRecord;
using static Tensorflow.TensorShapeProto.Types;

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
            this.TrialInfo(Str.MINOR_LINE);

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
            this.TrialInfo(Str.MINOR_LINE);
            this.TrialInfo($"Showing " + _activeTrial.ToStr());

            LogEvent(Str.TRIAL_SHOW, _activeTrial.Id);

            // Start logging cursor positions
            ExperiLogger.StartTrialCursorLog(_activeTrial.Id);

            // Set the target window based on the trial's target side
            _mainWindow.SetTargetWindow(_activeTrial.FuncSide, OnAuxWindowMouseEnter, OnAuxWindowMouseExit, OnAuxWindowMouseDown, OnAuxWindowMouseUp);

            // Clear the main window canvas (to add shapes)
            //_mainWindow.ClearCanvas();

            // Show Start Trial button
            MouseEvents startButtonEvents = new MouseEvents(
                OnStartButtonMouseEnter, OnStartButtonMouseDown, OnStartButtonMouseUp, OnStartButtonMouseExit);
            _mainWindow.ShowStart(_activeTrial.FuncSide, startButtonEvents);

            // Color the target button and set the handlers
            _activeTrialRecord.AddAllFunctions(_mainWindow.FindRandomFunctions(_activeTrial.FuncSide, _activeTrial.GetFunctionWidths()));
            this.TrialInfo($"Function Id(s): {_activeTrialRecord.GetFunctionIds().ToStr()}");
            _mainWindow.FillButtonsInAuxWindow(_activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds(), Config.FUNCTION_DEFAULT_COLOR);
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
            this.TrialInfo(Str.MAJOR_LINE);
            _activeTrialRecord.Result = result;
            LogEvent(Str.TRIAL_END, _activeTrial.Id); // Log the trial end timestamp

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
        public void GoToNextTrial()
        {
            if (_activeTrialNum < _activeBlock.Trials.Count)
            {
                //_mainWindow.ShowStartTrialButton(OnStartButtonMouseUp);
                _mainWindow.ResetTargetWindow(_activeTrial.FuncSide);
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

        //public virtual void OnObjectMouseDown(Object sender, MouseButtonEventArgs e)
        //{
        //    var objId = (int)((FrameworkElement)sender).Tag;
        //    LogEvent(Str.OBJ_PRESS, objId);

        //    // Rest of the handling is done in the derived classes
        //}

        //public virtual void OnObjectMouseUp(Object sender, MouseButtonEventArgs e)
        //{
        //    var objId = (int)((FrameworkElement)sender).Tag;
        //    LogEvent(Str.OBJ_RELEASE, objId);

        //    // Rest of the handling is done in the derived classes
        //}

        //---- Object area
        //public virtual void OnObjectAreaMouseEnter(Object sender, MouseEventArgs e)
        //{
        //    // Only log if entered from outside (NOT from the object)
        //    if (_activeTrialRecord.GetLastTrialEventType() != Str.OBJ_EXIT) LogEvent(Str.ARA_ENTER);
        //}

        //public virtual void OnObjectAreaMouseDown(Object sender, MouseButtonEventArgs e)
        //{

        //    if (!IsStartClicked()) // Start button not clicked yet
        //    {
        //        Sounder.PlayStartMiss();
        //        e.Handled = true; // Mark the event as handled to prevent further processing
        //        return;
        //    }

        //    if (!_activeTrialRecord.AreAllObjectsApplied())
        //    {
        //        e.Handled = true; // Mark the event as handled to prevent further processing
        //        EndActiveTrial(Result.MISS);
        //    }

        //    LogEvent(Str.ARA_PRESS);

        //    e.Handled = true; // Mark the event as handled to prevent further processing
        //}

        //public virtual void OnObjectAreaMouseUp(Object sender, MouseButtonEventArgs e)
        //{
        //    LogEvent(Str.ARA_RELEASE);

        //    if (!IsStartClicked())
        //    {
        //        this.TrialInfo($"Start wasn't clicked");
        //        Sounder.PlayStartMiss();
        //        e.Handled = true; // Mark the event as handled to prevent further processing
        //        return; // Do nothing if start button was not clicked
        //    }

        //    if (_activeTrialRecord.AreAllObjectsApplied())
        //    {
        //        EndActiveTrial(Result.HIT);
        //    }
        //    else
        //    {
        //        this.TrialInfo($"Not all objects applied");
        //        EndActiveTrial(Result.MISS);
        //    }

        //    e.Handled = true; // Mark the event as handled to prevent further processing
        //}

        //public virtual void OnObjectAreaMouseExit(Object sender, MouseEventArgs e)
        //{
        //    // Will be later removed if entered the object
        //    LogEvent(Str.ARA_EXIT);
        //}

        ////---- Function
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

            e.Handled = true; // Mark the event as handled to prevent further processing
        }
        public virtual void OnFunctionMouseUp(Object sender, MouseButtonEventArgs e)
        {
            int funId = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.FUN_RELEASE, funId);

            // If the trial has already ended, ignore further events
            //if (_activeTrialRecord.GetLastTrialEventType() == Str.TRIAL_END)
            //{
            //    e.Handled = true;
            //    return;
            //}

            if (!IsStartClicked())
            {
                Sounder.PlayStartMiss();
                e.Handled = true; // Mark the event as handled to prevent further processing
                return; // Do nothing if start button was not clicked
            }

            // Apply the function
            SetFunctionAsApplied(funId);

            // If all functions are applied, make Start button available
            if (_activeTrialRecord.AreAllFunctionsApplied())
            {
                _mainWindow.ChangeStartButtonColor(Config.FUNCTION_ENABLED_COLOR);
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public virtual void OnFunctionMouseExit(Object sender, MouseEventArgs e)
        {
            int funId = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.FUN_EXIT, funId);
        }

        public void OnNonTargetMouseDown(Object sender, MouseButtonEventArgs e)
        {
            if (!IsStartClicked()) // Start button not clicked yet
            {
                Sounder.PlayStartMiss();
            }
            else
            {
                EndActiveTrial(Result.MISS);
            }
            e.Handled = true; // Mark the event as handled to prevent further processing
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
                _mainWindow.SwitchStartToEnd(_activeTrial.FuncSide);
                UpdateScene();
            }
            else // Press was outside the button => miss
            {
                EndActiveTrial(Result.MISS);
            }

            //--- Correctly pressed
            if (_activeTrialRecord.AreAllFunctionsApplied())
            {
                EndActiveTrial(Result.HIT);
            }
            else
            {
                // Make functions available
                _mainWindow.EnableFunctions(_activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds());

                // Change START to END and unavailable (until all functions are applied)
                _mainWindow.ChangeStartButtonText(Str.END);
                _mainWindow.ChangeStartButtonColor(Config.DARK_ORANGE);
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

            UpdateScene();
        }

        public virtual void OnFunctionUnmarked(int funId)
        {
            _activeTrialRecord.UnmarkFunction(funId);
            LogEvent(Str.FUN_DEMARKED, funId.ToString());

            UpdateScene();
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
            _mainWindow.FillButtonInAuxWindow(
                _activeTrial.FuncSide,
                funcId,
                Config.FUNCTION_APPLIED_COLOR);
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
            return GetEventCount(Str.STR_PRESS) > 0;
        }

        protected bool IsStartClicked()
        {
            return GetEventCount(Str.STR_RELEASE) > 0;
        }
    }

}
