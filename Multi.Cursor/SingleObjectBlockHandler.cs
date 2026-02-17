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

        //public override bool FindPositionsForActiveBlock()
        //{
        //    // 1. GET THE RECT ONCE (The part I missed!)
        //    // We do this on the UI thread before the loop starts.
        //    Rect objAreaConstraintRect = _mainWindow.Dispatcher.Invoke(() =>
        //    {
        //        return _mainWindow.GetObjAreaCenterConstraintRect();
        //    });

        //    // 2. Pass it into each trial
        //    foreach (Trial trial in _activeBlock.Trials)
        //    {
        //        // We pass 'startRect' here so the trial method doesn't have to ask the UI for it
        //        if (!FindPositionsForTrial(trial, objAreaConstraintRect))
        //        {
        //            this.PositionInfo($"Failed to find positions for Trial#{trial.Id}");
        //            return false;
        //        }
        //    }
        //    this.TrialInfo($"Found positions for all trials. Now checking for middle button...");
        //    // 3. Shuffle logic (with the fixed OR condition)
        //    int maxAttempts = 1000;
        //    int attempt = 0;
        //    while (attempt < maxAttempts && DoesFirstTrialsFunInclMidBtn())
        //    {
        //        _activeBlock.Trials.Shuffle();
        //        attempt++;
        //    }

        //    return attempt < maxAttempts;
        //    //foreach (Trial trial in _activeBlock.Trials)
        //    //{
        //    //    if (!FindPositionsForTrial(trial))
        //    //    {
        //    //        this.PositionInfo($"Failed to find positions for Trial#{trial.Id}");
        //    //        return false; // If any trial fails, return false
        //    //    }
        //    //}


        //    //// If consecutive trials have the same function Ids or first trial's functions are over the middle button,
        //    //// re-order them (so marker doesn't stay on the same function)
        //    //int maxAttempts = 500;
        //    //int attempt = 0;
        //    //while (attempt < maxAttempts && (AreFunctionsRepeated() || DoesFirstTrialsFunInclMidBtn()))
        //    //{
        //    //    _activeBlock.Trials.Shuffle();
        //    //    attempt++;
        //    //}

        //    //if (attempt == maxAttempts)
        //    //{
        //    //    this.TrialInfo($"Warning: Could not eliminate repeated functions in consecutive trials after {maxAttempts} attempts.");
        //    //    return false;
        //    //}

        //    //return true;
        //}

        public override bool FindPositionsForTrial(Trial trial, Rect objectAreaConstraintRect)
        {
            base.FindPositionsForTrial(trial, objectAreaConstraintRect);

            // Set the Obj...
            Point objAreaPosition = _trialRecords[trial.Id].ObjectAreaRect.TopLeft;
            Point objPosition = objAreaPosition.OffsetPosition(
                (Experiment.GetObjAreaWidth() - Experiment.GetObjWidth()) / 2);
            Point objAreaCenter = _trialRecords[trial.Id].ObjectAreaRect.GetCenter();
            TObject obj = new(1, objPosition, objAreaCenter);
            _trialRecords[trial.Id].Objects.Add(obj);

            return true;

            //_trialRecords[trial.Id] = new();

            //// 1. Pick a random START (the Object Area)
            //Point objCenter = new(
            //    _random.NextDouble() * objectAreaConstraintRect.Width + objectAreaConstraintRect.Left,
            //    _random.NextDouble() * objectAreaConstraintRect.Height + objectAreaConstraintRect.Top
            //);

            //// 2. Define the "Ideal" spot for the buttons based on the required trial distance
            //double targetDist = trial.DistRangePX.Min + (_random.NextDouble() * (trial.DistRangePX.Max - trial.DistRangePX.Min));
            //double angle = _random.NextDouble() * Math.PI * 2;

            //Point idealCenter = new(
            //    objCenter.X + Math.Cos(angle) * targetDist,
            //    objCenter.Y + Math.Sin(angle) * targetDist
            //);

            //// 3. Find buttons NEAR that ideal center
            //List<TFunction> foundFunctions = _mainWindow.Dispatcher.Invoke(() =>
            //{
            //    return _mainWindow.FindRandomFunctionsNearPoint(trial.FuncSide, trial.GetFunctionWidths(), idealCenter);
            //});

            //if (foundFunctions.Count < trial.GetFunctionWidths().Count) return false;

            //// 4. Record and Validate
            //_trialRecords[trial.Id].Functions.AddRange(foundFunctions);

            //// Calculate the ACTUAL average distance to store in the record
            //double actualAvgDist = foundFunctions.Average(f => f.Center.DistanceTo(objCenter));
            //_trialRecords[trial.Id].AvgDistanceMM = UITools.PX2MM(actualAvgDist);

            //// Set the Rects...
            //Point objAreaPosition = objCenter.OffsetPosition(-(Experiment.GetObjAreaHalfWidth()));
            //_trialRecords[trial.Id].ObjectAreaRect = new Rect(objAreaPosition.X, objAreaPosition.Y,
            //                                                  Experiment.GetObjAreaWidth(), Experiment.GetObjAreaWidth());
            //// Set the Obj...
            //Point objPosition = objAreaPosition.OffsetPosition(
            //    (Experiment.GetObjAreaWidth() - Experiment.GetObjWidth()) / 2);
            //TObject obj = new(1, objPosition, objCenter);
            //_trialRecords[trial.Id].Objects.Add(obj);

            //return true;




            //// 1. Setup local variables (no logic change here)
            //int objW = Experiment.GetObjWidth();
            //int objAreaW = Experiment.GetObjAreaWidth();
            //int objAreaHalfW = Experiment.GetObjAreaHalfWidth();

            //_trialRecords[trial.Id] = new();

            //// 2. RETRY LOOP: This is the critical fix for "Failed to find positions"
            //// Instead of giving up if one button set is impossible, try a few others.
            //int maxTargetAttempts = 10;
            //for (int t = 0; t < maxTargetAttempts; t++)
            //{
            //    _trialRecords[trial.Id].Functions.Clear();

            //    // We still need Dispatcher here because FindRandomFunctions likely 
            //    // touches UI elements to check widths/positions.
            //    _mainWindow.Dispatcher.Invoke(() =>
            //    {
            //        _trialRecords[trial.Id].Functions.AddRange(
            //            _mainWindow.FindRandomFunctions(trial.FuncSide, trial.GetFunctionWidths())
            //        );
            //    });

            //    // 3. Use the PASSED-IN Rect (no more Dispatcher.Invoke here!)
            //    (Point objCenter, double avgDist) = objectAreaConstraintRect.FindPointWithinDistRangeFromMultipleSources(
            //        _trialRecords[trial.Id].GetFunctionCenters(), trial.DistRangePX);

            //    // 4. Check if we found a valid start position for these specific buttons
            //    if (objCenter.X != -1 && objCenter.Y != -1)
            //    {
            //        // SUCCESS: Set the values and return
            //        Point objAreaPosition = objCenter.OffsetPosition(-objAreaHalfW);

            //        _trialRecords[trial.Id].ObjectAreaRect = new Rect(
            //                objAreaPosition.X,
            //                objAreaPosition.Y,
            //                objAreaW,
            //                objAreaW);

            //        _trialRecords[trial.Id].AvgDistanceMM = avgDist;

            //        Point objPosition = objAreaPosition.OffsetPosition((objAreaW - objW) / 2);
            //        TObject obj = new(1, objPosition, objCenter);
            //        _trialRecords[trial.Id].Objects.Add(obj);

            //        return true;
            //    }

            //    // If we reach here, this set of buttons was impossible to pair with a start point.
            //    // The loop will try a different set of random buttons.
            //}

            //// Only if 10 different sets of buttons fail do we give up.
            //return false;
        }

        public override void ShowActiveTrial()
        {
            base.ShowActiveTrial();

            // Set the target window based on the trial's target side
            _mainWindow.SetTargetWindow(_activeTrial.FuncSide, OnAuxWindowMouseEnter, OnAuxWindowMouseExit, OnAuxWindowMouseDown, OnAuxWindowMouseUp);

            // Color the target button and set the handlers
            //this.TrialInfo($"Function Id(s): {_activeTrialRecord.GetFunctionIds().Str()}");
            //Brush funcDefaultColor = UIColors.COLOR_FUNCTION_DEFAULT;
            //UpdateScene(); // (comment for measuring panel selection time)
            //_mainWindow.FillButtonInTargetWindow(
            //    _activeTrial.FuncSide, 
            //    _activeTrialRecord.FunctionId, 
            //    funcDefaultColor);


            //-- Canvas is cleared in GoToNextTrial
            // Clear the main window canvas (to add shapes)
            //_mainWindow.ClearCanvas();

            // Show the area
            MouseEvents objAreaEvents = new(OnObjectAreaMouseEnter, OnObjectAreaMouseDown, OnObjectAreaMouseUp, OnObjectAreaMouseExit);
            _mainWindow.ShowObjectsArea(
                _activeTrialRecord.ObjectAreaRect,
                UIColors.COLOR_OBJ_AREA_BG,
                objAreaEvents);

            // Show objects
            Brush objDefaultColor = UIColors.COLOR_OBJ_DEFAULT;
            MouseEvents objectEvents = new(OnObjectMouseEnter, OnObjectMouseDown, OnObjectMouseUp, OnObjectMouseLeave);
            _mainWindow.ShowObjects(_activeTrialRecord.Objects, objDefaultColor, objectEvents);

            // Show Start Trial button
            MouseEvents startButtonEvents = new MouseEvents(
                OnStartButtonMouseEnter, OnStartButtonMouseDown, OnStartButtonMouseUp, OnStartButtonMouseExit);
            _mainWindow.ShowStartTrialButton(_activeTrialRecord.ObjectAreaRect, startButtonEvents);

            // Fill the function buttons and set their handlers
            //_mainWindow.FillButtonsInAuxWindow(
            //    _activeTrial.FuncSide,
            //    _activeTrialRecord.GetFunctionIds(),
            //    UIColors.COLOR_FUNCTION_DEFAULT);
            _mainWindow.SetButtonsAsFunc(
                _activeTrial.FuncSide,
                _activeTrialRecord.GetFunctionIds()
             );
            _mainWindow.SetAuxButtonsHandlers(
                _activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds(),
                OnFunctionMouseEnter, this.OnFunctionMouseDown, this.OnFunctionMouseUp,
                OnFunctionMouseExit, this.OnNonTargetMouseDown);
            //UpdateScene();

            // If on ToMo, activate the auxiliary window marker on all sides
            if (_activeBlock.Technique.IsTomo()) _mainWindow.ShowAllAuxMarkers();

            // Update info label
            _mainWindow.UpdateInfoLabel();

        }

        public override void GoToNextTrial()
        {
            if (_activeTrialNum < _activeBlock.Trials.Count)
            {
                //_mainWindow.ShowStartTrialButton(OnStartButtonMouseUp);
                //_mainWindow.ResetTargetWindow(_activeTrial.FuncSide);
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
                ExperiLogger.LogBlockTime(_activeBlock, _activeBlockNum);

                // Go to the next block
                _mainWindow.GoToNextBlock();

                // Show end of block window
                //BlockEndWindow blockEndWindow = new BlockEndWindow(_mainWindow.GoToNextBlock);
                //blockEndWindow.Owner = _mainWindow;
                //blockEndWindow.ShowDialog();
            }
        }

        public override void OnObjectAreaMouseUp(object sender, MouseButtonEventArgs e)
        {
            base.OnObjectAreaMouseUp(sender, e);

            if (!IsStartClicked())
            {
                MSounder.PlayStartMiss();
                e.Handled = true; // Mark the event as handled to prevent further processing
                return; // Do nothing if start button was not clicked
            }

            //-- Trial started:
            if (!_activeTrialRecord.AreAllFunctionsSelected())
            {
                EndActiveTrial(Result.MISS);
            }
            else // All functions are applied
            {
                EndActiveTrial(Result.HIT);
            }

            e.Handled = true; // Mark the event as handled to prevent further processing

        }

        public override void OnObjectMouseDown(Object sender, MouseButtonEventArgs e)
        {
            base.OnObjectMouseDown(sender, e); // Just logs the event

            // Pressed on the Object without starting the trial
            if (!IsStartClicked())
            {
                MSounder.PlayStartMiss();
            }

            //-- Trial started:

            // ToMo
            if (_activeTrial.IsTechniqueToMo())
            {
                int funcIdUnderMarker = _mainWindow.FunctionIdUnderMarker(_activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds());

                // Marker not over enabled function => MISS
                if (funcIdUnderMarker == -1)
                {
                    this.TrialInfo($"Marker not over enabled function");
                    EndActiveTrial(Result.MISS);
                    //e.Handled = true; // Mark the event as handled to prevent further processing
                    //return; // Do nothing if marker is not over enabled function
                }

                // Marker over already-applied function => MISS
                else if (_activeTrialRecord.IsFunctionSelected(funcIdUnderMarker))
                {
                    this.TrialInfo($"Function under marker already applied");
                    EndActiveTrial(Result.MISS);
                }
            }
            else // Mouse
            {
                if (_activeTrialRecord.IsObjectClicked(1)) // Object already clicked => MISS
                {
                    this.TrialInfo($"Object already clicked");
                    EndActiveTrial(Result.MISS);
                }
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnObjectMouseUp(Object sender, MouseButtonEventArgs e)
        {
            base.OnObjectMouseUp(sender, e); // For logging the event

            if (!IsStartClicked())
            {
                MSounder.PlayStartMiss();
                e.Handled = true; // Mark the event as handled to prevent further processing
                return; // Do nothing if start button was not clicked
            }

            //-- Trial started:
            if (!WasObjectPressed(1)) // Technique doesn't matter here
            {
                this.TrialInfo($"Object wasn't pressed");
                e.Handled = true; // Mark the event as handled to prevent further processing
                return; // Do nothing if object wasn't pressed
            }

            //-- Object is pressed:
            //this.TrialInfo($"Events: {_activeTrialRecord.TrialEventsToString()}");
            if (_activeTrial.IsTechniqueToMo())
            {
                int funcIdUnderMarker = _mainWindow.FunctionIdUnderMarker(_activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds());
                var markerOverEnabledFunc = funcIdUnderMarker != -1;
                this.TrialInfo($"funcIdUnderMarker: {funcIdUnderMarker}");
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
                //this.TrialInfo($"Events: {_activeTrialRecord.TrialEventsToString()}");
                _activeTrialRecord.MarkAllFunctions();
                // Enable all functions in aux
                _mainWindow.ChangeAuxButtonsState(
                    _activeTrial.FuncSide,
                    _activeTrialRecord.GetFunctionIds(),
                    ButtonState.ENABLED);
                // Enable object
                _activeTrialRecord.MarkObject(1);
                UpdateScene();
            }

            e.Handled = true;

        }

        public override void OnFunctionMarked(int funId, GridPos rowCol)
        {
            base.OnFunctionMarked(funId, rowCol);

            _activeTrialRecord.MarkObject(1);
            UpdateScene();

        }

        public override void OnFunctionUnmarked(int funId, GridPos rowCol)
        {
            base.OnFunctionUnmarked(funId, rowCol);

            _activeTrialRecord.UnmarkObject(1);
            UpdateScene();
        }

        public override void OnFunctionMouseUp(Object sender, MouseButtonEventArgs e)
        {
            // If the trial has already ended, ignore further events
            if (_activeTrialRecord.GetLastTrialEventType() == ExpStrs.TRIAL_END)
            {
                e.Handled = true;
                return;
            }

            base.OnFunctionMouseUp(sender, e); // Just logs the event

            if (!IsStartClicked())
            {
                MSounder.PlayStartMiss();
                e.Handled = true; // Mark the event as handled to prevent further processing
                return; // Do nothing if start button was not clicked
            }

            // Function id is sender's tag as int
            var functionId = (int)((FrameworkElement)sender).Tag;
            var objectMarked = GetEventCount(ExpStrs.OBJ_RELEASE) > 0;

            if (!objectMarked) // Technique doesn't matter here
            {
                EndActiveTrial(Result.MISS);
                e.Handled = true; // Mark the event as handled to prevent further processing
                return; // Do nothing if object is not marked
            }

            //-- Object is marked:
            //this.TrialInfo($"Events: {_activeTrialRecord.TrialEventsToString()}");
            if (_activeTrial.Technique == Technique.MOUSE)
            {
                _activeTrialRecord.ApplyFunction(functionId, 1);
                // Change obj's color only if all functions are selected
                if (_activeTrialRecord.AreAllFunctionsSelected())
                {
                    _activeTrialRecord.ChangeObjectState(1, ButtonState.SELECTED);
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
    }
}
