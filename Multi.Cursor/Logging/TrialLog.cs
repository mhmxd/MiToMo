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
        public int ptc;         // participant number
        public int block;       // block number
        public int trial;       // trial number
        public int id;          // number
        public string tech;     // technique
        public string cmplx;    // complexity
        public string tsk_type; // sosf, somf, mosf, momf 
        public int n_obj;       // number of objects
        public int n_fun;       // number of functions
        public string fun_side; // t, l, r
        public int func_width;  // mm
        public string dist_lvl; // s, m, l
        public string dist;     // mm
    }
}
