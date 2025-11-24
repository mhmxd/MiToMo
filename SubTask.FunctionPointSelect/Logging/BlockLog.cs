using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubTask.FunctionPointSelect.Logging
{
    internal class BlockLog : TopLog
    {
        public int ptc;         // participant number
        public int id;          // block id
        public string cmplx;    // complexity
        public int n_trials;    // number of trials

        public string block_time; // Average trial time in the block (sec, .2F)
    }
}
