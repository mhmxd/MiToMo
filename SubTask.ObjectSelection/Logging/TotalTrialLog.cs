using Common.Logs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubTask.ObjectSelection.Logging
{
    internal class TotalTrialLog : TrialLog
    {
        public int trial_time;      // Start release -\ object area press
        public int objs_sel_time;   // First object enter -\ last object release
    }
}
