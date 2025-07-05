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
            public Dictionary<string, int> EventCounts;
            public Dictionary<string, long> Timestamps;

            public TrialRecord()
            {
                StartPositions = new List<Point>();
                EventCounts = new Dictionary<string, int>();
                Timestamps = new Dictionary<string, long>();
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
        protected Trial _activeTrial;
        //protected Dictionary<string, long> _trialTimestamps = new Dictionary<string, long>(); // Trial timestamps for logging

        public abstract bool FindPositionsForActiveBlock();
        public abstract bool FindPositionsForTrial(Trial trial);
        public abstract void BeginActiveBlock();
        public abstract void ShowActiveTrial();
        public abstract void EndActiveTrial(Experiment.Result result);
        public abstract void GoToNextTrial();

        public abstract void OnMainWindowMouseDown(Object sender, MouseButtonEventArgs e);
        public abstract void OnMainWindowMouseUp(Object sender, MouseButtonEventArgs e);
        public abstract void OnStartMouseEnter(Object sender, MouseEventArgs e);
        public abstract void OnStartMouseLeave(Object sender, MouseEventArgs e);
        public abstract void OnStartMouseDown(Object sender, MouseButtonEventArgs e);
        public abstract void OnStartMouseUp(Object sender, MouseButtonEventArgs e);
        public abstract void OnTargetMouseDown(Object sender, MouseButtonEventArgs e);
        public abstract void OnTargetMouseUp(Object sender, MouseButtonEventArgs e);

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
            if (_mainWindow.GetActiveTechnique() == Technique.Auxursor_Tap)
            {
                if (IsStartReleased())
                {
                    _mainWindow.ActivateAuxGridNavigator(Side.Top);
                }
            }
            
        }

        public void IndexMove(double dX, double dY)
        {
            // Nothing for now
        }

        public void IndexMove(TouchPoint indPoint)
        {
            _mainWindow.MoveAuxNavigator(indPoint);
        }

        public void IndexUp()
        {
            _mainWindow.StopAuxNavigator();
        }

        public void ThumbSwipe(Direction dir)
        {
            if (_mainWindow.GetActiveTechnique() == Technique.Auxursor_Swipe)
            {
                if (IsStartReleased())
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
                if (IsStartReleased())
                {
                    _mainWindow.ActivateAuxGridNavigator(Side.Left);
                }
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
                if (IsStartReleased())
                {
                    _mainWindow.ActivateAuxGridNavigator(Side.Right);
                }
            }
        }

        public void RingTap()
        {
            
        }

        public void PinkyTap(Side loc)
        {
            
        }

        protected bool IsStartReleased()
        {
            return _trialRecords[_activeTrial.Id].Timestamps.ContainsPartialKey(Str.START_RELEASE);
        }
    }

}
