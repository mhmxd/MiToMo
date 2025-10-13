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

            // If consecutive trials have the same function Ids, re-order them (so marker doesn't stay on the same function)
            int maxAttempts = 100;
            int attempt = 0;
            while (attempt < maxAttempts && AreFunctionsRepeated())
            {
                _activeBlock.Trials.Shuffle();
                attempt++;
            }

            if (attempt == maxAttempts)
            {
                this.TrialInfo($"Warning: Could not eliminate repeated functions in consecutive trials after {maxAttempts} attempts.");
                return false;
            }

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

            // Find functions
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


            // Add objects
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

                // Randomly map objects to functions
                List<int> objIds = _trialRecords[trial.Id].Objects.Select(o => o.Id).ToList();
                objIds.Shuffle();
                for (int i = 0; i < objIds.Count; i++)
                {
                    int functionId = _trialRecords[trial.Id].Functions[i % _trialRecords[trial.Id].Functions.Count].Id;
                    _trialRecords[trial.Id].MapObjectToFunction(objIds[i], functionId);
                }

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
            //_activeTrialRecord.RecordEvent(Str.TRIAL_SHOW);
            LogEvent(Str.TRIAL_SHOW, _activeTrial.Id.ToString());

            // Set the target window based on the trial's target side
            _mainWindow.SetTargetWindow(_activeTrial.FuncSide, OnAuxWindowMouseEnter, OnAuxWindowMouseExit, OnAuxWindowMouseDown, OnAuxWindowMouseUp);

            // Color the function button and set the handlers
            
            //Brush funcDefaultColor = Config.FUNCTION_DEFAULT_COLOR;
            //_mainWindow.FillButtonInTargetWindow(
            //    _activeTrial.FuncSide, _activeTrialRecord.FunctionId, 
            //    funcDefaultColor);
            //_mainWindow.FillButtonsInAuxWindow(_activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds(), funcDefaultColor);
            //_mainWindow.SetGridButtonHandlers(
            //    _activeTrial.FuncSide, _activeTrialRecord.FunctionId,
            //    OnFunctionMouseDown, OnFunctionMouseUp, OnNonTargetMouseDown);
            UpdateScene();
            _mainWindow.SetAuxButtonsHandlers(
                _activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds(),
                OnFunctionMouseEnter, OnFunctionMouseDown, OnFunctionMouseUp, 
                OnFunctionMouseExit, OnNonTargetMouseDown);

            // If on ToMo, activate the auxiliary window marker on all sides
            if (_mainWindow.IsTechniqueToMo()) _mainWindow.ShowAllAuxMarkers();

            // Show the area
            MouseEvents objAreaEvents = new MouseEvents(OnObjectAreaMouseDown, OnObjectAreaMouseUp, OnObjectAreaMouseEnter, OnObjectAreaMouseExit);
            _mainWindow.ShowObjectsArea(
                _activeTrialRecord.ObjectAreaRect, Config.OBJ_AREA_BG_COLOR,
                objAreaEvents);

            // Show the objects
            MouseEvents objectEvents = new MouseEvents(
                OnObjectMouseEnter, OnObjectMouseDown, OnObjectMouseUp, OnObjectMouseLeave);
            _mainWindow.ShowObjects(_activeTrialRecord.Objects, Config.OBJ_DEFAULT_COLOR, objectEvents);

            // Show Start Trial button
            MouseEvents startButtonEvents = new MouseEvents(OnStartButtonMouseDown, OnStartButtonMouseUp, OnStartButtonMouseEnter, OnStartButtonMouseExit);
            _mainWindow.ShowStartTrialButton(_activeTrialRecord.ObjectAreaRect, startButtonEvents);

            // Update info label
            _mainWindow.UpdateInfoLabel();
        }

        public override void EndActiveTrial(Result result)
        {
            this.TrialInfo($"Trial#{_activeTrial.Id} completed: {result}");
            this.TrialInfo(Str.MAJOR_LINE);
            LogEvent(Str.TRIAL_END, _activeTrial.Id); // Log the trial end timestamp
            _mainWindow.DeactivateAuxWindow(); // Deactivate the aux window

            switch (result)
            {
                case Result.HIT:
                    Sounder.PlayHit();
                    double trialTime = GetDuration(Str.STR_RELEASE + "_1", Str.TRIAL_END);
                    _activeTrialRecord.AddTime(Str.TRIAL_TIME, trialTime);
                    // -- Log
                    if (_activeTrial.GetNumFunctions() == 1) ExperiLogger.LogMOSFTrial(_activeBlockNum, _activeTrialNum, _activeTrial, _activeTrialRecord);
                    else ExperiLogger.LogMOMFTrial(_activeBlockNum, _activeTrialNum, _activeTrial, _activeTrialRecord);
                        //ExperiLogger.LogTrialMessage($"{_activeTrial.ToStr().PadRight(34)} Trial Time = {trialTime:F2}s");
                        GoToNextTrial();
                    break;
                case Result.MISS:
                    Sounder.PlayTargetMiss();
                    _activeBlock.ShuffleBackTrial(_activeTrialNum);
                    GoToNextTrial();
                    break;
                case Result.ERROR:
                    Sounder.PlayStartMiss();
                    // Do nothing, just reset everything

                    break;
            }

        }

        public override void GoToNextTrial()
        {
            _mainWindow.ResetTargetWindow(_activeTrial.FuncSide); // Reset the target window
            _mainWindow.ClearCanvas(); // Clear the main canvas
            //_trialEventCounts.Clear(); // Reset the event counts for the trial
            //_trialTimestamps.Clear(); // Reset the timestamps for the trial
            _isTargetAvailable = false; // Reset the target availability
            _nSelectedObjects = 0; // Reset the number of applied objects
            _pressedObjectId = -1; // Reset the pressed object id
            _isFunctionClicked = false; // Reset the function clicked state

            if (_activeTrialNum < _activeBlock.Trials.Count)
            {
                //_mainWindow.ShowStartTrialButton(OnStartButtonMouseUp);
                //_mainWindow.ResetTargetWindow(_activeTrial.FuncSide);
                _activeTrialRecord.ClearTimestamps();
                _functionsVisitMap.Clear();
                _objectsVisitMap.Clear();

                _activeTrialNum++;
                _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
                _activeTrialRecord = _trialRecords[_activeTrial.Id];

                ShowActiveTrial();
            }
            else
            {
                // Log block time
                ExperiLogger.LogBlockTime(_activeBlock);

                // Show end of block window
                BlockEndWindow blockEndWindow = new BlockEndWindow(_mainWindow.GoToNextBlock);
                blockEndWindow.Owner = _mainWindow;
                blockEndWindow.ShowDialog();
            }

        }

        public override void OnMainWindowMouseDown(Object sender, MouseButtonEventArgs e)
        {
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TrialEventsToString()}");
        }

        public override void OnMainWindowMouseMove(Object sender, MouseEventArgs e)
        {
            LogEventOnce(Str.FIRST_MOVE);
        }

        public override void OnMainWindowMouseUp(Object sender, MouseButtonEventArgs e)
        {
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TrialEventsToString()}");
        }

        public override void OnFunctionMarked(int funId)
        {
            LogEvent(Str.FUN_MARKED, funId.ToString());

            // Enable the function and the corresponding objects
            if (_activeTrial.NFunctions > 1)
            {
                _activeTrialRecord.MarkFunction(funId);
                int objId = _activeTrialRecord.FindMappedObjectId(funId);
                _activeTrialRecord.MarkObject(objId);
                UpdateScene();
            }

        }

        public override void OnFunctionMouseEnter(Object sender, MouseEventArgs e)
        {
            // Add the id to the list of visited if not already there (will use the index for the order of visit)
            int funId = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.FUN_ENTER, funId);
        }

        public override void OnFunctionMouseDown(Object sender, MouseButtonEventArgs e)
        {
            //this.TrialInfo($"Events: {_activeTrialRecord.TrialEventsToString()}");
            int funId = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.FUN_PRESS, funId);
            //if (_activeTrial.NFunctions == 1) LogEventWithCount(Str.FUN_PRESS);
            //else LogEventWithIndex(Str.FUN_PRESS, funId);

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnFunctionMouseUp(Object sender, MouseButtonEventArgs e)
        {
            var funId = (int)((FrameworkElement)sender).Tag;
            var device = Utils.GetDevice(_activeBlock.Technique);
            LogEvent(Str.FUN_RELEASE, funId);
            //if (_activeTrial.NFunctions == 1) LogEventWithCount(Str.FUN_RELEASE);
            //else LogEventWithIndex(Str.FUN_RELEASE, funId);

            //this.TrialInfo($"Events: {_activeTrialRecord.TrialEventsToString()}");

            switch (device)
            {
                case Technique.MOUSE:
                    _activeTrialRecord.ApplyFunction(funId);
                    UpdateScene();
                    break;
            }


            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnFunctionMouseExit(Object sender, MouseEventArgs e)
        {
            int funId = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.FUN_EXIT, funId);
            //if (_activeTrial.NFunctions == 1) LogEventWithCount(Str.FUN_EXIT);
            //else LogEventWithIndex(Str.FUN_EXIT, funId);
        }

        public override void OnNonTargetMouseDown(Object sender, MouseButtonEventArgs e)
        {
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TrialEventsToString()}");
            SButton clickedButton = sender as SButton;
            var functionId = (int)((FrameworkElement)sender).Tag;
            this.TrialInfo($"Non-function id: {functionId}");

            if (clickedButton != null && clickedButton.Tag is Dictionary<string, int>)
            {
                // Show neighbor IDs
                Dictionary<string, int> tag = clickedButton.Tag as Dictionary<string, int>;
                this.TrialInfo($"Clicked button ID: {clickedButton.Id}, Left: {tag["LeftId"]}, Right: {tag["RightId"]}, Top: {tag["TopId"]}, Bottom: {tag["BottomId"]}");
            }

            // It's always a miss
            EndActiveTrial(Result.MISS);

            e.Handled = true; // Mark the event as handled to prevent further processing
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

        private List<TrialRecord.TObject> PlaceObjectsInArea(Point objAreaCenterPosition, int nObjects)
        {
            List<TrialRecord.TObject> placedObjects = new List<TrialRecord.TObject>();
            double objW = Utils.MM2PX(Experiment.OBJ_WIDTH_MM);
            double areaW = Utils.MM2PX(Experiment.OBJ_AREA_WIDTH_MM);

            int maxAttemptsPerObject = 1000; // Limit attempts to prevent infinite loops

            // Compute area bounds (top-left corner)
            double areaHalf = areaW / 2;

            for (int i = 0; i < nObjects; i++)
            {
                bool placed = false;
                for (int attempt = 0; attempt < maxAttemptsPerObject; attempt++)
                {
                    // 1. Generate a random potential center for the new square
                    Point potentialCenter = GenerateRandomPointInSquare(objAreaCenterPosition, areaW, objW);

                    // Calculate the top-left corner from the potential center
                    Point topLeft = new Point(potentialCenter.X - objW / 2, potentialCenter.Y - objW / 2);

                    // 2. Check for overlaps with already placed objects
                    if (!HasOverlap(topLeft, objW, placedObjects))
                    {
                        TrialRecord.TObject trialObject = new TrialRecord.TObject(i + 1, topLeft, potentialCenter);

                        placedObjects.Add(trialObject);
                        placed = true;
                        break; // Move to the next object
                    }
                }

                if (!placed)
                {
                    this.TrialInfo($"Warning: Could not place object {i + 1} after {maxAttemptsPerObject} attempts.");
                }
            }

            return placedObjects;
        }

        //private List<TrialRecord.TObject> PlaceObjectsInArea(Point objAreaCenterPosition, int nObjects)
        //{
        //    List<TrialRecord.TObject> placedObjects = new List<TrialRecord.TObject>();
        //    double objW = Utils.MM2PX(Experiment.OBJ_WIDTH_MM);
        //    double areaW = Utils.MM2PX(Experiment.REP_TRIAL_OBJ_AREA_RADIUS_MM);

        //    int maxAttemptsPerObject = 1000;
        //    int maxTotalRestarts = 5; // Maximum number of complete restarts
        //    int restartCount = 0;

        //    while (placedObjects.Count < nObjects && restartCount < maxTotalRestarts)
        //    {
        //        int i = placedObjects.Count; // Current object index
        //        bool placed = false;

        //        for (int attempt = 0; attempt < maxAttemptsPerObject; attempt++)
        //        {
        //            // 1. Generate a random potential center for the new square
        //            Point potentialCenter = GenerateRandomPointInCircle(objAreaCenterPosition, areaW - objW / 2);
        //            // Calculate the top-left corner from the potential center
        //            Point topLeft = new Point(potentialCenter.X - objW / 2, potentialCenter.Y - objW / 2);

        //            // 2. Check for overlaps with already placed objects
        //            if (!HasOverlap(topLeft, objW, placedObjects))
        //            {
        //                TrialRecord.TObject trialObject = new TrialRecord.TObject(i + 1, topLeft, potentialCenter);
        //                placedObjects.Add(trialObject);
        //                placed = true;
        //                break;
        //            }
        //        }

        //        if (!placed)
        //        {
        //            // Backtracking: remove last 1-2 objects and try again
        //            if (placedObjects.Count > 0)
        //            {
        //                int removeCount = Math.Min(2, placedObjects.Count);
        //                placedObjects.RemoveRange(placedObjects.Count - removeCount, removeCount);
        //                //this.TrialInfo($"Backtracking: removed {removeCount} object(s), now have {placedObjects.Count}");
        //            }
        //            else
        //            {
        //                // No objects to backtrack, need complete restart
        //                restartCount++;
        //                //this.TrialInfo($"Complete restart #{restartCount}");
        //            }
        //        }
        //    }

        //    if (placedObjects.Count < nObjects)
        //    {
        //        this.TrialInfo($"Warning: Could only place {placedObjects.Count} out of {nObjects} objects after {restartCount} restarts.");
        //    }

        //    return placedObjects;
        //}

        //private List<TrialRecord.TObject> PlaceObjectsInArea(Point objAreaCenterPosition, int nObjects)
        //{
        //    List<TrialRecord.TObject> placedObjects = new List<TrialRecord.TObject>();
        //    double objW = Utils.MM2PX(Experiment.OBJ_WIDTH_MM);
        //    double areaW = Utils.MM2PX(Experiment.OBJ_AREA_WIDTH_MM);

        //    // Generate a pool of candidate positions on a grid
        //    List<Point> candidatePositions = GenerateGridPositions(objAreaCenterPosition, areaW, objW);

        //    // Shuffle to randomize which positions we pick
        //    Random random = new Random();
        //    candidatePositions = candidatePositions.OrderBy(x => random.Next()).ToList();

        //    // Place objects by picking from shuffled candidates
        //    for (int i = 0; i < nObjects && i < candidatePositions.Count; i++)
        //    {
        //        Point center = candidatePositions[i];
        //        Point topLeft = new Point(center.X - objW / 2, center.Y - objW / 2);

        //        // Verify it doesn't overlap (should be rare with proper grid spacing)
        //        if (!HasOverlap(topLeft, objW, placedObjects))
        //        {
        //            TrialRecord.TObject trialObject = new TrialRecord.TObject(placedObjects.Count + 1, topLeft, center);
        //            placedObjects.Add(trialObject);
        //        }
        //    }

        //    if (placedObjects.Count < nObjects)
        //    {
        //        this.TrialInfo($"Warning: Could only place {placedObjects.Count} out of {nObjects} objects.");
        //    }

        //    return placedObjects;
        //}

        //private List<Point> GenerateGridPositions(Point center, double radius, double objW)
        //{
        //    List<Point> positions = new List<Point>();

        //    // Grid spacing: object width + small gap for safety
        //    double spacing = objW * 1.2; // 20% gap between objects

        //    // Create grid within bounding box, then filter to circle
        //    int gridRange = (int)Math.Ceiling(radius / spacing);

        //    for (int x = -gridRange; x <= gridRange; x++)
        //    {
        //        for (int y = -gridRange; y <= gridRange; y++)
        //        {
        //            Point gridPoint = new Point(
        //                center.X + x * spacing,
        //                center.Y + y * spacing
        //            );

        //            // Check if this point is within the circular area (with margin for object size)
        //            double distanceFromCenter = Math.Sqrt(
        //                Math.Pow(gridPoint.X - center.X, 2) +
        //                Math.Pow(gridPoint.Y - center.Y, 2)
        //            );

        //            if (distanceFromCenter <= radius - objW / 2)
        //            {
        //                positions.Add(gridPoint);
        //            }
        //        }
        //    }

        //    return positions;
        //}

        private Point GenerateRandomPointInSquare(Point areaCenter, double areaWidth, double objWidth)
        {
            double areaHalf = areaWidth / 2.0;
            double margin = objWidth / 2.0; // Ensure object stays fully inside

            // Define valid range for the object’s center
            double minX = areaCenter.X - areaHalf + margin;
            double maxX = areaCenter.X + areaHalf - margin;
            double minY = areaCenter.Y - areaHalf + margin;
            double maxY = areaCenter.Y + areaHalf - margin;

            // Generate a random point inside that range
            double x = minX + _random.NextDouble() * (maxX - minX);
            double y = minY + _random.NextDouble() * (maxY - minY);

            return new Point(x, y);
        }

        private bool HasOverlap(Point newObjTopLeft, double newObjW, List<TrialRecord.TObject> existingObjs)
        {
            double newObjRight = newObjTopLeft.X + newObjW;
            double newObjBottom = newObjTopLeft.Y + newObjW;

            foreach (TrialRecord.TObject existingObject in existingObjs)
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

        public override void OnObjectMouseEnter(object sender, MouseEventArgs e)
        {
            // Add the id to the list of visited if not already there (will use the index for the order of visit)
            int objId = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.OBJ_ENTER, objId);
        }

        public override void OnObjectMouseLeave(object sender, MouseEventArgs e)
        {
            int objId = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.OBJ_EXIT, objId);
        }

        public override void OnObjectMouseDown(object sender, MouseButtonEventArgs e)
        {
            var objId = (int)((FrameworkElement)sender).Tag;
            LogEvent(Str.OBJ_PRESS, objId);

            var startButtonClicked = GetEventCount(Str.STR_RELEASE) > 0;
            var device = Utils.GetDevice(_activeBlock.Technique);
            int funcIdUnderMarker = _mainWindow.FunctionIdUnderMarker(_activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds());
            var markerOverFunction = funcIdUnderMarker != -1;
            var allObjectsApplied = _activeTrialRecord.AreAllObjectsApplied();

            this.TrialInfo($"StartButtonClicked: {startButtonClicked}; Technique: {device}; " +
                $"MarkerOnFunction: {markerOverFunction}; AllObjApplied: {allObjectsApplied}");

            switch (startButtonClicked, device, markerOverFunction, allObjectsApplied)
            {
                case (false, _, _, _): // Start button not clicked, _
                    //EndActiveTrial(Result.ERROR); // Pressed on object without Start button clicked
                    break;

                case (true, Technique.TOMO, true, false): // ToMo, marker over function, not all objects applied
                    _activeTrialRecord.MarkObject(objId);
                    //UpdateScene();
                    break;

                case (true, Technique.MOUSE, _, false): // MOUSE, any marker state, not all objects selected
                    _activeTrialRecord.MarkObject(objId);
                    _pressedObjectId = objId;
                    UpdateScene();
                    //SetFunctionAsEnabled();
                    break;
                case (true, Technique.MOUSE, _, true): // MOUSE, any marker state, all objects selected
                    EndActiveTrial(Result.MISS); // Should not end on object press (should click area)
                    return;
            }

            e.Handled = true;

        }

        public override void OnObjectMouseUp(object sender, MouseButtonEventArgs e)
        {
            var objId = (int)((FrameworkElement)sender).Tag;
            TrialEvent lastTrialEvent = _activeTrialRecord.GetLastTrialEvent();

            var device = Utils.GetDevice(_activeBlock.Technique);
            var thisObjPressed = lastTrialEvent.Type == Str.OBJ_PRESS && lastTrialEvent.Id == objId.ToString();
            int funcIdUnderMarker = _mainWindow.FunctionIdUnderMarker(_activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds());
            var markerOverFunction = funcIdUnderMarker != -1;
            var allObjectsApplied = _activeTrialRecord.AreAllObjectsApplied();

            // Show all the flags
            this.TrialInfo($"Technique: {device}, ThisObjPressed: {thisObjPressed}; MarkerOnFunction: {markerOverFunction}, AllObjSelected: {allObjectsApplied}");

            switch (device, thisObjPressed, markerOverFunction, allObjectsApplied)
            {
                case (Technique.TOMO, true, true, false): // ToMo, object pressed, marker on function, not all objects applied
                    _activeTrialRecord.ApplyFunction(funcIdUnderMarker);
                    UpdateScene();
                    break;
                case (Technique.TOMO, true, false, false): // ToMo, object pressed, marker NOT on function, not all objects applied
                    // Act based on the flag
                    break;


                case (Technique.MOUSE, true, _, false): // MOUSE, _, not all functions selected
                    _activeTrialRecord.MarkObject(objId);
                    _activeTrialRecord.MarkFunction(_activeTrialRecord.FindMappedFunctionId(objId));
                    UpdateScene();
                    break;
                case (Technique.MOUSE, true, _, true): // MOUSE, _, all functions applied
                    EndActiveTrial(Result.HIT);
                    break;
            }

            LogEvent(Str.OBJ_RELEASE, objId);
            e.Handled = true;

        }
        public override void OnObjectAreaMouseEnter(object sender, MouseEventArgs e)
        {
            // Only log if entered from outside (NOT from the object)
            if (_activeTrialRecord.GetLastTrialEventType() != Str.OBJ_EXIT) LogEvent(Str.ARA_ENTER);
        }

        public override void OnObjectAreaMouseDown(Object sender, MouseButtonEventArgs e)
        {
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TrialEventsToString()}");
            LogEvent(Str.ARA_PRESS);

            var allObjectsApplied = _activeTrialRecord.AreAllObjectsApplied();

            switch (allObjectsApplied)
            {
                case true:
                    // All objects are selected, so we can end the trial
                    EndActiveTrial(Result.HIT);
                    break;
                case false:
                    // Not all objects are selected, so we treat it as a miss
                    EndActiveTrial(Result.MISS);
                    break;
            }

            e.Handled = true; // Mark the event as handled to prevent further processing

        }

        public override void OnObjectAreaMouseUp(object sender, MouseButtonEventArgs e)
        {
            LogEvent(Str.ARA_RELEASE);
        }

        public override void OnObjectAreaMouseExit(object sender, MouseEventArgs e)
        {
            LogEvent(Str.ARA_EXIT);
        }

        public override void IndexTap()
        {
            var technique = _activeBlock.GetSpecificTechnique();
            var allObjSelected = _nSelectedObjects == _activeTrialRecord.Objects.Count;
            Side correspondingSide = Side.Top;
            var funcOnCorrespondingSide = _activeTrial.FuncSide == correspondingSide;

            this.TrialInfo($"Technique: {Utils.GetDevice(_activeBlock.Technique)}");

            switch (technique, allObjSelected, funcOnCorrespondingSide)
            {
                case (Technique.TOMO_TAP, false, true): // Correct side activated
                    _mainWindow.ActivateAuxWindowMarker(correspondingSide);
                    break;
                case (Technique.TOMO_TAP, false, false): // Incorrect side activated
                    EndActiveTrial(Result.MISS);
                    break;
                case (Technique.TOMO_TAP, true, true): // Correct deactivation
                    EndActiveTrial(Result.HIT);
                    break;
                case (Technique.TOMO_TAP, true, false): // Incorrect deactivation
                    EndActiveTrial(Result.MISS);
                    break;

                case (Technique.TOMO_SWIPE, _, _): // Wrong _technique for index tap
                    EndActiveTrial(Result.MISS);
                    break;
            }
        }

        public override void ThumbTap(long downInstant, long upInstant)
        {
            var technique = _activeBlock.GetSpecificTechnique();
            var allObjSelected = _nSelectedObjects == _activeTrialRecord.Objects.Count;
            Side correspondingSide = Side.Left;
            var funcOnCorrespondingSide = _activeTrial.FuncSide == correspondingSide;

            this.TrialInfo($"Technique: {_activeBlock.GetSpecificTechnique()}");

            switch (technique, allObjSelected, funcOnCorrespondingSide)
            {
                case (Technique.TOMO_TAP, false, true): // Correct side activated
                    _mainWindow.ActivateAuxWindowMarker(correspondingSide);
                    break;
                case (Technique.TOMO_TAP, false, false): // Incorrect side activated
                    EndActiveTrial(Result.MISS);
                    break;
                case (Technique.TOMO_TAP, true, true): // Correct deactivation
                    EndActiveTrial(Result.HIT);
                    break;
                case (Technique.TOMO_TAP, true, false): // Incorrect deactivation
                    EndActiveTrial(Result.MISS);
                    break;

                case (Technique.TOMO_SWIPE, _, _): // Wrong _technique for index tap
                    EndActiveTrial(Result.MISS);
                    break;
            }
        }

        public override void MiddleTap()
        {
            var technique = _activeBlock.GetSpecificTechnique();
            var allObjSelected = _nSelectedObjects == _activeTrialRecord.Objects.Count;
            Side correspondingSide = Side.Right;
            var funcOnCorrespondingSide = _activeTrial.FuncSide == correspondingSide;

            this.TrialInfo($"Technique: {_activeBlock.GetSpecificTechnique()}");

            switch (technique, allObjSelected, funcOnCorrespondingSide)
            {
                case (Technique.TOMO_TAP, false, true): // Correct side activated
                    _mainWindow.ActivateAuxWindowMarker(correspondingSide);
                    break;
                case (Technique.TOMO_TAP, false, false): // Incorrect side activated
                    EndActiveTrial(Result.MISS);
                    break;
                case (Technique.TOMO_TAP, true, true): // Correct deactivation
                    EndActiveTrial(Result.HIT);
                    break;
                case (Technique.TOMO_TAP, true, false): // Incorrect deactivation
                    EndActiveTrial(Result.MISS);
                    break;

                case (Technique.TOMO_SWIPE, _, _): // Wrong _technique for index tap
                    EndActiveTrial(Result.MISS);
                    break;
            }
        }

        public override void ThumbTap(Side side)
        {
            //ThumbTap();
        }

        public override void ThumbSwipe(Direction dir)
        {
            var technique = _activeBlock.GetSpecificTechnique();
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

            this.TrialInfo($"Technique: {_activeBlock.GetSpecificTechnique()}");

            switch (technique, allObjSelected, dirMatchesSide, dirOppositeSide)
            {
                case (Technique.TOMO_SWIPE, true, _, true): // Deactivation success
                    EndActiveTrial(Result.HIT);
                    break;
                case (Technique.TOMO_SWIPE, true, _, false): // Deactivation failure
                    EndActiveTrial(Result.MISS);
                    break;
                case (Technique.TOMO_SWIPE, false, true, _): // Correct activation swipe
                    _mainWindow.ActivateAuxWindowMarker(_activeTrial.FuncSide);
                    break;
                case (Technique.TOMO_SWIPE, false, false, _): // Incorrect activation swipe
                    EndActiveTrial(Result.MISS);
                    break;

                case (Technique.TOMO_TAP, _, _, _): // Wrong _technique for swipe
                    EndActiveTrial(Result.MISS);
                    break;

            }
        }

        
    }
}
