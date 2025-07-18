using HarfBuzzSharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using static Multi.Cursor.Experiment;

namespace Multi.Cursor
{
    public class RepeatingBlockHandler : BlockHandler
    {
        private bool _isTargetAvailable = false; // Whether the target is available for clicking
        private int _pressedObjectId = -1; // Id of the object that was pressed in the current trial
        private int _markedObjectId = -1; // Id of the object that was marked in the current trial
        private bool _isFunctionClicked = false; // Whether the function button was clicked (for mouse)

        private const string CacheDirectory = "TrialPositionCache";
        private const int MaxCachedPositions = 100;

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
            //while (IsTargetRepeated())
            //{
            //    _activeBlock.Trials.Shuffle();
            //}

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

        public override bool FindPositionsForTrial(Trial trial)
        {
            int startW = Utils.MM2PX(Experiment.OBJ_WIDTH_MM);
            int startHalfW = startW / 2;
            //this.TrialInfo($"Trial#{trial.Id} [Target = {trial.TargetSide.ToString()}, " +
            //    $"TargetMult = {trial.TargetMultiple}, Dist range (mm) = {trial.DistRange.ToString()}]");

            // Ensure TrialRecord exists for this trial
            if (!_trialRecords.ContainsKey(trial.Id))
            {
                _trialRecords[trial.Id] = new TrialRecord();
            }

            // --- Attempt to find new positions ---
            //bool success = TryFindNewPositions(trial, startW, startHalfW);
            bool success = TryFindNewObjPositions(trial);

            if (success)
            {
                // If new positions were successfully found, save them to cache
                //SavePositionsToCache(trial, new CachedTrialPositions
                //{
                //    TargetId = _trialRecords[trial.Id].TargetId,
                //    StartPositions = _trialRecords[trial.Id].StartPositions
                //});
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
                        //_trialRecords[trial.Id].StartPositions = new List<Point>(selectedEntry.StartPositio   ns);
                        //this.TrialInfo($"Using random cached positions for Trial#{trial.Id}");
                        return true;
                    }
                }
                //this.TrialInfo($"No valid cached positions found for Trial#{trial.Id} either.");
                return false; // No cached or new positions found
            }
        }

        public override void ShowActiveTrial()
        {
            this.TrialInfo(Str.MINOR_LINE);
            this.TrialInfo($"Showing rep Trial#{_activeTrial.Id} | Target side: {_activeTrial.TargetSide} " +
                $"| Dist (mm): ({_activeTrial.DistRangeMM.Label}) {_activeTrial.DistRangeMM.ToString()}");

            // Log the trial show timestamp
            _activeTrialRecord.AddTimestamp(Str.TRIAL_SHOW);

            // Set the target window based on the trial's target side
            _mainWindow.SetTargetWindow(_activeTrial.TargetSide, OnAuxWindowMouseDown, OnAuxWindowMouseUp);

            // Color the function button and set the handlers
            Brush funcDefaultColor = _mainWindow.IsTechniqueToMo() ? Config.FUNCTION_MARKED_COLOR : Config.FUNCTION_DEFAULT_COLOR;
            _mainWindow.FillButtonInTargetWindow(
                _activeTrial.TargetSide, _activeTrialRecord.TargetId, 
                funcDefaultColor);
            _mainWindow.SetGridButtonHandlers(
                _activeTrial.TargetSide, _activeTrialRecord.TargetId,
                OnFunctionMouseDown, OnFunctionMouseUp, OnNonTargetMouseDown);

            // Activate the auxiliary window marker on all sides
            _mainWindow.ShowAllAuxMarkers();

            // Clear the main window canvas (to add shapes)
            _mainWindow.ClearCanvas();

            // Show the area
            _mainWindow.ShowObjectsArea(
                _activeTrialRecord.ObjectAreaRect, Config.OBJ_AREA_BG_COLOR,
                OnObjectAreaMouseDown);

            // Show the objects
            _mainWindow.ShowObjects(
                _activeTrialRecord.Objects, Config.OBJ_DEFAULT_COLOR,
                OnObjectMouseEnter, OnObjectMouseLeave,
                OnObjectMouseDown, OnObjectMouseUp);

            // Show Start Trial button
            _mainWindow.ShowStartTrialButton(_activeTrialRecord.ObjectAreaRect, OnStartButtonMouseUp);

            // Show the first Start
            //Point firstStartPos = _trialRecords[_activeTrial.Id].StartPositions.First();
            //_mainWindow.ShowStart(
            //    firstStartPos, Config.START_AVAILABLE_COLOR,
            //    OnObjectMouseEnter, OnObjectMouseLeave, OnObjectMouseDown, OnObjectMouseUp);
        }

        public override void EndActiveTrial(Experiment.Result result)
        {
            this.TrialInfo($"Trial#{_activeTrial.Id} completed: {result}");
            this.TrialInfo(Str.MAJOR_LINE);
            LogEvent(Str.TRIAL_END); // Log the trial end timestamp

            switch (result)
            {
                case Experiment.Result.HIT:
                    Sounder.PlayHit();
                    this.TrialInfo($"Trial time = {GetDuration(Str.OBJ_RELEASE + "_1", Str.TRIAL_END)}s");
                    GoToNextTrial();
                    break;
                case Experiment.Result.MISS:
                    Sounder.PlayTargetMiss();
                    _activeBlock.ShuffleBackTrial(_activeTrialNum);
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
            _mainWindow.ResetTargetWindow(_activeTrial.TargetSide); // Reset the target window
            //_trialEventCounts.Clear(); // Reset the event counts for the trial
            //_trialTimestamps.Clear(); // Reset the timestamps for the trial
            _isTargetAvailable = false; // Reset the target availability
            _nSelectedObjects = 0; // Reset the number of applied objects
            _pressedObjectId = -1; // Reset the pressed object id
            _isFunctionClicked = false; // Reset the function clicked state

            if (_activeTrialNum < _activeBlock.Trials.Count)
            {
                //_mainWindow.ShowStartTrialButton(OnStartButtonMouseUp);
                _mainWindow.ResetTargetWindow(_activeTrial.TargetSide);
                _activeTrialRecord.ClearTimestamps();

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

            //if (_activeTrialNum < _activeBlock.Trials.Count)
            //{
            //    _activeTrialNum++;
            //    _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
            //    ShowActiveTrial();
            //}
            //else
            //{
            //    this.TrialInfo("All trials in the block completed.");
            //}
        }

        public override void OnMainWindowMouseDown(Object sender, MouseButtonEventArgs e)
        {
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

            //if (_mainWindow.IsTechniqueToMo()) ///// ToMo
            //{
            //    if (GetEventCount(Str.START_RELEASE) > 0)
            //    {
            //        if (_mainWindow.IsMarkerOnButton(_activeTrial.TargetSide, _trialRecords[_activeTrial.Id].TargetId))
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
            //    if (GetEventCount(Str.START_RELEASE) > 0) EndActiveTrial(Experiment.Result.MISS);
            //    else EndActiveTrial(Experiment.Result.OBJ_NOT_CLICKED);
            //}
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
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

            //if (_mainWindow.IsTechniqueToMo()) //-- ToMo
            //{
            //    string key = Str.TARGET_RELEASE;

            //    if (_mainWindow.IsMarkerOnButton(_activeTrial.TargetSide, _trialRecords[_activeTrial.Id].TargetId))
            //    {
            //        OnFunctionRelease();
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
        }

        //public override void OnObjectMouseDown(Object sender, MouseButtonEventArgs e)
        //{
        //    // Show the current timestamps
        //    this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

        //    if (_mainWindow.IsTechniqueToMo()) //-- ToMo
        //    {
        //        if (_activeTrialRecord.GetLastTimestamp() == Str.OBJ_RELEASE) // Target press?
        //        {
        //            // If navigator is on the button, count the event
        //            if (_mainWindow.IsMarkerOnButton(_activeTrial.TargetSide, _activeTrialRecord.TargetId))
        //            {
        //                TargetPress();
        //            }
        //            else // Navator is not on the target button
        //            {
        //                EndActiveTrial(Experiment.Result.MISS);
        //                return;
        //            }
        //        }
        //        else
        //        {
        //            ObjectPress();
        //        }
        //    }
        //    else //-- Mouse
        //    {
        //        ObjectPress();
        //    }

        //    e.Handled = true; // Mark the event as handled to prevent further processing
        //}

        //public override void OnObjectMouseUp(Object sender, MouseButtonEventArgs e)
        //{
        //    // Show the current timestamps
        //    this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

        //    if (_mainWindow.IsTechniqueToMo()) //// ToMo
        //    {
        //        if (_activeTrialRecord.GetLastTimestamp() ==  Str.OBJ_RELEASE) // Start release?
        //        {
        //            ObjectRelease();
        //        }
        //        else // Target release?
        //        {
        //            // If navigator is on the button, count the event
        //            if (_mainWindow.IsMarkerOnButton(_activeTrial.TargetSide, _activeTrialRecord.TargetId))
        //            {
        //                OnFunctionRelease();
        //            }
        //            else // Navator is not on the target button
        //            {
        //                EndActiveTrial(Experiment.Result.MISS);
        //                return;
        //            }
        //        }
        //    }
        //    else //// Mouse
        //    {
        //        ObjectRelease();
        //    }
            
        //    e.Handled = true; // Mark the event as handled to prevent further processing
        //}

        public override void OnFunctionMouseDown(Object sender, MouseButtonEventArgs e)
        {
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

            if (_mainWindow.GetActiveTechnique() == Experiment.Technique.Mouse) //// Mouse
            {
                TargetPress();
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnFunctionMouseUp(Object sender, MouseButtonEventArgs e)
        {
            var technique = _mainWindow.GetActiveTechnique();
            
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

            switch (technique)
            {

                case Technique.Mouse:
                    SetObjectAsSelected(_pressedObjectId);
                    _nSelectedObjects++;
                    SetFunctionAsSelected();
                    break;
            }


            LogEvent(Str.FUNCTION_RELEASE);
            e.Handled = true; // Mark the event as handled to prevent further processing
        }


        private void ObjectPress()
        {
            LogEvent(Str.OBJ_PRESS);
        }

        private void ObjectRelease()
        {
            LogEvent(Str.OBJ_RELEASE);

            if (GetEventCount(Str.OBJ_RELEASE) == Experiment.REP_TRIAL_NUM_PASS) // All passes done
            {
                EndActiveTrial(Experiment.Result.HIT);
            }
            else // Still passes left
            {
                // Change available/unavailable
                _mainWindow.FillStart(Config.START_UNAVAILABLE_COLOR);
                _mainWindow.FillButtonInTargetWindow(
                    _activeTrial.TargetSide, _activeTrialRecord.TargetId, Config.FUNCTION_MARKED_COLOR);
                _isTargetAvailable = true;
            }
        }

        private void TargetPress()
        {
            // Continue normally
            //LogEvent(Str.TARGET_PRESS);

            //// Pressed on an unavaailable target
            //if (!_isTargetAvailable)
            //{
            //    EndActiveTrial(Experiment.Result.MISS);
            //    return;
            //}
        }

        private void OnFunctionRelease()
        {
            //LogEvent(string.Join("_", Str.FUNCTION, Str.RELEASE));
            _isFunctionClicked = true;

            

            // Show the next Start
            //int startClicksCount = GetEventCount(Str.START_RELEASE);
            //Point startAbsolutePosition = _trialRecords[_activeTrial.Id].StartPositions[startClicksCount];
            //_mainWindow.ShowStart(
            //    startAbsolutePosition, Config.START_AVAILABLE_COLOR,
            //    OnObjectMouseEnter, OnObjectMouseLeave, OnObjectMouseDown, OnObjectMouseUp);
            //_mainWindow.FillButtonInTargetWindow(
            //    _activeTrial.TargetSide, _trialRecords[_activeTrial.Id].TargetId, Config.TARGET_UNAVAILABLE_COLOR);
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

        //private bool TryFindNewPositions(Trial trial, int startW, int startHalfW)
        //{
        //    // Reset trial record for current attempt
        //    _trialRecords[trial.Id].StartPositions.Clear();
        //    _trialRecords[trial.Id].TargetId = -1;

        //    (int targetId, Point targetCenterAbsolute) = _mainWindow.Dispatcher.Invoke(() =>
        //    {
        //        return _mainWindow.GetRadomTarget(trial.TargetSide, trial.TargetMultiple, trial.DistRangePX);
        //    });

        //    if (targetId != -1)
        //    {
        //        _trialRecords[trial.Id].TargetId = targetId;
        //    }
        //    else
        //    {
        //        this.TrialInfo($"Failed to find a random target id for Trial#{trial.Id}.");
        //        return false;
        //    }

        //    // Get Start constraints
        //    Rect startConstraintRect = _mainWindow.Dispatcher.Invoke(() =>
        //    {
        //        return _mainWindow.GetStartConstraintRect();
        //    });

        //    // Find random Start positions for the number of passes
        //    int maxRetries = 1000;
        //    double randDistMM = trial.DistRangeMM.GetRandomValue();
        //    Point startCenter = startConstraintRect.FindRandPointWithDist(targetCenterAbsolute, Utils.MM2PX(randDistMM), trial.TargetSide.GetOpposite());

        //    if (startCenter.X == -1 || startCenter.Y == -1) // Couldn't find the first position
        //    {
        //        this.TrialInfo($"Failed to find a valid first Start position for Trial#{trial.Id}!");
        //        return false;
        //    }
        //    else
        //    {
        //        // Save the first position
        //        Point startPositionAbsolute = startCenter.OffsetPosition(-startHalfW, -startHalfW);
        //        _trialRecords[trial.Id].StartPositions.Add(startPositionAbsolute);

        //        // Find the rest
        //        for (int p = 0; p < Experiment.REP_TRIAL_NUM_PASS - 1; p++)
        //        {
        //            int nRetries = 0; // Reset retries for each pass
        //            Point currentStartCenter;
        //            do
        //            {
        //                randDistMM = trial.DistRangeMM.GetRandomValue();
        //                currentStartCenter = startConstraintRect.FindRandPointWithDist(targetCenterAbsolute, Utils.MM2PX(randDistMM), trial.TargetSide.GetOpposite());

        //                if (nRetries >= maxRetries)
        //                {
        //                    this.TrialInfo($"Failed to find a valid Start position after {maxRetries} retries for pass {p + 1} of Trial#{trial.Id}!");
        //                    return false; // Failed to find a valid position for this pass
        //                }

        //                nRetries++;
        //            } while (currentStartCenter.X == -1 || currentStartCenter.Y == -1 || Utils.Dist(currentStartCenter, _trialRecords[trial.Id].StartPositions[0]) > Utils.MM2PX(Experiment.REP_TRIAL_MAX_DIST_STARTS_MM));

        //            // Valid position found
        //            startPositionAbsolute = currentStartCenter.OffsetPosition(-startHalfW, -startHalfW);
        //            _trialRecords[trial.Id].StartPositions.Add(startPositionAbsolute);
        //        }
        //    }

        //    // If we reached here, all positions for all passes were successfully found
        //    return _trialRecords[trial.Id].StartPositions.Count == Experiment.REP_TRIAL_NUM_PASS;
        //}

        private bool TryFindNewObjPositions(Trial trial)
        {
            // Reset trial record for current attempt
            //_trialRecords[trial.Id].ObjectPositions.Clear();
            _trialRecords[trial.Id].TargetId = -1;

            // Get a random Target from the main window (which calls the side window)
            (int targetId, Point targetCenterAbsolute) = _mainWindow.Dispatcher.Invoke(() =>
            {
                return _mainWindow.GetRadomTarget(trial.TargetSide, trial.TargetMultiple, trial.DistRangePX);
            });

            // If a target was found, set the target id
            if (targetId != -1)
            {
                _trialRecords[trial.Id].TargetId = targetId;
            }
            else
            {
                this.TrialInfo($"Failed to find a random target id for Trial#{trial.Id}.");
                return false;
            }

            // Get object constraint Rect
            Rect objConstraintRect = _mainWindow.Dispatcher.Invoke(() =>
            {
                return _mainWindow.GetObjAreaCenterConstraintRect();
            });

            // Find a valid object area position
            int maxRetries = 1000;
            double randDistMM = trial.DistRangeMM.GetRandomValue();

            Point objAreaCenterPosition = new Point(0, 0);
            for (int t = 0; t < maxRetries; t++) {
                objAreaCenterPosition = objConstraintRect.FindRandPointWithDist(
                    targetCenterAbsolute, 
                    Utils.MM2PX(randDistMM), 
                    trial.TargetSide.GetOpposite());

                if (objAreaCenterPosition.X != 0 && objAreaCenterPosition.Y != 0)
                {
                    this.TrialInfo($"Found a valid object area center position for Trial#{trial.Id} at {objAreaCenterPosition}.");
                    // Set the Rect
                    _trialRecords[trial.Id].ObjectAreaRect = new Rect(
                        objAreaCenterPosition.X - Utils.MM2PX(OBJ_AREA_WIDTH_MM / 2),
                        objAreaCenterPosition.Y - Utils.MM2PX(OBJ_AREA_WIDTH_MM / 2),
                        Utils.MM2PX(OBJ_AREA_WIDTH_MM),
                        Utils.MM2PX(OBJ_AREA_WIDTH_MM));
                    _trialRecords[trial.Id].Objects = PlaceObjectsInArea(objAreaCenterPosition, Experiment.REP_TRIAL_NUM_PASS);
                    return true; // Successfully found positions
                }
            }

            return false;
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

        private List<TObject> PlaceObjectsInArea(Point objAreaCenterPosition, int nObjects)
        {
            List<TObject> placedObjects = new List<TObject>();
            double objW = Utils.MM2PX(Experiment.OBJ_WIDTH_MM);
            double areaRadius = Utils.MM2PX(Experiment.REP_TRIAL_OBJ_AREA_RADIUS_MM);

            int maxAttemptsPerObject = 1000; // Limit attempts to prevent infinite loops

            for (int i = 0; i < nObjects; i++)
            {
                bool placed = false;
                for (int attempt = 0; attempt < maxAttemptsPerObject; attempt++)
                {
                    // 1. Generate a random potential center for the new square
                    Point potentialCenter = GenerateRandomPointInCircle(objAreaCenterPosition, areaRadius - objW / 2);

                    // Calculate the top-left corner from the potential center
                    Point topLeft = new Point(potentialCenter.X - objW / 2, potentialCenter.Y - objW / 2);

                    // 2. Check for overlaps with already placed objects
                    if (!HasOverlap(topLeft, objW, placedObjects))
                    {
                        TObject trialObject = new TObject(i + 1, topLeft);

                        placedObjects.Add(trialObject);
                        placed = true;
                        break; // Move to the next object
                    }
                }

                if (!placed)
                {
                    // Handle cases where an object couldn't be placed after maxAttempts
                    // You might log a warning, throw an exception, or return partial results.
                    Console.WriteLine($"Warning: Could not place object {i + 1} after {maxAttemptsPerObject} attempts.");
                }
            }

            return placedObjects;


        }

        private Point GenerateRandomPointInCircle(Point center, double effectiveRadius)
        {
            // Generate a random angle between 0 and 2*PI
            double angle = _random.NextDouble() * 2 * Math.PI;

            // Generate a random distance from the center (0 to effectiveRadius)
            // To ensure a uniform distribution, we take the square root of a uniform random number
            // This biases points towards the center if not done, making the outer regions less dense.
            double distance = effectiveRadius * Math.Sqrt(_random.NextDouble());

            // Calculate the coordinates
            double x = center.X + distance * Math.Cos(angle);
            double y = center.Y + distance * Math.Sin(angle);

            return new Point(x, y);
        }

        private bool HasOverlap(Point newObjTopLeft, double newObjW, List<TObject> existingObjs)
        {
            double newObjRight = newObjTopLeft.X + newObjW;
            double newObjBottom = newObjTopLeft.Y + newObjW;

            foreach (TObject existingObject in existingObjs)
            {
                double existingObjRight = existingObject.Position.X + newObjW; // Assuming all objects have the same width
                double existingObjBottom = existingObject.Position.Y + newObjW;

                // Check for overlap using the bounding box method
                if (newObjTopLeft.X < existingObjRight &&
                    newObjRight > existingObject.Position.X &&
                    newObjTopLeft.Y < existingObjBottom &&
                    newObjBottom > existingObject.Position.Y)
                {
                    return true; // Overlap detected
                }
            }

            return false; // No overlap with any existing object
        }

        public override void OnObjectMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Treat sender as a Rectangle and get its Tag
            FrameworkElement element = (FrameworkElement)sender;
            //if (element.Tag is int)
            //{
            //    int tag = (int)element.Tag;
            //    _pressedObjectId = tag; // Store the pressed object id

            //    string timeKey = string.Join("_", Str.OBJ, ((int)element.Tag).ToString(), Str.PRESS);

            //    // Was this object already pressed?
            //    if (_activeTrialRecord.HasTimestamp(timeKey))
            //    {
            //        EndActiveTrial(Experiment.Result.MISS);
            //        return;
            //    }


            //    _activeTrialRecord.AddTimestamp(timeKey); // Log the object press timestamp
            //}
            //else
            //{
            //    this.TrialInfo("Pressed on an object without a valid Tag.");
            //}

            LogEvent(string.Join("_", Str.OBJ, ((int)element.Tag).ToString(), Str.PRESS));
            e.Handled = true;

        }

        public override void OnObjectMouseUp(object sender, MouseButtonEventArgs e)
        {
            var elementTag = (int)((FrameworkElement)sender).Tag;

            var isToMo = _mainWindow.IsTechniqueToMo();
            var markerOnFunction = _mainWindow.IsMarkerOnButton(_activeTrial.TargetSide, _activeTrialRecord.TargetId);
            var allObjSelected = _nSelectedObjects == _activeTrialRecord.Objects.Count;

            // Show all the flags
            this.TrialInfo($"isToMo: {isToMo}, markerOnFunction: {markerOnFunction}, allObjSelected: {allObjSelected}");

            switch (isToMo, markerOnFunction, allObjSelected)
            {
                case (true, true, false): // ToMo, marker on function, not all objects selected
                    SetObjectAsSelected(elementTag);
                    _nSelectedObjects++;
                    break;
                case (true, true, true): // ToMo, marker on function, all objects selected
                    EndActiveTrial(Experiment.Result.MISS);
                    break;

                case (false, _, false): // Mouse, any marker state, not all objects selected
                    SetObjectAsMarked(elementTag);
                    _pressedObjectId = elementTag;
                    SetFunctionAsEnabled();
                    break;
                case (false, _, true): // Mouse, any marker state, all objects selected
                    EndActiveTrial(Experiment.Result.HIT);
                    break;
            }

            LogEvent(string.Join("_", Str.OBJ, elementTag.ToString(), Str.RELEASE));
            e.Handled = true;


            // Treat sender as a Rectangle and get its Tag
            //FrameworkElement element = (FrameworkElement)sender;
            //if (element.Tag is int)
            //{
            //    int tag = (int)element.Tag;

            //    // If not the same object as pressed => trial is missed
            //    if (_pressedObjectId != tag)
            //    {
            //        this.TrialInfo($"Pressed on a different object than was pressed: {_pressedObjectId} != {tag}");
            //        EndActiveTrial(Experiment.Result.MISS);
            //        return;
            //    }

            //    // Object clicked
            //    OnObjectClick(tag);

            //    _pressedObjectId = -1; // Reset the pressed object id

            //}
            //else
            //{
            //    this.TrialInfo("Pressed on an object without a valid Tag.");
            //}
        }

        private void OnObjectClick(int objId)
        {
            // Set the timestamp for the object release
            string timeKey = string.Join("_", Str.OBJ, objId.ToString(), Str.RELEASE);
            _trialRecords[_activeTrial.Id].AddTimestamp(timeKey); // Log the object release timestamp

            // Handle based on the technique
            if (_mainWindow.GetActiveTechnique() == Experiment.Technique.Mouse) /// Mouse
            {
                _nSelectedObjects++;
                this.TrialInfo($"Object#{objId} clicked. Total applied objects: {_nSelectedObjects}");

                _markedObjectId = objId;

                // Color the object as marked and target as available
                _mainWindow.FillObject(objId, Config.OBJ_MARKED_COLOR);
                _mainWindow.FillButtonInTargetWindow(
                    _activeTrial.TargetSide, _trialRecords[_activeTrial.Id].TargetId, Config.FUNCTION_MARKED_COLOR);

                // Apply the function if it was clicked
                //if (_isFunctionClicked)
                //{

                //}
                //else
                //{
                //    this.TrialInfo($"Object#{objId} clicked, but ignored.");
                //}
            }
            else if (_mainWindow.GetActiveTechnique() == Experiment.Technique.Auxursor_Tap)
            {
                // For Auxursor_Tap technique, we might need to do something specific
                // For now, just log the event
                LogEvent(string.Join("_", Str.OBJ, objId.ToString(), Str.RELEASE));
            }
        }

        public override void IndexTap()
        {
            var technique = _mainWindow.GetActiveTechnique();
            var allObjSelected = _nSelectedObjects == _activeTrialRecord.Objects.Count;
            Side correspondingSide = Side.Top;
            var funcOnRightWindow = _activeTrial.TargetSide == correspondingSide;

            this.TrialInfo($"Technique: {_mainWindow.GetActiveTechnique()}");

            switch (technique, allObjSelected, funcOnRightWindow)
            {
                case (Technique.Auxursor_Tap, false, true): // Correct side activated
                    _mainWindow.ActivateAuxWindowMarker(correspondingSide);
                    break;
                case (Technique.Auxursor_Tap, false, false): // Incorrect side activated
                    EndActiveTrial(Result.MISS);
                    break;
                case (Technique.Auxursor_Tap, true, true): // Correct deactivation
                    EndActiveTrial(Result.HIT);
                    break;
                case (Technique.Auxursor_Tap, true, false): // Incorrect deactivation
                    EndActiveTrial(Result.MISS);
                    break;

                case (Technique.Auxursor_Swipe, _, _): // Wrong technique for index tap
                    EndActiveTrial(Result.MISS);
                    break;
            }
        }

        public override void ThumbTap()
        {
            var technique = _mainWindow.GetActiveTechnique();
            var allObjSelected = _nSelectedObjects == _activeTrialRecord.Objects.Count;
            Side correspondingSide = Side.Top;
            var funcOnRightWindow = _activeTrial.TargetSide == correspondingSide;

            this.TrialInfo($"Technique: {_mainWindow.GetActiveTechnique()}");

            switch (technique, allObjSelected, funcOnRightWindow)
            {
                case (Technique.Auxursor_Tap, false, true): // Correct side activated
                    _mainWindow.ActivateAuxWindowMarker(correspondingSide);
                    break;
                case (Technique.Auxursor_Tap, false, false): // Incorrect side activated
                    EndActiveTrial(Result.MISS);
                    break;
                case (Technique.Auxursor_Tap, true, true): // Correct deactivation
                    EndActiveTrial(Result.HIT);
                    break;
                case (Technique.Auxursor_Tap, true, false): // Incorrect deactivation
                    EndActiveTrial(Result.MISS);
                    break;

                case (Technique.Auxursor_Swipe, _, _): // Wrong technique for index tap
                    EndActiveTrial(Result.MISS);
                    break;
            }
        }

        public override void MiddleTap()
        {
            var technique = _mainWindow.GetActiveTechnique();
            var allObjSelected = _nSelectedObjects == _activeTrialRecord.Objects.Count;
            Side correspondingSide = Side.Top;
            var funcOnRightWindow = _activeTrial.TargetSide == correspondingSide;

            this.TrialInfo($"Technique: {_mainWindow.GetActiveTechnique()}");

            switch (technique, allObjSelected, funcOnRightWindow)
            {
                case (Technique.Auxursor_Tap, false, true): // Correct side activated
                    _mainWindow.ActivateAuxWindowMarker(correspondingSide);
                    break;
                case (Technique.Auxursor_Tap, false, false): // Incorrect side activated
                    EndActiveTrial(Result.MISS);
                    break;
                case (Technique.Auxursor_Tap, true, true): // Correct deactivation
                    EndActiveTrial(Result.HIT);
                    break;
                case (Technique.Auxursor_Tap, true, false): // Incorrect deactivation
                    EndActiveTrial(Result.MISS);
                    break;

                case (Technique.Auxursor_Swipe, _, _): // Wrong technique for index tap
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
            var allObjSelected = _nSelectedObjects == _activeTrialRecord.Objects.Count;
            var dirMatchesSide = dir switch
            {
                Direction.Left => _activeTrial.TargetSide == Side.Left,
                Direction.Right => _activeTrial.TargetSide == Side.Right,
                Direction.Up => _activeTrial.TargetSide == Side.Top,
                _ => false
            };

            var dirOppositeSide = dir switch
            {
                Direction.Left => _activeTrial.TargetSide == Side.Right,
                Direction.Right => _activeTrial.TargetSide == Side.Left,
                Direction.Down => _activeTrial.TargetSide == Side.Top,
                _ => false
            };

            this.TrialInfo($"Technique: {_mainWindow.GetActiveTechnique()}");

            switch (technique, allObjSelected, dirMatchesSide, dirOppositeSide)
            {
                case (Technique.Auxursor_Swipe, true, _, true): // Deactivation success
                    EndActiveTrial(Result.HIT);
                    break;
                case (Technique.Auxursor_Swipe, true, _, false): // Deactivation failure
                    EndActiveTrial(Result.MISS);
                    break;
                case (Technique.Auxursor_Swipe, false, true, _): // Correct activation swipe
                    _mainWindow.ActivateAuxWindowMarker(_activeTrial.TargetSide);
                    break;
                case (Technique.Auxursor_Swipe, false, false, _): // Incorrect activation swipe
                    EndActiveTrial(Result.MISS);
                    break;

                case (Technique.Auxursor_Tap, _, _, _): // Wrong technique for swipe
                    EndActiveTrial(Result.MISS);
                    break;

            }
        }
    }
}
