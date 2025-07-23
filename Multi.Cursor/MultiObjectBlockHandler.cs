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
using static Multi.Cursor.Utils;

namespace Multi.Cursor
{
    public class MultiObjectBlockHandler : BlockHandler
    {
        private bool _isTargetAvailable = false; // Whether the target is available for clicking
        private int _pressedObjectId = -1; // Id of the object that was pressed in the current trial
        private int _markedObjectId = -1; // Id of the object that was marked in the current trial
        private bool _isFunctionClicked = false; // Whether the function button was clicked (for mouse)

        private const string CacheDirectory = "TrialPositionCache";
        private const int MaxCachedPositions = 100;

        public MultiObjectBlockHandler(MainWindow mainWindow, Block activeBlock)
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

            // If consecutive trials have the same FunctionId, re-order them
            //while (IsTargetRepeated())
            //{
            //    _activeBlock.Trials.Shuffle();
            //}

            // Show all the target ids for the trials
            //this.TrialInfo($"Target IDs: {string.Join(", ", _activeBlock.Trials.Select(t => $"{_trialRecords[t.Id].FunctionId}"))}");
            return true;
        }

        public override bool FindPositionsForTrial(Trial trial)
        {
            int objW = Utils.MM2PX(Experiment.OBJ_WIDTH_MM);
            int objHalfW = objW / 2;
            int objAreaW = Utils.MM2PX(OBJ_AREA_WIDTH_MM);
            int objAreaHalfW = objAreaW / 2;
            //this.TrialInfo($"Trial#{trial.Id} [Target = {trial.FuncSide.ToString()}, " +
            //    $"TargetMult = {trial.TargetMultiple}, Dist range (mm) = {trial.DistRange.ToString()}]");

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

            Point objAreaCenter = objectAreaConstraintRect.FindPointWithinDistRangeFromMultipleSources(
                _trialRecords[trial.Id].GetFunctionCenters(), trial.DistRangePX);


            if (objAreaCenter.X == -1 && objAreaCenter.Y == -1) // Failed to find a valid position 
            {
                this.TrialInfo($"No valid position found for object in Trial#{trial.Id}!");
                return false; // Return false to indicate failure
            }
            else
            {
                this.TrialInfo($"Found object position: {objAreaCenter.ToStr()}");

                // Get the top-left corner of the object area rectangle
                Point objAreaPosition = objAreaCenter.OffsetPosition(-objAreaHalfW);

                this.TrialInfo($"Found object position: {objAreaPosition.ToStr()}");

                _trialRecords[trial.Id].ObjectAreaRect = new Rect(
                        objAreaPosition.X,
                        objAreaPosition.Y,
                        objAreaW,
                        objAreaW);

                // Place objects in the area
                _trialRecords[trial.Id].Objects = PlaceObjectsInArea(objAreaCenter, trial.NObjects);

                return true;
            }

            // --- Attempt to find new positions ---
            //bool success = TryFindNewPositions(trial, startW, startHalfW);
            //bool success = TryFindNewPositions(trial);

            //if (success)
            //{
            //    // If new positions were successfully found, save them to cache
            //    //SavePositionsToCache(trial, new CachedTrialPositions
            //    //{
            //    //    FunctionId = _trialRecords[trial.Id].FunctionId,
            //    //    StartPositions = _trialRecords[trial.Id].StartPositions
            //    //});
            //    return true;
            //}
            //else
            //{
            //    // If finding new positions failed, attempt to load from cache
            //    //this.TrialInfo($"Failed to find new positions for Trial#{trial.Id}. Attempting to use cached positions.");
            //    //List<CachedTrialPositions> cachedData = LoadPositionsFromCache(trial);
            //    //if (cachedData.Any())
            //    //{
            //    //    // Pick a random valid cached entry
            //    //    var randomCachedEntry = cachedData.Where(c => c.StartPositions.Count == Experiment.REP_TRIAL_NUM_PASS).ToList();
            //    //    if (randomCachedEntry.Any())
            //    //    {
            //    //        var selectedEntry = randomCachedEntry[_random.Next(randomCachedEntry.Count)];
            //    //        _trialRecords[trial.Id].FunctionId = selectedEntry.TargetId;
            //    //        //_trialRecords[trial.Id].StartPositions = new List<Point>(selectedEntry.StartPositio   ns);
            //    //        //this.TrialInfo($"Using random cached positions for Trial#{trial.Id}");
            //    //        return true;
            //    //    }
            //    //}
            //    //this.TrialInfo($"No valid cached positions found for Trial#{trial.Id} either.");
            //    return false; // No cached or new positions found
            //}
        }

