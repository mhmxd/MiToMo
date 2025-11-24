using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubTask.FunctionPointSelect.Logging
{
    internal class DetailTrialLog : TrialLog
    {
        public int trlsh_curmv;    // trial show -\ first move
        public int curmv_strnt;   // first move -\ start entered *Last* (before press)
        public int strnt_strpr;    // start enter -\ start press
        public int strpr_strrl;     // start press -\ start release

        public int strrl_objnt;    // start exit -\ object1 entered *Last* (before press)

        public int objnt_objpr;     // object last entered -\ object pressed
        public int objpr_objrl;     // object press -\ object release
        public int objrl_pnlnt;     // object exit -\ panel enter
        public int pnlnt_funnt;     // (last) panel enter -\ (last) function enter
        public int funnt_funpr;     // (last) function enter -\ (last) function press
        public int funpr_funrl;     // function press -\ function relese
        public int funrl_arant;     // panel exit -\ object area enter
        public int arant_arapr;     // object area enter -\ object area press (trial end)

        public int strrl_gstst;     // start release -\ gesture start (tap: down, swipe: start)
        public int gstst_gstnd;     // gesture start -\ gesture end (tap: up, swipe: end)
        public int gstnd_fstfl;     // gesture end -\ first flick (moving the finger over surface)
        public int fstfl_funmk;     // first flick -\ marker on function
        public int funmk_objnt;     // marker on function -\ object enter

        public DetailTrialLog(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        : base(blockNum, trialNum, trial, trialRecord)
        {
        }
    }
}
