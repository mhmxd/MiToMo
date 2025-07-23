using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static Multi.Cursor.Experiment;
using static Tensorflow.TensorShapeProto.Types;

namespace Multi.Cursor
{

    public abstract class BlockHandler : IGestureHandler
    {
        // Classes
        public class TObject
        {
            public int Id { get; set; }
            public Point Position { get; set; }

            public TObject(int id, Point position)
            {
                Id = id;
                Position = position;
            }
        }

        public class TFunction
        {
            public int Id { get; set; }
            public int WidthInUnits { get; set; }
            public Point Center { get; set; }
            public Point Position { get; set; } // Top-left corner of the button

            public TFunction(int id, int widthInUnits, Point center, Point position)
            {
                Id = id;
                Center = center;
                Position = position;
                WidthInUnits = widthInUnits;
            }

        }

        //protected class CachedTrialPositions
        //{
        //    public int TargetId { get; set; }
        //    public List<Point> StartPositions { get; set; } = new List<Point>();
        //}

        // Attributes
        protected Dictionary<int, TrialRecord> _trialRecords = new Dictionary<int, TrialRecord>();
        protected MainWindow _mainWindow;
        protected Block _activeBlock;
        protected Trial _activeTrial;
        protected int _activeTrialNum = 0;
        protected TrialRecord _activeTrialRecord;
        protected int _nSelectedObjects = 0; // Number of clicked objects in the current trial

        protected Random _random = new Random();

        public abstract bool FindPositionsForActiveBlock();
        public abstract bool FindPositionsForTrial(Trial trial);
        public void BeginActiveBlock()
        {
            this.TrialInfo("------------------- Beginning block ----------------------------");

            _activeTrialNum = 1;
            _activeTrial = _activeBlock.GetTrial(_activeTrialNum);
            _activeTrialRecord = _trialRecords[_activeTrial.Id];

            // Update the main window label
            this.TrialInfo($"nTrials = {_activeBlock.GetNumTrials()}");
            _mainWindow.UpdateInfoLabel(_activeTrialNum, _activeBlock.GetNumTrials());

            // Show the Start Trial button
            //_mainWindow.ShowStartTrialButton(OnStartButtonMouseUp);
            ShowActiveTrial();
        }
        public abstract void ShowActiveTrial();
        public abstract void EndActiveTrial(Experiment.Result result);
        public abstract void GoToNextTrial();

        public abstract void OnMainWindowMouseDown(Object sender, MouseButtonEventArgs e);
        public abstract void OnMainWindowMouseMove(Object sender, MouseEventArgs e);
        public abstract void OnMainWindowMouseUp(Object sender, MouseButtonEventArgs e);
        public void OnAuxWindowMouseDown(Object sender, MouseButtonEventArgs e)
        {
            if (GetEventCount(Str.OBJ_RELEASE) == 0) // Not yet clicked on the Object => ERROR
            {
                EndActiveTrial(Experiment.Result.ERROR);
            }
            else // Clicked on the Start => MISS
            {
                EndActiveTrial(Experiment.Result.MISS);
            }

            e.Handled = true; // Mark the event as handled to prevent further processing
        }
        public void OnAuxWindowMouseMove(Object sender, MouseEventArgs e)
        {
            // Nothing for now
        }
        public void OnAuxWindowMouseUp(Object sender, MouseButtonEventArgs e)
        {
            // Nothing for now
        }
        public void OnObjectMouseEnter(Object sender, MouseEventArgs e)
        {
            LogEvent(Str.START_ENTER);
        }
        public void OnObjectMouseLeave(Object sender, MouseEventArgs e)
        {
            LogEvent(Str.START_LEAVE);
        }
        public abstract void OnObjectMouseDown(Object sender, MouseButtonEventArgs e);
        public abstract void OnObjectMouseUp(Object sender, MouseButtonEventArgs e);

