using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using static Multi.Cursor.Experiment;
using static Multi.Cursor.Utils;

namespace Multi.Cursor
{
    public class SingleObjectBlockHandler : BlockHandler
    {
        private bool _objectSelected = false; // Is object selected?

        public SingleObjectBlockHandler(MainWindow mainWindow, Block activeBlock, int blockNum)
        {
            _mainWindow = mainWindow;
            _activeBlock = activeBlock;
            _activeBlockNum = blockNum;
        }

        public override bool FindPositionsForActiveBlock()
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

        public override bool FindPositionsForTrial(Trial trial)
        {
            int objW = Utils.MM2PX(Experiment.OBJ_WIDTH_MM);
            int objHalfW = objW / 2;
            int objAreaW = Utils.MM2PX(OBJ_AREA_WIDTH_MM);
            int objAreaHalfW = objAreaW / 2;

            //this.TrialInfo(trial.ToStr());

            // Ensure TrialRecord exists for this trial
            if (!_trialRecords.ContainsKey(trial.Id))
            {
                _trialRecords[trial.Id] = new TrialRecord();
            }
            //this.TrialInfo($"Trial function widths: {trial.GetFunctionWidths()}");
            _mainWindow.Dispatcher.Invoke(() => {
                _trialRecords[trial.Id].Functions.AddRange(
                    _mainWindow.FindRandomFunctions(trial.FuncSide, trial.GetFunctionWidths(), trial.DistRangePX)
                    );
            });

            //this.TrialInfo($"Found functions: {_trialRecords[trial.Id].GetFunctionIds().ToStr()}");

            // Find a position for the object area
            Rect objectAreaConstraintRect = _mainWindow.Dispatcher.Invoke(() =>
            {
                return _mainWindow.GetObjAreaCenterConstraintRect();
            });

            Point objCenter = objectAreaConstraintRect.FindPointWithinDistRangeFromMultipleSources(
                _trialRecords[trial.Id].GetFunctionCenters(), trial.DistRangePX);


            if (objCenter.X == -1 && objCenter.Y == -1) // Failed to find a valid position 
            {
                this.TrialInfo($"No valid position found for object in Trial#{trial.Id}!");
                return false; // Return false to indicate failure
            }
            else
            {
                //this.TrialInfo($"Found object position: {objCenter.ToStr()}");

                // Get the top-left corner of the object area rectangle
                Point objAreaPosition = objCenter.OffsetPosition(-objAreaHalfW);

                //this.TrialInfo($"Found object area position: {objAreaPosition.ToStr()}");

                _trialRecords[trial.Id].ObjectAreaRect = new Rect(
                        objAreaPosition.X,
                        objAreaPosition.Y,
                        objAreaW,
                        objAreaW);

                // Put the object at the center
                Point objPosition = objAreaPosition.OffsetPosition((objAreaW - objW) / 2);
                TrialRecord.TObject obj = new TrialRecord.TObject(1, objPosition, objCenter); // Object is always 1 in this case
                _trialRecords[trial.Id].Objects.Add(obj);

                return true;
            }
        }

