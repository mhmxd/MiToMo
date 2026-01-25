using Common.Helpers;

namespace SubTask.PanelNavigation
{
    public class TrialEvent
    {
        public string Type; // e.g., "obj_enter", "fun_press", etc.
        public string Value; // e.g., object or function ID
        public long Time; // timestamp in milliseconds

        public TrialEvent(string type, string val)
        {
            this.Type = type;
            this.Value = val;
            this.Time = Timer.GetCurrentMillis();
        }

        public TrialEvent(string type, string val, long time)
        {
            this.Type = type;
            this.Value = val;
            this.Time = time;
        }

        public bool HasTypeVal(string type, string val)
        {
            return this.Type == type && this.Value == val;
        }

        public override string ToString()
        {
            if (Value == "")
            {
                return $"{Type}: {Time}";
            }
            else
            {
                return $"{Type}-{Value}: {Time}";
            }
        }

        public string GetTypeVal()
        {
            if (Value == "")
            {
                return $"{Type}";
            }
            else
            {
                return $"{Type}-{Value}";
            }
        }
    }
}