        public void OnObjectAreaMouseDown(Object sender, MouseButtonEventArgs e)
        {
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

            var allObjectsSelected = _nSelectedObjects == _activeTrialRecord.Objects.Count;

            switch (allObjectsSelected)
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

        public void OnObjectAreaMouseUp(Object sender, MouseButtonEventArgs e)
        {
            // Nothing for now
        }

        public void OnTargetMouseEnter(Object sender, MouseEventArgs e)
        {
            LogEvent(Str.TARGET_ENTER);
        }
        public void OnTargetMouseLeave(Object sender, MouseEventArgs e)
        {
            LogEvent(Str.TARGET_LEAVE);
        }
        public abstract void OnFunctionMouseDown(Object sender, MouseButtonEventArgs e);
        public abstract void OnFunctionMouseUp(Object sender, MouseButtonEventArgs e);
        public void OnNonTargetMouseDown(Object sender, MouseButtonEventArgs e)
        {
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");
            SButton clickedButton = sender as SButton;
            if (clickedButton != null && clickedButton.Tag is Dictionary<string, int>)
            {
                // Show neighbor IDs
                Dictionary<string, int> tag = clickedButton.Tag as Dictionary<string, int>;
                this.TrialInfo($"Clicked button ID: {clickedButton.Id}, Left: {tag["LeftId"]}, Right: {tag["RightId"]}, Top: {tag["TopId"]}, Bottom: {tag["BottomId"]}");
            }

            // It's always a miss
            EndActiveTrial(Experiment.Result.MISS);

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public void OnStartButtonMouseDown(Object sender, MouseButtonEventArgs e)
        {
            LogEvent(Str.START_PRESS);
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public void OnStartButtonMouseUp(Object sender, MouseButtonEventArgs e)
        {
            LogEvent(Str.START_RELEASE);
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

            var startButtonPressed = GetEventCount(Str.START_PRESS) > 0;

            switch (startButtonPressed)
            {
                case true: // Start button was pressed => valid trial started
                    _mainWindow.ColorStartButton(Brushes.DarkGray);
                    break;
                case false: // Start button was not pressed => invalid trial
                    EndActiveTrial(Experiment.Result.MISS);
                    break;
            }

            e.Handled = true; // Mark the event as handled to prevent further processing

            //_activeTrialNum++;
            //_activeTrial = _activeBlock.GetTrial(_activeTrialNum);
            //_activeTrialRecord = _trialRecords[_activeTrial.Id];

            //ShowActiveTrial();

        }

        protected void SetFunctionAsEnabled(int funcId)
        {
            _mainWindow.FillButtonInAuxWindow(
                _activeTrial.FuncSide,
                funcId, 
                Config.FUNCTION_MARKED_COLOR);
        }

        protected void SetFunctionAsDisabled(int funcId)
        {
            _mainWindow.FillButtonInAuxWindow(
                _activeTrial.FuncSide, 
                funcId, 
                Config.FUNCTION_DEFAULT_COLOR);
        }

        protected void SetFunctionAsSelected(int funcId)
        {
            _mainWindow.FillButtonInAuxWindow(
                _activeTrial.FuncSide, 
                funcId, 
                Config.FUNCTION_SELECTED_COLOR);
        }

        protected void SetObjectAsMarked(int objId)
        {
            _mainWindow.FillObject(objId, Config.OBJ_MARKED_COLOR);
        }

        protected void SetObjectAsSelected(int objId)
        {
            _mainWindow.FillObject(objId, Config.OBJ_SELECTED_COLOR);
        }

        protected void SetObjectAsDisabled(int objId)
        {
            _mainWindow.FillObject(objId, Config.OBJ_DEFAULT_COLOR);
        }

        public void LeftPress()
        {
            
        }

        public void RightPress()
        {
            
        }

        public void TopPress()
        {
            
        }

        public void LeftMove(double dX, double dY)
        {
            
        }

        public void IndexDown(TouchPoint indPoint)
        {
            
        }

        public abstract void IndexTap();

        public void IndexMove(double dX, double dY)
        {

        }

        public void IndexMove(TouchPoint indPoint)
        {
            _mainWindow?.MoveMarker(indPoint);
        }

        public void IndexUp()
        {
            _mainWindow.StopAuxNavigator();
        }

        public abstract void ThumbSwipe(Direction dir);

        public abstract void ThumbTap();

        public void ThumbMove(TouchPoint thumbPoint)
        {
            // Nothing for now
        }

        public void ThumbUp()
        {
            // Nothing for now
        }

        public abstract void MiddleTap();

        public void RingTap()
        {
            
        }

        public void PinkyTap(Side loc)
        {
            
        }

        protected void LogEvent(string eventName)
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
            _activeTrialRecord.AddTimestamp(timeKey);
        }

        protected int GetEventCount(string eventName)
        {
            if (_trialRecords[_activeTrial.Id].EventCounts.ContainsKey(eventName))
            {
                return _trialRecords[_activeTrial.Id].EventCounts[eventName];
            }

            return 0; // Event has not occurred
        }

        protected double GetDuration(string begin, string end)
        {
            if (_activeTrialRecord.HasTimestamp(begin) && _activeTrialRecord.HasTimestamp(end))
            {
                return (_activeTrialRecord.GetTime(end) - _activeTrialRecord.GetTime(begin)) / 1000.0; // Convert to seconds
            }

            return 0;
        }

        public abstract void ThumbTap(Side side);
    }

}