        public override void ShowActiveTrial()
        {
            this.TrialInfo(Str.MINOR_LINE);
            this.TrialInfo($"Showing rep Trial#{_activeTrial.Id} | Function side: {_activeTrial.FuncSide} " +
                $"| Dist (mm): ({_activeTrial.DistRangeMM.Label}) {_activeTrial.DistRangeMM.ToString()}");
            
            // Log the trial show timestamp
            _activeTrialRecord.AddTimestamp(Str.TRIAL_SHOW);

            // Set the target window based on the trial's target side
            _mainWindow.SetTargetWindow(_activeTrial.FuncSide, OnAuxWindowMouseDown, OnAuxWindowMouseUp);

            // Color the function button and set the handlers
            
            Brush funcDefaultColor = Config.FUNCTION_DEFAULT_COLOR;
            //_mainWindow.FillButtonInTargetWindow(
            //    _activeTrial.FuncSide, _activeTrialRecord.FunctionId, 
            //    funcDefaultColor);
            _mainWindow.FillButtonsInAuxWindow(_activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds(), funcDefaultColor);
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
                _activeTrialRecord.ObjectAreaRect, Config.OBJ_AREA_BG_COLOR,
                objAreaEvents);

            // Show the objects
            MouseEvents objectEvents = new MouseEvents(
                OnObjectMouseEnter, OnObjectMouseDown, OnObjectMouseUp, OnObjectMouseLeave);
            _mainWindow.ShowObjects(_activeTrialRecord.Objects, Config.OBJ_DEFAULT_COLOR, objectEvents);

            // Show Start Trial button
            MouseEvents startButtonEvents = new MouseEvents(OnStartButtonMouseDown, OnStartButtonMouseUp);
            _mainWindow.ShowStartTrialButton(_activeTrialRecord.ObjectAreaRect, startButtonEvents);
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
                case Experiment.Result.ERROR:
                    Sounder.PlayStartMiss();
                    // Do nothing, just reset everything

