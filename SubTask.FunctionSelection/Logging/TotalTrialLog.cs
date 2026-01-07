using Common.Logs;

namespace SubTask.FunctionSelection.Logging
{
    internal class TotalTrialLog : TrialLog
    {
        public int trial_time; // Start release -\ end press
        public int funcs_sel_time; // first function enter -\ last function release
    }
}
