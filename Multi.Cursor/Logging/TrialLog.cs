using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace Multi.Cursor.Logging
{
    internal class TrialLog
    {
        public int ptc;             // participant number
        public int block;         // block number
        public int trial;         // trial number
        public int id;              // number
        public string tech;         // technique
        public string cmplx;        // complexity
        public string tsk_type;     // sosf, somf, mosf, momf 
        public int n_obj;           // number of objects
        public int n_fun;           // number of functions
        public string fun_side;     // t, l, r
        public int func_width;      // mm
        public string dist_lvl;     // s, m, l
        public string dist;         // mm

        public int trlsh_fstmv;     // trial show -\ first move
        public int fstmv_strnt;     // first move -\ start enter
        public int strnt_strpr;     // start enter -\ start press
        public int strpr_strrl;     // start press -\ start release
        public int strrl_strxt;     // start release -\ start exit

        public int strxt_objnt;     // start exit -\ object enter
        public int objnt_objpr;     // object enter -\ object press
        public int objpr_objrl;     // object press -\ object release
        public int objrl_objxt;     // object release -\ object exit
        public int objxt_obaxt;     // object exit -\ object area exit
        public int obaxt_pnlnt;     // object exit -\ panel enter
        public int pnlnt_funnt;     // panel enter -\ function enter
        public int funnt_funpr;     // function enter -\ function press
        public int funpr_funrl;     // function press -\ function relese
        public int funrl_funxt;     // function release -\ function exit
        public int funxt_pnlxt;     // function exit -\ panel exit
        public int pnlxt_obant;     // panel exit -\ object area enter
        public int obant_obapr;     // object area enter -\ object area press (trial end)

        public int strrl_gstst;     // start release -\ gesture start (tap: down, swipe: start)
        public int gstst_gstnd;     // gesture start -\ gesture end (tap: up, swipe: end)
        public int gstnd_fstfl;     // gesture end -\ first flick (moving the finger over surface)
        public int fstfl_funmk;     // first flick -\ marker on function
        public int funmk_obant;     // marker on function -\ object area enter
        public int obant_objnt;     // object area enter -\ object enter
        public int objrl_obant;     // object release -\ object area enter

    }
}
