using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static Multi.Cursor.Experiment;
using static Multi.Cursor.TrialRecord;
using static Tensorflow.TensorShapeProto.Types;

namespace Multi.Cursor
{

    public abstract class BlockHandler : IGestureHandler
    {
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
            this.TrialInfo($"Active block id: {_activeBlock.Id}");
            // Start the log
            ExperiLogger.StartBlockLog(_activeBlock.Id, _activeBlock.GetBlockType(), _activeBlock.GetComplexity());

            // Update the main window label
            //this.TrialInfo($"nTrials = {_activeBlock.GetNumTrials()}");
            //_mainWindow.UpdateInfoLabel(_activeTrialNum, _activeBlock.GetNumTrials());

            // Show the Start Trial button
            //_mainWindow.ShowStartTrialButton(OnStartButtonMouseUp);
            
            // Clear the main window canvas (to add shapes)
            _mainWindow.ClearCanvas();
            _mainWindow.ResetAllAuxWindows();

            // Show layout
            //_mainWindow.SetupLayout(_activeBlock.GetComplexity());

            // Show the first trial
            ShowActiveTrial();
        }
        public abstract void ShowActiveTrial();
        public abstract void EndActiveTrial(Result result);
        public abstract void GoToNextTrial();

        public abstract void OnMainWindowMouseDown(Object sender, MouseButtonEventArgs e);
        public abstract void OnMainWindowMouseMove(Object sender, MouseEventArgs e);
        public abstract void OnMainWindowMouseUp(Object sender, MouseButtonEventArgs e);
        public void OnAuxWindowMouseDown(Object sender, MouseButtonEventArgs e)
        {
            if (GetEventCount(Str.OBJ_RELEASE) == 0) // Not yet clicked on the Object => ERROR
            {
                EndActiveTrial(Result.ERROR);
            }
            else // Clicked on the Start => MISS
            {
                EndActiveTrial(Result.MISS);
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
            LogEvent(Str.OBJ_ENTER);
        }
        public void OnObjectMouseLeave(Object sender, MouseEventArgs e)
        {
            LogEvent(Str.OBJ_EXIT);
        }
        public abstract void OnObjectMouseDown(Object sender, MouseButtonEventArgs e);
        public abstract void OnObjectMouseUp(Object sender, MouseButtonEventArgs e);

        public abstract void OnObjectAreaMouseDown(Object sender, MouseButtonEventArgs e);

        public void OnObjectAreaMouseUp(Object sender, MouseButtonEventArgs e)
        {
            LogEvent(Str.OBJ_AREA_RELEASE);
        }

        public abstract void OnFunctionMouseDown(Object sender, MouseButtonEventArgs e);
        public abstract void OnFunctionMouseUp(Object sender, MouseButtonEventArgs e);
        public abstract void OnNonTargetMouseDown(Object sender, MouseButtonEventArgs e);

        public void OnStartButtonMouseDown(Object sender, MouseButtonEventArgs e)
        {
            LogEvent(Str.START_PRESS);
            this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

            e.Handled = true; // Mark the event as handled to prevent further processing
        }

        public void OnStartButtonMouseUp(Object sender, MouseButtonEventArgs e)
        {
            LogEvent(Str.START_RELEASE);
            //this.TrialInfo($"Timestamps: {_activeTrialRecord.TimestampsToString()}");

            var startButtonPressed = GetEventCount(Str.START_PRESS) > 0;

            switch (startButtonPressed)
            {
                case true: // Start button was pressed => valid trial started
                    _mainWindow.RemoveStartTrialButton();
                    break;
                case false: // Start button was not pressed => invalid trial
                    EndActiveTrial(Result.MISS);
                    break;
            }

            e.Handled = true; // Mark the event as handled to prevent further processing

            //_activeTrialNum++;
            //_activeTrial = _activeBlock.GetTrial(_activeTrialNum);
            //_activeTrialRecord = _trialRecords[_activeTrial.Id];

            //ShowActiveTrial();

        }

        public void SetFunctionAsEnabled(int funcId)
        {
            _mainWindow.FillButtonInAuxWindow(
                _activeTrial.FuncSide,
                funcId, 
                Config.FUNCTION_ENABLED_COLOR);
        }

        public void SetFunctionAsDisabled(int funcId)
        {
            _mainWindow.FillButtonInAuxWindow(
                _activeTrial.FuncSide, 
                funcId, 
                Config.FUNCTION_DEFAULT_COLOR);
        }

        public void SetFunctionAsApplied(int funcId)
        {
            _activeTrialRecord.SetFunctionAsApplied(funcId);
        }

        protected void SetObjectAsDisabled(int objId)
        {
            _mainWindow.FillObject(objId, Config.OBJ_DEFAULT_COLOR);
        }

        public void UpdateScene()
        {
            foreach (var func in _activeTrialRecord.Functions)
            {
                Brush funcColor = Config.FUNCTION_DEFAULT_COLOR;
                switch (func.State)
                {
                    case ButtonState.MARKED:
                        funcColor = Config.FUNCTION_ENABLED_COLOR;
                        break;
                    case ButtonState.APPLIED:
                        funcColor = Config.FUNCTION_APPLIED_COLOR;
                        break;
                }

                _mainWindow.FillButtonInAuxWindow(_activeTrial.FuncSide, func.Id, funcColor);
            }

            foreach (var obj in _activeTrialRecord.Objects)
            {
                Brush objColor = Config.OBJ_DEFAULT_COLOR;
                switch (obj.State)
                {
                    case ButtonState.MARKED:
                        objColor = Config.OBJ_MARKED_COLOR;
                        break;
                    case ButtonState.APPLIED:
                        objColor = Config.OBJ_APPLIED_COLOR;
                        break;
                }

                _mainWindow.FillObject(obj.Id, objColor);
            }
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
            LogEvent("index_up");
        }

        public abstract void ThumbSwipe(Direction dir);

        public abstract void ThumbTap(long downInstant, long upInstant);

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

            //string timeKey = eventName + "_" + _trialRecords[_activeTrial.Id].EventCounts[eventName];
            _activeTrialRecord.AddTimestamp(eventName); // Let them have the same name. We know the count from EventCounts

        }

