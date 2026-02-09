using Common.Constants;
using Common.Helpers;
using Common.Settings;
using CommonUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static Common.Constants.ExpEnums;

namespace SubTask.ObjectSelection
{
    internal class BlockHandler
    {

        // Attributes
        protected List<TrialRecord> _trialRecords = new List<TrialRecord>(); // Trial id -> Record
        protected MainWindow _mainWindow;
        protected Block _activeBlock;
        protected int _activeBlockNum;
        protected Trial _activeTrial;
        protected int _activeTrialNum = 0;
        protected TrialRecord _activeTrialRecord;
        protected int _nSelectedObjects = 0; // Number of clicked objects in the current trial

        protected List<int> _functionsVisitMap = new List<int>();
        protected List<int> _objectsVisitMap = new List<int>();

        protected Random _random = new Random();

        public BlockHandler(
            MainWindow mainWindow,
            Block activeBlock,
            int activeBlockNum)
        {
            _mainWindow = mainWindow;
            _activeBlock = activeBlock;
            _activeBlockNum = activeBlockNum;
        }

        public void BeginActiveBlock()
        {
            this.TrialInfo("------------------- Beginning block ----------------------------");
            this.TrialInfo(ExpStrs.MINOR_LINE);

            _activeTrialNum = 1;
            _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
            _trialRecords.Add(new TrialRecord(_activeTrial.Id));
            _activeTrialRecord = _trialRecords.Last();
            this.TrialInfo($"Active block id: {_activeBlock.Id}");

            // Clear the main window canvas (to add shapes)
            _mainWindow.ClearCanvas();

            // Show the first trial
            ShowActiveTrial();
        }

        public void ShowActiveTrial()
        {
            this.TrialInfo(ExpStrs.MINOR_LINE);
            this.TrialInfo($"Showing " + _activeTrial.ToStr());

            LogEvent(ExpStrs.TRIAL_SHOW, _activeTrial.Id);

            // Start logging cursor positions
            ExperiLogger.StartTrialCursorLog(_activeTrial.Id, _activeTrialNum);

            // Set object area position
            _activeTrialRecord.ObjectAreaRect.Location = _mainWindow.FindRandomPositionForObjectArea(_activeTrialRecord.ObjectAreaRect.Size);

            // Place objects in the area
            Point objAreaCenter = new(
                _activeTrialRecord.ObjectAreaRect.X + _activeTrialRecord.ObjectAreaRect.Width / 2,
                _activeTrialRecord.ObjectAreaRect.Y + _activeTrialRecord.ObjectAreaRect.Height / 2);
            this.TrialInfo($"Center at: {_activeTrialRecord.ObjectAreaRect.Y}");
            _activeTrialRecord.Objects = PlaceObjectsInArea(
                objAreaCenter,
                _activeTrial.NObjects);

            // Show the area
            MouseEvents objAreaEvents = new MouseEvents(OnObjectAreaMouseDown, OnObjectAreaMouseUp, OnObjectAreaMouseEnter, OnObjectAreaMouseExit);
            _mainWindow.ShowObjectsArea(
                _activeTrialRecord.ObjectAreaRect, UIColors.COLOR_OBJ_AREA_BG,
                objAreaEvents);

            // Show the objects
            MouseEvents objectEvents = new MouseEvents(
                OnObjectMouseEnter, OnObjectMouseDown, OnObjectMouseUp, OnObjectMouseLeave);
            _mainWindow.ShowObjects(_activeTrialRecord.Objects, UIColors.COLOR_OBJ_DEFAULT, objectEvents);

            // Show Start Trial button
            MouseEvents startButtonEvents = new MouseEvents(OnStartButtonMouseDown, OnStartButtonMouseUp, OnStartButtonMouseEnter, OnStartButtonMouseExit);
            _mainWindow.ShowStartTrialButton(
                _activeTrialRecord.ObjectAreaRect,
                UITools.MM2PX(ExpLayouts.START_BUTTON_SMALL_DIM_MM.W),
                UITools.MM2PX(ExpLayouts.START_BUTTON_SMALL_DIM_MM.H),
                UIColors.COLOR_START_INIT,
                startButtonEvents);

            // Update info label
            _mainWindow.UpdateInfoLabel();
        }

