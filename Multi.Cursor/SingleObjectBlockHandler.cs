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
                TrialRecord.TObject obj = new TrialRecord.TObject(1, objPosition); // Object is always 1 in this case
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
            //Brush funcDefaultColor = Config.FUNCTION_DEFAULT_COLOR;
            //_mainWindow.FillButtonsInAuxWindow(_activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds(), funcDefaultColor);
            UpdateScene();
            //_mainWindow.FillButtonInTargetWindow(
            //    _activeTrial.FuncSide, 
            //    _activeTrialRecord.FunctionId, 
            //    funcDefaultColor);

            _mainWindow.SetAuxButtonsHandlers(
                _activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds(),
                OnFunctionMouseDown, OnFunctionMouseUp, OnNonTargetMouseDown);

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
                    // Record everything and reset
                    // TODO: Record times

                    // Reset timestamps
                    _activeTrialRecord.ClearTimestamps();
                    
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
            else //-- MOUSE
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
            var technique = _activeBlock.GetGeneralTechnique();
            int funcIdUnderMarker = _mainWindow.FunctionIdUnderMarker(_activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds());
            var markerOverEnabledFunc = funcIdUnderMarker != -1;
            var allFunctionsApplied = _activeTrialRecord.AreAllFunctionsApplied();

            this.TrialInfo($"StartButtonClicked: {startButtonClicked}");

            switch (startButtonClicked, technique, markerOverEnabledFunc, allFunctionsApplied)
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
            LogEvent(Str.OBJ_RELEASE);

            var technique = _activeBlock.GetGeneralTechnique();
            var objectPressed = GetEventCount(Str.OBJ_PRESS) > 0; // Check if the object was pressed
            int funcIdUnderMarker = _mainWindow.FunctionIdUnderMarker(_activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds());
            var markerOverEnabledFunc = funcIdUnderMarker != -1;
            var functionClicked = GetEventCount(Str.FUNCTION_RELEASE) == 1; // Check if the function was clicked
            var allFunctionsApplied = _activeTrialRecord.AreAllFunctionsApplied();
            var anyFunctionEnabled = _activeTrialRecord.IsAnyFunctionEnabled();
            var functionWindowActivated = _mainWindow.IsAuxWindowActivated(_activeTrial.FuncSide);

            // Show the current timestamps
            this.TrialInfo($"Technique: {technique == Technique.TOMO}, ObjPressed: {objectPressed}, MarkerOnFunction: {markerOverEnabledFunc}, FuncWinActive: {functionWindowActivated}");
            switch (technique, objectPressed, markerOverEnabledFunc, anyFunctionEnabled, allFunctionsApplied, functionWindowActivated, _objectSelected)
            {
                case (_, false, _, _, _, _, _): // MOUSE, object not pressed
                    // Do nothing because the object was not pressed
                    break;

                case (Technique.TOMO, true, true, _, _, true, _): // ToMo, object pressed, marker on function, function window activated 
                    _activeTrialRecord.ApplyFunction(funcIdUnderMarker);
                    UpdateScene();
                    break;
                case (Technique.TOMO, _, false, _, _, false, _): // ToMo, marker not on function, _, function window not activated
                    _activeTrialRecord.EnableObject(1);
                    UpdateScene();
                    break;
                case (Technique.TOMO, _, false, _, _, true, _): // ToMo, marker not on function, _, function window activated
                    EndActiveTrial(Result.MISS);
                    break;
                
                case (Technique.MOUSE, true, _, _, _, _, _): // MOUSE, object correctly pressed
                    _activeTrialRecord.EnableAllFunctions();
                    _activeTrialRecord.EnableObject(1);
                    UpdateScene();
                    break;

                
                default:
                    this.TrialInfo($"Unexpected combination of events in Trial#{_activeTrial.Id}");
                    break;
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnFunctionMouseDown(Object sender, MouseButtonEventArgs e)
        {
            LogEvent(Str.FUNCTION_PRESS);

            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnFunctionMouseUp(Object sender, MouseButtonEventArgs e)
        {
            LogEvent(Str.FUNCTION_RELEASE);

            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

            // Function id is sender's tag as int
            var functionId = (int)((FrameworkElement)sender).Tag;

            var technique = _activeBlock.GetGeneralTechnique();
            var objectMarked = GetEventCount(Str.OBJ_RELEASE) > 0;

            this.TrialInfo($"ObjectMarked: {objectMarked};");

            switch (technique, objectMarked)
            {
                case (Technique.TOMO, _):
                    // Nothing for now, handled in OnObjectMouseUp
                    break;

                case (Technique.MOUSE, true): // MOUSE, object marked
                    _activeTrialRecord.ApplyFunction(functionId);
                    UpdateScene();
                    break;
                case (Technique.MOUSE, false): // MOUSE, object not marked
                    EndActiveTrial(Experiment.Result.MISS);
                    break;
                
                default:
                    this.TrialInfo($"Unexpected combination of events in Trial#{_activeTrial.Id}");
                    break;
            }

            
            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void IndexTap()
        {
            var technique = _activeBlock.GetSpecificTechnique();

            this.TrialInfo($"Technique: {_activeBlock.GetSpecificTechnique()}");

            switch (technique)
            {
                case Experiment.Technique.TOMO_TAP:
                    _mainWindow.ActivateAuxWindowMarker(Side.Top);
                    break;

                case Experiment.Technique.TOMO_SWIPE: // Wrong technique for index tap
                    EndActiveTrial(Result.MISS);
                    break;
            }
        }

        public override void ThumbTap()
        {
            var technique = _activeBlock.GetSpecificTechnique();

            this.TrialInfo($"Technique: {_activeBlock.GetSpecificTechnique()}");

            switch (technique)
            {
                case Experiment.Technique.TOMO_TAP:
                    _mainWindow.ActivateAuxWindowMarker(Side.Left);
                    break;

                case Experiment.Technique.TOMO_SWIPE: // Wrong technique for index tap
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
                case Experiment.Technique.TOMO_TAP:
                    _mainWindow.ActivateAuxWindowMarker(Side.Right);
                    break;

                case Experiment.Technique.TOMO_SWIPE: // Wrong technique for index tap
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
            var technique = _activeBlock.GetSpecificTechnique();
            this.TrialInfo($"Technique: {_activeBlock.GetSpecificTechnique()}, Direction: {dir}");

            switch (technique)
            {
                case Experiment.Technique.TOMO_SWIPE:
                    _mainWindow.ActivateAuxWindowMarker(Utils.DirToSide(dir));
                    break;
                case Experiment.Technique.TOMO_TAP: // Wrong technique for thumb swipe
                    EndActiveTrial(Result.MISS);
                    break;
            }
        }
    }
}
