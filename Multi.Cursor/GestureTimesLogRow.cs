using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace Multi.Cursor
{
    internal class GestureTimesLogRow
    {
        // Trial info
        public int TrialId { get; set; }
        public double TargetWidthMM { get; set; }
        public double DistanceMM { get; set; }
        public Side TargetLocation { get; set; }

    }
}
