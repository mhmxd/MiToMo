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
    public class AlternatingBlockHandler : IBlockHandler
    {
        private MainWindow _mainWindow;
        private Block _activeBlock;
        private int _activeTrialNum = 0;
        private Trial _activeTrial;
        private Dictionary<int, int> _trialTargetIds = new Dictionary<int, int>(); // Trial Id -> Target Button Id
        private Dictionary<int, Point> _trialStartPosition = new Dictionary<int, Point>(); // Trial Id -> Position
        private Stopwatch _trialtWatch = new Stopwatch();
        private Dictionary<string, long> _trialTimestamps = new Dictionary<string, long>(); // Trial timestamps for logging

        public AlternatingBlockHandler(MainWindow mainWindow, Block activeBlock)
        {
            _mainWindow = mainWindow;
            _activeBlock = activeBlock;
        }

        public bool FindPositionsForActiveBlock()
        {
            foreach (Trial trial in _activeBlock.Trials)
            {
                if (!FindPositionsForTrial(trial))
                {
                    this.TrialInfo($"Failed to find positions for Trial#{trial.Id}");
                    return false; // If any trial fails, return false
                }
            }

            return true;
        }

        public bool FindPositionsForTrial(Trial trial)
        {
            int startW = Utils.MM2PX(Experiment.START_WIDTH_MM);
            int startHalfW = startW / 2;
            this.TrialInfo($"Finding positions for Trial#{trial.Id} [Target = {trial.TargetSide.ToString()}, " +
                $"TargetMult = {trial.TargetMultiple}]");

            // Find a random target id for the active trial
            //int targetId = FindRandomTargetIdForTrial(trial); // Was checking for unique target ids, which couldn't work
            int targetId = _mainWindow.GetRadomTargetId(trial.TargetSide, trial.TargetMultiple);
            if (targetId != -1)
            {
                _trialTargetIds[trial.Id] = targetId;
                this.TrialInfo($"Found Target Id: {targetId} for Trial#{trial.Id}");
            }
            else
            {
                this.TrialInfo($"Failed to find a random target id for Trial#{trial.Id}");
                return false;
            }

            // Get the absolute position of the target center
            Point targetCenterAbsolute = _mainWindow.GetCenterAbsolutePosition(trial.TargetSide, targetId);

            // Find a Start position for each distance in the passes
            _trialStartPosition[trial.Id] = new Point(); // Initialize the start position dictionary for this trial
            
            // Find a position for the Start
            Rect startConstraintRect = _mainWindow.GetStartConstraintRect();
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
                _trialStartPosition[trial.Id] = startPositionAbsolute; // Add the position to the dictionary
            }

            return true;
        }

        private int FindRandomTargetIdForTrial(Trial trial)
        {
            // Based on the width multiple, find a random target button id that haven't been used before
            int targetMultiple = trial.TargetMultiple;
            int targetId = -1;
            do
            {
                targetId = _mainWindow.GetRadomTargetId(trial.TargetSide, targetMultiple);
            } while (_trialTargetIds.ContainsValue(targetId));

            return targetId;

        }

        public void BeginActiveBlock()
        {
            _trialtWatch.Restart();
            this.TrialInfo($"Target Ids: {_trialTargetIds.Stringify()}");
            _activeTrialNum = 1;
            _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
            ShowActiveTrial();
        }

        public void ShowActiveTrial()
        {
            this.TrialInfo($"Showing Trial#{_activeTrial.Id} | Side: {_activeTrial.TargetSide} | W: {_activeTrial.TargetMultiple} | Dist: {_activeTrial.DistancePX}");
            // Log the trial show timestamp
            _trialTimestamps[Str.TRIAL_SHOW] = _trialtWatch.ElapsedMilliseconds; 

            // Set the target window based on the trial's target side
            _mainWindow.SetTargetWindow(_activeTrial.TargetSide);

            // Color the target button and set the handlers
            _mainWindow.FillButtonInTargetWindow(_activeTrial.TargetSide, _trialTargetIds[_activeTrial.Id], Config.TARGET_UNAVAILABLE_COLOR);
            _mainWindow.SetGridButtonHandlers(_activeTrial.TargetSide, _trialTargetIds[_activeTrial.Id], OnTargetMouseDown, OnTargetMouseUp);

            // Show the first Start
            _mainWindow.ShowStart(
                _trialStartPosition[_activeTrial.Id], Config.START_AVAILABLE_COLOR,
                OnStartMouseEnter, OnStartMouseLeave, OnStartMouseDown, OnStartMouseUp);
        }

        public void EndActiveTrial(Experiment.Result result)
        {
            this.TrialInfo($"Trial#{_activeTrial.Id} completed.");
            _trialTimestamps[Str.TRIAL_END] = _trialtWatch.ElapsedMilliseconds; // Log the trial end timestamp

            switch (result)
            {
                case Experiment.Result.HIT:
                    Sounder.PlayHit();
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

            this.TrialInfo("-----------------------------------------------------------------------");
            
        }

        public void GoToNextTrial()
        {
            _mainWindow.ResetTargetWindow(_activeTrial.TargetSide); // Reset the target window
            _trialTimestamps.Clear();

            if (_activeTrialNum < _activeBlock.Trials.Count)
            {
                _activeTrialNum++;
                _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
                ShowActiveTrial();
            }
            else
            {
                this.TrialInfo("End of block reached.");
            }
        }

        public void OnStartMouseEnter(Object sender, MouseEventArgs e)
        {
            if (_trialTimestamps.ContainsKey(Str.TARGET_RELEASE))
            {
                _trialTimestamps[Str.START2_LAST_ENTRY] = _trialtWatch.ElapsedMilliseconds;
            } else
            {
                _trialTimestamps[Str.START1_LAST_ENTRY] = _trialtWatch.ElapsedMilliseconds;
            }
            
        }

        public void OnStartMouseLeave(Object sender, MouseEventArgs e)
        {
            if (_trialTimestamps.ContainsKey(Str.TARGET_RELEASE))
            {
                _trialTimestamps[Str.START2_LAST_EXIT] = _trialtWatch.ElapsedMilliseconds;
            }
            else
            {
                _trialTimestamps[Str.START1_LAST_EXIT] = _trialtWatch.ElapsedMilliseconds;
            }
        }

        public void OnMainWindowMouseDown(Object sender, MouseButtonEventArgs e)
        {
            if (_mainWindow.IsTechniqueToMo()) //-- ToMo
            {
                if (_trialTimestamps.ContainsKey(Str.START_RELEASE_ONE)) // Target click
                {
                    if (_mainWindow.IsGridNavigatorOnButton(_activeTrial.TargetSide, _trialTargetIds[_activeTrial.Id])) // Navigator on button
                    {
                        TargetPress();
                    }
                    else // Click outside Target
                    {
                        EndActiveTrial(Experiment.Result.MISS); // End the active trial with MISS
                        return;
                    }
                }
                else
                {
                    EndActiveTrial(Experiment.Result.MISS); // End the active trial with MISS
                    return;
                }
            }
            else //-- Mouse
            {
                if (_trialTimestamps.ContainsAny(Str.TARGET_RELEASE, Str.START_RELEASE_ONE)) // Phase 3: Window press => Outside Start
                {
                    EndActiveTrial(Experiment.Result.MISS);
                }
                else // Phase 1: Aiming for Start
                {
                    EndActiveTrial(Experiment.Result.NO_START);
                }
            }
        }

        public void OnMainWindowMouseUp(Object sender, MouseButtonEventArgs e)
        {
            if (_mainWindow.IsTechniqueToMo()) //-- ToMo
            {
                if (_trialTimestamps.ContainsKey(Str.TARGET_PRESS)) // Target click
                {
                    if (_mainWindow.IsGridNavigatorOnButton(_activeTrial.TargetSide, _trialTargetIds[_activeTrial.Id])) // Navigator on button
                    {
                        TargetRelease(); // Release the target
                    }
                    else // Navigator moved
                    {
                        EndActiveTrial(Experiment.Result.MISS);
                        return;
                    }
                }
            }
            else //-- Mouse
            {
                if (_trialTimestamps.ContainsAny(Str.START_PRESS_TWO, Str.START_PRESS_ONE, Str.TARGET_PRESS)) // Released outside
                {
                    EndActiveTrial(Experiment.Result.MISS);
                }
            }
        }

        public void OnStartMouseDown(Object sender, MouseButtonEventArgs e)
        {
            if (_mainWindow.IsTechniqueToMo()) //-- ToMo
            {
                if (GetLatestEvent().Key == Str.START_RELEASE_ONE) // Target click
                {
                    if (_mainWindow.IsGridNavigatorOnButton(_activeTrial.TargetSide, _trialTargetIds[_activeTrial.Id])) // Navigator on button
                    {
                        TargetPress();
                    }
                    else // Click outside Target
                    {
                        EndActiveTrial(Experiment.Result.MISS); // End the active trial with MISS
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

        public void OnStartMouseUp(Object sender, MouseButtonEventArgs e)
        {
            if (_mainWindow.IsTechniqueToMo()) //-- ToMo
            {
                if (GetLatestEvent().Key == Str.TARGET_PRESS) // Target click
                {
                    if (_mainWindow.IsGridNavigatorOnButton(_activeTrial.TargetSide, _trialTargetIds[_activeTrial.Id])) // Navigator on button
                    {
                        TargetRelease(); // Release the target
                    }
                    else // Navigator moved
                    {
                        EndActiveTrial(Experiment.Result.MISS);
                        return;
                    }
                }
                else
                {
                    StartRelease();
                }
            }
            else //-- Mouse
            {
                StartRelease();
            }

            e.Handled = true; // Mark the event as handled to prevent further processing

        }

        public void OnTargetMouseDown(Object sender, MouseButtonEventArgs e)
        {
            if (_mainWindow.IsTechniqueToMo()) //-- ToMo => Shouldn't click on the Target
            {
                EndActiveTrial(Experiment.Result.MISS);
                return;
            }
            else //-- Mouse
            {
                TargetPress();
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public void OnTargetMouseUp(Object sender, MouseButtonEventArgs e)
        {
            if (_mainWindow.IsTechniqueToMo()) //-- ToMo
            {
                // Nothing to do here, as the target should not be clicked
            }
            else //-- Mouse
            {
                TargetRelease();
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        private void StartPress()
        {
            if (_trialTimestamps.ContainsKey(Str.TARGET_RELEASE)) // Second Start click
            {
                _trialTimestamps[Str.START_PRESS_TWO] = _trialtWatch.ElapsedMilliseconds;
            }
            else // First Start click
            {
                _trialTimestamps[Str.START_PRESS_ONE] = _trialtWatch.ElapsedMilliseconds;
            }
        }

        private void StartRelease()
        {
            this.TrialInfo($"Trial#{_activeTrial.Id} Timestamps: {string.Join(", ", _trialTimestamps.Select(kv => $"{kv.Key}: {kv.Value}"))}");
            if (_trialTimestamps.ContainsKey(Str.START_PRESS_TWO)) // Second Start click => End trial
            {
                _trialTimestamps[Str.START_RELEASE_TWO] = _trialtWatch.ElapsedMilliseconds;

                EndActiveTrial(Experiment.Result.HIT); // End the active trial with HIT
            }
            else // First Start click
            {
                _trialTimestamps[Str.START_RELEASE_ONE] = _trialtWatch.ElapsedMilliseconds;

                // Change Target and Start colors
                _mainWindow.FillButtonInTargetWindow(
                    _activeTrial.TargetSide,
                    _trialTargetIds[_activeTrial.Id],
                    Config.TARGET_AVAILABLE_COLOR);
                _mainWindow.FillStart(Config.START_UNAVAILABLE_COLOR);
            }
        }

        private void TargetPress()
        {
            this.TrialInfo($"Trial#{_activeTrial.Id} Timestamps: {string.Join(", ", _trialTimestamps.Select(kv => $"{kv.Key}: {kv.Value}"))}");
            if (_trialTimestamps.ContainsKey(Str.START_RELEASE_ONE)) // First pass
            {
                _trialTimestamps[Str.TARGET_PRESS] = _trialtWatch.ElapsedMilliseconds;
            }
            else // No Start click yet
            {
                this.TrialInfo($"Target clicked before Start click in Trial#{_activeTrial.Id}. Ignoring.");
                return; // Ignore the target click if no Start click has been made
            }
        }

        private void TargetRelease()
        {
            this.TrialInfo($"Trial#{_activeTrial.Id} Timestamps: {string.Join(", ", _trialTimestamps.Select(kv => $"{kv.Key}: {kv.Value}"))}");
            if (_trialTimestamps.ContainsKey(Str.TARGET_PRESS)) // Target clicked after Start click
            {
                _trialTimestamps[Str.TARGET_RELEASE] = _trialtWatch.ElapsedMilliseconds;

                _mainWindow.FillButtonInTargetWindow(_activeTrial.TargetSide, _trialTargetIds[_activeTrial.Id], Config.TARGET_UNAVAILABLE_COLOR);
                _mainWindow.FillStart(Config.START_AVAILABLE_COLOR);
            }
            else // Target clicked before Start click
            {
                this.TrialInfo($"Target clicked before Start click in Trial#{_activeTrial.Id}. Ignoring.");
            }
        }

        private KeyValuePair<string, long> GetLatestEvent()
        {
            if (_trialTimestamps == null || !_trialTimestamps.Any())
            {
                throw new InvalidOperationException("The dictionary is null or empty.");
            }

            // Order the dictionary by timestamp in descending order and take the first one
            var lastEvent = _trialTimestamps.OrderByDescending(kv => kv.Value).FirstOrDefault();

            return lastEvent;
        }



    }
}
