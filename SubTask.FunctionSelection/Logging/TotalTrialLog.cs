using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubTask.FunctionSelection.Logging
{
    internal class TotalTrialLog : TrialLog
    {
        public int trial_time; // Start release -\ end press
        public int funcs_sel_time; // first function enter -\ last function release

        public TotalTrialLog(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
            : base(blockNum, trialNum, trial, trialRecord)
        {

        }
    }
}
