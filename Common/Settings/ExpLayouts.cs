using Common.Constants;

namespace Common.Settings
{
    public class ExpLayouts
    {
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