        public virtual void EndActiveTrial(Result result)
        {
            this.TrialInfo($"Trial#{_activeTrial.Id} completed: {result}");
            this.TrialInfo(ExpStrs.MAJOR_LINE);
            _activeTrialRecord.Result = result;
            LogEvent(ExpStrs.TRIAL_END, _activeTrial.Id); // Log the trial end timestamp

            switch (result)
            {
                case Result.HIT:
                    MSounder.PlayHit();
                    double trialTime = GetDuration(ExpStrs.STR_RELEASE + "_1", ExpStrs.TRIAL_END);
                    _activeTrialRecord.AddTime(ExpStrs.TRIAL_TIME, trialTime);

                    break;
                case Result.MISS:
                    MSounder.PlayTargetMiss();

                    _activeBlock.ShuffleBackTrial(_activeTrialNum);
                    //_trialRecords[_activeTrial.Id].ClearTimestamps();
                    //_trialRecords[_activeTrial.Id].ResetStates();
                    break;
            }

            //-- Log
            ExperiLogger.LogDetailTrial(_activeBlockNum, _activeTrialNum, _activeTrial, _activeTrialRecord);
            ExperiLogger.LogTotalTrialTime(_activeBlockNum, _activeTrialNum, _activeTrial, _activeTrialRecord);
            ExperiLogger.LogCursorPositions();
            ExperiLogger.LogTrialEvents(_activeTrialRecord.GetTrialEvents());

            GoToNextTrial();
        }
        public void GoToNextTrial()
        {
            _mainWindow.ClearCanvas(); // Clear the main canvas

            _nSelectedObjects = 0; // Reset the number of applied objects

            if (_activeTrialNum < _activeBlock.Trials.Count)
            {
                _functionsVisitMap.Clear();
                _objectsVisitMap.Clear();

                _activeTrialNum++;
                _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
                _trialRecords.Add(new TrialRecord(_activeTrial.Id));
                _activeTrialRecord = _trialRecords.Last();

                ShowActiveTrial();
            }
            else
            {
                // Log block time
                ExperiLogger.LogBlockTime(_activeBlock);

                _mainWindow.GoToNextBlock();

            }
        }

        private List<TObject> PlaceObjectsInArea(Point objAreaCenterPosition, int nObjects)
        {
            List<TObject> placedObjects = new List<TObject>();
            double objW = UITools.MM2PX(ExpLayouts.OBJ_WIDTH_MM);
            double areaW = UITools.MM2PX(ExpLayouts.OBJ_AREA_WIDTH_MM);

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
                    this.PositionInfo($"Object {i + 1}, Attempt {attempt + 1}: Potential center at {potentialCenter}");
                    // Calculate the top-left corner from the potential center
                    Point topLeft = new Point(potentialCenter.X - objW / 2, potentialCenter.Y - objW / 2);

                    // 2. Check for overlaps with already placed objects
                    if (!HasOverlap(topLeft, objW, placedObjects))
                    {
                        TObject trialObject = new TObject(i + 1, topLeft, potentialCenter);

                        placedObjects.Add(trialObject);
                        placed = true;
                        break; // Move to the next object
                    }
                }

