using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multi.Cursor.Logging
{

    internal class MOSFTrialLog : TrialLog
    {
        public int trlsh_curmvF;     // trial show -\ first move
        public int curmvF_strntL;     // first move -\ start entered *Last* (before press)
        public int strntL_strpr;     // start enter -\ start press
        public int strpr_strrl;     // start press -\ start release

        public int strrl_obj1ntL;    // start exit -\ object1 entered *Last* (before press)

        // ---------------- Object 1 ----------------
        public int obj1ntL_obj1pr;   // object1 enter -\ object1 press
        public int obj1pr_obj1rl;   // object1 press -\ object1 release
        public int obj1rl_pnlnt1;   // object area 1st exit -\ panel entered for the first Time
        public int pnlnt1_funnt1;   // panel 1st enter -\ function 1st enter (before function press)
        public int funnt1_funpr1;   // function enter first Time -\ function press
        public int funpr1_funrl1;   // function 1st press -\ function 1st release
        public int funrl1_obj2ntL;    // object area enter -\ object2 enter *Last* (before press)

        // ==== Object 2 ====
        public int obj2ntL_obj2pr;   // object2 enter -\ object2 press
        public int obj2pr_obj2rl;    // object2 press -\ object2 release
        public int obj2rl_pnlnt2;    // object area 1st exit -\ panel entered for the second Time
        public int pnlnt2_funnt2;    // panel 2nd enter -\ function 2nd enter (before function press)
        public int funnt2_funpr2;    // function enter second Time -\ function press
        public int funpr2_funrl2;    // function 2nd press -\ function 2nd release
        public int funrl2_obj3ntL;   // function 2nd exit -\ object3 enter *Last* (before press)

        // ==== Object 3 ====
        public int obj3ntL_obj3pr;   // object3 enter -\ object3 press
        public int obj3pr_obj3rl;    // object3 press -\ object3 release
        public int obj3rl_pnlnt3;    // object area 1st exit -\ panel entered for the third Time
        public int pnlnt3_funnt3;    // panel 3rd enter -\ function 3rd enter (before function press)
        public int funnt3_funpr3;    // function enter third Time -\ function press
        public int funpr3_funrl3;    // function 3rd press -\ function 3rd release
        public int funrl3_obj4ntL;   // function 3rd exit -\ object4 enter *Last* (before press)

        // ==== Object 4 ====
        public int obj4ntL_obj4pr;   // object4 enter -\ object4 press
        public int obj4pr_obj4rl;    // object4 press -\ object4 release
        public int obj4rl_pnlnt4;    // object area 1st exit -\ panel entered for the fourth Time
        public int pnlnt4_funnt4;    // panel 4th enter -\ function 4th enter (before function press)
        public int funnt4_funpr4;    // function enter fourth Time -\ function press
        public int funpr4_funrl4;    // function 4th press -\ function 4th release
        public int funrl4_obj5ntL;   // function 4th exit -\ object5 enter *Last* (before press)

        // ==== Object 5 ====
        public int obj5ntL_obj5pr;   // object5 enter -\ object5 press
        public int obj5pr_obj5rl;    // object5 press -\ object5 release
        public int obj5rl_pnlnt5;    // object area 1st exit -\ panel entered for the fifth Time
        public int pnlnt5_funnt5;    // panel 5th enter -\ function 5th enter (before function press)
        public int funnt5_funpr5;    // function enter fifth Time -\ function press
        public int funpr5_funrl5;    // function 5th press -\ function 5th release

        public int funrl5_arant;    // Last function release -\ object area enter
        public int arant_arpr;      // object area enter -\ object area press (trial end)

        public int strrl_gstst;     // start release -\ gesture start (tap: down, swipe: start)
        public int gstst_gstnd;     // gesture start -\ gesture end (tap: up, swipe: end)
        public int gstnd_fstfl;     // gesture end -\ first flick (moving the finger over surface)
        public int fstfl_funmk;     // first flick -\ marker on function
        public int funmk_obant;     // marker on function -\ object area enter
        public int obant_obj1nt;    // object area enter -\ object1 enter
        // Rest is same as above :)
        public int obj1xt_obj2ntL;    // object1 exit -\ object2 enter
        public int obj2xt_obj3ntL;    // object2 exit -\ object3 enter
        public int obj3xt_obj4ntL;    // object3 exit -\ object4 enter
        public int obj4xt_obj5ntL;    // object4 exit -\ object5 enter
        
        public int objNrl_arapr;    // last object release -\ object area press (trial end)
    }
}
