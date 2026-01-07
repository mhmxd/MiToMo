using Common.Logs;

namespace SubTask.FunctionPointSelect.Logging
{
    internal class DetailTrialLog : TrialLog
    {
        public int trlsh_curmv;    // trial show -\ first move
        public int curmv_strnt;   // first move -\ start entered *Last* (before press)
        public int strnt_strpr;    // start enter -\ start press
        public int strpr_strrl;     // start press -\ start release

        public int strrl_strxt;    // start release -\ start exit 

        public int strxt_pnlnt;     // start exit -\ panel enter

        public int pnlnt_funnt;     // (last) panel enter -\ (last) function enter
        public int funnt_funpr;     // (last) function enter -\ (last) function press
        public int funpr_funrl;     // function press -\ function relese (trial end)

        //public DetailTrialLog(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        //: base(blockNum, trialNum, trial, trialRecord)
        //{
        //}
    }
}
