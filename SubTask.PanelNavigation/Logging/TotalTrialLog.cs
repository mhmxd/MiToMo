using Common.Logs;

namespace SubTask.PanelNavigation.Logging
{
    internal class TotalTrialLog : TrialLog
    {
        public int trial_time; // Start release -\ gesture end
        public int gesture_start_time; // Start release -\ gesture start
        public int gesture_duration; // gesture start -\ gesture end
    }
}
