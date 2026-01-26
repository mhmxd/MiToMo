using Common.Constants;
using static Common.Constants.ExpEnums;

namespace Common.Settings
{
    public class ExpLayouts
    {
        //--- Button multiples -------------------------------------------
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

        //--- Grid -------------------------------------------------------
        public static double GRID_ROW_HEIGHT_MM = 6 * ExpSizes.GRID_UNIT_MM; // Height of the grid rows (= 24px)
        public static double GUTTER_SIDE_SIMPLE_MM = 4;
        public static double GRID_MAX_ELEMENT_WIDTH_MM = 45; // Width of the widest element in the grid
        public static double GRID_MIN_ELEMENT_WIDTH_MM = 3; // Width of the narrowest element in the grid

        public static readonly double MAX_GROUP_WIDTH_MM = 2 * BUTTON_MULTIPLES[ExpStrs.x18] + ExpSizes.GUTTER_05MM; // Max width of a group (mm)
        public static readonly double COLUMN_HEIGHT_MM = 3 * GRID_ROW_HEIGHT_MM + 2 * ExpSizes.GUTTER_05MM; // Height of a column (mm)
        public static double SIDE_COL_MAX_WIDTH_MM = 2 * BUTTON_MULTIPLES[ExpStrs.x15] + ExpSizes.GUTTER_05MM;

        public static int ELEMENT_BORDER_THICKNESS = 2; // Thickness of the border around the grid elements

        //--- Windows ----------------------------------------------------
        public static readonly double WINDOW_PADDING_MM = 4;
        public static double SIDE_WINDOW_WIDTH_MM = 2 * SIDE_COL_MAX_WIDTH_MM + 2 * WINDOW_PADDING_MM + ExpSizes.GUTTER_05MM;
        public static double TOP_WINDOW_HEIGTH_MM = 3 * GRID_ROW_HEIGHT_MM + 2 * ExpSizes.GUTTER_05MM + 2 * WINDOW_PADDING_MM;

        //-- Start Button ------------------------------------------------
        public static readonly double START_BUTTON_LARGE_SIDE_MM = 40;
        public static readonly (int W, int H) START_BUTTON_SMALL_DIM_MM = (20, 10);
        public static readonly (double W, double H) START_BUTTON_IN_SIDE_MM = (ExpSizes.EXCEL_CELL_W, ExpSizes.EXCEL_CELL_W);
        public static readonly double START_BUTTON_DIST_MM = 10;
        public static readonly int START_BUTTON_FONT_SIZE = 16;

        //--- Objects ----------------------------------------------------
        public static readonly double OBJ_WIDTH_MM = ExpSizes.EXCEL_CELL_W; // Width of the square objects (mm)

        //--- Object Area ------------------------------------------------
        public static readonly double OBJ_AREA_WIDTH_MM = OBJ_WIDTH_MM * 5; // Width of the *square* object area (mm)

        //--- Complexities ------------------------------------------------
        public static readonly string[] SimpleTopRow = {
            ExpStrs.x18, ExpStrs.x6, ExpStrs.x6, ExpStrs.x6,
            ExpStrs.x18, ExpStrs.x6, ExpStrs.x18, ExpStrs.x18,
            ExpStrs.x18, ExpStrs.x6, ExpStrs.x18, ExpStrs.x6,
            ExpStrs.x6,  ExpStrs.x18
        };

        public static readonly string[] ModerateTopRow1 = {
            ExpStrs.x18,
            ExpStrs.x6, ExpStrs.x3,   // Combo
            ExpStrs.x18, ExpStrs.x6, ExpStrs.x6,
            ExpStrs.x6, ExpStrs.x3,   // Combo
            ExpStrs.x18,
            ExpStrs.x6, ExpStrs.x3,   // Combo
            ExpStrs.x18, ExpStrs.x6,
            ExpStrs.x6, ExpStrs.x3,   // Combo
            ExpStrs.x6, ExpStrs.x18, ExpStrs.x6,
            ExpStrs.x6, ExpStrs.x3,   // Combo
            ExpStrs.x6, ExpStrs.x3,   // Combo
            ExpStrs.x6, ExpStrs.x18,
            ExpStrs.x6, ExpStrs.x3,   // Combo
            ExpStrs.x18,
            ExpStrs.x6                // Last item
        };