        protected void LogEvent(string eventName, long eventTime)
        {
            if (_trialRecords[_activeTrial.Id].EventCounts.ContainsKey(eventName))
            {
                _trialRecords[_activeTrial.Id].EventCounts[eventName]++;
            }
            else
            {
                _trialRecords[_activeTrial.Id].EventCounts[eventName] = 1;
            }

            //string timeKey = eventName + "_" + _trialRecords[_activeTrial.Id].EventCounts[eventName];
            _activeTrialRecord.AddTimestamp(eventName);

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
                return (_activeTrialRecord.GetFirstTimestamp(end) - _activeTrialRecord.GetFirstTimestamp(begin)) / 1000.0; // Convert to seconds
            }

            return 0;
        }


        public abstract void ThumbTap(Side side);

        public int GetMappedObjId(int funcId)
        {
            return _activeTrialRecord.FindMappedObjectId(funcId);
        }

        public void MarkAllObjects()
        {
            _activeTrialRecord.MarkAllObjects();
        }

        public void MarkMappedObject(int funcId)
        {
            switch (_activeBlock.GetFunctionType())
            {
                case TaskType.ONE_FUNCTION: // One function => mark all objects
                    MarkAllObjects();
                    break;
                case TaskType.MULTI_FUNCTION: // Multi function => mark the mapped object 
                    _activeTrialRecord.MarkMappedObject(funcId);
                    break;
            }
        }

        public int GetActiveTrialNum()
        {
            return _activeTrialNum;
        }

        public int GetNumTrialsInBlock()
        {
            return _activeBlock.GetNumTrials();
        }

        public void LogAverageTimeOnDistances()
        {
            // Find the average trial time from TrialRecord.Times
            List<double> shortDistTimes = new List<double>();
            List<double> midDistTimes = new List<double>();
            List<double> longDistTimes = new List<double>();

            foreach (int trialId in _trialRecords.Keys)
            {
                Trial trial = _activeBlock.GetTrialById(trialId);
                if (trial != null && _trialRecords[trialId].HasTime(Str.TRIAL_TIME))
                {
                    if (trial.DistRangeMM.Label == Str.SHORT_DIST)
                        shortDistTimes.Add(_trialRecords[trialId].GetTime(Str.TRIAL_TIME));
                    if (trial.DistRangeMM.Label == Str.MID_DIST)
                        midDistTimes.Add(_trialRecords[trialId].GetTime(Str.TRIAL_TIME));
                    if (trial.DistRangeMM.Label == Str.LONG_DIST)
                        longDistTimes.Add(_trialRecords[trialId].GetTime(Str.TRIAL_TIME));
                }
            }

            // Log the averages using ExperiLogger
            double shortDistAvg = shortDistTimes.Avg();
            double midDistAvg = midDistTimes.Avg();
            double lonDistAvg = longDistTimes.Avg();
            //ExperiLogger.LogTrialMessage($"Average Time per Distance --- " +
            //    $"Short({shortDistTimes.Count}): {shortDistAvg:F2}; " +
            //    $"Mid({midDistTimes.Count}): {midDistAvg:F2}; " +
            //    $"Long({longDistTimes.Count}): {lonDistAvg:F2}");
        }

        public void RecordToMoAction(Finger finger, string action)
        {
            LogEvent(finger.ToString().ToLower() + "_" + action);
        }
    }

}
