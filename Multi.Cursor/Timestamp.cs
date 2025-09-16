using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multi.Cursor
{
    public class TimeStamp
    {
        public string label;
        public long time;

        public TimeStamp(string label)
        {
            this.label = label;
            this.time = Timer.GetCurrentMillis();
        }

        public TimeStamp(string label, long time)
        {
            this.label = label;
            this.time = time;
        }

        public string ToString()
        {
            return $"{label}: {time} ms";
        }
    }
}
