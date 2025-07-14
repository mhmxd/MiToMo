using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using static Multi.Cursor.Experiment;

namespace Multi.Cursor
{
    public class AlternatingBlockHandler : BlockHandler
    {
        //private Dictionary<int, Point> _trialStartPosition = new Dictionary<int, Point>(); // Trial Id -> Position

        public AlternatingBlockHandler(MainWindow mainWindow, Block activeBlock)
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

            // If consecutive trials have the same TargetId, re-order them
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
            //    if (_trialRecords[_activeBlock.Trials[i].Id]?.TargetId == _trialRecords[_activeBlock.Trials[i + 1].Id]?.TargetId)
            //    {
            //        return true;
            //    }
            //}

            return false;
        }

        public override bool FindPositionsForTrial(Trial trial)
        {
            int startW = Utils.MM2PX(Experiment.START_WIDTH_MM);
            int startHalfW = startW / 2;
            this.TrialInfo($"Trial#{trial.Id} [Target = {trial.TargetSide.ToString()}, " +
                $"TargetMult = {trial.TargetMultiple}, Dist (mm) = {trial.DistanceMM:F2}, Dist (px) = {trial.DistancePX}]");

            // Ensure TrialRecord exists for this trial
            if (!_trialRecords.ContainsKey(trial.Id))
            {
                _trialRecords[trial.Id] = new TrialRecord();
            }

            // Find a random target id for the active trial
            //int targetId = FindRandomTargetIdForTrial(trial); // Was checking for unique target ids, which couldn't work
            (int targetId, Point targetCenterAbsolute) = _mainWindow.Dispatcher.Invoke(() =>
            {
                return _mainWindow.GetRadomTarget(trial.TargetSide, trial.TargetMultiple, trial.DistancePX);
            });
                
                
            if (targetId != -1)
            {
                _trialRecords[trial.Id].TargetId = targetId;
                this.TrialInfo($"Found Target Id: {targetId} for Trial#{trial.Id}");
            }
            else
            {
                this.TrialInfo($"Failed to find a random target id for Trial#{trial.Id}");
                return false;
            }

            // Get the absolute position of the target center
            //Point targetCenterAbsolute = _mainWindow.GetCenterAbsolutePosition(trial.TargetSide, targetId);

            // Find a position for the Start
            // Get Start constraints
            Rect startConstraintRect = _mainWindow.Dispatcher.Invoke(() =>
            {
                return _mainWindow.GetStartConstraintRect();
            });

            Point objCenter = startConstraintRect.FindRandPointWithDist(targetCenterAbsolute, trial.DistancePX, trial.TargetSide.GetOpposite());
            Point objPositionAbsolute = objCenter.OffsetPosition(-startHalfW, -startHalfW);
            this.TrialInfo($"Target: {targetCenterAbsolute}; Dist (px): {trial.DistancePX}");
            if (objCenter.X == -1 && objCenter.Y == -1) // Failed to find a valid position
            {
                this.TrialInfo($"No valid position found for object!");
                return false;
            }
            else // Valid position found
            {
                this.TrialInfo($"Found object position: {objPositionAbsolute.ToStr()}");
                _trialRecords[trial.Id].Objects.Add(new TObject(1, objPositionAbsolute)); // Object is always 1 in this case
            }

            return true;
        }

