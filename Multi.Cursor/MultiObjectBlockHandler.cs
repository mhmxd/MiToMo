using Common.Constants;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CommonUI;
using Common.Helpers;
using static Common.Constants.ExpEnums;

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
            //int maxAttempts = 100;
            //int attempt = 0;
            //while (attempt < maxAttempts && AreFunctionsRepeated())
            //{
            //    _activeBlock.Trials.Shuffle();
            //    attempt++;
            //}

            //if (attempt == maxAttempts)
            //{
            //    this.TrialInfo($"Warning: Could not eliminate repeated functions in consecutive trials after {maxAttempts} attempts.");
            //    return false;
            //}

            return true;
        }

        public override bool FindPositionsForTrial(Trial trial)
        {
            int objW = UITools.MM2PX(ExpSizes.OBJ_WIDTH_MM);
            int objHalfW = objW / 2;
            int objAreaW = UITools.MM2PX(ExpSizes.OBJ_AREA_WIDTH_MM);
            int objAreaHalfW = objAreaW / 2;
            this.PositionInfo($"{trial.ToStr()}");

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

            this.PositionInfo($"Found functions: {_trialRecords[trial.Id].GetFunctionIds().Str()}");

            // Find a position for the object area
            Rect objectAreaConstraintRect = _mainWindow.Dispatcher.Invoke(() =>
            {
                return _mainWindow.GetObjAreaCenterConstraintRect();
            });

            (Point objAreaCenter, double avgDist) = objectAreaConstraintRect.FindPointWithinDistRangeFromMultipleSources(
                _trialRecords[trial.Id].GetFunctionCenters(), trial.DistRangePX);


            // Add objects
            if (objAreaCenter.X == -1 && objAreaCenter.Y == -1) // Failed to find a valid position 
            {
                this.PositionInfo($"No valid position found for object in Trial#{trial.Id}!");
                return false; // Return false to indicate failure
            }
            else
            {
                // Get the top-left corner of the object area rectangle
                Point objAreaPosition = objAreaCenter.OffsetPosition(-objAreaHalfW);

                this.PositionInfo($"Found object area position: {objAreaPosition.Str()}");

                _trialRecords[trial.Id].ObjectAreaRect = new Rect(
                        objAreaPosition.X,
                        objAreaPosition.Y,
                        objAreaW,
                        objAreaW);

                _trialRecords[trial.Id].AvgDistanceMM = avgDist;

                // Place objects in the area
                _trialRecords[trial.Id].Objects = PlaceObjectsInArea(objAreaCenter, trial.NObjects);
                this.PositionInfo($"Placed {_trialRecords[trial.Id].Objects.Count} objects");

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
            base.ShowActiveTrial();

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

        //public override void EndActiveTrial(Result result)
        //{
        //    this.TrialInfo($"Trial#{_activeTrial.Id} completed: {result}");
        //    this.TrialInfo(ExpStrs.MAJOR_LINE);
        //    LogEvent(ExpStrs.TRIAL_END, _activeTrial.Id); // Log the trial end timestamp
        //    _mainWindow.DeactivateAuxWindow(); // Deactivate the aux window

        //    switch (result)
        //    {
        //        case Result.HIT:
        //            Sounder.PlayHit();
        //            double trialTime = GetDuration(ExpStrs.STR_RELEASE + "_1", ExpStrs.TRIAL_END);
        //            _activeTrialRecord.AddTime(ExpStrs.TRIAL_TIME, trialTime);
                    
        //            //ExperiLogger.LogTrialMessage($"{_activeTrial.ToStr().PadRight(34)} Trial Time = {trialTime:F2}s");
        //            GoToNextTrial();
        //            break;
        //        case Result.MISS:
        //            Sounder.PlayTargetMiss();

        //            _activeBlock.ShuffleBackTrial(_activeTrialNum);
        //            _trialRecords[_activeTrial.Id].ClearTimestamps();
        //            _trialRecords[_activeTrial.Id].ResetStates();

        //            GoToNextTrial();
        //            break;
        //    }

        //    //-- Log
        //    if (_activeTrial.GetNumFunctions() == 1) ExperiLogger.LogMOSFTrial(_activeBlockNum, _activeTrialNum, _activeTrial, _activeTrialRecord);
        //    else ExperiLogger.LogMOMFTrial(_activeBlockNum, _activeTrialNum, _activeTrial, _activeTrialRecord);
        //}

        public override void GoToNextTrial()
        {
            _mainWindow.ResetTargetWindow(_activeTrial.FuncSide); // Reset the target window
            _mainWindow.ClearCanvas(); // Clear the main canvas

            _isTargetAvailable = false; // Reset the target availability
            _nSelectedObjects = 0; // Reset the number of applied objects
            _pressedObjectId = -1; // Reset the pressed object id
            _isFunctionClicked = false; // Reset the function clicked state

            if (_activeTrialNum < _activeBlock.Trials.Count)
            {
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

        public override void OnFunctionMarked(int funId)
        {
            base.OnFunctionMarked(funId);

            // Enable the function and the corresponding objects
            if (_activeTrial.NFunctions > 1)
            {
                int objId = _activeTrialRecord.FindMappedObjectId(funId);
                _activeTrialRecord.MarkObject(objId);   
            }
            else // One function => mark all objects
            {
                _activeTrialRecord.MarkAllObjects();
            }

            UpdateScene();

        }

        public override void OnFunctionUnmarked(int funId)
        {
            base.OnFunctionUnmarked(funId);

            // Disable the function and the corresponding objects
            if (_activeTrial.NFunctions > 1)
            {
                int objId = _activeTrialRecord.FindMappedObjectId(funId);
                this.TrialInfo($"Demark obj#{objId}");
                _activeTrialRecord.UnmarkObject(objId);
            }
            else // One function => unmark all objects
            {
                _activeTrialRecord.UnmarkAllObjects();
            }

            UpdateScene();
        }

        public override void OnFunctionMouseUp(Object sender, MouseButtonEventArgs e)
        {
            // If the trial has already ended, ignore further events
            if (_activeTrialRecord.GetLastTrialEventType() == ExpStrs.TRIAL_END)
            {
                e.Handled = true;
                return;
            }

            base.OnFunctionMouseUp(sender, e);

            if (!IsStartClicked())
            {
                Sounder.PlayStartMiss();
                e.Handled = true;
                return;
            }

            //-- Trial started:

            int markedObjId = _activeTrialRecord.GetMarketObjectId();
            
            if (markedObjId == -1) // No object marked
            {
                EndActiveTrial(Result.MISS);
                e.Handled = true;
                return;
            }

            //-- An object is marked:

            if (_activeTrial.Technique == Technique.MOUSE)
            {
                var funId = (int)((FrameworkElement)sender).Tag;
                _activeTrialRecord.ApplyFunction(funId, markedObjId);
                UpdateScene();
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
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
            //        UITools.MM2PX(randDistMM), 
            //        trial.FuncSide.GetOpposite());

            //    if (objAreaCenterPosition.x != 0 && objAreaCenterPosition.y != 0)
            //    {
            //        this.TrialInfo($"Found a valid object area center position for Trial#{trial.Id} at {objAreaCenterPosition}.");
            //        // Set the Rect
            //        _trialRecords[trial.Id].ObjectAreaRect = new Rect(
            //            objAreaCenterPosition.x - UITools.MM2PX(OBJ_AREA_WIDTH_MM / 2),
            //            objAreaCenterPosition.y - UITools.MM2PX(OBJ_AREA_WIDTH_MM / 2),
            //            UITools.MM2PX(OBJ_AREA_WIDTH_MM),
            //            UITools.MM2PX(OBJ_AREA_WIDTH_MM));
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
            double objW = UITools.MM2PX(ExpSizes.OBJ_WIDTH_MM);
            double areaW = UITools.MM2PX(ExpSizes.OBJ_AREA_WIDTH_MM);

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

        public override void OnObjectAreaMouseUp(object sender, MouseButtonEventArgs e)
        {
            base.OnObjectAreaMouseUp(sender, e);

            if (!IsStartClicked())
            {
                Sounder.PlayStartMiss();
                e.Handled = true; // Mark the event as handled to prevent further processing
                return; // Do nothing if start button was not clicked
            }

            //-- Trial started:
            if (_activeTrialRecord.AreAllObjectsApplied())
            {
                EndActiveTrial(Result.HIT);
            }
            else
            {
                EndActiveTrial(Result.MISS);
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public override void OnObjectMouseDown(object sender, MouseButtonEventArgs e)
        {
            base.OnObjectMouseDown(sender, e);

            // Pressed on the Object without starting the trial
            if (!IsStartClicked())
            {
                Sounder.PlayStartMiss();
                e.Handled = true; // Mark the event as handled to prevent further processing
                return; // Do nothing if start button was not clicked
            }

            //-- Trial started:

            if (_activeTrial.IsTechniqueToMo())
            {
                // Nothing for now
            }
            else // MOUSE
            {
                var allObjectsApplied = _activeTrialRecord.AreAllObjectsApplied();
                var objId = (int)((FrameworkElement)sender).Tag;

                if (allObjectsApplied)
                {
                    EndActiveTrial(Result.MISS); // Should not end on object press (should click area)
                    return;
                }
                else
                {
                    // Is object already clicked? => MISS
                    if (_activeTrialRecord.IsObjectClicked(objId))
                    {
                        EndActiveTrial(Result.MISS);
                    } 
                    else
                    {
                        _pressedObjectId = objId;
                    }

                        
                }

            }

                

            //var startButtonClicked = GetEventCount(ExpStrs.STR_RELEASE) > 0;
            //var device = Utils.GetDevice(_activeBlock.Technique);
            //int funcIdUnderMarker = _mainWindow.FunctionIdUnderMarker(_activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds());
            //var markerOverFunction = funcIdUnderMarker != -1;
            

            //this.TrialInfo($"StartButtonClicked: {startButtonClicked}; Technique: {device}; " +
            //    $"MarkerOnFunction: {markerOverFunction}; AllObjApplied: {allObjectsApplied}");

            //switch (startButtonClicked, device, markerOverFunction, allObjectsApplied)
            //{

            //    case (true, Technique.TOMO, true, false): // ToMo, marker over function, not all objects applied
            //        //_activeTrialRecord.MarkObject(objId);
            //        //UpdateScene();
            //        break;

            //    case (true, Technique.MOUSE, _, false): // MOUSE, any marker state, not all objects selected
            //        //_activeTrialRecord.MarkObject(objId);
                    
            //        //UpdateScene();
            //        //SetFunctionAsEnabled();
            //        break;
            //    case (true, Technique.MOUSE, _, true): // MOUSE, any marker state, all objects selected
            //        EndActiveTrial(Result.MISS); // Should not end on object press (should click area)
            //        return;
            //}

            e.Handled = true;

        }

        public override void OnObjectMouseUp(object sender, MouseButtonEventArgs e)
        {
            base.OnObjectMouseUp(sender, e); // For logging the event

            var objId = (int)((FrameworkElement)sender).Tag;

            if (!IsStartClicked())
            {
                this.TrialInfo($"Start wasn't clicked");
                Sounder.PlayStartMiss();
                e.Handled = true; // Mark the event as handled to prevent further processing
                return; // Do nothing if start button was not clicked
            }

            //-- Trial started:

            if (!WasObjectPressed(objId)) // Technique doesn't matter here
            {
                this.TrialInfo($"Object wasn't pressed");
                e.Handled = true; // Mark the event as handled to prevent further processing
                return; // Do nothing if object wasn't pressed
            }

            //-- Object is pressed:

            var allObjectsApplied = _activeTrialRecord.AreAllObjectsApplied(); // Probably pressed on the area, then here to release

            if (allObjectsApplied)
            {
                e.Handled = true;
                EndActiveTrial(Result.MISS);
                return;
            }

            //-- Not all objects applied:

            if (_activeTrial.IsTechniqueToMo())
            {
                int funcIdUnderMarker = _mainWindow.FunctionIdUnderMarker(_activeTrial.FuncSide, _activeTrialRecord.GetFunctionIds());
                var markerOverEnabledFunc = funcIdUnderMarker != -1;

                if (markerOverEnabledFunc)
                {
                    _activeTrialRecord.ApplyFunction(funcIdUnderMarker, objId);
                    UpdateScene();
                }
                else
                {
                    EndActiveTrial(Result.MISS);
                }
            }
            else // MOUSE
            {
                _activeTrialRecord.MarkObject(objId);
                _activeTrialRecord.MarkFunction(_activeTrialRecord.FindMappedFunctionId(objId));
                UpdateScene();
            }

        }
        //public override void OnObjectAreaMouseEnter(object sender, MouseEventArgs e)
        //{
        //    // Only log if entered from outside (NOT from the object)
        //    if (_activeTrialRecord.GetLastTrialEventType() != ExpStrs.OBJ_EXIT) LogEvent(ExpStrs.ARA_ENTER);
        //}

        //public override void OnObjectAreaMouseDown(Object sender, MouseButtonEventArgs e)
        //{
        //    this.TrialInfo($"Timestamps: {_activeTrialRecord.TrialEventsToString()}");
            

        //}

        //public override void OnObjectAreaMouseUp(object sender, MouseButtonEventArgs e)
        //{
        //    LogEvent(ExpStrs.ARA_RELEASE);

        //    if (!IsStartClicked())
        //    {
        //        this.TrialInfo($"Start wasn't clicked");
        //        Sounder.PlayStartMiss();
        //        e.Handled = true; // Mark the event as handled to prevent further processing
        //        return; // Do nothing if start button was not clicked
        //    }
        //}

        //public override void OnObjectAreaMouseExit(object sender, MouseEventArgs e)
        //{
        //    LogEvent(ExpStrs.ARA_EXIT);
        //}

        
    }
}
