using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubTask.ObjectSelection.Logging
{
    internal class BlockLog : TopLog
    {
        public int ptc;         // participant number
        public int id;          // block id
        public string tech;     // technique
        public string cmplx;    // complexity
        public string tsk_type; // sosf, somf, mosf, momf
        public int n_obj;       // number of objects
        public int n_fun;       // number of functions
        public int n_trials;    // number of trials

        public string block_time; // Average trial time in the block (sec, .2F)
    }
}
