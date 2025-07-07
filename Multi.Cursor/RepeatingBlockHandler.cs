using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace Multi.Cursor
{
    public class RepeatingBlockHandler : BlockHandler
    {
        private Block _activeBlock;
        private int _activeTrialNum = 0;
        //private Dictionary<int, int> _trialTargetIds = new Dictionary<int, int>(); // Trial Id -> Target Button Id
        //private Dictionary<int, Dictionary<int, Point>> _trialStartPositions = new Dictionary<int, Dictionary<int, Point>>(); // Trial Id -> [Dist (px) -> Position]

        private bool _isTargetAvailable = false; // Whether the target is available for clicking

        //private Stopwatch _trialtWatch = new Stopwatch();
        //private Dictionary<string, long> _trialTimestamps = new Dictionary<string, long>(); // Trial timestamps for logging
        //private Dictionary<string, int> _trialEventCounts = new Dictionary<string, int>(); // Ex.: "start_press" -> 1, "target_release" -> 2, etc.

        private const string CacheDirectory = "TrialPositionCache";
        private const int MaxCachedPositions = 100;

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

            // Make sure the required directory exists
            if (!Directory.Exists(CacheDirectory))
            {
                Directory.CreateDirectory(CacheDirectory);
                this.TrialInfo($"Created cache directory at: {Path.GetFullPath(CacheDirectory)}");
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

            // If consecutive trials have the same TargetId, re-order them
            while (IsTargetRepeated())
            {
                _activeBlock.Trials.Shuffle();
            }

            // Show all the target ids for the trials
            this.TrialInfo($"Target IDs: {string.Join(", ", _activeBlock.Trials.Select(t => $"{_trialRecords[t.Id].TargetId}"))}");
            return true;
        }

        private bool IsTargetRepeated()
        {
            for (int i = 0; i < _activeBlock.Trials.Count - 1; i++)
            {
                if (_trialRecords[_activeBlock.Trials[i].Id].TargetId == _trialRecords[_activeBlock.Trials[i + 1].Id].TargetId)
                {
                    return true;
                }
            }

            return false;
        }

        //public override bool FindPositionsForTrial(Trial trial)
        //{
        //    int startW = Utils.MM2PX(Experiment.START_WIDTH_MM);
        //    int startHalfW = startW / 2;
        //    this.TrialInfo($"Trial#{trial.Id} [Target = {trial.TargetSide.ToString()}, " +
        //        $"TargetMult = {trial.TargetMultiple}, Dist range (mm) = {trial.DistRange.ToString()}]");

        //    // Find a random target id for the active trial
        //    //int targetId = FindRandomTargetIdForTrial(trial);
        //    (int targetId, Point targetCenterAbsolute) = _mainWindow.Dispatcher.Invoke(() =>
        //    {
        //        return _mainWindow.GetRadomTarget(trial.TargetSide, trial.TargetMultiple, trial.DistRange);
        //    });

        //    if (targetId != -1)
        //    {
        //        _trialRecords[trial.Id].TargetId = targetId;
        //        //_trialTargetIds[trial.Id] = targetId;
        //    }
        //    else
        //    {
        //        this.TrialInfo($"Failed to find a random target id for Trial#{trial.Id}");
        //        return false;
        //    }

        //    // Get the absolute position of the target center
        //    //Point targetCenterAbsolute = _mainWindow.GetCenterAbsolutePosition(trial.TargetSide, targetId);

        //    // Get Start constraints
        //    Rect startConstraintRect = _mainWindow.Dispatcher.Invoke(() =>
        //    {
        //        return _mainWindow.GetStartConstraintRect();
        //    }); 

        //    // Find random Start positions for the number of passes
        //    //_trialStartPositions[trial.Id] = new Dictionary<int, Point>();
        //    Point firstStartCenter = new Point(-1, -1); // Other positions must be close the first one
        //    int maxRetries = 1000; // Max number of retries to find a valid Start position
        //    int nRetries = 0;
        //    double randDistMM = trial.DistRange.Min + (_random.NextDouble() * (trial.DistRange.Max - trial.DistRange.Min));
        //    Point startCenter = startConstraintRect.FindRandPointWithDist(targetCenterAbsolute, Utils.MM2PX(randDistMM), trial.TargetSide.GetOpposite());
        //    if (startCenter.X == -1) // Couldn't find the first position
        //    {
        //        this.TrialInfo($"Failed to find a valid first Start position!");
        //        return false; // Failed to find a valid position
        //    }
        //    else
        //    {
        //        // Save the first position
        //        Point startPositionAbsolute = startCenter.OffsetPosition(-startHalfW, -startHalfW);
        //        _trialRecords[trial.Id].StartPositions.Add(startPositionAbsolute);
        //        //this.TrialInfo($"Start position 1 added");

        //        // Find the rest
        //        for (int p = 0; p < Experiment.REP_TRIAL_NUM_PASS - 1; p++)
        //        {
        //            // Try finding a position for each pass
        //            do
        //            {
        //                randDistMM = trial.DistRange.Min + (_random.NextDouble() * (trial.DistRange.Max - trial.DistRange.Min));
        //                startCenter = startConstraintRect.FindRandPointWithDist(targetCenterAbsolute, Utils.MM2PX(randDistMM), trial.TargetSide.GetOpposite());

        //                if (nRetries >= maxRetries)
        //                {
        //                    this.TrialInfo($"Failed to find a valid Start position after {maxRetries} retries!");
        //                    return false; // Failed to find a valid position
        //                }

        //                nRetries++;
        //                //this.TrialInfo($"Dist = {Utils.Dist(startCenter, _trialRecords[trial.Id].StartPositions[0]):F2}, Max dist = {Utils.MM2PX(Experiment.REP_TRIAL_MAX_DIST_STARTS_MM):F2}");
        //            } while (Utils.Dist(startCenter, _trialRecords[trial.Id].StartPositions[0]) > Utils.MM2PX(Experiment.REP_TRIAL_MAX_DIST_STARTS_MM));

        //            // Valid position found
        //            startPositionAbsolute = startCenter.OffsetPosition(-startHalfW, -startHalfW);
        //            if (startCenter.X == -1 && startCenter.Y == -1) // Failed to find a valid position
        //            {
        //                this.TrialInfo($"No valid position {p} found for Start!");
        //                return false;
        //            }
        //            else // Valid position found
        //            {
        //                _trialRecords[trial.Id].StartPositions.Add(startPositionAbsolute); // Add the position to the dictionary
        //                //this.TrialInfo($"Start position {p} added");
        //            }
        //        }


        //    }


        //    return true;
        //}

        public override bool FindPositionsForTrial(Trial trial)
        {
            int startW = Utils.MM2PX(Experiment.START_WIDTH_MM);
            int startHalfW = startW / 2;
            //this.TrialInfo($"Trial#{trial.Id} [Target = {trial.TargetSide.ToString()}, " +
            //    $"TargetMult = {trial.TargetMultiple}, Dist range (mm) = {trial.DistRange.ToString()}]");

            // Ensure TrialRecord exists for this trial
            if (!_trialRecords.ContainsKey(trial.Id))
            {  
                _trialRecords[trial.Id] = new TrialRecord();
            }

            // --- Attempt to find new positions ---
            bool success = TryFindNewPositions(trial, startW, startHalfW);

            if (success)
            {
                // If new positions were successfully found, save them to cache
                SavePositionsToCache(trial, new CachedTrialPositions
                {
                    TargetId = _trialRecords[trial.Id].TargetId,
                    StartPositions = _trialRecords[trial.Id].StartPositions
                });
                return true;
            }
            else
            {
                // If finding new positions failed, attempt to load from cache
                //this.TrialInfo($"Failed to find new positions for Trial#{trial.Id}. Attempting to use cached positions.");
                List<CachedTrialPositions> cachedData = LoadPositionsFromCache(trial);
                if (cachedData.Any())
                {
                    // Pick a random valid cached entry
                    var randomCachedEntry = cachedData.Where(c => c.StartPositions.Count == Experiment.REP_TRIAL_NUM_PASS).ToList();
                    if (randomCachedEntry.Any())
                    {
                        var selectedEntry = randomCachedEntry[_random.Next(randomCachedEntry.Count)];
                        _trialRecords[trial.Id].TargetId = selectedEntry.TargetId;
                        _trialRecords[trial.Id].StartPositions = new List<Point>(selectedEntry.StartPositions);
                        //this.TrialInfo($"Using random cached positions for Trial#{trial.Id}");
                        return true;
                    }
                }
                //this.TrialInfo($"No valid cached positions found for Trial#{trial.Id} either.");
                return false; // No cached or new positions found
            }
        }

        public override void BeginActiveBlock()
        {
            this.TrialInfo("------------------- Beginning block ----------------------------");
            //_trialtWatch.Restart();
            // List all the trials in the block
            //foreach (Trial trial in _activeBlock.Trials)
            //{
            //    this.TrialInfo($"Trial#{trial.Id} | Target side: {trial.TargetSide} | Distances: {trial.Distances}");
            //}

            _activeTrialNum = 1;
            _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
            ShowActiveTrial();
        }

        public override void ShowActiveTrial()
        {
            this.TrialInfo(Str.MINOR_LINE);
            this.TrialInfo($"Showing rep Trial#{_activeTrial.Id} | Target side: {_activeTrial.TargetSide} " +
                $"| Dist (mm): ({_activeTrial.DistRangeMM.Label}) {_activeTrial.DistRangeMM.ToString()}");
            //this.TrialInfo($"Start positions: {string.Join(", ", _trialRecords[_activeTrial.Id].StartPositions)}");

            // Update the main window label
            _mainWindow.UpdateInfoLabel(_activeTrialNum);

            // Log the trial show timestamp
            _trialRecords[_activeTrial.Id].Timestamps[Str.TRIAL_SHOW] = Timer.GetCurrentMillis();

            // Set the target window based on the trial's target side
            _mainWindow.SetTargetWindow(_activeTrial.TargetSide);

            // Color the target button and set the handlers
            _mainWindow.FillButtonInTargetWindow(_activeTrial.TargetSide, _trialRecords[_activeTrial.Id].TargetId, Config.TARGET_UNAVAILABLE_COLOR);
            _mainWindow.SetGridButtonHandlers(
                _activeTrial.TargetSide, _trialRecords[_activeTrial.Id].TargetId, 
                OnTargetMouseDown, OnTargetMouseUp, OnNonTargetMouseDown);

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

        public override void OnMainWindowMouseDown(Object sender, MouseButtonEventArgs e)
        {
            this.TrialInfo($"Timestamps: {_trialRecords[_activeTrial.Id].Timestamps.Stringify()}");

            if (_mainWindow.IsTechniqueToMo()) ///// ToMo
            {
                if (GetEventCount(Str.START_RELEASE) > 0)
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
        }

        public override void OnMainWindowMouseMove(Object sender, MouseEventArgs e)
        {
            if (_mainWindow.IsTechniqueToMo()) //// ToMo
            {
                // Nothing specifically
            }
            else //// Mouse
            {
                // Nothing specifically
            }
        }

        public override void OnMainWindowMouseUp(Object sender, MouseButtonEventArgs e)
        {
            this.TrialInfo($"Timestamps: {_trialRecords[_activeTrial.Id].Timestamps.Stringify()}");

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
            this.TrialInfo($"Timestamps: {_trialRecords[_activeTrial.Id].Timestamps.Stringify()}");

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
            this.TrialInfo($"Timestamps: {_trialRecords[_activeTrial.Id].Timestamps.Stringify()}");

            if (_mainWindow.IsTechniqueToMo()) //// ToMo
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
            else //// Mouse
            {
                StartRelease();
            }
            
            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnTargetMouseDown(Object sender, MouseButtonEventArgs e)
        {
            this.TrialInfo($"Timestamps: {_trialRecords[_activeTrial.Id].Timestamps.Stringify()}");

            if (_mainWindow.GetActiveTechnique() == Experiment.Technique.Mouse) //// Mouse
            {
                TargetPress();
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnTargetMouseUp(Object sender, MouseButtonEventArgs e)
        {
            this.TrialInfo($"Timestamps: {_trialRecords[_activeTrial.Id].Timestamps.Stringify()}");

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

            if (GetEventCount(Str.START_RELEASE) == Experiment.REP_TRIAL_NUM_PASS) // All passes done
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
            // Continue normally
            LogEvent(Str.TARGET_PRESS);

            // Pressed on an unavaailable target
            if (!_isTargetAvailable)
            {
                EndActiveTrial(Experiment.Result.MISS);
                return;
            }
        }

        private void TargetRelease()
        {
            LogEvent(Str.TARGET_RELEASE);

            // Show the next Start
            int startClicksCount = GetEventCount(Str.START_RELEASE);
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

        private List<CachedTrialPositions> LoadPositionsFromCache(Trial trial)
        {
            string fileName = trial.GetCacheFileName(CacheDirectory);
            if (File.Exists(fileName))
            {
                try
                {
                    string json = File.ReadAllText(fileName);
                    return JsonConvert.DeserializeObject<List<CachedTrialPositions>>(json) ?? new List<CachedTrialPositions>();
                }
                catch (Exception ex)
                {
                    this.TrialInfo($"Error loading cache from {fileName}: {ex.Message}");
                    return new List<CachedTrialPositions>();
                }
            }
            return new List<CachedTrialPositions>();
        }

        private bool TryFindNewPositions(Trial trial, int startW, int startHalfW)
        {
            // Reset trial record for current attempt
            _trialRecords[trial.Id].StartPositions.Clear();
            _trialRecords[trial.Id].TargetId = -1;

            (int targetId, Point targetCenterAbsolute) = _mainWindow.Dispatcher.Invoke(() =>
            {
                return _mainWindow.GetRadomTarget(trial.TargetSide, trial.TargetMultiple, trial.DistRangePX);
            });

            if (targetId != -1)
            {
                _trialRecords[trial.Id].TargetId = targetId;
            }
            else
            {
                this.TrialInfo($"Failed to find a random target id for Trial#{trial.Id}.");
                return false;
            }

            // Get Start constraints
            Rect startConstraintRect = _mainWindow.Dispatcher.Invoke(() =>
            {
                return _mainWindow.GetStartConstraintRect();
            });

            // Find random Start positions for the number of passes
            int maxRetries = 1000;
            double randDistMM = trial.DistRangeMM.GetRandomValue();
            Point startCenter = startConstraintRect.FindRandPointWithDist(targetCenterAbsolute, Utils.MM2PX(randDistMM), trial.TargetSide.GetOpposite());

            if (startCenter.X == -1 || startCenter.Y == -1) // Couldn't find the first position
            {
                this.TrialInfo($"Failed to find a valid first Start position for Trial#{trial.Id}!");
                return false;
            }
            else
            {
                // Save the first position
                Point startPositionAbsolute = startCenter.OffsetPosition(-startHalfW, -startHalfW);
                _trialRecords[trial.Id].StartPositions.Add(startPositionAbsolute);

                // Find the rest
                for (int p = 0; p < Experiment.REP_TRIAL_NUM_PASS - 1; p++)
                {
                    int nRetries = 0; // Reset retries for each pass
                    Point currentStartCenter;
                    do
                    {
                        randDistMM = trial.DistRangeMM.GetRandomValue();
                        currentStartCenter = startConstraintRect.FindRandPointWithDist(targetCenterAbsolute, Utils.MM2PX(randDistMM), trial.TargetSide.GetOpposite());

                        if (nRetries >= maxRetries)
                        {
                            this.TrialInfo($"Failed to find a valid Start position after {maxRetries} retries for pass {p + 1} of Trial#{trial.Id}!");
                            return false; // Failed to find a valid position for this pass
                        }

                        nRetries++;
                    } while (currentStartCenter.X == -1 || currentStartCenter.Y == -1 || Utils.Dist(currentStartCenter, _trialRecords[trial.Id].StartPositions[0]) > Utils.MM2PX(Experiment.REP_TRIAL_MAX_DIST_STARTS_MM));

                    // Valid position found
                    startPositionAbsolute = currentStartCenter.OffsetPosition(-startHalfW, -startHalfW);
                    _trialRecords[trial.Id].StartPositions.Add(startPositionAbsolute);
                }
            }

            // If we reached here, all positions for all passes were successfully found
            return _trialRecords[trial.Id].StartPositions.Count == Experiment.REP_TRIAL_NUM_PASS;
        }

        private void SavePositionsToCache(Trial trial, CachedTrialPositions positionsToCache)
        {
            string fileName = trial.GetCacheFileName(CacheDirectory);
            List<CachedTrialPositions> cachedPositions = LoadPositionsFromCache(trial);

            // Add new positions if not already present and manage size
            if (!cachedPositions.Any(c => c.TargetId == positionsToCache.TargetId && c.StartPositions.SequenceEqual(positionsToCache.StartPositions)))
            {
                cachedPositions.Add(positionsToCache);
                while (cachedPositions.Count > MaxCachedPositions)
                {
                    cachedPositions.RemoveAt(0); // Remove the oldest entry
                }
            }

            try
            {
                string json = JsonConvert.SerializeObject(cachedPositions, Formatting.Indented);
                File.WriteAllText(fileName, json);
                //this.TrialInfo($"Saved {cachedPositions.Count} positions to cache: {fileName}");
            }
            catch (Exception ex)
            {
                this.TrialInfo($"Error saving cache to {fileName}: {ex.Message}");
            }
        }
    }
}