        public static readonly string[] ModerateTopRow2 = {
            ExpStrs.x6,
            ExpStrs.x6, ExpStrs.x3,  // Combo (X6 + Dropdown)
            ExpStrs.x18,
            ExpStrs.x6, ExpStrs.x3,  // Combo
            ExpStrs.x18, ExpStrs.x18, ExpStrs.x6,
            ExpStrs.x6, ExpStrs.x3,  // Combo
            ExpStrs.x18, ExpStrs.x6, ExpStrs.x6,
            ExpStrs.x6, ExpStrs.x3,  // Combo
            ExpStrs.x6, ExpStrs.x18, ExpStrs.x18,
            ExpStrs.x6, ExpStrs.x3,  // Combo
            ExpStrs.x6,
            ExpStrs.x6, ExpStrs.x3,  // Combo
            ExpStrs.x6, ExpStrs.x3,  // Combo
            ExpStrs.x6, ExpStrs.x18
        };

        // Column 1: A vertical stack of ten X6 buttons
        public static readonly string[] ModerateSideCol1 = {
            ExpStrs.x6, ExpStrs.x6, ExpStrs.x6, ExpStrs.x6, ExpStrs.x6,
            ExpStrs.x6, ExpStrs.x6, ExpStrs.x6, ExpStrs.x6, ExpStrs.x6
        };

        // Column 2: A vertical stack of ten X30 buttons
        public static readonly string[] ModerateSideCol2 = {
            ExpStrs.x30, ExpStrs.x30, ExpStrs.x30, ExpStrs.x30, ExpStrs.x30,
            ExpStrs.x30, ExpStrs.x30, ExpStrs.x30, ExpStrs.x30, ExpStrs.x30
        };

        public static readonly string[] ComplexTopRowType1 = {
            ExpStrs.x6,
            ExpStrs.x18, ExpStrs.x3
        };

        //-- Complex Side Group1
        public static readonly string[] ComplexSideGroup1Row1 = { ExpStrs.x30, ExpStrs.x18 };
        public static readonly string[] ComplexSideGroup1Row2 = { ExpStrs.x18, ExpStrs.x6, ExpStrs.x6, ExpStrs.x6, ExpStrs.x6, ExpStrs.x3 };
        public static readonly string[] ComplexSideGroup1Row3 = { ExpStrs.x6, ExpStrs.x6, ExpStrs.x18, ExpStrs.x3, ExpStrs.x18 };

        //-- Complex Side Group2
        public static readonly string[] ComplexSideGroup2Row1 = {
            ExpStrs.x6, ExpStrs.x6, ExpStrs.x6, ExpStrs.x6,
            ExpStrs.x18,
            ExpStrs.x6
        };

        public static readonly string[] ComplexSideGroup2Row2 = {
            ExpStrs.x6,
            ExpStrs.x6, ExpStrs.x3, // Combo (Automatic gutter skip)
            ExpStrs.x6,
            ExpStrs.x30
        };

        //-- Complex Side Group3
        public static readonly string[] ComplexSideGroup3Row1 = {
            ExpStrs.x6,
            ExpStrs.x6,
            ExpStrs.x6, ExpStrs.x3, // Combo: Button + Dropdown (No gutter between)
            ExpStrs.x18,
            ExpStrs.x6,
            ExpStrs.x6
        };

        //-- Complex Side Group4
        public static readonly string[] ComplexSideGroup4Row1 = { ExpStrs.x18, ExpStrs.x6, ExpStrs.x30 };
        public static readonly string[] ComplexSideGroup4Row2 = { ExpStrs.x6, ExpStrs.x18, ExpStrs.x6, ExpStrs.x18 };
        public static readonly string[] ComplexSideGroup4Row3 = { ExpStrs.x30, ExpStrs.x6, ExpStrs.x3, ExpStrs.x6, ExpStrs.x6 };

        //-- Complex Side Group5
        // Row 1: X6, X6, X18, then the Combo (X18 + X3 Dropdown)
        public static readonly string[] ComplexSideGroup5Row1 = {
            ExpStrs.x6,
            ExpStrs.x6,
            ExpStrs.x18,
            ExpStrs.x18, ExpStrs.x3
        };

        // Row 2: X6, then the Combo (X6 + X3 Dropdown), X6, X30
        public static readonly string[] ComplexSideGroup5Row2 = {
            ExpStrs.x6,
            ExpStrs.x6, ExpStrs.x3,
            ExpStrs.x6,
            ExpStrs.x30
        };

        //-- Complex Side Group6
        // Row 1: Combo (X6 + X3), X30, X6, X6
        public static readonly string[] ComplexSideGroup6Row1 = {
            ExpStrs.x6, ExpStrs.x3, // Combo (Gutter skipped automatically)
            ExpStrs.x30,
            ExpStrs.x6,
            ExpStrs.x6
        };
    }
}
