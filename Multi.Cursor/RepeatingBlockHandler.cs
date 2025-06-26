using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Multi.Cursor
{
    public class RepeatingBlockHandler : IBlockHandler
    {
        private MainWindow _mainWindow;
        private Block _activeBlock;
        private int _activeTrialNum = 0;
        private Trial _activeTrial;
        private Dictionary<int, int> _trialTargetIds = new Dictionary<int, int>(); // Trial Id -> Target Button Id
        private Dictionary<int, Dictionary<int, Point>> _trialStartPositions = new Dictionary<int, Dictionary<int, Point>>(); // Trial Id -> [Dist (px) -> Position]

        private bool _isTargetAvailable = false; // Whether the target is available for clicking

        private Stopwatch _trialtWatch = new Stopwatch();
        private Dictionary<string, long> _trialTimestamps = new Dictionary<string, long>(); // Trial timestamps for logging
        private Dictionary<string, int> _trialEventCounts = new Dictionary<string, int>(); // Ex.: "start_press" -> 1, "target_release" -> 2, etc.

        public RepeatingBlockHandler(MainWindow mainWindow, Block activeBlock)
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
            //int targetId = FindRandomTargetIdForTrial(trial);
            int targetId = _mainWindow.GetRadomTargetId(trial.TargetSide, trial.TargetMultiple);
            if (targetId != -1)
            {
                _trialTargetIds[trial.Id] = targetId;
            }
            else
            {
                this.TrialInfo($"Failed to find a random target id for Trial#{trial.Id}");
                return false;
            }

            // Get the absolute position of the target center
            Point targetCenterAbsolute = _mainWindow.GetCenterAbsolutePosition(trial.TargetSide, targetId);

            // Get Start constraints
            Rect startConstraintRect = _mainWindow.GetStartConstraintRect();

            // Find a Start position for each distance in the passes
            _trialStartPositions[trial.Id] = new Dictionary<int, Point>();
            Point firstStartCenter = new Point(-1, -1); // Other positions must be close the first one
            int maxRetries = 1000; // Max number of retries to find a valid Start position
            int nRetries = 0;
            foreach (int dist in trial.Distances)
            {
                Point startCenter = startConstraintRect.FindRandPointWithDist(targetCenterAbsolute, dist, trial.TargetSide.GetOpposite());

                if (firstStartCenter.X != -1) // Not the first Start
                {
                    
                    while (Utils.Dist(startCenter, firstStartCenter) > Utils.MM2PX(Experiment.REP_TRIAL_MAX_DIST_STARTS_MM))
                    {
                        this.TrialInfo($"Distance to first Start = {Utils.PX2MM(Utils.Dist(startCenter, firstStartCenter))}");
                        if (nRetries >= maxRetries)
                        {
                            this.TrialInfo($"Failed to find a valid Start position for dist {dist} after {maxRetries} retries!");
                            return false; // Failed to find a valid position
                        }
                        startCenter = startConstraintRect.FindRandPointWithDist(targetCenterAbsolute, dist, trial.TargetSide.GetOpposite());
                        nRetries++;
                    }
                } 
                else
                {
                    firstStartCenter = startCenter; // Save the first Start position
                }

                Point startPositionAbsolute = startCenter.OffsetPosition(-startHalfW, -startHalfW);

                this.TrialInfo($"Target: {targetCenterAbsolute}; Dist (px): {dist}; Start pos: {startPositionAbsolute}");
                if (startCenter.X == -1 && startCenter.Y == -1) // Failed to find a valid position
                {
                    this.TrialInfo($"No valid position found for Start for dist {dist}!");
                    return false;
                }
                else // Valid position found
                {
                    _trialStartPositions[trial.Id][dist] = startPositionAbsolute; // Add the position to the dictionary
                }
            }

            return true;
        }

        public void BeginActiveBlock()
        {
            this.TrialInfo("------------------- Beginning block ----------------------------");
            _trialtWatch.Restart();
            // List all the trials in the block
            foreach (Trial trial in _activeBlock.Trials)
            {
                this.TrialInfo($"Trial#{trial.Id} | Target side: {trial.TargetSide} | Distances: {trial.Distances}");
            }
            _trialtWatch.Restart();
            _activeTrialNum = 1;
            _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
            ShowActiveTrial();
        }

        public void ShowActiveTrial()
        {
            this.TrialInfo(Str.MINOR_LINE);
            this.TrialInfo($"Showing rep Trial#{_activeTrial.Id} | Target side: {_activeTrial.TargetSide}");
            this.TrialInfo($"Start positions: {string.Join(", ", _trialStartPositions[_activeTrial.Id])}");

            // Log the trial show timestamp
            _trialTimestamps[Str.TRIAL_SHOW] = _trialtWatch.ElapsedMilliseconds;

            // Set the target window based on the trial's target side
            _mainWindow.SetTargetWindow(_activeTrial.TargetSide);

            // Color the target button and set the handlers
            _mainWindow.FillButtonInTargetWindow(_activeTrial.TargetSide, _trialTargetIds[_activeTrial.Id], Config.TARGET_UNAVAILABLE_COLOR);
            _mainWindow.SetGridButtonHandlers(_activeTrial.TargetSide, _trialTargetIds[_activeTrial.Id], OnTargetMouseDown, OnTargetMouseUp);

            // Show the first Start
            Point firstStartPos = _trialStartPositions[_activeTrial.Id].First().Value;
            _mainWindow.ShowStart(
                firstStartPos, Config.START_AVAILABLE_COLOR,
                OnStartMouseEnter, OnStartMouseLeave, OnStartMouseDown, OnStartMouseUp);
        }

        public void EndActiveTrial(Experiment.Result result)
        {
            this.TrialInfo($"Trial#{_activeTrial.Id} completed: {result}");
            this.TrialInfo(Str.MAJOR_LINE);
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

        }

        public void GoToNextTrial()
        {
            _mainWindow.ResetTargetWindow(_activeTrial.TargetSide); // Reset the target window
            _trialEventCounts.Clear(); // Reset the event counts for the trial
            _trialTimestamps.Clear(); // Reset the timestamps for the trial
            _isTargetAvailable = false; // Reset the target availability

            if (_activeTrialNum < _activeBlock.Trials.Count)
            {
                _activeTrialNum++;
                _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
                ShowActiveTrial();
            }
            else
            {
                this.TrialInfo("All trials in the block completed.");
            }
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

        public void OnStartMouseEnter(Object sender, MouseEventArgs e)
        {
            
        }

        public void OnStartMouseLeave(Object sender, MouseEventArgs e)
        {
            
        }

        public void OnMainWindowMouseDown(Object sender, MouseButtonEventArgs e)
        {
            if (_mainWindow.IsTechniqueToMo()) //-- ToMo
            {

                if (_mainWindow.IsGridNavigatorOnButton(_activeTrial.TargetSide, _trialTargetIds[_activeTrial.Id]))
                {
                    TargetPress();
                }
                else // Navigator is not on the target button
                {
                    EndActiveTrial(Experiment.Result.MISS);
                    return;
                }
            }
            else //-- Mouse
            {
                if (HasEventOccured(Str.START_RELEASE)) EndActiveTrial(Experiment.Result.MISS);
                else EndActiveTrial(Experiment.Result.NO_START);
            }
        }

        public void OnMainWindowMouseUp(Object sender, MouseButtonEventArgs e)
        {
            if (_mainWindow.IsTechniqueToMo()) //-- ToMo
            {
                string key = Str.TARGET_RELEASE;

                if (_mainWindow.IsGridNavigatorOnButton(_activeTrial.TargetSide, _trialTargetIds[_activeTrial.Id]))
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
        }

        public void OnStartMouseDown(Object sender, MouseButtonEventArgs e)
        {
            // Show the current timestamps
            this.TrialInfo($"Trial timestamps: {string.Join(", ", _trialTimestamps.Select(kv => $"{kv.Key}: {kv.Value}"))}");

            if (_mainWindow.IsTechniqueToMo()) //-- ToMo
            {
                if (GetLatestEvent().Key.Contains(Str.START_RELEASE)) // Target press?
                {
                    // If navigator is on the button, count the event
                    if (_mainWindow.IsGridNavigatorOnButton(_activeTrial.TargetSide, _trialTargetIds[_activeTrial.Id]))
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

        public void OnStartMouseUp(Object sender, MouseButtonEventArgs e)
        {
            // Show the current timestamps
            this.TrialInfo($"Trial timestamps: {string.Join(", ", _trialTimestamps.Select(kv => $"{kv.Key}: {kv.Value}"))}");

            if (_mainWindow.IsTechniqueToMo()) //-- ToMo
            {
                if (GetLatestEvent().Key.Contains(Str.START_PRESS)) // Start release?
                {
                    StartRelease();
                }
                else // Target release?
                {
                    // If navigator is on the button, count the event
                    if (_mainWindow.IsGridNavigatorOnButton(_activeTrial.TargetSide, _trialTargetIds[_activeTrial.Id]))
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
            else //-- Mouse
            {
                StartRelease();
            }
            
            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public void OnTargetMouseDown(Object sender, MouseButtonEventArgs e)
        {
            // Show the current timestamps
            this.TrialInfo($"Trial timestamps: {string.Join(", ", _trialTimestamps.Select(kv => $"{kv.Key}: {kv.Value}"))}");

            string key = Str.TARGET_PRESS;

            if (_mainWindow.IsTechniqueToMo()) //-- ToMo
            {

            }
            else //-- Mouse
            {
                TargetPress();
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public void OnTargetMouseUp(Object sender, MouseButtonEventArgs e)
        {
            string key = Str.TARGET_RELEASE;

            if (_mainWindow.IsTechniqueToMo()) //-- ToMo
            {

            }
            else //-- Mouse
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
            string key = Str.START_RELEASE;

            LogEvent(key);

            if (_trialEventCounts[key] == _activeTrial.Distances.Count) // All passes done
            {
                EndActiveTrial(Experiment.Result.HIT);
            }
            else // Still passes left
            {
                _mainWindow.FillStart(Config.START_UNAVAILABLE_COLOR);
                _mainWindow.FillButtonInTargetWindow(
                    _activeTrial.TargetSide, _trialTargetIds[_activeTrial.Id], Config.TARGET_AVAILABLE_COLOR);
                _isTargetAvailable = true; // Target is now available for clicking
            }
        }

        private void TargetPress()
        {
            if (!GetLatestEvent().Key.Contains(Str.START_RELEASE)) // Not following the sequence => MISS
            {
                EndActiveTrial(Experiment.Result.MISS);
                return;
            }

            // Continue normally
            LogEvent(Str.TARGET_PRESS);
        }

        private void TargetRelease()
        {
            LogEvent(Str.TARGET_RELEASE);

            // Show the next Start
            int nStartClicks = _trialEventCounts[Str.START_RELEASE];
            //int nextStartInd = nStartClicks - 1; // Starts are indexed from 0
            Point startAbsolutePosition = _trialStartPositions[_activeTrial.Id].Values.ToList()[nStartClicks];
            _mainWindow.ShowStart(
                startAbsolutePosition, Config.START_AVAILABLE_COLOR,
                OnStartMouseEnter, OnStartMouseLeave, OnStartMouseDown, OnStartMouseUp);
            _mainWindow.FillButtonInTargetWindow(
                _activeTrial.TargetSide, _trialTargetIds[_activeTrial.Id], Config.TARGET_UNAVAILABLE_COLOR);
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

        private bool HasEventOccured(string eventName)
        {
            foreach (var kv in _trialTimestamps)
            {
                if (kv.Key.StartsWith(eventName))
                {
                    return true; // Event has occurred
                }
            }

            return false; // Event has not occurred
        }

        private void LogEvent(string eventName)
        {
            if (_trialEventCounts.ContainsKey(eventName))
            {
                _trialEventCounts[eventName]++;
            }
            else
            {
                _trialEventCounts[eventName] = 1;
            }

            _trialTimestamps[eventName + "_" + _trialEventCounts[eventName]] = _trialtWatch.ElapsedMilliseconds;
        }



    }
}
