using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Multi.Cursor
{

    public abstract class BlockHandler
    {

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

        protected Dictionary<int, TrialRecord> _trialRecords = new Dictionary<int, TrialRecord>();

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

    }

}