        public override void ShowActiveTrial()
        {
            this.TrialInfo(Str.MINOR_LINE);
            this.TrialInfo($"Showing " + _activeTrial.ToStr());
            //ExperiLogger.StartTrialLog(_activeTrial);

            // Update the main window label
            //_mainWindow.UpdateInfoLabel(_activeTrialNum, _activeBlock.GetNumTrials());

            // Log the trial show timestamp
            //_activeTrialRecord.RecordEvent(Str.TRIAL_SHOW);
            LogEvent(Str.TRIAL_SHOW, _activeTrial.Id);
            // Set the target window based on the trial's target side
            _mainWindow.SetTargetWindow(_activeTrial.FuncSide, OnAuxWindowMouseEnter, OnAuxWindowMouseExit, OnAuxWindowMouseDown, OnAuxWindowMouseUp);

            // Color the target button and set the handlers
            this.TrialInfo($"Function Id(s): {_activeTrialRecord.GetFunctionIds().ToStr()}");
            Brush funcDefaultColor = Config.FUNCTION_DEFAULT_COLOR;
            UpdateScene();
            //_mainWindow.FillButtonInTargetWindow(
            //    _activeTrial.FuncSide, 
            //    _activeTrialRecord.FunctionId, 
            //    funcDefaultColor);

            _mainWindow.SetAuxButtonsHandlers(
                _activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds(),
                OnFunctionMouseEnter, this.OnFunctionMouseDown, this.OnFunctionMouseUp, 
                OnFunctionMouseExit, this.OnNonTargetMouseDown);
            
            // If on ToMo, activate the auxiliary window marker on all sides
            if (_mainWindow.IsTechniqueToMo()) _mainWindow.ShowAllAuxMarkers();

            // Clear the main window canvas (to add shapes)
            _mainWindow.ClearCanvas();
            
            // Show the area
            MouseEvents objAreaEvents = new MouseEvents(OnObjectAreaMouseEnter, OnObjectAreaMouseDown, OnObjectAreaMouseUp, OnObjectAreaMouseExit);
            _mainWindow.ShowObjectsArea(
                _activeTrialRecord.ObjectAreaRect, 
                Config.OBJ_AREA_BG_COLOR, 
                objAreaEvents);
            
            // Show objects
            Brush objDefaultColor = Config.OBJ_DEFAULT_COLOR;
            MouseEvents objectEvents = new MouseEvents(
                OnObjectMouseEnter, OnObjectMouseDown, OnObjectMouseUp, OnObjectMouseLeave);
            _mainWindow.ShowObjects(
                _activeTrialRecord.Objects, objDefaultColor, objectEvents);
            
            // Show Start Trial button
            MouseEvents startButtonEvents = new MouseEvents(
                OnStartButtonMouseEnter, OnStartButtonMouseDown, OnStartButtonMouseUp, OnStartButtonMouseExit);
            _mainWindow.ShowStartTrialButton(_activeTrialRecord.ObjectAreaRect, startButtonEvents);

            // Update info label
            _mainWindow.UpdateInfoLabel();
            
        }

        public override void EndActiveTrial(Result result)
        {
            this.TrialInfo($"Trial#{_activeTrial.Id} completed: {result}");
            LogEvent(Str.TRIAL_END, _activeTrial.Id); // Log the trial end event
            _mainWindow.DeactivateAuxWindow(); // Deactivate the aux window

            switch (result)
            {
                case Result.HIT:
                    Sounder.PlayHit();
                    //double trialTime = GetDuration(Str.STR_RELEASE + "_1", Str.TRIAL_END);
                    double trialTime = GetDuration(Str.OBJ_RELEASE + "_1", Str.TRIAL_END);
                    _activeTrialRecord.AddTime(Str.TRIAL_TIME, trialTime);
                    
                    //ExperiLogger.LogSingleObjTrialTimes(_activeTrialRecord);
                    if (_activeTrial.GetNumFunctions() == 1)
                    {
                        ExperiLogger.LogSOSFTrial(_activeBlockNum, _activeTrialNum, _activeTrial, _activeTrialRecord);
                    }
                    else
                    {
                        ExperiLogger.LogSOMFTrial(_activeBlockNum, _activeTrialNum, _activeTrial, _activeTrialRecord);
                    }

                    //this.TrialInfo($"Trial Time = {trialTime:F2}s");
                    //ExperiLogger.LogTrialMessage($"{_activeTrial.ToStr().PadRight(34)} Trial Time = {trialTime:F2}s");
                    this.TrialInfo(Str.MAJOR_LINE);
                    GoToNextTrial();
                    break;
                case Result.MISS:
                    Sounder.PlayTargetMiss();
                    //-- Log

                    _activeBlock.ShuffleBackTrial(_activeTrialNum);
                    _trialRecords[_activeTrial.Id].ClearTimestamps();
                    _trialRecords[_activeTrial.Id].ResetStates();
                    this.TrialInfo(Str.MAJOR_LINE);
                    GoToNextTrial();
                    break;
                case Result.ERROR:
                    Sounder.PlayStartMiss();
                    // Do nothing
                    
                    break;
            }

        }

