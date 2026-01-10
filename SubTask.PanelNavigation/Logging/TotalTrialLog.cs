using Common.Logs;

namespace SubTask.PanelNavigation.Logging
{
    internal class TotalTrialLog : TrialLog
    {
        public int trial_time;          // Start release -\ marker stops
        public int marker_start_time;   // Start release -\ marker starts moving
        public int marker_move_time;    // marker starts moving -\ marker starts
    }
}
