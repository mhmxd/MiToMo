using System;
using System.Collections.Generic;
using System.Text;
using static Common.Constants.ExpEnums;

namespace Common.Constants
{
    public class ExpSizes
    {
        private const double EXCEL_CELL_W = 15;
        private const double EXCEL_CELL_H = 5;

        //--- Grid -------------------------------------------------------
        public static double GUTTER_05MM = 0.5; // Space in-between the grid elements within a group
        public static double GRID_UNIT_MM = 1; // Unit of measurement for the grid (1mm = 4px)
        
        
        public static Dictionary<string, int> BUTTON_MULTIPLES = new Dictionary<string, int>()
        {
            { ExpStrs.x3, 3 },
            { ExpStrs.x6, 6 },
            { ExpStrs.x12, 12 },
            { ExpStrs.x15, 15 },
            { ExpStrs.x18, 18 },
            { ExpStrs.x30, 30 },
            { ExpStrs.x36, 36 }
        };

        public static Dictionary<Complexity, Dictionary<Side, List<int>>> BUTTON_WIDTHS = new Dictionary<Complexity, Dictionary<Side, List<int>>>()
        {
            {
                Complexity.Simple, new Dictionary<Side, List<int>>()
                {
                    { Side.Top, new List<int>() { 6, 18 } },
                    { Side.Left, new List<int>() { 36 } },
                    { Side.Right, new List<int>() { 36 } }
                }
            },

            {
                Complexity.Moderate, new Dictionary<Side, List<int>>()
                {
                    { Side.Top, new List<int>() { 3, 6, 18 } },
                    { Side.Left, new List<int>() { 6, 30 } },
                    { Side.Right, new List<int>() { 6, 30 } }
                }
            },
            {
                Complexity.Complex, new Dictionary<Side, List<int>>()
                {
                    { Side.Top, new List<int>() { 3, 6, 18, 30 } },
                    { Side.Left, new List<int>() { 3, 6, 18, 30 } },
                    { Side.Right, new List<int>() { 3, 6, 18, 30 } }
                }
            }
        };

       

        //--- START button -----------------------------------------------
        public static readonly double START_BUTTON_LARGER_SIDE_MM = 40;
        public static readonly double START_BUTTON_DIST_MM = 10;
        public static readonly double START_BUTTON_SMALL_H_MM = 10;

        //--- Objects ----------------------------------------------------
        public static readonly double OBJ_WIDTH_MM = EXCEL_CELL_W; // Width of the square objects (mm)

        //--- Object Area ------------------------------------------------
        public static readonly double OBJ_AREA_WIDTH_MM = OBJ_WIDTH_MM * 5; // Width of the *square* object area (mm)

        //--- Touch ------------------------------------------------------
        public static int LAST_TOUCH_COL = 14; // Total number of touch columns
        public static int LAST_LEFT_TOUCH_COL = 2; // Width of the left touch area (in columns)
        public static int LAST_RIGHT_TOUCH_COL = 2; // Width of the right touch area (in columns)
        public static int LAST_TOP_TOUCH_ROW = 5; // Width of the top touch area (in rows)
    }
}
