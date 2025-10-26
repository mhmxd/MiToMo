using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace Multi.Cursor.Logging
{
    internal class TrialLog : TopLog
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
        public int result;      // hit (1), miss (0)

        public TrialLog(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            this.ptc = trial.PtcNum;
            this.block = blockNum;
            this.trial = trialNum;
            this.id = trial.Id;
            this.tech = trial.Technique.ToString().ToLower();
            this.cmplx = trial.Complexity.ToString().ToLower();
            this.tsk_type = Str.TASKTYPE_ABBR[trial.TaskType];
            this.fun_side = trial.FuncSide.ToString().ToLower();
            this.func_width = trial.GetFunctionWidthMM();
            this.n_obj = trial.NObjects;
            this.n_fun = trial.GetNumFunctions();
            this.dist_lvl = trial.DistRangeMM.Label.Split('-')[0].ToLower();
            this.dist = $"{Utils.PX2MM(trialRecord.Distance):F2}";
            this.result = (int)trialRecord.Result;
        }

        public override string ToString()
        {
            // Use reflection to get the values of all fields declared in this class.
            // We use BindingFlags.DeclaredOnly to avoid printing base class (TrialLog) fields
            // if the base class also has many fields, but we include Public and NonPublic fields
            // to be comprehensive. If you only want Public fields, use BindingFlags.Public.

            Type type = this.GetType();

            // Get all instance fields (public, protected, private) declared in THIS class ONLY.
            FieldInfo[] fields = type.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly
            );

            // If you need inherited fields as well, you would use:
            // FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            StringBuilder sb = new StringBuilder();

            // 1. Add a header for context
            sb.AppendLine($"--- {type.Name} Data ---");

            // 2. Optional: Include base class ToString() if base.ToString() provides useful info
            // sb.AppendLine(base.ToString()); 
            //sb.AppendLine();

            // 3. Append the name and value for each field
            foreach (FieldInfo field in fields.OrderBy(f => f.MetadataToken))
            {
                // Get the field's current value on this instance
                object value = field.GetValue(this);

                // Format the output as "FIELD_NAME: VALUE"
                sb.AppendLine($"{field.Name}: {value}");
            }

            sb.AppendLine("------------------------");

            return sb.ToString();
        }
    }
}
