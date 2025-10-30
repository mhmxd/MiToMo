using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Panel.Select.Logging
{
    internal class SOMFTrialLog : TrialLog
    {
        public int trlsh_curmv;    // trial show -\ first move
        public int curmv_strnt;   // first move -\ start entered *Last* (before press)
        public int strnt_strpr;    // start enter -\ start press
        public int strpr_strrl;     // start press -\ start release

        public int strrl_objnt;    // start exit -\ object1 entered *Last* (before press)

        public int objnt_objpr;     // object enter -\ object press
        public int objpr_objrl;     // object press -\ object release
        public int objrl_pnlnt;     // object exit -\ panel enter
        public int pnlnt_fun1nt;    // panel enter -\ function1 enter

        public int fstfl_fun1mk;    // first flick -\ marker on function1

        // ---------------- Function 1 ----------------
        public int fun1nt_fun1pr;   // function1 enter -\ function1 press
        public int fun1pr_fun1rl;   // function1 press -\ function1 release

        public int fun1rl_fun2nt;   // function1 exit -\ function2 enter

        //public int fun1mk_objnt;   // function1 marked -\ object enter
        public int fun1mk_objpr_1;    // object enter -\ object press (1)
        public int objpr_objrl_1;   // object press -\ object release (1)

        public int objrl_fun2mk;   // object release -\ function2 marker on

        // ---------------- Function 2 ----------------
        public int fun2nt_fun2pr;   // function2 enter -\ function2 press
        public int fun2pr_fun2rl;   // function2 press -\ function2 release

        public int fun2rl_fun3nt;   // function2 exit -\ function3 enter

        //public int fun2mk_objnt;   // function2 marked -\ object enter
        public int fun2mk_objpr_2;    // object enter -\ object press (2)
        public int objpr_objrl_2;   // object press -\ object release (2)

        public int objrl_fun3mk;   // object release -\ function3 marker on

        // ---------------- Function 3 ----------------
        public int fun3nt_fun3pr;   // function3 enter -\ function3 press
        public int fun3pr_fun3rl;   // function3 press -\ function3 release

        public int fun3rl_fun4nt;   // function3 exit -\ function4 enter

        //public int fun3mk_objnt;   // function3 marked -\ object enter
        public int fun3mk_objpr_3;    // object enter -\ object press (3)
        public int objpr_objrl_3;   // object press -\ object release (3)

        public int objrl_fun4mk;   // object release -\ function4 marker on

        // ---------------- Function 4 ----------------
        public int fun4nt_fun4pr;   // function4 enter -\ function4 press
        public int fun4pr_fun4rl;   // function4 press -\ function4 release

        public int fun4rl_fun5nt;   // function4 exit -\ function5 enter

        //public int fun4mk_objnt;   // function4 marked -\ object enter
        public int fun4mk_objpr_4;    // object enter -\ object press (4)
        public int objpr_objrl_4;   // object press -\ object release (4)

        public int objrl_fun5mk;   // object release -\ function5 marker on

        // ---------------- Function 5 ----------------
        public int fun5nt_fun5pr;   // function5 enter -\ function5 press
        public int fun5pr_fun5rl;   // function5 press -\ function5 release

        //public int fun5mk_objnt;   // function5 marked -\ object enter
        public int fun5mk_objpr_5;    // object enter -\ object press (5)
        public int objpr_objrl_5;   // object press -\ object release (5)

        public int objrl_arapr;     // object release -\ object area enter

        public int funNrl_arapr;     // last function release -\ object area enter
        //public int arant_arapr;     // object area enter -\ object area press (trial end)

        public int strrl_gstst;     // start release -\ gesture start (tap: down, swipe: start)
        public int gstst_gstnd;     // gesture start -\ gesture end (tap: up, swipe: end)
        public int gstnd_fstfl;     // gesture end -\ first flick (moving the finger over surface)

        public SOMFTrialLog(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
            : base(blockNum, trialNum, trial, trialRecord)
        {
        }
    }
}
