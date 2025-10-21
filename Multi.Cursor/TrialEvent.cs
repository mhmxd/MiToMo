using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multi.Cursor
{
    public class TrialEvent
    {
        public string Type; // e.g., "obj_enter", "fun_press", etc.
        public string Id; // e.g., object or function ID
        public long Time; // Timestamp in milliseconds

        public TrialEvent(string type, string id)
        {
            this.Type = type;
            this.Id = id;
            this.Time = Timer.GetCurrentMillis();
        }

        public TrialEvent(string type, string id, long time)
        {
            this.Type = type;
            this.Id = id;
            this.Time = time;
        }

        public override string ToString()
        {
            if (Id == "")
            {
                return $"{Type}: {Time}";
            }
            else
            {
                return $"{Type}-{Id}: {Time}";
            }
        }
    }
}
