using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panel.Select
{
    public class TrialEvent
    {
        public string Type; // e.g., "obj_enter", "fun_press", etc.
        public string Id; // e.g., object or function ID
        public long Time; // timestamp in milliseconds

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

        public bool HasTypeId(string type, string id)
        {
            return this.Type == type && this.Id == id;
        }

        public bool HasTypeId(string type, int id)
        {
            return this.Type == type && this.Id == id.ToString();
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

        public string GetTypeId()
        {
            if (Id == "")
            {
                return $"{Type}";
            }
            else
            {
                return $"{Type}-{Id}";
            }
        }
    }
}
