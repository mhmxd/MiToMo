using Common.Helpers;

namespace SubTask.ObjectSelection
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
            this.Time = MTimer.GetCurrentMillis();
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

        public bool HasTypeAndId(string type, int id)
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

        public string ToLogString()
        {
            return $"{Time};{Type};{Id}";
        }

        public static string GetHeader()
        {
            return "timestamp;type;id";
        }
    }
}