        public override void ShowActiveTrial()
        {
            this.TrialInfo($"Showing Trial#{_activeTrial.Id} | Side: {_activeTrial.TargetSide} | W: {_activeTrial.TargetMultiple} | Dist: {_activeTrial.DistanceMM:F2}mm");

            // Update the main window label
            _mainWindow.UpdateInfoLabel(_activeTrialNum, _activeBlock.GetNumTrials());

            // Log the trial show timestamp
            _activeTrialRecord.AddTimestamp(Str.TRIAL_SHOW); 

            // Set the target window based on the trial's target side
            _mainWindow.SetTargetWindow(_activeTrial.TargetSide, OnAuxWindowMouseDown, OnAuxWindowMouseUp);

            // Color the target button and set the handlers
            Brush funcDefaultColor = _mainWindow.IsTechniqueToMo() ? Config.FUNCTION_AVAILABLE_COLOR : Config.FUNCTION_UNAVAILABLE_COLOR;
            _mainWindow.FillButtonInTargetWindow(
                _activeTrial.TargetSide, 
                _activeTrialRecord.TargetId, 
                funcDefaultColor);

            _mainWindow.SetGridButtonHandlers(
                _activeTrial.TargetSide, _activeTrialRecord.TargetId, 
                OnFunctionMouseDown, OnFunctionMouseUp, OnNonTargetMouseDown);

            // Show the first Start
            Brush objDefaultColor = _mainWindow.IsTechniqueToMo() ? Config.OBJ_ENABLED_COLOR : Config.OBJ_ENABLED_COLOR;
            _mainWindow.ShowObjects(
                _activeTrialRecord.Objects,
                objDefaultColor,
                OnObjectMouseEnter, OnObjectMouseLeave, OnObjectMouseDown, OnObjectMouseUp);
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
                case Experiment.Result.OBJ_NOT_CLICKED:
                    Sounder.PlayStartMiss();
                    // Do nothing, just reset everything

                    break;
            }

        }

        public override void GoToNextTrial()
        {
            if (_activeTrialNum < _activeBlock.Trials.Count)
            {
                _mainWindow.ShowStartTrialButton(OnStartButtonMouseUp);
                _mainWindow.ResetTargetWindow(_activeTrial.TargetSide);

                _activeTrialRecord.ClearTimestamps();
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
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

            //if (_mainWindow.IsTechniqueToMo()) ///// ToMo
            //{
            //    if (GetEventCount(Str.OBJ_RELEASE) > 0)
            //    {
            //        if (_mainWindow.IsMarkerOnButton(_activeTrial.TargetSide, _activeTrialRecord.TargetId))
            //        {
            //            TargetPress();
            //        }
            //        else // Navigator is not on the target button
            //        {
            //            EndActiveTrial(Experiment.Result.MISS);
            //            return;
            //        }
            //    }
            //    else // Missed the Start
            //    {
            //        EndActiveTrial(Experiment.Result.OBJ_NOT_CLICKED);
            //        return;
            //    }


            //}
            //else ///// Mouse
            //{
            //    if (GetEventCount(Str.OBJ_RELEASE) > 0) EndActiveTrial(Experiment.Result.MISS);
            //    else EndActiveTrial(Experiment.Result.OBJ_NOT_CLICKED);
            //}

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

            //if (_mainWindow.IsTechniqueToMo()) //-- ToMo
            //{
            //    string key = Str.TARGET_RELEASE;

            //    if (_mainWindow.IsMarkerOnButton(_activeTrial.TargetSide, _activeTrialRecord.TargetId))
            //    {
            //        TargetRelease();
            //    }
            //    else // Navigator is not on the target button
            //    {
            //        EndActiveTrial(Experiment.Result.MISS);
            //    }
            //}
            //else //-- Mouse
            //{
            //    // Nothing specifically
            //}

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnObjectMouseDown(Object sender, MouseButtonEventArgs e)
        {
            // Show the current timestamps
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");



            //if (_mainWindow.IsTechniqueToMo()) //// ToMo
            //{

            //    if (_activeTrialRecord.GetLastTimestamp().Contains(Str.OBJ_RELEASE)) // Target press?
            //    {
            //        // If navigator is on the button, count the event
            //        if (_mainWindow.IsMarkerOnButton(_activeTrial.TargetSide, _activeTrialRecord.TargetId))
            //        {
            //            TargetPress();
            //        }
            //        else // Navator is not on the target button
            //        {
            //            EndActiveTrial(Experiment.Result.MISS);
            //            return;
            //        }
            //    }
            //    else
            //    {
            //        ObjectPress();
            //    }
            //}
            //else //-- Mouse
            //{
            //    ObjectPress();
            //}

            LogEvent(Str.OBJ_PRESS);
            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnObjectMouseUp(Object sender, MouseButtonEventArgs e)
        {
            var technique = _mainWindow.GetActiveTechnique();
            var markerOnFunction = _mainWindow.IsMarkerOnButton(_activeTrial.TargetSide, _activeTrialRecord.TargetId);
            var functionClicked = GetEventCount(Str.FUNCTION_RELEASE) == 1; // Check if the function was clicked

            // Show the current timestamps
            this.TrialInfo($"Technique: {technique}, MarkerOnButton: {markerOnFunction}, FunctionClicked: {functionClicked}");
            switch (technique, markerOnFunction, functionClicked)
            {
                case (Experiment.Technique.Auxursor_Tap, true, _): // Tap, marker on function, _
                    LogEvent(Str.OBJ_RELEASE);
                    EndActiveTrial(Experiment.Result.HIT);
                    break;

                case (Experiment.Technique.Auxursor_Tap, false, _): // Tap, marker not on function, _,
                    // Is it after target window activation or before?
                    if (_mainWindow.IsAuxWindowActivated(_activeTrial.TargetSide)) // Function window is activated
                    {
                        // Hit only if the marker is on the function
                        if (_mainWindow.IsMarkerOnButton(_activeTrial.TargetSide, _activeTrialRecord.TargetId))
                        {
                            EndActiveTrial(Experiment.Result.HIT);
                        }
                        else
                        {
                            EndActiveTrial(Experiment.Result.MISS);
                        }
                    }
                    else // Clicked BEFORE function window activation
                    {
                        SetObjectAsSelected(1);
                    }
                    break;

                case (Experiment.Technique.Mouse, _, true): // Mouse, _, function clicked
                    EndActiveTrial(Experiment.Result.HIT);
                    break;

                case (Experiment.Technique.Mouse, _, false): // Mouse, _, function not clicked
                    SetFunctionAsEnabled();
                    SetObjectAsSelected(1);
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

            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

            switch (technique)
            {
                case Experiment.Technique.Mouse:
                    // Nothing for now
                    break;

            }

            LogEvent(Str.FUNCTION_PRESS);
            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnFunctionMouseUp(Object sender, MouseButtonEventArgs e)
        {
            var technique = _mainWindow.GetActiveTechnique();
            var objectClicked = GetEventCount(Str.OBJ_RELEASE) == 1;

            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

            switch (technique, objectClicked)
            {
                case (Experiment.Technique.Mouse, true): // Mouse, object clicked
                    SetObjectAsEnabled(1);
                    SetFunctionAsSelected();
                    break;
                case (Experiment.Technique.Mouse, false): // Mouse, object not clicked
                    EndActiveTrial(Experiment.Result.MISS);
                    break;
                case (Experiment.Technique.Auxursor_Tap, _):
                    // Nothing for now, handled in OnObjectMouseUp
                    break;
                default:
                    this.TrialInfo($"Unexpected combination of events in Trial#{_activeTrial.Id}");
                    break;
            }

            LogEvent(Str.FUNCTION_RELEASE);
            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void IndexTap()
        {
            var technique = _mainWindow.GetActiveTechnique();

            this.TrialInfo($"Technique: {_mainWindow.GetActiveTechnique()}");

            switch (technique)
            {
                case Experiment.Technique.Auxursor_Tap:
                    _mainWindow.ActivateAuxGridNavigator(Side.Top);
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
                    _mainWindow.ActivateAuxGridNavigator(Side.Left);
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
                    _mainWindow.ActivateAuxGridNavigator(Side.Right);
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
                    _mainWindow.ActivateAuxGridNavigator(Utils.DirToSide(dir));
                    break;
                case Experiment.Technique.Auxursor_Tap: // Wrong technique for thumb swipe
                    EndActiveTrial(Result.MISS);
                    break;
            }
        }
    }
}
