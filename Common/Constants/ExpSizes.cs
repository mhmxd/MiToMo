using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Constants
{
    public class ExpSizes
    {
        public static readonly double PPI = 109; // BenQ = 93.54; Apple = 109
        private const double EXCEL_CELL_W = 15;
        private const double EXCEL_CELL_H = 5;

        //--- START button -----------------------------------------------
        public static readonly double START_BUTTON_LARGER_SIDE_MM = 40;
        public static readonly double START_BUTTON_DIST_MM = 10;
        public static readonly (double W, double H) START_BUTTON_SMALL_MM = (40, 10);

        //--- Object Area ------------------------------------------------
        public static readonly double OBJ_AREA_WIDTH_MM = EXCEL_CELL_W * 5; // Width of the *square* object area (mm)

        //--- Objects ----------------------------------------------------
        public static readonly double OBJ_WIDTH_MM = EXCEL_CELL_W; // Width of the square objects (mm)
    }
}
