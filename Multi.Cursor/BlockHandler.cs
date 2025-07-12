using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Multi.Cursor.Experiment;
using static Tensorflow.TensorShapeProto.Types;

namespace Multi.Cursor
{

    public abstract class BlockHandler : IGestureHandler
    {
        // Classes
        protected class TrialRecord
        {
            public int TargetId;
            public List<Point> StartPositions;
            public List<TObject> Objects;
            public Dictionary<string, int> EventCounts;
            private List<Timestamp> Timestamps;

            public TrialRecord()
            {
                StartPositions = new List<Point>();
                Objects = new List<TObject>();
                EventCounts = new Dictionary<string, int>();
                Timestamps = new List<Timestamp>();
            }

            public void AddTimestamp(string label)
            {
                Timestamps.Add(new Timestamp(label));
            }

            public string TimestampsToString()
            {
                return string.Join(", ", Timestamps.Select(ts => $"{ts.label}: {ts.time}"));
            }

            public string GetLastTimestamp()
            {
                return Timestamps.Count > 0 ? Timestamps.Last().label : "No timestamps recorded";
            }

            public long GetTime(string label)
            {
                var timestamp = Timestamps.FirstOrDefault(ts => ts.label == label);
                if (timestamp != null)
                {
                    return timestamp.time;
                }
                return -1; // Return -1 if the label is not found
            }

            public bool HasTimestamp(string label)
            {
                return Timestamps.Any(ts => ts.label == label);
            }

            public void ClearTimestamps()
            {
                Timestamps.Clear();
            }
        }

        public class TObject
        {
            public int Id { get; set; }
            public Point Position { get; set; }
        }

        protected class Timestamp
        {
            public string label;
            public long time;

            public Timestamp(string label)
            {
                this.label = label;
                this.time = Timer.GetCurrentMillis();
            }
        }

        protected class CachedTrialPositions
        {
            public int TargetId { get; set; }
            public List<Point> StartPositions { get; set; } = new List<Point>();
        }

        // Attributes
        protected Dictionary<int, TrialRecord> _trialRecords = new Dictionary<int, TrialRecord>();
        protected MainWindow _mainWindow;
        protected Block _activeBlock;
        protected Trial _activeTrial;
        protected int _activeTrialNum = 0;
        protected TrialRecord _activeTrialRecord;

        public abstract bool FindPositionsForActiveBlock();
        public abstract bool FindPositionsForTrial(Trial trial);
        public abstract void BeginActiveBlock();
        public abstract void ShowActiveTrial();
        public abstract void EndActiveTrial(Experiment.Result result);
        public abstract void GoToNextTrial();

        public abstract void OnMainWindowMouseDown(Object sender, MouseButtonEventArgs e);
        public abstract void OnMainWindowMouseMove(Object sender, MouseEventArgs e);
        public abstract void OnMainWindowMouseUp(Object sender, MouseButtonEventArgs e);
        public void OnAuxWindowMouseDown(Object sender, MouseButtonEventArgs e)
        {
            if (GetEventCount(Str.START_RELEASE) == 0) // Not yet clicked on the Start => NO_START
            {
                EndActiveTrial(Experiment.Result.NO_START);
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
        public void OnStartMouseEnter(Object sender, MouseEventArgs e)
        {
            LogEvent(Str.START_ENTER);
        }
        public void OnStartMouseLeave(Object sender, MouseEventArgs e)
        {
            LogEvent(Str.START_LEAVE);
        }
        public abstract void OnStartMouseDown(Object sender, MouseButtonEventArgs e);
        public abstract void OnStartMouseUp(Object sender, MouseButtonEventArgs e);
        public abstract void OnOjectMouseDown(Object sender, MouseButtonEventArgs e);
        public abstract void OnOjectMouseUp(Object sender, MouseButtonEventArgs e);
        public void OnTargetMouseEnter(Object sender, MouseEventArgs e)
        {
            LogEvent(Str.TARGET_ENTER);
        }
        public void OnTargetMouseLeave(Object sender, MouseEventArgs e)
        {
            LogEvent(Str.TARGET_LEAVE);
        }
        public abstract void OnTargetMouseDown(Object sender, MouseButtonEventArgs e);
        public abstract void OnTargetMouseUp(Object sender, MouseButtonEventArgs e);
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

        public void IndexTap()
        {
            this.TrialInfo($"IndexTap: {_mainWindow.GetActiveTechnique()}");
            if (_mainWindow.GetActiveTechnique() == Technique.Auxursor_Tap)
            {
                _mainWindow.ActivateAuxGridNavigator(Side.Top);
                //if (GetEventCount(Str.START_RELEASE) > 0)
                //{
                //    _mainWindow.ActivateAuxGridNavigator(Side.Top);
                //}
            }
            
        } 

        public void IndexMove(double dX, double dY)
        {
            // Nothing for now
        }

        public void IndexMove(TouchPoint indPoint)
        {
            _mainWindow?.MoveAuxNavigator(indPoint);
        }

        public void IndexUp()
        {
            _mainWindow.StopAuxNavigator();
        }

        public void ThumbSwipe(Direction dir)
        {
            if (_mainWindow.GetActiveTechnique() == Technique.Auxursor_Swipe)
            {
                if (GetEventCount(Str.START_RELEASE) > 0)
                {
                    switch (dir)
                    {
                        case Direction.Left:
                            _mainWindow.ActivateAuxGridNavigator(Side.Left);
                            break;
                        case Direction.Right:
                            _mainWindow.ActivateAuxGridNavigator(Side.Right);
                            break;
                        case Direction.Up:
                            _mainWindow.ActivateAuxGridNavigator(Side.Top);
                            break;
                    }
                }
                
            }
            
        }

        public void ThumbTap(Side loc)
        {
            if (_mainWindow.GetActiveTechnique() == Technique.Auxursor_Tap)
            {
                _mainWindow.ActivateAuxGridNavigator(Side.Left);
                //if (GetEventCount(Str.START_RELEASE) > 0)
                //{
                //    _mainWindow.ActivateAuxGridNavigator(Side.Left);
                //}
            }
        }

        public void ThumbMove(TouchPoint thumbPoint)
        {
            // Nothing for now
        }

        public void ThumbUp()
        {
            // Nothing for now
        }

        public void MiddleTap()
        {
            if (_mainWindow.GetActiveTechnique() == Technique.Auxursor_Tap)
            {
                _mainWindow.ActivateAuxGridNavigator(Side.Right);
                //if (GetEventCount(Str.START_RELEASE) > 0)
                //{
                //    _mainWindow.ActivateAuxGridNavigator(Side.Right);
                //}
            }
        }

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
    }

}
