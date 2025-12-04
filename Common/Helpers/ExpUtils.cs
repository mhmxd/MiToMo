using Common.Constants;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Helpers
{
    public class ExpUtils
    {
        private const double MM_IN_INCH = 25.4;

        public static double PX2MM(double px)
        {
            return px * MM_IN_INCH / ExpSizes.PPI;
        }

        public static int MM2PX(double mm)
        {
            return (int)Math.Round(mm / MM_IN_INCH * ExpSizes.PPI);
        }
    }
}
