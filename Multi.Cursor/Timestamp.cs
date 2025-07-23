using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multi.Cursor
{
    public class Timestamp
    {
        public string label;
        public long time;

        public Timestamp(string label)
        {
            this.label = label;
            this.time = Timer.GetCurrentMillis();
        }
    }
}
