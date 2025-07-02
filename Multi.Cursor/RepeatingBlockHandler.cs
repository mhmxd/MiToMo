using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace Multi.Cursor
{
    public class RepeatingBlockHandler : BlockHandler
    {
        private MainWindow _mainWindow;
        private Block _activeBlock;
        private int _activeTrialNum = 0;
        private Trial _activeTrial;
        //private Dictionary<int, int> _trialTargetIds = new Dictionary<int, int>(); // Trial Id -> Target Button Id
        //private Dictionary<int, Dictionary<int, Point>> _trialStartPositions = new Dictionary<int, Dictionary<int, Point>>(); // Trial Id -> [Dist (px) -> Position]

        private bool _isTargetAvailable = false; // Whether the target is available for clicking

        //private Stopwatch _trialtWatch = new Stopwatch();
        //private Dictionary<string, long> _trialTimestamps = new Dictionary<string, long>(); // Trial timestamps for logging
        //private Dictionary<string, int> _trialEventCounts = new Dictionary<string, int>(); // Ex.: "start_press" -> 1, "target_release" -> 2, etc.

        private Random _random = new Random();

        public RepeatingBlockHandler(MainWindow mainWindow, Block activeBlock)
        {
            _mainWindow = mainWindow;
            _activeBlock = activeBlock;

            // Create records for all trials in the block
            foreach (Trial trial in _activeBlock.Trials)
            {
                _trialRecords[trial.Id] = new TrialRecord();
            }
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

            return true;
        }

        public override bool FindPositionsForTrial(Trial trial)
        {
            int startW = Utils.MM2PX(Experiment.START_WIDTH_MM);
            int startHalfW = startW / 2;
            this.TrialInfo($"Trial#{trial.Id} [Target = {trial.TargetSide.ToString()}, " +
                $"TargetMult = {trial.TargetMultiple}]");

            // Find a random target id for the active trial
            //int targetId = FindRandomTargetIdForTrial(trial);
            int targetId = _mainWindow.GetRadomTargetId(trial.TargetSide, trial.TargetMultiple, Utils.MM2PX(trial.DistRange.Max));
            if (targetId != -1)
            {
                _trialRecords[trial.Id].TargetId = targetId;
                //_trialTargetIds[trial.Id] = targetId;
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

            // Find random Start positions for the number of passes
            //_trialStartPositions[trial.Id] = new Dictionary<int, Point>();
            Point firstStartCenter = new Point(-1, -1); // Other positions must be close the first one
            int maxRetries = 1000; // Max number of retries to find a valid Start position
            int nRetries = 0;
            double randDistMM = trial.DistRange.Min + (_random.NextDouble() * (trial.DistRange.Max - trial.DistRange.Min));
            Point startCenter = startConstraintRect.FindRandPointWithDist(targetCenterAbsolute, Utils.MM2PX(randDistMM), trial.TargetSide.GetOpposite());
            if (startCenter.X == -1) // Couldn't find the first position
            {
                this.TrialInfo($"Failed to find a valid first Start position!");
                return false; // Failed to find a valid position
            }
            else
            {
                // Save the first position
                Point startPositionAbsolute = startCenter.OffsetPosition(-startHalfW, -startHalfW);
                _trialRecords[trial.Id].StartPositions.Add(startPositionAbsolute);
                this.TrialInfo($"Start position 1 added");

                // Find the rest
                for (int p = 0; p < Experiment.REP_TRIAL_NUM_PASS - 1; p++)
                {
                    // Try finding a position for each pass
                    do
                    {
                        randDistMM = trial.DistRange.Min + (_random.NextDouble() * (trial.DistRange.Max - trial.DistRange.Min));
                        startCenter = startConstraintRect.FindRandPointWithDist(targetCenterAbsolute, Utils.MM2PX(randDistMM), trial.TargetSide.GetOpposite());

                        if (nRetries >= maxRetries)
                        {
                            this.TrialInfo($"Failed to find a valid Start position after {maxRetries} retries!");
                            return false; // Failed to find a valid position
                        }

                        nRetries++;
                        this.TrialInfo($"Dist = {Utils.Dist(startCenter, _trialRecords[trial.Id].StartPositions[0]):F2}, Max dist = {Utils.MM2PX(Experiment.REP_TRIAL_MAX_DIST_STARTS_MM):F2}");
                    } while (Utils.Dist(startCenter, _trialRecords[trial.Id].StartPositions[0]) > Utils.MM2PX(Experiment.REP_TRIAL_MAX_DIST_STARTS_MM));

                    // Valid position found
                    startPositionAbsolute = startCenter.OffsetPosition(-startHalfW, -startHalfW);
                    if (startCenter.X == -1 && startCenter.Y == -1) // Failed to find a valid position
                    {
                        this.TrialInfo($"No valid position {p} found for Start!");
                        return false;
                    }
                    else // Valid position found
                    {
                        _trialRecords[trial.Id].StartPositions.Add(startPositionAbsolute); // Add the position to the dictionary
                        this.TrialInfo($"Start position {p} added");
                    }
                }
                

            }


            return true;
        }

        public override void BeginActiveBlock()
        {
            this.TrialInfo("------------------- Beginning block ----------------------------");
            //_trialtWatch.Restart();
            // List all the trials in the block
            foreach (Trial trial in _activeBlock.Trials)
            {
                this.TrialInfo($"Trial#{trial.Id} | Target side: {trial.TargetSide} | Distances: {trial.Distances}");
            }

            _activeTrialNum = 1;
            _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
            ShowActiveTrial();
        }

        public override void ShowActiveTrial()
        {
            this.TrialInfo(Str.MINOR_LINE);
            this.TrialInfo($"Showing rep Trial#{_activeTrial.Id} | Target side: {_activeTrial.TargetSide}");
            this.TrialInfo($"Start positions: {string.Join(", ", _trialRecords[_activeTrial.Id].StartPositions)}");

            // Update the main window label
            _mainWindow.UpdateInfoLabel(_activeTrialNum);

            // Log the trial show timestamp
            _trialRecords[_activeTrial.Id].Timestamps[Str.TRIAL_SHOW] = Timer.GetCurrentMillis();

            // Set the target window based on the trial's target side
            _mainWindow.SetTargetWindow(_activeTrial.TargetSide);

            // Color the target button and set the handlers
            _mainWindow.FillButtonInTargetWindow(_activeTrial.TargetSide, _trialRecords[_activeTrial.Id].TargetId, Config.TARGET_UNAVAILABLE_COLOR);
            _mainWindow.SetGridButtonHandlers(_activeTrial.TargetSide, _trialRecords[_activeTrial.Id].TargetId, OnTargetMouseDown, OnTargetMouseUp);

            // Show the first Start
            Point firstStartPos = _trialRecords[_activeTrial.Id].StartPositions.First();
            _mainWindow.ShowStart(
                firstStartPos, Config.START_AVAILABLE_COLOR,
                OnStartMouseEnter, OnStartMouseLeave, OnStartMouseDown, OnStartMouseUp);
        }

        public override void EndActiveTrial(Experiment.Result result)
        {
            this.TrialInfo($"Trial#{_activeTrial.Id} completed: {result}");
            this.TrialInfo(Str.MAJOR_LINE);
            _trialRecords[_activeTrial.Id].Timestamps[Str.TRIAL_END] = Timer.GetCurrentMillis(); // Log the trial end timestamp

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

        public override void GoToNextTrial()
        {
            _mainWindow.ResetTargetWindow(_activeTrial.TargetSide); // Reset the target window
            //_trialEventCounts.Clear(); // Reset the event counts for the trial
            //_trialTimestamps.Clear(); // Reset the timestamps for the trial
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

        public override void OnStartMouseEnter(Object sender, MouseEventArgs e)
        {
            
        }

        public override void OnStartMouseLeave(Object sender, MouseEventArgs e)
        {
            
        }

        public override void OnMainWindowMouseDown(Object sender, MouseButtonEventArgs e)
        {
            if (_mainWindow.IsTechniqueToMo()) //-- ToMo
            {

                if (_mainWindow.IsGridNavigatorOnButton(_activeTrial.TargetSide, _trialRecords[_activeTrial.Id].TargetId))
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

        public override void OnMainWindowMouseUp(Object sender, MouseButtonEventArgs e)
        {
            if (_mainWindow.IsTechniqueToMo()) //-- ToMo
            {
                string key = Str.TARGET_RELEASE;

                if (_mainWindow.IsGridNavigatorOnButton(_activeTrial.TargetSide, _trialRecords[_activeTrial.Id].TargetId))
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

        public override void OnStartMouseDown(Object sender, MouseButtonEventArgs e)
        {
            // Show the current timestamps
            this.TrialInfo($"Trial timestamps: {string.Join(", ", _trialRecords[_activeTrial.Id].Timestamps.Select(kv => $"{kv.Key}: {kv.Value}"))}");

            if (_mainWindow.IsTechniqueToMo()) //-- ToMo
            {
                if (GetLatestEvent().Key.Contains(Str.START_RELEASE)) // Target press?
                {
                    // If navigator is on the button, count the event
                    if (_mainWindow.IsGridNavigatorOnButton(_activeTrial.TargetSide, _trialRecords[_activeTrial.Id].TargetId))
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
            this.TrialInfo($"Trial timestamps: {string.Join(", ", _trialRecords[_activeTrial.Id].Timestamps.Select(kv => $"{kv.Key}: {kv.Value}"))}");

            if (_mainWindow.IsTechniqueToMo()) //-- ToMo
            {
                if (GetLatestEvent().Key.Contains(Str.START_PRESS)) // Start release?
                {
                    StartRelease();
                }
                else // Target release?
                {
                    // If navigator is on the button, count the event
                    if (_mainWindow.IsGridNavigatorOnButton(_activeTrial.TargetSide, _trialRecords[_activeTrial.Id].TargetId))
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

        public override void OnTargetMouseDown(Object sender, MouseButtonEventArgs e)
        {
            // Show the current timestamps
            this.TrialInfo($"Trial timestamps: {string.Join(", ", _trialRecords[_activeTrial.Id].Timestamps.Select(kv => $"{kv.Key}: {kv.Value}"))}");

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

        public override void OnTargetMouseUp(Object sender, MouseButtonEventArgs e)
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

            if (_trialRecords[_activeTrial.Id].EventCounts[key] == Experiment.REP_TRIAL_NUM_PASS) // All passes done
            {
                EndActiveTrial(Experiment.Result.HIT);
            }
            else // Still passes left
            {
                // Change available/unavailable
                _mainWindow.FillStart(Config.START_UNAVAILABLE_COLOR);
                _mainWindow.FillButtonInTargetWindow(
                    _activeTrial.TargetSide, _trialRecords[_activeTrial.Id].TargetId, Config.TARGET_AVAILABLE_COLOR);
                _isTargetAvailable = true;
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
            //int nStartClicks = _trialEventCounts[Str.START_RELEASE];
            //int nextStartInd = nStartClicks - 1; // Starts are indexed from 0
            this.TrialInfo($"Num of clicks = {_trialRecords[_activeTrial.Id].EventCounts[Str.START_RELEASE]}, num of pos = {_trialRecords[_activeTrial.Id].StartPositions.Count}");
            int startClicksCount = _trialRecords[_activeTrial.Id].EventCounts[Str.START_RELEASE];
            Point startAbsolutePosition = _trialRecords[_activeTrial.Id].StartPositions[startClicksCount];
            _mainWindow.ShowStart(
                startAbsolutePosition, Config.START_AVAILABLE_COLOR,
                OnStartMouseEnter, OnStartMouseLeave, OnStartMouseDown, OnStartMouseUp);
            _mainWindow.FillButtonInTargetWindow(
                _activeTrial.TargetSide, _trialRecords[_activeTrial.Id].TargetId, Config.TARGET_UNAVAILABLE_COLOR);
        }

        private KeyValuePair<string, long> GetLatestEvent()
        {
            if (!_trialRecords[_activeTrial.Id].Timestamps.Any())
            {
                throw new InvalidOperationException("The dictionary is null or empty.");
            }

            // Order the dictionary by timestamp in descending order and take the first one
            var lastEvent = _trialRecords[_activeTrial.Id].Timestamps.OrderByDescending(kv => kv.Value).FirstOrDefault();

            return lastEvent;
        }

        private bool HasEventOccured(string eventName)
        {
            foreach (var kv in _trialRecords[_activeTrial.Id].Timestamps)
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
            if (_trialRecords[_activeTrial.Id].EventCounts.ContainsKey(eventName))
            {
                _trialRecords[_activeTrial.Id].EventCounts[eventName]++;
            }
            else
            {
                _trialRecords[_activeTrial.Id].EventCounts[eventName] = 1;
            }

            string timeKey = eventName + "_" + _trialRecords[_activeTrial.Id].EventCounts[eventName];
            _trialRecords[_activeTrial.Id].Timestamps[timeKey] = Timer.GetCurrentMillis();
        }
    }
}
