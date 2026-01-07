using Common.Logs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubTask.ObjectSelection.Logging
{
    internal class DetailedTrialLog : TrialLog
    {
        public int trlsh_curmv;    // trial show -\ first move
        public int curmv_strnt;   // first move -\ start entered *Last* (before press)
        public int strnt_strpr;    // start enter -\ start press
        public int strpr_strrl;     // start press -\ start release

        public int strrl_obj1nt;    // start exit -\ object1 entered *Last* (before press)

        // ---------------- Object 1 ----------------
        public int obj1nt_obj1pr;   // object1 enter -\ object1 press
        public int obj1pr_obj1rl;   // object1 press -\ object1 release

        public int obj1rl_obj2nt;  // object1 release -\ object2 entered *Last* (before press)

        // ---------------- Object 2 ----------------
        public int obj2nt_obj2pr;   // object2 enter -\ object2 press
        public int obj2pr_obj2rl;   // object2 press -\ object2 release

        public int obj2rl_obj3nt;  // object2 release -\ object3 entered *Last* (before press)

        // ---------------- Object 3 ----------------
        public int obj3nt_obj3pr;   // object3 enter -\ object3 press
        public int obj3pr_obj3rl;   // object3 press -\ object3 release

        public int obj3rl_obj4nt;  // object3 release -\ object4 entered *Last* (before press)

        // ---------------- Object 4 ----------------
        public int obj4nt_obj4pr;   // object4 enter -\ object4 press
        public int obj4pr_obj4rl;   // object4 press -\ object4 release

        public int obj4rl_obj5nt;  // object4 release -\ object5 entered *Last* (before press)


        // ---------------- Object 5 ----------------
        public int obj5nt_obj5pr;   // object5 enter -\ object5 press
        public int obj5pr_obj5rl;   // object5 press -\ object5 release

        public int objNrl_arapr;    // last object release -\ object area press
        public int arapr_ararl;     // object area press -\ object area release (trial end)
    }
}
