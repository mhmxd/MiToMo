using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multi.Cursor.Logging
{
    internal class MOMFTrialLong : TrialLog
    {
        // --- Initial Trial Setup ---
        public int trlsh_fstmv;      // trial show -> first move
        public int fstmv_strnt;      // first move -> start enter
        public int strnt_strpr;      // start enter -> start press
        public int strpr_strrl;      // start press -> start release
        public int strrl_strxt;      // start release -> start exit

        // --- Object 1 Cycle ---
        public int strxt_obj1nt;     // start exit -> object 1 enter
        public int obj1nt_obj1pr;
        public int obj1pr_obj1rl;
        public int obj1rl_obj1xt;
        public int obj1xt_ara1xt;
        public int ara1xt_pnl1nt;
        public int pnl1nt_fun1nt;
        public int fun1nt_fun1pr;
        public int fun1pr_fun1rl;
        public int fun1rl_fun1xt;
        public int fun1xt_pnl1xt;
        public int pnl1xt_ara1nt;
        public int ara1nt_obj2nt;    // -> Object 2 enter

        // --- Object 2 Cycle ---
        public int obj2nt_obj2pr;
        public int obj2pr_obj2rl;
        public int obj2rl_obj2xt;
        public int obj2xt_ara2xt;
        public int ara2xt_pnl2nt;
        public int pnl2nt_fun2nt;
        public int fun2nt_fun2pr;
        public int fun2pr_fun2rl;
        public int fun2rl_fun2xt;
        public int fun2xt_pnl2xt;
        public int pnl2xt_ara2nt;
        public int ara2nt_obj3nt;    // -> Object 3 enter

        // --- Object 3 Cycle ---
        public int obj3nt_obj3pr;
        public int obj3pr_obj3rl;
        public int obj3rl_obj3xt;
        public int obj3xt_ara3xt;
        public int ara3xt_pnl3nt;
        public int pnl3nt_fun3nt;
        public int fun3nt_fun3pr;
        public int fun3pr_fun3rl;
        public int fun3rl_fun3xt;
        public int fun3xt_pnl3xt;
        public int pnl3xt_ara3nt;
        public int ara3nt_obj4nt;    // -> Object 4 enter

        // --- Object 4 Cycle ---
        public int obj4nt_obj4pr;
        public int obj4pr_obj4rl;
        public int obj4rl_obj4xt;
        public int obj4xt_ara4xt;
        public int ara4xt_pnl4nt;
        public int pnl4nt_fun4nt;
        public int fun4nt_fun4pr;
        public int fun4pr_fun4rl;
        public int fun4rl_fun4xt;
        public int fun4xt_pnl4xt;
        public int pnl4xt_ara4nt;
        public int ara4nt_obj5nt;    // -> Object 5 enter

        // --- Object 5 Cycle ---
        public int obj5nt_obj5pr;
        public int obj5pr_obj5rl;
        public int obj5rl_obj5xt;
        public int obj5xt_ara5xt;
        public int ara5xt_pnl5nt;
        public int pnl5nt_fun5nt;
        public int fun5nt_fun5pr;
        public int fun5pr_fun5rl;
        public int fun5rl_fun5xt;

        // --- Final steps
        public int funNxt_pnlNxt;
        public int pnlNxt_araNnt;
        public int araNnt_araNpr;

        // --- Gesture/Flick Timings (Independent of the main object/function sequence) ---
        public int strrl_gstst;      // start release -> gesture start
        public int gstst_gstnd;      // gesture start -> gesture end
        public int gstnd_fstfl;      // gesture end -> first flick
        public int fstfl_funmk;      // first flick -> marker on function
        public int funmk_obant;      // marker on function -> object area enter
        public int arant_obj1nt;     // object area enter -> object 1 enter
        public int objrl_arant;      // object release -> object area enter (This seems like a potential restart or error path)
    }
}
