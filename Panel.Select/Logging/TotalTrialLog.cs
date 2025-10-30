﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panel.Select.Logging
{
    internal class TotalTrialLog : TrialLog
    {
        public int trial_time; // Start release -\ object area press

        public TotalTrialLog(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
            : base(blockNum, trialNum, trial, trialRecord)
        {
            
        }
    }
}
