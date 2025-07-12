using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

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
                $"TargetMult = {trial.TargetMultiple}, Dist (mm) = {trial.DistanceMM}, Dist (px) = {trial.DistancePX}]");

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

            Point startCenter = startConstraintRect.FindRandPointWithDist(targetCenterAbsolute, trial.DistancePX, trial.TargetSide.GetOpposite());
            Point startPositionAbsolute = startCenter.OffsetPosition(-startHalfW, -startHalfW);
            this.TrialInfo($"Target: {targetCenterAbsolute}; Dist (px): {trial.DistancePX}; Start pos: {startPositionAbsolute}");
            if (startCenter.X == -1 && startCenter.Y == -1) // Failed to find a valid position
            {
                this.TrialInfo($"No valid position found for Start for dist {trial.DistancePX}!");
                return false;
            }
            else // Valid position found
            {
                _trialRecords[trial.Id].StartPositions.Add(startPositionAbsolute); // Add the position to the trial record
            }

            return true;
        }

        public override void BeginActiveBlock()
        {
            //_trialtWatch.Restart();
            //this.TrialInfo($"Target Ids: {_trialTargetIds.Stringify()}");
            _activeTrialNum = 1;
            _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
           
            ShowActiveTrial();
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
            _mainWindow.FillButtonInTargetWindow(_activeTrial.TargetSide, _activeTrialRecord.TargetId, Config.TARGET_UNAVAILABLE_COLOR);
            _mainWindow.SetGridButtonHandlers(
                _activeTrial.TargetSide, _activeTrialRecord.TargetId, 
                OnTargetMouseDown, OnTargetMouseUp, OnNonTargetMouseDown);

            // Show the first Start
            _mainWindow.ShowStart(
                _activeTrialRecord.StartPositions[0], Config.START_AVAILABLE_COLOR,
                OnStartMouseEnter, OnStartMouseLeave, OnStartMouseDown, OnStartMouseUp);
        }

        public override void EndActiveTrial(Experiment.Result result)
        {
            this.TrialInfo($"Trial#{_activeTrial.Id} completed: {result}");
            this.TrialInfo(Str.MAJOR_LINE);
            _activeTrialRecord.AddTimestamp(Str.TRIAL_END); // Log the trial end timestamp

            switch (result)
            {
                case Experiment.Result.HIT:
                    Sounder.PlayHit();
                    this.TrialInfo($"Trial time = {GetDuration(Str.START_RELEASE + "_1", Str.TRIAL_END)}s");
                    GoToNextTrial();
                    break;
                case Experiment.Result.MISS:
                    Sounder.PlayTargetMiss();
                    _activeBlock.ShuffleBackTrial(_activeTrialNum);
                    GoToNextTrial();
                    break;
                case Experiment.Result.NO_START:
                    Sounder.PlayStartMiss();
                    // Do nothing, just reset everything

                    break;
            }

        }

        public override void GoToNextTrial()
        {
            _mainWindow.ResetTargetWindow(_activeTrial.TargetSide); // Reset the target window
            _activeTrialRecord.ClearTimestamps();

            if (_activeTrialNum < _activeBlock.Trials.Count)
            {
                _activeTrialNum++;
                _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
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
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

            if (_mainWindow.IsTechniqueToMo()) ///// ToMo
            {
                if (GetEventCount(Str.START_RELEASE) > 0)
                {
                    if (_mainWindow.IsMarkerOnButton(_activeTrial.TargetSide, _activeTrialRecord.TargetId))
                    {
                        TargetPress();
                    }
                    else // Navigator is not on the target button
                    {
                        EndActiveTrial(Experiment.Result.MISS);
                        return;
                    }
                }
                else // Missed the Start
                {
                    EndActiveTrial(Experiment.Result.NO_START);
                    return;
                }


            }
            else ///// Mouse
            {
                if (GetEventCount(Str.START_RELEASE) > 0) EndActiveTrial(Experiment.Result.MISS);
                else EndActiveTrial(Experiment.Result.NO_START);
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

            if (_mainWindow.IsTechniqueToMo()) //-- ToMo
            {
                string key = Str.TARGET_RELEASE;

                if (_mainWindow.IsMarkerOnButton(_activeTrial.TargetSide, _activeTrialRecord.TargetId))
                {
                    TargetRelease();
                }
                else // Navigator is not on the target button
                {
                    EndActiveTrial(Experiment.Result.MISS);
                }
            }
            else //-- Mouse
            {
                // Nothing specifically
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnStartMouseDown(Object sender, MouseButtonEventArgs e)
        {
            if (_mainWindow.IsTechniqueToMo()) //-- ToMo
            {
                if (_activeTrialRecord.GetLastTimestamp().Contains(Str.START_RELEASE)) // Target press?
                {
                    // If navigator is on the button, count the event
                    if (_mainWindow.IsMarkerOnButton(_activeTrial.TargetSide, _activeTrialRecord.TargetId))
                    {
                        TargetPress();
                    }
                    else // Navator is not on the target button
                    {
                        EndActiveTrial(Experiment.Result.MISS);
                        return;
                    }
                }
                else
                {
                    StartPress();
                }
            }
            else //-- Mouse
            {
                StartPress();
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnStartMouseUp(Object sender, MouseButtonEventArgs e)
        {

            // Show the current timestamps
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

            if (_mainWindow.IsTechniqueToMo()) //// ToMo
            {
                if (_activeTrialRecord.GetLastTimestamp().Contains(Str.START_PRESS)) // Start release?
                {
                    StartRelease();
                }
                else // Target release?
                {
                    // If navigator is on the button, count the event
                    if (_mainWindow.IsMarkerOnButton(_activeTrial.TargetSide, _activeTrialRecord.TargetId))
                    {
                        TargetRelease();
                    }
                    else // Navator is not on the target button
                    {
                        EndActiveTrial(Experiment.Result.MISS);
                        return;
                    }
                }
            }
            else //// Mouse
            {
                StartRelease();
            }

            e.Handled = true; // Mark the event as handled to prevent further processing

        }

        public override void OnTargetMouseDown(Object sender, MouseButtonEventArgs e)
        {

            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

            if (_mainWindow.GetActiveTechnique() == Experiment.Technique.Mouse) //// Mouse
            {
                TargetPress();
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnTargetMouseUp(Object sender, MouseButtonEventArgs e)
        {

            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

            if (_mainWindow.GetActiveTechnique() == Experiment.Technique.Mouse) //// Mouse
            {
                TargetRelease();
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        private void StartPress()
        {
            LogEvent(Str.START_PRESS);
        }

        private void StartRelease()
        {
            LogEvent(Str.START_RELEASE);

            if (GetEventCount(Str.START_RELEASE) == 2) // Second release
            {
                EndActiveTrial(Experiment.Result.HIT);
            }
            else // Still passes left
            {
                // Change available/unavailable
                _mainWindow.FillStart(Config.START_UNAVAILABLE_COLOR);
                _mainWindow.FillButtonInTargetWindow(
                    _activeTrial.TargetSide, _activeTrialRecord.TargetId, Config.TARGET_AVAILABLE_COLOR);
            }
        }

        private void TargetPress()
        {
            LogEvent(Str.TARGET_PRESS);

            if (GetEventCount(Str.START_RELEASE) == 0) // First pass
            {
                EndActiveTrial(Experiment.Result.NO_START); // End the active trial with NO_START
            }
        }

        private void TargetRelease()
        {
            LogEvent(Str.TARGET_RELEASE);

            if (GetEventCount(Str.START_RELEASE) == 1) // Target clicked after Start click
            {
                _mainWindow.FillButtonInTargetWindow(_activeTrial.TargetSide, _activeTrialRecord.TargetId, Config.TARGET_UNAVAILABLE_COLOR);
                _mainWindow.FillStart(Config.START_AVAILABLE_COLOR);
            }
            else // Target clicked before Start click
            {
                this.TrialInfo($"Target clicked before Start click in Trial#{_activeTrial.Id}. Ignoring.");
            }
        }

        public override void OnOjectMouseDown(object sender, MouseButtonEventArgs e)
        {
            throw new NotImplementedException();
        }

        public override void OnOjectMouseUp(object sender, MouseButtonEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