                    break;
            }

        }

        public override void GoToNextTrial()
        {
            _mainWindow.ResetTargetWindow(_activeTrial.FuncSide); // Reset the target window
            //_trialEventCounts.Clear(); // Reset the event counts for the trial
            //_trialTimestamps.Clear(); // Reset the timestamps for the trial
            _isTargetAvailable = false; // Reset the target availability
            _nSelectedObjects = 0; // Reset the number of applied objects
            _pressedObjectId = -1; // Reset the pressed object id
            _isFunctionClicked = false; // Reset the function clicked state

            if (_activeTrialNum < _activeBlock.Trials.Count)
            {
                //_mainWindow.ShowStartTrialButton(OnStartButtonMouseUp);
                _mainWindow.ResetTargetWindow(_activeTrial.FuncSide);
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

        }

        public override void OnMainWindowMouseDown(Object sender, MouseButtonEventArgs e)
        {
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");
        }

        public override void OnMainWindowMouseMove(Object sender, MouseEventArgs e)
        {
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");
        }

        public override void OnMainWindowMouseUp(Object sender, MouseButtonEventArgs e)
        {
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");
        }

        public override void OnFunctionMouseDown(Object sender, MouseButtonEventArgs e)
        {
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");


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
                    //SetFunctionAsSelected();
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
        }

        private void OnFunctionRelease()
        {
            //LogEvent(string.Join("_", Str.FUNCTION, Str.RELEASE));
            _isFunctionClicked = true;

        }

        //private List<CachedTrialPositions> LoadPositionsFromCache(Trial trial)
        //{
        //    string fileName = trial.GetCacheFileName(CacheDirectory);
        //    if (File.Exists(fileName))
        //    {
        //        try
        //        {
        //            string json = File.ReadAllText(fileName);
        //            return JsonConvert.DeserializeObject<List<CachedTrialPositions>>(json) ?? new List<CachedTrialPositions>();
        //        }
        //        catch (Exception ex)
        //        {
        //            this.TrialInfo($"Error loading cache from {fileName}: {ex.Message}");
        //            return new List<CachedTrialPositions>();
        //        }
        //    }
        //    return new List<CachedTrialPositions>();
        //}

        private bool TryFindNewPositions(Trial trial)
        {
            // Reset trial record for current attempt
            //_trialRecords[trial.Id].ObjectPositions.Clear();
            //_trialRecords[trial.Id].FunctionId = -1;

            // Find random functions from the main window
            _mainWindow.Dispatcher.Invoke(() => {
                _trialRecords[trial.Id].Functions.AddRange(
                    _mainWindow.FindRandomFunctions(trial.FuncSide, trial.GetFunctionWidths(), trial.DistRangePX));
                foreach (int funcWidthMX in trial.GetFunctionWidths())
                {
                    _trialRecords[trial.Id].Functions.Add(
                        _mainWindow.FindRandomFunction(trial.FuncSide, funcWidthMX, trial.DistRangeMM));
                }
            });

            // Get a random Target from the main window (which calls the side window)
            //(int functionId, Point targetCenterAbsolute) = _mainWindow.Dispatcher.Invoke(() =>
            //{
            //    return _mainWindow.FindRandomFunction(trial.FuncSide, trial.TargetMultiple, trial.DistRangePX);
            //});

            // If a target was found, set the target id
            //if (functionId != -1)
            //{
            //    _trialRecords[trial.Id].FunctionId = functionId;
            //}
            //else
            //{
            //    this.TrialInfo($"Failed to find a random target id for Trial#{trial.Id}.");
            //    return false;
            //}

            // Get object constraint Rect
            Rect objConstraintRect = _mainWindow.Dispatcher.Invoke(() =>
            {
                return _mainWindow.GetObjAreaCenterConstraintRect();
            });

            // Find a valid object area position
            int maxRetries = 1000;
            double randDistMM = trial.DistRangeMM.GetRandomValue();

            Point objAreaCenterPosition = new Point(0, 0);
            //for (int t = 0; t < maxRetries; t++) {
            //    objAreaCenterPosition = objConstraintRect.FindRandPointWithDist(
            //        targetCenterAbsolute, 
            //        Utils.MM2PX(randDistMM), 
            //        trial.FuncSide.GetOpposite());

            //    if (objAreaCenterPosition.X != 0 && objAreaCenterPosition.Y != 0)
            //    {
            //        this.TrialInfo($"Found a valid object area center position for Trial#{trial.Id} at {objAreaCenterPosition}.");
            //        // Set the Rect
            //        _trialRecords[trial.Id].ObjectAreaRect = new Rect(
            //            objAreaCenterPosition.X - Utils.MM2PX(OBJ_AREA_WIDTH_MM / 2),
            //            objAreaCenterPosition.Y - Utils.MM2PX(OBJ_AREA_WIDTH_MM / 2),
            //            Utils.MM2PX(OBJ_AREA_WIDTH_MM),
            //            Utils.MM2PX(OBJ_AREA_WIDTH_MM));
            //        _trialRecords[trial.Id].Objects = PlaceObjectsInArea(objAreaCenterPosition, Experiment.REP_TRIAL_NUM_PASS);
            //        return true; // Successfully found positions
            //    }
            //}

            return false;
        }

        //private void SavePositionsToCache(Trial trial, CachedTrialPositions positionsToCache)
        //{
        //    string fileName = trial.GetCacheFileName(CacheDirectory);
        //    List<CachedTrialPositions> cachedPositions = LoadPositionsFromCache(trial);

        //    // Add new positions if not already present and manage size
        //    if (!cachedPositions.Any(c => c.TargetId == positionsToCache.TargetId && c.StartPositions.SequenceEqual(positionsToCache.StartPositions)))
        //    {
        //        cachedPositions.Add(positionsToCache);
        //        while (cachedPositions.Count > MaxCachedPositions)
        //        {
        //            cachedPositions.RemoveAt(0); // Remove the oldest entry
        //        }
        //    }

        //    try
        //    {
        //        string json = JsonConvert.SerializeObject(cachedPositions, Formatting.Indented);
        //        File.WriteAllText(fileName, json);
        //        //this.TrialInfo($"Saved {cachedPositions.Count} positions to cache: {fileName}");
        //    }
        //    catch (Exception ex)
        //    {
        //        this.TrialInfo($"Error saving cache to {fileName}: {ex.Message}");
        //    }
        //}

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
            var elementTag = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.Join(Str.OBJ, elementTag.ToString(), Str.PRESS));

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
                    SetObjectAsMarked(elementTag);
                    _pressedObjectId = elementTag;
                    //SetFunctionAsEnabled();
                    break;
                case (true, false, _, true): // Mouse, any marker state, all objects selected
                    EndActiveTrial(Experiment.Result.MISS); // Should not end on object press (should click area)
                    return;
            }

            e.Handled = true;

        }

        public override void OnObjectMouseUp(object sender, MouseButtonEventArgs e)
        {
            var elementTag = (int)((FrameworkElement)sender).Tag;
            LogEvent(string.Join("_", Str.OBJ, elementTag.ToString(), Str.RELEASE));

            var isToMo = _mainWindow.IsTechniqueToMo();
            //var markerOnFunction = _mainWindow.IsMarkerOnButton(_activeTrial.FuncSide, _activeTrialRecord.FunctionId);
            var markerOnFunction = true;
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
                    //SetFunctionAsEnabled();
                    break;
                case (false, _, true): // Mouse, any marker state, all objects selected
                    EndActiveTrial(Experiment.Result.HIT);
                    break;
            }

            
            e.Handled = true;

        }

        public override void IndexTap()
        {
            var technique = _mainWindow.GetActiveTechnique();
            var allObjSelected = _nSelectedObjects == _activeTrialRecord.Objects.Count;
            Side correspondingSide = Side.Top;
            var funcOnCorrespondingSide = _activeTrial.FuncSide == correspondingSide;

            this.TrialInfo($"Technique: {_mainWindow.GetActiveTechnique()}");

            switch (technique, allObjSelected, funcOnCorrespondingSide)
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
            Side correspondingSide = Side.Left;
            var funcOnCorrespondingSide = _activeTrial.FuncSide == correspondingSide;

            this.TrialInfo($"Technique: {_mainWindow.GetActiveTechnique()}");

            switch (technique, allObjSelected, funcOnCorrespondingSide)
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
            Side correspondingSide = Side.Right;
            var funcOnCorrespondingSide = _activeTrial.FuncSide == correspondingSide;

            this.TrialInfo($"Technique: {_mainWindow.GetActiveTechnique()}");

            switch (technique, allObjSelected, funcOnCorrespondingSide)
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
                Direction.Left => _activeTrial.FuncSide == Side.Left,
                Direction.Right => _activeTrial.FuncSide == Side.Right,
                Direction.Up => _activeTrial.FuncSide == Side.Top,
                _ => false
            };

            var dirOppositeSide = dir switch
            {
                Direction.Left => _activeTrial.FuncSide == Side.Right,
                Direction.Right => _activeTrial.FuncSide == Side.Left,
                Direction.Down => _activeTrial.FuncSide == Side.Top,
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
                    _mainWindow.ActivateAuxWindowMarker(_activeTrial.FuncSide);
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
