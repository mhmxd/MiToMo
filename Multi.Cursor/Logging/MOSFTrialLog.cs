using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multi.Cursor.Logging
{

    internal class MOSFTrialLog : TrialLog
    {
        public int trlsh_fstmv;     // trial show -\ first move
        public int fstmv_strnt;     // first move -\ start enter
        public int strnt_strpr;     // start enter -\ start press
        public int strpr_strrl;     // start press -\ start release
        public int strrl_strxt;     // start release -\ start exit

        // ---------------- Object 1 ----------------
        public int strxt_obj1nt;    // start exit -\ object enter
        public int obj1nt_obj1pr;   // object1 enter -\ object1 press
        public int obj1pr_obj1rl;   // object1 press -\ object1 release
        public int obj1rl_obj1xt;   // object1 release -\ object1 exit
        public int obj1xt_ara1xt;   // object1 exit -\ object area 1st exit
        public int ara1xt_pnl1nt;   // object area 1st exit -\ panel 1st enter
        public int pnl1nt_fun1nt;   // panel 1st enter -\ function 1st enter
        public int fun1nt_fun1pr;   // function 1st enter -\ function 1st press
        public int fun1pr_fun1rl;   // function 1st press -\ function 1st release
        public int fun1rl_fun1xt;   // function 1st release -\ function 1st exit
        public int fun1xt_pnl1xt;   // function 1st exit -\ panel 1st exit
        public int pnl1xt_ara1nt;    // panel 1st exit -\ object area 1st enter (from outside)
        public int ara1nt_obj2nt;    // object area enter -\ object2 enter

        // ---------------- Object 2 ----------------
        public int obj2nt_obj2pr;   // object2 enter -\ object2 press
        public int obj2pr_obj2rl;   // object2 press -\ object2 release
        public int obj2rl_obj2xt;   // object2 release -\ object2 exit
        public int obj2xt_ara2xt;   // object2 exit -\ object area 2nd exit
        public int ara2xt_pnl2nt;   // object area 2nd exit -\ panel 2nd enter
        public int pnl2nt_fun2nt;   // panel 2nd enter -\ function 2nd enter
        public int fun2nt_fun2pr;   // function 2nd enter -\ function 2nd press
        public int fun2pr_fun2rl;   // function 2nd press -\ function 2nd release
        public int fun2rl_fun2xt;   // function 2nd release -\ function 2nd exit
        public int fun2xt_pnl2xt;   // function 2nd exit -\ panel 2nd exit
        public int pnl2xt_ara2nt;   // panel 2nd exit -\ object area 2nd enter (from outside)
        public int ara2nt_obj3nt;   // object area 2nd enter -\ object3 enter

        // ---------------- Object 3 ----------------
        public int strxt_obj3nt;    // start exit -\ object3 enter
        public int obj3nt_obj3pr;   // object3 enter -\ object3 press
        public int obj3pr_obj3rl;   // object3 press -\ object3 release
        public int obj3rl_obj3xt;   // object3 release -\ object3 exit
        public int obj3xt_ara3xt;   // object3 exit -\ object area 3rd exit
        public int ara3xt_pnl3nt;   // object area 3rd exit -\ panel 3rd enter
        public int pnl3nt_fun3nt;   // panel 3rd enter -\ function 3rd enter
        public int fun3nt_fun3pr;   // function 3rd enter -\ function 3rd press
        public int fun3pr_fun3rl;   // function 3rd press -\ function 3rd release
        public int fun3rl_fun3xt;   // function 3rd release -\ function 3rd exit
        public int fun3xt_pnl3xt;   // function 3rd exit -\ panel 3rd exit
        public int pnl3xt_ara3nt;   // panel 3rd exit -\ object area 3rd enter (from outside)
        public int ara3nt_obj4nt;   // object area 3rd enter -\ object4 enter

        // ---------------- Object 4 ----------------
        public int strxt_obj4nt;    // start exit -\ object4 enter
        public int obj4nt_obj4pr;   // object4 enter -\ object4 press
        public int obj4pr_obj4rl;   // object4 press -\ object4 release
        public int obj4rl_obj4xt;   // object4 release -\ object4 exit
        public int obj4xt_ara4xt;   // object4 exit -\ object area 4th exit
        public int ara4xt_pnl4nt;   // object area 4th exit -\ panel 4th enter
        public int pnl4nt_fun4nt;   // panel 4th enter -\ function 4th enter
        public int fun4nt_fun4pr;   // function 4th enter -\ function 4th press
        public int fun4pr_fun4rl;   // function 4th press -\ function 4th release
        public int fun4rl_fun4xt;   // function 4th release -\ function 4th exit
        public int fun4xt_pnl4xt;   // function 4th exit -\ panel 4th exit
        public int pnl4xt_ara4nt;   // panel 4th exit -\ object area 4th enter (from outside)
        public int ara4nt_obj5nt;   // object area 4th enter -\ object5 enter

        // ---------------- Object 5 ----------------
        public int strxt_obj5nt;    // start exit -\ object5 enter
        public int obj5nt_obj5pr;   // object5 enter -\ object5 press
        public int obj5pr_obj5rl;   // object5 press -\ object5 release
        public int obj5rl_obj5xt;   // object5 release -\ object5 exit
        public int obj5xt_ara5xt;   // object5 exit -\ object area 5th exit
        public int ara5xt_pnl5nt;   // object area 5th exit -\ panel 5th enter
        public int pnl5nt_fun5nt;   // panel 5th enter -\ function 5th enter
        public int fun5nt_fun5pr;   // function 5th enter -\ function 5th press
        public int fun5pr_fun5rl;   // function 5th press -\ function 5th release
        public int fun5rl_fun5xt;   // function 5th release -\ function 5th exit
        public int fun5xt_pnl5xt;   // function 5th exit -\ panel 5th exit
        public int pnl5xt_ara5nt;   // panel 5th exit -\ object area 5th enter (from outside)

        public int strrl_gstst;     // start release -\ gesture start (tap: down, swipe: start)
        public int gstst_gstnd;     // gesture start -\ gesture end (tap: up, swipe: end)
        public int gstnd_fstfl;     // gesture end -\ first flick (moving the finger over surface)
        public int fstfl_funmk;     // first flick -\ marker on function
        public int funmk_obant;     // marker on function -\ object area enter
        public int obant_obj1nt;    // object area enter -\ object1 enter
        // Rest is on above :)
        public int obj1xt_obj2nt;    // object1 exit -\ object2 enter
        public int obj2xt_obj3nt;    // object2 exit -\ object3 enter
        public int obj3xt_obj4nt;    // object3 exit -\ object4 enter
        public int obj4xt_obj5nt;    // object4 exit -\ object5 enter
        
        public int objNxt_obapr;    // object5 exit -\ object area press (trial end)
    }
}
