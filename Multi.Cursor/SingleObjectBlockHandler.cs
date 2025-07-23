using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        public SingleObjectBlockHandler(MainWindow mainWindow, Block activeBlock)
        {
            _mainWindow = mainWindow;
            _activeBlock = activeBlock;
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

            // If consecutive trials have the same FunctionId, re-order them
            while (IsTargetRepeated())
            {
                _activeBlock.Trials.Shuffle();
            }

            return true;
        }

        private bool IsTargetRepeated()
        {
            //for (int i = 0; i < _activeBlock.Trials.Count - 1; i++)
            //{
            //    if (_trialRecords[_activeBlock.Trials[i].Id]?.FunctionId == _trialRecords[_activeBlock.Trials[i + 1].Id]?.FunctionId)
            //    {
            //        return true;
            //    }
            //}

            return false;
        }

        public override bool FindPositionsForTrial(Trial trial)
        {
            int objW = Utils.MM2PX(Experiment.OBJ_WIDTH_MM);
            int objHalfW = objW / 2;
            int objAreaW = Utils.MM2PX(OBJ_AREA_WIDTH_MM);
            int objAreaHalfW = objAreaW / 2;

            this.TrialInfo($"Trial#{trial.Id} [Target = {trial.FuncSide.ToString()}, " +
                $"FunctionWidths = {trial.GetFunctionWidths().ToStr()}, Dist Range (mm) = {trial.DistRangeMM.ToString()}]");

            // Ensure TrialRecord exists for this trial
            if (!_trialRecords.ContainsKey(trial.Id))
            {
                _trialRecords[trial.Id] = new TrialRecord();
            }

            _mainWindow.Dispatcher.Invoke(() => {
                _trialRecords[trial.Id].Functions.AddRange(
                    _mainWindow.FindRandomFunctions(trial.FuncSide, trial.GetFunctionWidths(), trial.DistRangePX)
                    );
            });

            this.TrialInfo($"Found functions: {_trialRecords[trial.Id].GetFunctionIds().ToStr()}");

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
                this.TrialInfo($"Found object position: {objCenter.ToStr()}");

                // Get the top-left corner of the object area rectangle
                Point objAreaPosition = objCenter.OffsetPosition(-objAreaHalfW);

                this.TrialInfo($"Found object position: {objAreaPosition.ToStr()}");

                _trialRecords[trial.Id].ObjectAreaRect = new Rect(
                        objAreaPosition.X,
                        objAreaPosition.Y,
                        objAreaW,
                        objAreaW);

                // Put the object at the center
                Point objPosition = objAreaPosition.OffsetPosition((objAreaW - objW) / 2);
                TObject obj = new TObject(1, objPosition); // Object is always 1 in this case
                _trialRecords[trial.Id].Objects.Add(obj);

                return true;
            }

            // Find a random target id for the active trial
            //int targetId = FindRandomTargetIdForTrial(trial); // Was checking for unique target ids, which couldn't work
            //(int targetId, Point targetCenterAbsolute) = _mainWindow.Dispatcher.Invoke(() =>
            //{
            //    return _mainWindow.GetRadomTarget(trial.FuncSide, trial.TargetMultiple, trial.DistancePX);
            //});


            //if (targetId != -1)
            //{
            //    _trialRecords[trial.Id].FunctionId = targetId;
            //    this.TrialInfo($"Found Target Id: {targetId} for Trial#{trial.Id}");
            //    //_mainWindow.Dispatcher.Invoke(() =>
            //    //{
            //    //    _mainWindow.FillButtonInAuxWindow(
            //    //        trial.FuncSide,
            //    //        targetId,
            //    //        Config.DARK_ORANGE);
            //    //});

            //}
            //else
            //{
            //    this.TrialInfo($"Failed to find a random target id for Trial#{trial.Id}");
            //    return false;
            //}

            // Get the absolute position of the target center
            //Point targetCenterAbsolute = _mainWindow.GetCenterAbsolutePosition(trial.FuncSide, targetId);




            //Point objCenter = objectAreaConstraintRect.FindRandPointWithDist(targetCenterAbsolute, trial.DistancePX, trial.FuncSide.GetOpposite());

            //this.TrialInfo($"Target: {targetCenterAbsolute}; Object Area Rect: {objectAreaConstraintRect.ToString()}");
            //if (objCenter.X == -1 && objCenter.Y == -1) // Failed to find a valid position
            //{
            //    this.TrialInfo($"No valid position found for object!");
            //    return false;
            //}
            //else // Valid position found
            //{
            //    // Get the top-left corner of the object area rectangle
            //    Point objAreaPosition = objCenter.OffsetPosition(-objAreaHalfW);

            //    this.TrialInfo($"Found object position: {objAreaPosition.ToStr()}");

            //    _trialRecords[trial.Id].ObjectAreaRect = new Rect(
            //            objAreaPosition.X,
            //            objAreaPosition.Y,
            //            objAreaW,
            //            objAreaW);

            //    // Put the object at the center
            //    Point objPosition = objAreaPosition.OffsetPosition((objAreaW - objW) / 2);
            //    TObject obj = new TObject(1, objPosition); // Object is always 1 in this case
            //    _trialRecords[trial.Id].Objects.Add(obj);
            //}

            //return true;
        }

        public override void ShowActiveTrial()
        {
            this.TrialInfo($"Showing Trial#{_activeTrial.Id} | Side: {_activeTrial.FuncSide} | W: {_activeTrial.GetFunctionWidths().ToStr()} | Dist: {_activeTrial.DistanceMM:F2}mm");

            // Update the main window label
            _mainWindow.UpdateInfoLabel(_activeTrialNum, _activeBlock.GetNumTrials());

            // Log the trial show timestamp
            _activeTrialRecord.AddTimestamp(Str.TRIAL_SHOW); 

            // Set the target window based on the trial's target side
            _mainWindow.SetTargetWindow(_activeTrial.FuncSide, OnAuxWindowMouseDown, OnAuxWindowMouseUp);

            // Color the target button and set the handlers
            this.TrialInfo($"Function Ids: {_activeTrialRecord.GetFunctionIds().ToStr()}");
            Brush funcDefaultColor = Config.FUNCTION_DEFAULT_COLOR;
            _mainWindow.FillButtonsInAuxWindow(_activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds(), funcDefaultColor);
            //_mainWindow.FillButtonInTargetWindow(
            //    _activeTrial.FuncSide, 
            //    _activeTrialRecord.FunctionId, 
            //    funcDefaultColor);

            //_mainWindow.SetGridButtonHandlers(
            //    _activeTrial.FuncSide, _activeTrialRecord.FunctionId, 
            //    OnFunctionMouseDown, OnFunctionMouseUp, OnNonTargetMouseDown);

            // If on ToMo, activate the auxiliary window marker on all sides
            if (_mainWindow.IsTechniqueToMo()) _mainWindow.ShowAllAuxMarkers();

            // Clear the main window canvas (to add shapes)
            _mainWindow.ClearCanvas();

            // Show the area
            MouseEvents objAreaEvents = new MouseEvents(OnObjectAreaMouseDown, OnObjectAreaMouseUp);
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
            MouseEvents startButtonEvents = new MouseEvents(OnStartButtonMouseDown, OnStartButtonMouseUp);
            _mainWindow.ShowStartTrialButton(_activeTrialRecord.ObjectAreaRect, startButtonEvents);
        }

        public override void EndActiveTrial(Experiment.Result result)
        {
            this.TrialInfo($"Trial#{_activeTrial.Id} completed: {result}");
            _activeTrialRecord.AddTimestamp(Str.TRIAL_END); // Log the trial end timestamp

            switch (result)
            {
                case Experiment.Result.HIT:
                    Sounder.PlayHit();
                    this.TrialInfo($"Trial time = {GetDuration(Str.OBJ_RELEASE + "_1", Str.TRIAL_END)}s");
                    this.TrialInfo(Str.MAJOR_LINE);
                    GoToNextTrial();
                    break;
                case Experiment.Result.MISS:
                    Sounder.PlayTargetMiss();
                    _activeBlock.ShuffleBackTrial(_activeTrialNum);
                    this.TrialInfo(Str.MAJOR_LINE);
                    GoToNextTrial();
                    break;
                case Experiment.Result.ERROR:
                    Sounder.PlayStartMiss();
                    // Do nothing, just reset everything

                    break;
            }

        }

        public override void GoToNextTrial()
        {
            if (_activeTrialNum < _activeBlock.Trials.Count)
            {
                //_mainWindow.ShowStartTrialButton(OnStartButtonMouseUp);
                _mainWindow.ResetTargetWindow(_activeTrial.FuncSide);
                _activeTrialRecord.ClearTimestamps();
                _nSelectedObjects = 0; // Reset the number of selected objects

                _activeTrialNum++;
                _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
                _activeTrialRecord = _trialRecords[_activeTrial.Id];

                ShowActiveTrial();
            }
            else
            {
                // Show end of block window
                BlockEndWindow blockEndWindow = new BlockEndWindow(_mainWindow.GoToNextBlock);
                blockEndWindow.Owner = _mainWindow;
                blockEndWindow.ShowDialog();
            }
        }

        public override void OnMainWindowMouseDown(Object sender, MouseButtonEventArgs e)
        {
            LogEvent(Str.MAIN_WIN_PRESS);

            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

            var startButtonClicked = GetEventCount(Str.START_RELEASE) == 1;

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
            if (_mainWindow.IsTechniqueToMo()) //-- ToMo
            {
                // Nothing for now
            }
            else //-- Mouse
            {
                // Nothing for now
            }
        }

        public override void OnMainWindowMouseUp(Object sender, MouseButtonEventArgs e)
        {
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnObjectMouseDown(Object sender, MouseButtonEventArgs e)
        {
            LogEvent(Str.OBJ_PRESS);

            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

            var startButtonClicked = GetEventCount(Str.START_RELEASE) > 0;
            var isToMo = _mainWindow.IsTechniqueToMo();
            //var markerOnFunction = _mainWindow.IsMarkerOnButton(_activeTrial.FuncSide, _activeTrialRecord.FunctionId);
            var markerOnFunction = true; // TEMP
            var allObjSelected = _nSelectedObjects == _activeTrialRecord.Objects.Count;

            switch (startButtonClicked, isToMo, markerOnFunction, allObjSelected)
            {
                case (false, _, _, _): // Start button not clicked, _
                    EndActiveTrial(Result.ERROR); // Pressed on object without Start button clicked
                    break;
                case (true, true, true, false): // ToMo, marker on function, not all objects selected
                    // Nothing to do, just log the event
                    break;
                case (true, true, true, true): // ToMo, marker on function, all objects selected
                    EndActiveTrial(Experiment.Result.MISS);
                    return;
                case (true, true, false, false): // ToMo, marker not on function, not all objects selected
                    EndActiveTrial(Result.MISS); // Pressed on object without marker on function 
                    break;
                case (true, false, _, false): // Mouse, any marker state, not all objects selected
                    SetObjectAsMarked(1);
                    //SetFunctionAsEnabled();
                    break;
                case (true, false, _, true): // Mouse, any marker state, all objects selected
                    EndActiveTrial(Experiment.Result.MISS);
                    return;
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnObjectMouseUp(Object sender, MouseButtonEventArgs e)
        {
            var technique = _mainWindow.IsTechniqueToMo() ? Str.ToMo : Str.Mouse;
            //var markerOnFunction = _mainWindow.IsMarkerOnButton(_activeTrial.FuncSide, _activeTrialRecord.FunctionId);
            var markerOnFunction = true; // TEMP
            var functionClicked = GetEventCount(Str.FUNCTION_RELEASE) == 1; // Check if the function was clicked
            var functionWindowActivated = _mainWindow.IsAuxWindowActivated(_activeTrial.FuncSide);

            // Show the current timestamps
            this.TrialInfo($"Technique: {technique}, MarkerOnButton: {markerOnFunction}, FunctionClicked: {functionClicked}");
            switch (technique, markerOnFunction, functionClicked, functionWindowActivated, _objectSelected)
            {
                case ("tomo", true, _, _, _): // ToMo, marker on function, _, 
                    LogEvent(Str.OBJ_RELEASE);
                    SetObjectAsSelected(1);
                    _nSelectedObjects++;
                    break;
                case ("tomo", false, _, false, _): // ToMo, marker not on function, _, function window not activated
                    SetObjectAsMarked(1);
                    break;
                case ("tomo", false, _, true, _): // ToMo, marker not on function, _, function window activated
                    EndActiveTrial(Result.MISS);
                    break;

                case ("mouse", _, true, _, _): // Mouse, _, function clicked
                    EndActiveTrial(Result.HIT);
                    break;
                case ("mouse", _, false, _, _): // Mouse, _, function not clicked
                    //SetFunctionAsEnabled();
                    SetObjectAsMarked(1);
                    break;
                
                default:
                    this.TrialInfo($"Unexpected combination of events in Trial#{_activeTrial.Id}");
                    break;
            }

            LogEvent(Str.OBJ_RELEASE);
            e.Handled = true; // Mark the event as handled to prevent further processing

        }

        public override void OnFunctionMouseDown(Object sender, MouseButtonEventArgs e)
        {
            var technique = _mainWindow.GetActiveTechnique();

            LogEvent(Str.FUNCTION_PRESS);
            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnFunctionMouseUp(Object sender, MouseButtonEventArgs e)
        {
            LogEvent(Str.FUNCTION_RELEASE);

            var technique = _mainWindow.GetActiveTechnique();
            var objectMarked = GetEventCount(Str.OBJ_RELEASE) == 1;

            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

            switch (technique, objectMarked)
            {
                case (Technique.Mouse, true): // Mouse, object marked
                    SetObjectAsSelected(1);
                    //SetFunctionAsSelected();
                    _nSelectedObjects++;
                    break;
                case (Technique.Mouse, false): // Mouse, object not marked
                    EndActiveTrial(Experiment.Result.MISS);
                    break;
                case (Technique.Auxursor_Tap, _):
                    // Nothing for now, handled in OnObjectMouseUp
                    break;
                default:
                    this.TrialInfo($"Unexpected combination of events in Trial#{_activeTrial.Id}");
                    break;
            }

            
            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void IndexTap()
        {
            var technique = _mainWindow.GetActiveTechnique();

            this.TrialInfo($"Technique: {_mainWindow.GetActiveTechnique()}");

            switch (technique)
            {
                case Experiment.Technique.Auxursor_Tap:
                    _mainWindow.ActivateAuxWindowMarker(Side.Top);
                    break;

                case Experiment.Technique.Auxursor_Swipe: // Wrong technique for index tap
                    EndActiveTrial(Result.MISS);
                    break;
            }
        }

        public override void ThumbTap()
        {
            var technique = _mainWindow.GetActiveTechnique();

            this.TrialInfo($"Technique: {_mainWindow.GetActiveTechnique()}");

            switch (technique)
            {
                case Experiment.Technique.Auxursor_Tap:
                    _mainWindow.ActivateAuxWindowMarker(Side.Left);
                    break;

                case Experiment.Technique.Auxursor_Swipe: // Wrong technique for index tap
                    EndActiveTrial(Result.MISS);
                    break;
            }
        }

        public override void MiddleTap()
        {
            var technique = _mainWindow.GetActiveTechnique();

            this.TrialInfo($"Technique: {_mainWindow.GetActiveTechnique()}");

            switch (technique)
            {
                case Experiment.Technique.Auxursor_Tap:
                    _mainWindow.ActivateAuxWindowMarker(Side.Right);
                    break;

                case Experiment.Technique.Auxursor_Swipe: // Wrong technique for index tap
                    EndActiveTrial(Result.MISS);
                    break;
            }
        }

        public override void ThumbTap(Side side)
        {
            ThumbTap();
        }

        public override void ThumbSwipe(Direction dir)
        {
            var technique = _mainWindow.GetActiveTechnique();
            this.TrialInfo($"Technique: {_mainWindow.GetActiveTechnique()}, Direction: {dir}");

            switch (technique)
            {
                case Experiment.Technique.Auxursor_Swipe:
                    _mainWindow.ActivateAuxWindowMarker(Utils.DirToSide(dir));
                    break;
                case Experiment.Technique.Auxursor_Tap: // Wrong technique for thumb swipe
                    EndActiveTrial(Result.MISS);
                    break;
            }
        }
    }
}
