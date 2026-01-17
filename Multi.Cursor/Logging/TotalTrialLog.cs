using Common.Logs;

namespace Multi.Cursor.Logging
{
    internal class TotalTrialLog : TrialLog
    {
        public int trial_time; // Start release -\ object area press
        public int funcs_sel_time; // Object release -\ last function release
        public int objs_sel_time; // Object area enter -\ last object release
        public int func_po_sel_time; // Object release -\ function release (in SOSF)
        public int panel_sel_time; // Start release -\ marker activated
        public int panel_nav_time; // Panel selected -\ object presse
    }
}
