using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Multi.Cursor.Logging
{
    internal class SOMFTrialLog : TrialLog
    {
        public int trlsh_fstmv;     // trial show -\ first move
        public int fstmv_strnt;     // first move -\ start enter
        public int strnt_strpr;     // start enter -\ start press
        public int strpr_strrl;     // start press -\ start release
        public int strrl_strxt;     // start release -\ start exit

        public int strxt_objnt;     // start exit -\ object enter
        public int objnt_objpr;     // object enter -\ object press
        public int objpr_objrl;     // object press -\ object release
        public int objrl_objxt;     // object release -\ object exit
        public int objxt_araxt;     // object exit -\ object area exit
        public int araxt_pnlnt;     // object exit -\ panel enter
        public int pnlnt_fun1nt;    // panel enter -\ function1 enter

        // ---------------- Function 1 ----------------
        public int fun1nt_fun1pr;   // function1 enter -\ function1 press
        public int fun1pr_fun1rl;   // function1 press -\ function1 release
        public int fun1rl_fun1xt;   // function1 release -\ function1 exit
        public int fun1xt_fun2nt;   // function1 exit -\ function2 enter

        // ---------------- Function 2 ----------------
        public int fun2nt_fun2pr;   // function2 enter -\ function2 press
        public int fun2pr_fun2rl;   // function2 press -\ function2 release
        public int fun2rl_fun2xt;   // function2 release -\ function2 exit
        public int fun2xt_fun3nt;   // function2 exit -\ function3 enter

        // ---------------- Function 3 ----------------
        public int fun3nt_fun3pr;   // function3 enter -\ function3 press
        public int fun3pr_fun3rl;   // function3 press -\ function3 release
        public int fun3rl_fun3xt;   // function3 release -\ function3 exit
        public int fun3xt_fun4nt;   // function3 exit -\ function4 enter

        // ---------------- Function 4 ----------------
        public int fun4nt_fun4pr;   // function4 enter -\ function4 press
        public int fun4pr_fun4rl;   // function4 press -\ function4 release
        public int fun4rl_fun4xt;   // function4 release -\ function4 exit
        public int fun4xt_fun5nt;   // function4 exit -\ function5 enter

        // ---------------- Function 5 ----------------
        public int fun5nt_fun5pr;   // function5 enter -\ function5 press
        public int fun5pr_fun5rl;   // function5 press -\ function5 release
        public int fun5rl_fun5xt;   // function5 release -\ function5 exit

        public int funNxt_pnlxt;    // functionN (last function) exit -\ panel exit

        public int pnlxt_arant;     // panel exit -\ object area enter
        public int arant_arapr;     // object area enter -\ object area press (trial end)

        public int strrl_gstst;     // start release -\ gesture start (tap: down, swipe: start)
        public int gstst_gstnd;     // gesture start -\ gesture end (tap: up, swipe: end)
        public int gstnd_fstfl;     // gesture end -\ first flick (moving the finger over surface)
        public int fstfl_funmk;     // first flick -\ marker on function
        public int funmk_obant;     // marker on function -\ object area enter
        public int obant_objnt;     // object area enter -\ object enter
        public int objrl_obant;     // object release -\ object area enter

        public SOMFTrialLog()
        {

        }
    }
}
