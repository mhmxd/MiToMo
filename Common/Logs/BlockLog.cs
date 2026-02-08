using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Logs
{
    public class BlockLog
    {
        public int ptc;                 // participant number
        public int id;                  // block id
        public string tech = "";        // technique
        public string cmplx = "";       // complexity
        public string exptype = "";     // experiment type
        public string tsk_type = "";    // sosf, somf, mosf, momf
        public int n_obj;               // number of objects
        public int n_fun;               // number of functions
        public int n_trials;            // number of trials
        public string blck_time = "-1"; // Total time for the block (sec, .2F)
        public string avg_time = "-1"; // Average trial time in the block (sec, .2F)
    }
}
