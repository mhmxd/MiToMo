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
            base.ShowActiveTrial();

            // Update the main window label
            //_mainWindow.UpdateInfoLabel(_activeTrialNum, _activeBlock.GetNumTrials());
            
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

        //public override void EndActiveTrial(Result result)
        //{

        //    switch (result)
        //    {
        //        case Result.HIT:
        //            Sounder.PlayHit();
        //            //double trialTime = GetDuration(Str.STR_RELEASE + "_1", Str.TRIAL_END);
        //            double trialTime = GetDuration(Str.OBJ_RELEASE + "_1", Str.TRIAL_END);
        //            _activeTrialRecord.AddTime(Str.TRIAL_TIME, trialTime);
                   
        //            //this.TrialInfo($"Trial Time = {trialTime:F2}s");
        //            //ExperiLogger.LogTrialMessage($"{_activeTrial.ToStr().PadRight(34)} Trial Time = {trialTime:F2}s");
        //            this.TrialInfo(Str.MAJOR_LINE);
        //            GoToNextTrial();
        //            break;
        //        case Result.MISS:
        //            Sounder.PlayTargetMiss();

        //            _activeBlock.ShuffleBackTrial(_activeTrialNum);
        //            _trialRecords[_activeTrial.Id].ClearTimestamps();
        //            _trialRecords[_activeTrial.Id].ResetStates();
        //            this.TrialInfo(Str.MAJOR_LINE);
        //            GoToNextTrial();
        //            break;
        //    }

        //    // Log the records
        //    if (_activeTrial.GetNumFunctions() == 1)
        //    {
        //        ExperiLogger.LogSOSFTrial(_activeBlockNum, _activeTrialNum, _activeTrial, _activeTrialRecord);
        //    }
        //    else
        //    {
        //        ExperiLogger.LogSOMFTrial(_activeBlockNum, _activeTrialNum, _activeTrial, _activeTrialRecord);
        //    }

        //}

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

        public override void OnObjectMouseDown(Object sender, MouseButtonEventArgs e)
        {
            base.OnObjectMouseDown(sender, e); // Just logs the event

            // Pressed on the Object without starting the trial
            if (!IsStartClicked())
            {
                Sounder.PlayStartMiss();
            }

            //-- Trial started:

            if (_activeTrial.IsTechniqueToMo())
            {
                int funcIdUnderMarker = _mainWindow.FunctionIdUnderMarker(_activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds());

                if (funcIdUnderMarker == -1) // Marker not over enabled function => miss
                {
                    this.TrialInfo($"Marker not over enabled function");
                    EndActiveTrial(Result.MISS);
                    e.Handled = true; // Mark the event as handled to prevent further processing
                    return; // Do nothing if marker is not over enabled function
                }
            }

            //var objId = (int)((FrameworkElement)sender).Tag;

            //var startButtonClicked = (GetEventCount(Str.STR_RELEASE) > 0);
            //var device = Utils.GetDevice(_activeBlock.Technique);
            //int funcIdUnderMarker = _mainWindow.FunctionIdUnderMarker(_activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds());
            //var markerOverEnabledFunc = funcIdUnderMarker != -1;
            //var allFunctionsApplied = _activeTrialRecord.AreAllFunctionsApplied();

            //this.TrialInfo($"StartButtonClicked: {startButtonClicked}");

            //switch (startButtonClicked, device, markerOverEnabledFunc, allFunctionsApplied)
            //{
            //    case (false, _, _, _): // Start button not clicked, _

            //        break;

            //    case (true, Technique.TOMO, _, false): // ToMo, _, not all functions applied

            //        break;

            //    case (true, Technique.MOUSE, _, false):

            //        break;
            //}

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnObjectMouseUp(Object sender, MouseButtonEventArgs e)
        {
            if (!IsStartClicked())
            {
                Sounder.PlayStartMiss();
                var objId = (int)((FrameworkElement)sender).Tag;
                LogEvent(Str.OBJ_RELEASE, objId);
                e.Handled = true; // Mark the event as handled to prevent further processing
                return; // Do nothing if start button was not clicked
            }

            //-- Trial started:

            if (!IsObjectJustPressed(1)) // Technique doesn't matter here
            {
                this.TrialInfo($"Object wasn't pressed");
                var objId = (int)((FrameworkElement)sender).Tag;
                LogEvent(Str.OBJ_RELEASE, objId);
                e.Handled = true; // Mark the event as handled to prevent further processing
                return; // Do nothing if object wasn't pressed
            }

            //-- Object is pressed (while marker was over function):

            if (_activeTrial.IsTechniqueToMo())
            {
                int funcIdUnderMarker = _mainWindow.FunctionIdUnderMarker(_activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds());
                var markerOverEnabledFunc = funcIdUnderMarker != -1;

                if (markerOverEnabledFunc)
                {
                    _activeTrialRecord.ApplyFunction(funcIdUnderMarker, 1);
                    UpdateScene();
                }
                else
                {
                    EndActiveTrial(Result.MISS);
                }
            }
            else // MOUSE
            {
                _activeTrialRecord.EnableAllFunctions();
                _activeTrialRecord.MarkObject(1);
                UpdateScene();
            }

            e.Handled = true;

            //var device = Utils.GetDevice(_activeBlock.Technique);
            //var allFunctionsApplied = _activeTrialRecord.AreAllFunctionsApplied();
            //var anyFunctionEnabled = _activeTrialRecord.IsAnyFunctionEnabled();
            //var functionWindowActivated = _mainWindow.IsAuxWindowActivated(_activeTrial.FuncSide);

            //// Show the current timestamps
            //this.TrialInfo($"Technique: {device == Technique.TOMO}, ObjPressed: {objectPressed}, MarkerOnFunction: {markerOverEnabledFunc}, FuncWinActive: {functionWindowActivated}");
            //switch (device, objectPressed, markerOverEnabledFunc, anyFunctionEnabled, allFunctionsApplied, functionWindowActivated, _objectSelected)
            //{

            //    case (Technique.TOMO, true, true, _, _, true, _): // ToMo, object pressed, marker on function, function window activated 
                    
            //        break;
            //    case (Technique.TOMO, _, false, _, _, false, _): // ToMo, marker not on function, _, function window not activated
            //        _activeTrialRecord.MarkObject(1);
            //        UpdateScene();
            //        break;
            //    case (Technique.TOMO, _, false, _, _, true, _): // ToMo, marker not on function, _, function window activated
            //        //EndActiveTrial(Result.MISS);
            //        break;
                
            //    case (Technique.MOUSE, true, _, _, _, _, _): // MOUSE, object correctly pressed
                    
            //        break;
            //}

            
        }

        public override void OnFunctionMarked(int funId)
        {
            base.OnFunctionMarked(funId);

            _activeTrialRecord.MarkObject(1);
            UpdateScene();

        }

        public override void OnFunctionUnmarked(int funId)
        {
            base.OnFunctionUnmarked(funId);

            _activeTrialRecord.UnmarkObject(1);
            UpdateScene();
        }

        public override void OnFunctionMouseUp(Object sender, MouseButtonEventArgs e)
        {
            // If the trial has already ended, ignore further events
            if (_activeTrialRecord.GetLastTrialEventType() == Str.TRIAL_END)
            {
                e.Handled = true;
                return;
            }

            base.OnFunctionMouseUp(sender, e); // Just logs the event

            if (!IsStartClicked())
            {
                Sounder.PlayStartMiss();
                e.Handled = true; // Mark the event as handled to prevent further processing
                return; // Do nothing if start button was not clicked
            }


            //this.TrialInfo($"Events: {_activeTrialRecord.TrialEventsToString()}");

            // Function id is sender's tag as int
            var functionId = (int)((FrameworkElement)sender).Tag;
            var device = Utils.GetDevice(_activeBlock.Technique);
            var objectMarked = GetEventCount(Str.OBJ_RELEASE) > 0;

            if (!objectMarked) // Technique doesn't matter here
            {
                EndActiveTrial(Result.MISS);
                e.Handled = true; // Mark the event as handled to prevent further processing
                return; // Do nothing if object is not marked
            }

            //-- Object is marked:

            if (_activeTrial.Technique == Technique.MOUSE)
            {
                _activeTrialRecord.ApplyFunction(functionId, 1);
                // Change obj's color only if all functions are selected
                if (_activeTrialRecord.AreAllFunctionsApplied())
                {
                    _activeTrialRecord.ChangeObjectState(1, ButtonState.APPLIED);
                }

                UpdateScene();
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

        //public override void OnObjectAreaMouseDown(Object sender, MouseButtonEventArgs e)
        //{
        //    // Pressed on the Object without starting the trial
        //    if (!IsStartClicked())
        //    {
        //        Sounder.PlayStartMiss();
        //    }

            

        //    var allFunctionsApplied = _activeTrialRecord.AreAllFunctionsApplied();

        //    switch (allFunctionsApplied)
        //    {
        //        case true:
        //            // All objects are selected, so we can end the trial
        //            EndActiveTrial(Result.HIT);
        //            break;
        //        case false:
        //            // Not all objects are selected, so we treat it as a miss
        //            EndActiveTrial(Result.MISS);
        //            break;
        //    }

        //    e.Handled = true; // Mark the event as handled to prevent further processing
        //}
    }
}
