namespace SubTask.PanelNavigation.Logging
{
    internal class DetailTrialLog : TrialLog
    {
        public int trlsh_curmv;    // trial show \ first move
        public int curmv_strnt;   // first move \ Start entered *Last* (before press)
        public int strnt_strpr;    // Start enter \ Start press
        public int strpr_strrl;     // Start press \ Start release
        
        public int strrl_fngup; // (Tap) Start release \ finger up | (Swipe) doesn't matter
        public int fngup_fngdn; // (Tap) finger up \ finger down) | (Swipe) doesn't matter
        public int fngdn_fngup; // (Tap) finger down \ finger up | (Swipe) doesn't matter
        // string, so that we can long only two decimal places
        public string tap_xlen; // (Tap) length of the touch area along Xaxis (mm)
        public string tap_ylen; // (Tap) length of the touch area along Yaxis (mm)
        
        public int strrl_fngmv; // (Swipe) Start release \ finger start moving to swipe | (Tap) doesn't matter
        public int fngmv_swpthr; // (Swipe) finger start moving \ swipe threshold reaches
        // string, so that we can long only two decimal places
        public string swipe_xlen; // (Swipe) length of the movement along Xaxis (mm)
        public string swipe_ylen; // (Swipe) length of the movement along Yaxis (mm)

        public DetailTrialLog(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        : base(blockNum, trialNum, trial, trialRecord)
        {
        }
    }
}
