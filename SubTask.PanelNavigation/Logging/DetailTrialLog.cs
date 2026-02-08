using Common.Logs;

namespace SubTask.PanelNavigation.Logging
{
    internal class DetailTrialLog : TrialLog
    {
        public int trlsh_curmv;    // trial show \ first move
        public int curmv_strnt;   // first move \ Start entered *Last* (before press)
        public int strnt_strpr;    // Start enter \ Start press
        public int strpr_strrl;     // Start press \ Start release

        public int strrl_fngmv; // Start release \ finger start moving
        public int fngmv_mrkmv; // finger start moving \ marker starts moving
        public int mrkmv_mrksp; // marker starts moving \ marker stops
        public int mrksp_endpr; // marker stops \ End button press
        public int endpr_endrl; // End button press \ button release
    }
}
