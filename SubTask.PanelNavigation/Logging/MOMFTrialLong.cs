using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubTask.PanelNavigation.Logging
{
    internal class MOMFTrialLong : TrialLog
    {
        public int trlsh_curmv;    // trial show -\ first move
        public int curmv_strnt;   // first move -\ start entered *Last* (before press)
        public int strnt_strpr;    // start enter -\ start press
        public int strpr_strrl;     // start press -\ start release

        public int strrl_obj1pr;    // start exit -\ object1 pressed

        // --- Object 1 Cycle ---
        //public int obj1nt_obj1pr;
        public int obj1pr_obj1rl;
        public int obj1rl_pnlnt_1;
        public int pnlnt_fun1nt;
        public int fun1nt_fun1pr;
        public int fun1pr_fun1rl;
        public int fun1rl_obj2pr;

        public int fstfl_fun1mk;    // first flick -\ marker on function1
        public int fun1mk_obj1pr;    // object enter -\ object press (1)

        public int obj1rl_fun2mk;   // object release -\ function2 marker on

        // ---------------- Object 2 ----------------
        public int obj2pr_obj2rl;
        public int obj2rl_pnlnt_2;
        public int pnlnt_fun2nt;
        public int fun2nt_fun2pr;
        public int fun2pr_fun2rl;
        public int fun2rl_obj3pr;

        public int fun2mk_obj2pr;    // object enter -\ object press (1)

        public int obj2rl_fun3mk;   // object release -\ function2 marker on


        // ---------------- Object 3 ----------------
        public int obj3pr_obj3rl;
        public int obj3rl_pnlnt_3;
        public int pnlnt_fun3nt;
        public int fun3nt_fun3pr;
        public int fun3pr_fun3rl;
        public int fun3rl_obj4pr;

        public int fun3mk_obj3pr;    // object enter -\ object press (1)

        public int obj3rl_fun4mk;   // object release -\ function2 marker on


        // ---------------- Object 4 ----------------
        public int obj4pr_obj4rl;
        public int obj4rl_pnlnt_4;
        public int pnlnt_fun4nt;
        public int fun4nt_fun4pr;
        public int fun4pr_fun4rl;
        public int fun4rl_obj5pr;

        public int fun4mk_obj4pr;    // object enter -\ object press (1)

        public int obj4rl_fun5mk;   // object release -\ function2 marker on


        // ---------------- Object 5 ----------------
        public int obj5pr_obj5rl;
        public int obj5rl_pnlnt_5;
        public int pnlnt_fun5nt;
        public int fun5nt_fun5pr;
        public int fun5pr_fun5rl;

        public int fun5mk_obj5pr;    // object enter -\ object press (1)

        public int objNrl_arapr;

        // --- Final steps
        public int funNrl_arapr;     // last function release -\ object area enter

        // --- Gesture/Flick Timings (Independent of the main object/function sequence) ---
        public int strrl_gstst;      // start release -> gesture start
        public int gstst_gstnd;      // gesture start -> gesture end
        public int gstnd_fstfl;      // gesture end -> first flick


        public MOMFTrialLong(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
            : base(blockNum, trialNum, trial, trialRecord)
        {
        }
    }
}
