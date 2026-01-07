using Common.Logs;

namespace Multi.Cursor.Logging
{

    internal class MOSFTrialLog : TrialLog
    {
        public int trlsh_curmv;    // trial show -\ first move
        public int curmv_strnt;   // first move -\ start entered *Last* (before press)
        public int strnt_strpr;    // start enter -\ start press
        public int strpr_strrl;     // start press -\ start release

        public int strrl_obj1pr;    // start exit -\ object1 entered *Last* (before press)

        // ---------------- Object 1 ----------------
        //public int obj1nt_obj1pr;   // object1 enter -\ object1 press
        public int obj1pr_obj1rl;   // object1 press -\ object1 release
        public int obj1rl_pnlnt_1;   // object area 1st exit -\ panel entered for the first Time
        public int pnlnt_funnt_1;   // panel 1st enter -\ function 1st enter (before function press)
        public int funnt_funpr_1;   // function enter first Time -\ function press
        public int funpr_funrl_1;   // function 1st press -\ function 1st release
        public int funrl_obj2pr;    // object area enter -\ object2 enter *Last* (before press)


        // ---------------- Object 2 ----------------
        //public int obj2nt_obj2pr;   // object2 enter -\ object2 press
        public int obj2pr_obj2rl;   // object2 press -\ object2 release
        public int obj2rl_pnlnt_2;  // object area 2nd exit -\ panel entered for the second Time
        public int pnlnt_funnt_2;   // panel 2nd enter -\ function 2nd enter (before function press)
        public int funnt_funpr_2;   // function enter second Time -\ function press
        public int funpr_funrl_2;   // function 2nd press -\ function 2nd release
        public int funrl_obj3pr;    // object area enter -\ object3 enter *Last* (before press)


        // ---------------- Object 3 ----------------
        //public int obj3nt_obj3pr;   // object3 enter -\ object3 press
        public int obj3pr_obj3rl;   // object3 press -\ object3 release
        public int obj3rl_pnlnt_3;  // object area 3rd exit -\ panel entered for the third Time
        public int pnlnt_funnt_3;   // panel 3rd enter -\ function 3rd enter (before function press)
        public int funnt_funpr_3;   // function enter third Time -\ function press
        public int funpr_funrl_3;   // function 3rd press -\ function 3rd release
        public int funrl_obj4pr;    // object area enter -\ object4 enter *Last* (before press)


        // ---------------- Object 4 ----------------
        //public int obj4nt_obj4pr;   // object4 enter -\ object4 press
        public int obj4pr_obj4rl;   // object4 press -\ object4 release
        public int obj4rl_pnlnt_4;  // object area 4th exit -\ panel entered for the fourth Time
        public int pnlnt_funnt_4;   // panel 4th enter -\ function 4th enter (before function press)
        public int funnt_funpr_4;   // function enter fourth Time -\ function press
        public int funpr_funrl_4;   // function 4th press -\ function 4th release
        public int funrl_obj5pr;    // object area enter -\ object5 enter *Last* (before press)


        // ---------------- Object 5 ----------------
        //public int obj5nt_obj5pr;   // object5 enter -\ object5 press
        public int obj5pr_obj5rl;   // object5 press -\ object5 release
        public int obj5rl_pnlnt_5;  // object area 5th exit -\ panel entered for the fifth Time
        public int pnlnt_funnt_5;   // panel 5th enter -\ function 5th enter (before function press)
        public int funnt_funpr_5;   // function enter fifth Time -\ function press
        public int funpr_funrl_5;   // function 5th press -\ function 5th release

        public int funrl_arapr;    // Last function release -\ object area press (trial end)

        public int strrl_gstst;     // start release -\ gesture start (tap: down, swipe: start)
        public int gstst_gstnd;     // gesture start -\ gesture end (tap: up, swipe: end)
        public int gstnd_fstfl;     // gesture end -\ first flick (moving the finger over surface)
        public int fstfl_funmk;     // first flick -\ marker on function

        public int funmk_obj1pr;    // object area enter -\ object1 press
        // Rest is same as above :)
        public int obj1rl_obj2pr;    // object1 exit -\ object2 enter
        public int obj2rl_obj3pr;    // object2 exit -\ object3 enter
        public int obj3rl_obj4pr;    // object3 exit -\ object4 enter
        public int obj4rl_obj5pr;    // object4 exit -\ object5 enter
        
        public int objNrl_arapr;    // last object release -\ object area press (trial end)


        //public MOSFTrialLog(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        //    : base(blockNum, trialNum, trial, trialRecord)
        //{
        //}
    }
}
