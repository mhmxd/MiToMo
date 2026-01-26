namespace Common.Constants
{
    public class ExpSizes
    {
        public const double EXCEL_CELL_W = 15;
        public const double EXCEL_CELL_H = 5;

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


        //--- Touch ------------------------------------------------------
        public static int LAST_TOUCH_COL = 14; // Total number of touch columns
        public static int LAST_LEFT_TOUCH_COL = 2; // Width of the left touch area (in columns)
        public static int LAST_RIGHT_TOUCH_COL = 2; // Width of the right touch area (in columns)
        public static int LAST_TOP_TOUCH_ROW = 5; // Width of the top touch area (in rows)
    }
}
