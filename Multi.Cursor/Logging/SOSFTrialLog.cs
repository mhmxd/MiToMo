using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multi.Cursor.Logging
{
    internal class SOSFTrialLog : TrialLog
    {
        public int trlsh_fstmv;     // trial show -\ first move
        public int fstmv_strnt;     // first move -\ start enter
        public int strnt_strpr;     // start enter -\ start press
        public int strpr_strrl;     // start press -\ start release

        public int strxt_objnt;     // start exit -\ object enter
        public int objnt_objpr;     // object enter -\ object press
        public int objpr_objrl;     // object press -\ object release
        public int objrl_objxt;     // object release -\ object exit
        public int objxt_araxt;     // object exit -\ object area exit
        public int araxt_pnlnt;     // object exit -\ panel enter
        public int pnlnt_funnt;     // (last) panel enter -\ (last) function enter
        public int funnt_funpr;     // (last) function enter -\ (last) function press
        public int funpr_funrl;     // function press -\ function relese
        public int funrl_funxt;     // function release -\ function exit
        public int funxt_pnlxt;     // function exit -\ panel exit
        public int pnlxt_arant;     // panel exit -\ object area enter
        public int arant_obapr;     // object area enter -\ object area press (trial end)

        public int strrl_gstst;     // start release -\ gesture start (tap: down, swipe: start)
        public int gstst_gstnd;     // gesture start -\ gesture end (tap: up, swipe: end)
        public int gstnd_fstfl;     // gesture end -\ first flick (moving the finger over surface)
        public int fstfl_funmk;     // first flick -\ marker on function
        public int funmk_obant;     // marker on function -\ object area enter
        public int arant_objnt;     // object area enter -\ object enter
        public int objrl_arant;     // object release -\ object area enter
    }
}