        public override void GoToNextTrial()
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

        public override void OnMainWindowMouseDown(Object sender, MouseButtonEventArgs e)
        {
            LogEvent(Str.MAIN_WIN_PRESS);

            this.TrialInfo($"Timestamps: {_activeTrialRecord.TrialEventsToString()}");

            var startButtonClicked = GetEventCount(Str.STR_RELEASE) == 1;

            switch (startButtonClicked)
            {
                case true: // Start button was clicked
                    EndActiveTrial(Result.MISS);
                    break;
                case false: // Start button was not clicked
                    EndActiveTrial(Result.ERROR);
                    break;
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnMainWindowMouseMove(Object sender, MouseEventArgs e)
        {
            LogEventOnce(Str.FIRST_MOVE); // Will manage the 'first'
        }

        public override void OnMainWindowMouseUp(Object sender, MouseButtonEventArgs e)
        {
            
            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnObjectMouseDown(Object sender, MouseButtonEventArgs e)
        {
            var objId = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.OBJ_PRESS, objId);

            this.TrialInfo($"Timestamps: {_activeTrialRecord.TrialEventsToString()}");

            var startButtonClicked = (GetEventCount(Str.STR_RELEASE) > 0);
            var device = Utils.GetDevice(_activeBlock.Technique);
            int funcIdUnderMarker = _mainWindow.FunctionIdUnderMarker(_activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds());
            var markerOverEnabledFunc = funcIdUnderMarker != -1;
            var allFunctionsApplied = _activeTrialRecord.AreAllFunctionsApplied();

            this.TrialInfo($"StartButtonClicked: {startButtonClicked}");

            switch (startButtonClicked, device, markerOverEnabledFunc, allFunctionsApplied)
            {
                case (false, _, _, _): // Start button not clicked, _
                    EndActiveTrial(Result.ERROR); // Pressed on object without Start button clicked
                    break;

                case (true, Technique.TOMO, _, false): // ToMo, _, not all functions applied
                    
                    break;

                case (true, Technique.MOUSE, _, false):

                    break;
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnObjectMouseUp(Object sender, MouseButtonEventArgs e)
        {
            var objId = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.OBJ_RELEASE, objId);

            this.TrialInfo($"Trial Id: {_activeTrial.Id} | Obj: {this.GetHashCode()}");
            var device = Utils.GetDevice(_activeBlock.Technique);
            var objectPressed = GetEventCount(Str.OBJ_PRESS) > 0; // Check if the object was pressed
            int funcIdUnderMarker = _mainWindow.FunctionIdUnderMarker(_activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds());
            var markerOverEnabledFunc = funcIdUnderMarker != -1;
            var functionClicked = GetEventCount(Str.FUN_RELEASE) == 1; // Check if the function was clicked
            var allFunctionsApplied = _activeTrialRecord.AreAllFunctionsApplied();
            var anyFunctionEnabled = _activeTrialRecord.IsAnyFunctionEnabled();
            var functionWindowActivated = _mainWindow.IsAuxWindowActivated(_activeTrial.FuncSide);

            // Show the current timestamps
            this.TrialInfo($"Technique: {device == Technique.TOMO}, ObjPressed: {objectPressed}, MarkerOnFunction: {markerOverEnabledFunc}, FuncWinActive: {functionWindowActivated}");
            switch (device, objectPressed, markerOverEnabledFunc, anyFunctionEnabled, allFunctionsApplied, functionWindowActivated, _objectSelected)
            {
                case (_, false, _, _, _, _, _): // MOUSE, object not pressed
                    // Do nothing because the object was not pressed
                    break;

                case (Technique.TOMO, true, true, _, _, true, _): // ToMo, object pressed, marker on function, function window activated 
                    _activeTrialRecord.ApplyFunction(funcIdUnderMarker);
                    if (_activeTrialRecord.AreAllFunctionsApplied())
                    {
                        _activeTrialRecord.ChangeObjectState(1, ButtonState.APPLIED);
                    }
                    UpdateScene();
                    break;
                case (Technique.TOMO, _, false, _, _, false, _): // ToMo, marker not on function, _, function window not activated
                    _activeTrialRecord.MarkObject(1);
                    UpdateScene();
                    break;
                case (Technique.TOMO, _, false, _, _, true, _): // ToMo, marker not on function, _, function window activated
                    //EndActiveTrial(Result.MISS);
                    break;
                
                case (Technique.MOUSE, true, _, _, _, _, _): // MOUSE, object correctly pressed
                    _activeTrialRecord.EnableAllFunctions();
                    _activeTrialRecord.MarkObject(1);
                    UpdateScene();
                    break;

                
                default:
                    this.TrialInfo($"Unexpected combination of events in Trial#{_activeTrial.Id}");
                    break;
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnFunctionMarked(int funId)
        {
            LogEvent(Str.FUN_MARKED, funId.ToString());
        }

        public override void OnFunctionDeMarked(int funId)
        {
            LogEvent(Str.FUN_DEMARKED, funId.ToString());
        }

        public override void OnFunctionMouseDown(Object sender, MouseButtonEventArgs e)
        {
            int funId = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.FUN_PRESS, funId);

            //this.TrialInfo($"Trial Id: {_activeTrial.Id} | Obj: {this.GetHashCode()}");
            //this.TrialInfo($"Events: {_activeTrialRecord.TrialEventsToString()}");

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnFunctionMouseUp(Object sender, MouseButtonEventArgs e)
        {
            int funId = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.FUN_RELEASE, funId);

            //this.TrialInfo($"Events: {_activeTrialRecord.TrialEventsToString()}");

            // Function id is sender's tag as int
            var functionId = (int)((FrameworkElement)sender).Tag;
            var device = Utils.GetDevice(_activeBlock.Technique);
            var objectMarked = GetEventCount(Str.OBJ_RELEASE) > 0;

            this.TrialInfo($"ObjectMarked: {objectMarked};");

            switch (device, objectMarked)
            {
                case (Technique.TOMO, _):
                    // Nothing for now, handled in OnObjectMouseUp
                    break;

                case (Technique.MOUSE, true): // MOUSE, object marked
                    _activeTrialRecord.ApplyFunction(functionId);
                    // Change obj's color only if all functions are selected
                    if (_activeTrialRecord.AreAllFunctionsApplied())
                    {
                        _activeTrialRecord.ChangeObjectState(1, ButtonState.APPLIED);
                    }
                    UpdateScene();
                    break;
                case (Technique.MOUSE, false): // MOUSE, object not marked
                    EndActiveTrial(Result.MISS);
                    break;
                
                default:
                    this.TrialInfo($"Unexpected combination of events in Trial#{_activeTrial.Id}");
                    break;
            }

            
            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnNonTargetMouseDown(Object sender, MouseButtonEventArgs e)
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

        public override void OnObjectAreaMouseDown(Object sender, MouseButtonEventArgs e)
        {
            //this.TrialInfo($"Events: {_activeTrialRecord.TrialEventsToString()}");
            LogEvent(Str.ARA_PRESS);

            var allFunctionsApplied = _activeTrialRecord.AreAllFunctionsApplied();

            switch (allFunctionsApplied)
            {
                case true:
                    // All objects are selected, so we can end the trial
                    EndActiveTrial(Result.HIT);
                    break;
                case false:
                    // Not all objects are selected, so we treat it as a miss
                    EndActiveTrial(Result.MISS);
                    break;
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void IndexTap()
        {
            this.TrialInfo($"Index tap detected.");

            var technique = _activeBlock.GetSpecificTechnique();

            //this.TrialInfo($"Technique: {_activeBlock.GetSpecificTechnique()}");

            // Log
            //LogEvent(Str.Join(Str.INDEX, Str.DOWN), downInstant);
            //LogEvent(Str.Join(Str.INDEX, Str.UP), upInstant);

            switch (technique)
            {
                case Technique.TOMO_TAP:
                    _mainWindow.ActivateAuxWindowMarker(Side.Top);
                    break;

                case Technique.TOMO_SWIPE: // Wrong _technique for index tap
                    EndActiveTrial(Result.MISS);
                    break;
            }
        }

        public override void ThumbTap(long downInstant, long upInstant)
        {
            var technique = _activeBlock.GetSpecificTechnique();

            this.TrialInfo($"Technique: {_activeBlock.GetSpecificTechnique()}");

            // Log
            //LogEvent(Str.Join(Str.THUMB, Str.DOWN), downInstant);
            //LogEvent(Str.Join(Str.THUMB, Str.UP), upInstant);

            switch (technique)
            {
                case Technique.TOMO_TAP:
                    _mainWindow.ActivateAuxWindowMarker(Side.Left);
                    break;

                case Technique.TOMO_SWIPE: // Wrong _technique for index tap
                    EndActiveTrial(Result.MISS);
                    break;
            }
        }

        public override void MiddleTap()
        {
            var technique = _activeBlock.GetSpecificTechnique();

            this.TrialInfo($"Technique: {_activeBlock.GetSpecificTechnique()}");

            switch (technique)
            {
                case Technique.TOMO_TAP:
                    _mainWindow.ActivateAuxWindowMarker(Side.Right);
                    break;

                case Technique.TOMO_SWIPE: // Wrong _technique for index tap
                    EndActiveTrial(Result.MISS);
                    break;
            }
        }

        public override void ThumbTap(Side side)
        {
            //ThumbTap();
        }

        public override void ThumbSwipe(Direction dir)
        {
            var technique = _activeBlock.GetSpecificTechnique();
            this.TrialInfo($"Technique: {_activeBlock.GetSpecificTechnique()}, Direction: {dir}");

            switch (technique)
            {
                case Technique.TOMO_SWIPE:
                    _mainWindow.ActivateAuxWindowMarker(Utils.DirToSide(dir));
                    break;
                case Technique.TOMO_TAP: // Wrong _technique for thumb swipe
                    EndActiveTrial(Result.MISS);
                    break;
            }
        }

        public override void OnObjectMouseEnter(object sender, MouseEventArgs e)
        {
            // If the last timestamp was ARA_EXIT, remove that
            if (_activeTrialRecord.GetLastTrialEventType() == Str.ARA_EXIT) _activeTrialRecord.RemoveLastTimestamp();
            var objId = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.OBJ_ENTER, objId);
        }

        public override void OnObjectMouseLeave(object sender, MouseEventArgs e)
        {
            var objId = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.OBJ_EXIT, objId);
        }

        public override void OnFunctionMouseEnter(object sender, MouseEventArgs e)
        {
            // Add the id to the list of visited if not already there (will use the index for the order of visit)
            int funId = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.FUN_ENTER, funId);
        }

        public override void OnFunctionMouseExit(object sender, MouseEventArgs e)
        {
            // Check the Id in the visited list. If visited, log the event.
            int funId = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.FUN_EXIT, funId);
        }

        public override void OnObjectAreaMouseEnter(object sender, MouseEventArgs e)
        {
            // Only log if entered from outside (NOT from the object)
            if (_activeTrialRecord.GetLastTrialEventType() != Str.OBJ_EXIT) LogEvent(Str.ARA_ENTER);
        }

        public override void OnObjectAreaMouseUp(object sender, MouseButtonEventArgs e)
        {
            LogEvent(Str.ARA_RELEASE);
        }

        public override void OnObjectAreaMouseExit(object sender, MouseEventArgs e)
        {
            // Will be later removed if entered the object
            LogEvent(Str.ARA_EXIT);
        }
    }
}
