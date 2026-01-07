using Common.Logs;

namespace SubTask.FunctionSelection.Logging
{
    internal class DetailedTrialLog : TrialLog
    {
        public int trlsh_curmv;    // trial show -\ first move
        public int curmv_strnt;   // first move -\ start entered *Last* (before press)
        public int strnt_strpr;    // start enter -\ start press
        public int strpr_strrl;     // start press -\ start release

        public int strrl_pnlnt;    // start release -\ panel enter

        public int pnlnt_fun1nt;    // panel enter -\ function1 enter

        // ---------------- Function 1 ----------------
        public int fun1nt_fun1pr;   // function1 enter -\ function1 press
        public int fun1pr_fun1rl;   // function1 press -\ function1 release

        public int fun1rl_fun2nt;   // function1 release -\ function2 enter

        // ---------------- Function 2 ----------------
        public int fun2nt_fun2pr;   // function2 enter -\ function2 press
        public int fun2pr_fun2rl;   // function2 press -\ function2 release

        public int fun2rl_fun3nt;   // function2 release -\ function3 enter

        // ---------------- Function 3 ----------------
        public int fun3nt_fun3pr;   // function3 enter -\ function3 press
        public int fun3pr_fun3rl;   // function3 press -\ function3 release

        public int fun3rl_pnlex;   // function3 release -\ panel exit

        public int pnlex_endnt;   // panel exit -\ end enter
        public int endnt_endpr;   // end enter -\ end press
        public int endpr_endrl;   // end press -\ end release (trial end)
    }
}
