using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace SubTask.ObjectSelection.Logging
{
    internal class TrialLog : TopLog
    {
        public int ptc;         // participant number
        public int block;       // block number
        public int trial;       // trial number
        public int id;          // number
        public string tech;     // technique
        public string cmplx;    // complexity
        public int n_obj;       // number of objects
        public int result;      // hit (1), miss (0)

        public TrialLog(int blockNum, int trialNum, Trial trial, TrialRecord trialRecord)
        {
            this.ptc = trial.PtcNum;
            this.block = blockNum;
            this.trial = trialNum;
            this.id = trial.Id;
            this.tech = trial.Technique.ToString().ToLower();
            this.cmplx = trial.Complexity.ToString().ToLower();
            this.n_obj = trial.NObjects;
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