                if (!placed)
                {
                    this.PositionInfo($"Warning: Could not place object {i + 1} after {maxAttemptsPerObject} attempts.");
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
            this.PositionInfo($"Valid range X: [{minX}, {maxX}], Y: [{minY}, {maxY}]");
            // Generate a random point inside that range
            double x = minX + _random.NextDouble() * (maxX - minX);
            double y = minY + _random.NextDouble() * (maxY - minY);

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


        public virtual void OnMainWindowMouseDown(Object sender, MouseButtonEventArgs e)
        {
            LogEvent(ExpStrs.MAIN_WIN_PRESS);

            if (!IsStartClicked()) // Start button not clicked yet
            {
                MSounder.PlayStartMiss();
            }
            else
            {
                EndActiveTrial(Result.MISS);
            }

            //e.Handled = true; // Mark the event as handled to prevent further processing
        }
        public void OnMainWindowMouseMove(Object sender, MouseEventArgs e)
        {
            LogEventOnce(ExpStrs.FIRST_MOVE);

            // Log cursor movement
            ExperiLogger.RecordCursorPosition(e.GetPosition(_mainWindow.Owner));
        }

        public virtual void OnMainWindowMouseUp(Object sender, MouseButtonEventArgs e)
        {
            if (IsStartPressed() && !IsStartClicked()) // Start button not clicked yet
            {
                MSounder.PlayStartMiss();
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public void OnAuxWindowMouseEnter(Side side, Object sender, MouseEventArgs e)
        {
            LogEvent(ExpStrs.PNL_ENTER, side.ToString().ToLower());
        }

        public void OnAuxWindowMouseDown(Side side, Object sender, MouseButtonEventArgs e)
        {
            LogEvent(ExpStrs.PNL_PRESS, side.ToString().ToLower());

            if (!IsStartClicked())
            {
                MSounder.PlayStartMiss();
            }
            else
            {
                EndActiveTrial(Result.MISS);
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public void OnAuxWindowMouseMove(Object sender, MouseEventArgs e)
        {
            // Nothing for now
        }
        public void OnAuxWindowMouseUp(Side side, Object sender, MouseButtonEventArgs e)
        {
            LogEvent(ExpStrs.PNL_RELEASE, side.ToString().ToLower());

            if (IsStartPressed()) // Pressed in Start, released in aux window
            {
                EndActiveTrial(Result.MISS);
            }

            e.Handled = true;
        }

        public void OnAuxWindowMouseExit(Side side, Object sender, MouseEventArgs e)
        {
            LogEvent(ExpStrs.PNL_EXIT, side.ToString().ToLower());
        }

        public virtual void OnObjectMouseEnter(Object sender, MouseEventArgs e)
        {
            // If the last timestamp was ARA_EXIT, remove that
            if (_activeTrialRecord.GetLastTrialEventType() == ExpStrs.ARA_EXIT) _activeTrialRecord.RemoveLastTimestamp();
            var objId = (int)((FrameworkElement)sender).Tag;

            // Add the id to the list of visited if not already there (will use the index for the order of visit)
            LogEvent(ExpStrs.OBJ_ENTER, objId);

            // Log the event
            LogEvent(ExpStrs.OBJ_ENTER, objId);
        }

        public virtual void OnObjectMouseLeave(Object sender, MouseEventArgs e)
        {
            var objId = (int)((FrameworkElement)sender).Tag;
            LogEvent(ExpStrs.OBJ_EXIT, objId);
        }

        public void OnObjectMouseDown(Object sender, MouseButtonEventArgs e)
        {
            var objId = (int)((FrameworkElement)sender).Tag;
            LogEvent(ExpStrs.OBJ_PRESS, objId);

            // Pressed on the Object without starting the trial
            if (!IsStartClicked())
            {
                MSounder.PlayStartMiss();
                e.Handled = true; // Mark the event as handled to prevent further processing
                return; // Do nothing if start button was not clicked
            }

            //-- Trial started:
            var allObjectsApplied = _activeTrialRecord.AreAllObjectsApplied();
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

            }
            //else
            //{
            //    _pressedObjectId = objId;
            //}


            e.Handled = true;
        }

        public void OnObjectMouseUp(Object sender, MouseButtonEventArgs e)
        {
            var objId = (int)((FrameworkElement)sender).Tag;
            LogEvent(ExpStrs.OBJ_RELEASE, objId);

            if (!IsStartClicked())
            {
                this.TrialInfo($"Start wasn't clicked");
                MSounder.PlayStartMiss();
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

            var allObjectsApplied = _activeTrialRecord.AreAllObjectsApplied();

            if (allObjectsApplied) // Probably pressed on the area, then here to release
            {
                e.Handled = true;
                EndActiveTrial(Result.MISS);
                return;
            }

            //-- Not all objects applied:
            _activeTrialRecord.SelectObject(objId);
            UpdateScene();

            //if (_activeTrialRecord.AreAllObjectsApplied()) // NOW all objects are applied => HIT
            //{
            //    EndActiveTrial(Result.HIT);
            //}
        }

        //---- Object area
        public virtual void OnObjectAreaMouseEnter(Object sender, MouseEventArgs e)
        {
            // Only log if entered from outside (NOT from the object)
            if (_activeTrialRecord.GetLastTrialEventType() != ExpStrs.OBJ_EXIT) LogEvent(ExpStrs.ARA_ENTER);
        }

        public virtual void OnObjectAreaMouseDown(Object sender, MouseButtonEventArgs e)
        {

            if (!IsStartClicked()) // Start button not clicked yet
            {
                MSounder.PlayStartMiss();
                e.Handled = true; // Mark the event as handled to prevent further processing
                return;
            }

            if (!_activeTrialRecord.AreAllObjectsApplied())
            {
                e.Handled = true; // Mark the event as handled to prevent further processing
                EndActiveTrial(Result.MISS);
            }

            LogEvent(ExpStrs.ARA_PRESS);

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public virtual void OnObjectAreaMouseUp(Object sender, MouseButtonEventArgs e)
        {
            LogEvent(ExpStrs.ARA_RELEASE);

            if (!IsStartClicked())
            {
                this.TrialInfo($"Start wasn't clicked");
                MSounder.PlayStartMiss();
                e.Handled = true; // Mark the event as handled to prevent further processing
                return; // Do nothing if start button was not clicked
            }

            if (_activeTrialRecord.AreAllObjectsApplied())
            {
                EndActiveTrial(Result.HIT);
            }
            else
            {
                this.TrialInfo($"Not all objects applied");
                EndActiveTrial(Result.MISS);
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public virtual void OnObjectAreaMouseExit(Object sender, MouseEventArgs e)
        {
            // Will be later removed if entered the object
            LogEvent(ExpStrs.ARA_EXIT);
        }

        public void OnStartButtonMouseEnter(Object sender, MouseEventArgs e)
        {
            LogEvent(ExpStrs.STR_ENTER);
        }

        public void OnStartButtonMouseDown(Object sender, MouseButtonEventArgs e)
        {
            LogEvent(ExpStrs.STR_PRESS);
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TrialEventsToString()}");

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public void OnStartButtonMouseUp(Object sender, MouseButtonEventArgs e)
        {
            LogEvent(ExpStrs.STR_RELEASE);
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TrialEventsToString()}");

            var startButtonPressed = GetEventCount(ExpStrs.STR_PRESS) > 0;

            if (startButtonPressed)
            {
                // Remove Start (option1)
                _mainWindow.RemoveStartTrialButton();

                // Change the button to END and disable it (option2)
                //_mainWindow.ChangeStartButtonColor(UIColors.COLOR_START_UNAVAILABLE);
                //_mainWindow.ChangeStartButtonText(ExpStrs.END);

                // Make objects available
                _activeTrialRecord.MakeAllObjectsAvailable(ButtonState.ENABLED);
                UpdateScene();

                //UpdateScene(); // Temp (for measuring time)
            }
            else // Pressed outside the button => miss
            {
                EndActiveTrial(Result.MISS);
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public void OnStartButtonMouseExit(Object sender, MouseEventArgs e)
        {
            LogEvent(ExpStrs.STR_EXIT);
        }

        protected void SetObjectAsDisabled(int objId)
        {
            _mainWindow.FillObject(objId, UIColors.COLOR_OBJ_DEFAULT);
        }

        public void UpdateScene()
        {

            foreach (var obj in _activeTrialRecord.Objects)
            {
                Brush objColor = UIColors.COLOR_OBJ_DEFAULT;
                switch (obj.State)
                {
                    case ButtonState.ENABLED:
                        objColor = UIColors.COLOR_OBJ_MARKED;
                        break;
                    case ButtonState.SELECTED:
                        objColor = UIColors.COLOR_OBJ_APPLIED;
                        break;
                }

                _mainWindow.FillObject(obj.Id, objColor);
            }
        }

        protected void LogEvent(string type, string id)
        {
            //if (_trialRecords[_activeTrial.Id].EventCounts.ContainsKey(type))
            //{
            //    _trialRecords[_activeTrial.Id].EventCounts[type]++;
            //}
            //else
            //{
            //    _trialRecords[_activeTrial.Id].EventCounts[type] = 1;
            //}

            //string timeKey = type + "_" + _trialRecords[_activeTrial.Id].EventCounts[type];
            _activeTrialRecord.RecordEvent(type, id); // Let them have the same name. We know the count from EventCounts

        }

        protected void LogEvent(string type, int id)
        {
            LogEvent(type, id.ToString());
        }

        protected void LogEvent(string type)
        {
            LogEvent(type, "");
        }

        protected void LogEventOnce(string type)
        {
            if (GetEventCount(type) == 0) // Not yet logged
            {
                LogEvent(type);
            }
        }

        protected int GetEventCount(string type)
        {
            //if (_trialRecords[_activeTrial.Id].EventCounts.ContainsKey(type))
            //{
            //    return _trialRecords[_activeTrial.Id].EventCounts[type];
            //}
            //return 0; // TrialEvent has not occurred

            return _activeTrialRecord.CountEvent(type);

        }

        protected double GetDuration(string begin, string end)
        {
            if (_activeTrialRecord.HasTimestamp(begin) && _activeTrialRecord.HasTimestamp(end))
            {
                return (_activeTrialRecord.GetFirstTime(end) - _activeTrialRecord.GetFirstTime(begin)) / 1000.0; // Convert to seconds
            }

            return 0;
        }

        public void MarkAllObjects()
        {
            _activeTrialRecord.MarkAllObjects();
        }

        public int GetActiveTrialNum()
        {
            return _activeTrialNum;
        }

        public int GetNumTrialsInBlock()
        {
            return _activeBlock.GetNumTrials();
        }

        protected bool IsStartPressed()
        {
            return GetEventCount(ExpStrs.STR_PRESS) > 0;
        }

        protected bool IsStartClicked()
        {
            return GetEventCount(ExpStrs.STR_RELEASE) > 0;
        }

        protected bool WasObjectPressed(int objId)
        {
            this.TrialInfo($"Last event: {_activeTrialRecord.GetBeforeLastTrialEvent().ToString()}");
            return _activeTrialRecord.GetEventIndex(ExpStrs.OBJ_PRESS) != -1;
        }
    }

}
