using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Math;
using static Multi.Cursor.Output;

namespace Multi.Cursor
{
    public class Experiment
    {
        //-- Variables
        private static List<double> TARGET_WIDTHS_MM = new List<double>() { 4, 12, 20 }; // BenQ
        //private static List<double> TARGET_WIDTHS_MM = new List<double>() { 4, 9, 18 }; // Apple Display
        private static List<double> GRID_TARGET_WIDTHS_MM = new List<double>() { 3, 12, 30}; // BenQ
        //public static List<int> SIDE_BUTTONS_WIDTH_MULTIPLES = new List<int>() { 3, 6, 18, 30, 52 }; // Multiples of the UNIT (1mm = 4px) widths for grid
        //public static List<int> TOP_BUTTONS_WIDTH_MULTIPLES = new List<int>() { 3, 6, 15, 18, 30 }; // Multiples of the UNIT (1mm = 4px) widths for top buttons
        public static Dictionary<string, int> BUTTON_MULTIPLES = new Dictionary<string, int>()
        {
            { Str.x3, 3 },
            { Str.x6, 6 },
            { Str.x15, 15 },
            { Str.x18, 18 },
            { Str.x30, 30 }
        };

        //private static List<double> _distances = new List<double>(); // Generated in constructor
        private Range _shortDistRangeMM; // Short distances range (mm)
        private Range _midDistRangeMM; // Mid distances range (mm)
        private Range _longDistRangeMM; // Long distances range (mm)


        private static int N_BLOCKS = 3; // Number of blocks in the experiment

        public static int REP_TRIAL_NUM_PASS = 5; // Trial ends on Start
        public static double REP_TRIAL_MAX_DIST_STARTS_MM = Config.EXCEL_CELL_W; // Max distance between Starts in a repeating trial (mm)
        public static double REP_TRIAL_OBJ_AREA_RADIUS_MM = Config.EXCEL_CELL_W; // Radius of the object area in repeating trials (mm)
        public static double OBJ_AREA_WIDTH_MM = Config.EXCEL_CELL_W * 2; // Width of the *square* object area (mm)

        private double Dist_PADDING_MM = 2.5; // Padding to each side of the dist thresholds

        public static double Min_Target_Width_MM = TARGET_WIDTHS_MM.Min();
        public static double Max_Target_Width_MM = TARGET_WIDTHS_MM.Max();

        //-- Calculated
        public double Longest_Dist_MM;
        public double Shortest_Dist_MM;
        //private double LONGEST_DIST_MM = 293; // BenQ = 293 mm
        //private double SHORTEST_DIST_MM = 10; // BenQ = 10 mm

        public enum Technique { Auxursor_Swipe, Auxursor_Tap, Mouse }

        //-- Constants
        public static double OBJ_WIDTH_MM = 5; // Apple Display Excel Cell H // In click experiment was 6mm

        //-- Current state
        
        public enum Result { MISS, HIT, OBJ_NOT_CLICKED }

        //-- Information
        public Technique Active_Technique = Technique.Auxursor_Tap; // Set in the info dialog
        public int Participant_Number { get; set; } // Set in the info dialog
        
        private List<Block> _blocks = new List<Block>();
        public List<Block> Blocks { get { return _blocks; } }

        public Experiment(double shortDistMM, double longDistMM)
        {
            Participant_Number = 100; // Default
            Shortest_Dist_MM = shortDistMM;
            Longest_Dist_MM = longDistMM;

            //--- Generate the distances
            double distDiff = Longest_Dist_MM - Shortest_Dist_MM;
            //Dist_PADDING_MM = 0.1 * distDiff;
            double oneThird = Shortest_Dist_MM + distDiff / 3;
            double twoThird = Shortest_Dist_MM + distDiff * 2 / 3;

            // Set the distRanges
            _shortDistRangeMM = new Range(Shortest_Dist_MM, oneThird - Dist_PADDING_MM, Str.SHORT); // Short distances range
            _midDistRangeMM = new Range(oneThird + Dist_PADDING_MM, twoThird - Dist_PADDING_MM, Str.MID); // Middle distances range (will be set later)
            _longDistRangeMM = new Range(twoThird + Dist_PADDING_MM, Longest_Dist_MM, Str.LONG); // Long distances range

            this.TrialInfo($"Short dist range (mm): {_shortDistRangeMM.ToString()}");
            this.TrialInfo($"Mid dist range (mm): {_midDistRangeMM.ToString()}");
            this.TrialInfo($"Long dist range (mm): {_longDistRangeMM.ToString()}");

        }

        public void Init(int ptc, string tech)
        {
            this.TrialInfo($"Participant: {ptc}, Technique: {tech}");
            Participant_Number = ptc;
            if (tech == Str.TOUCH_MOUSE_TAP)
            {
                Active_Technique = Technique.Auxursor_Tap;
                Config.SetMode(0);
            }
            else if (tech == Str.TOUCH_MOUSE_SWIPE)
            {
                Active_Technique = Technique.Auxursor_Swipe;
                Config.SetMode(1);
            }
            else if (tech == Str.MOUSE)
            {
                Active_Technique = Technique.Mouse;
            }

            // Create repeting blocks
            List<Range> distRanges = new List<Range>()
            {
                _shortDistRangeMM, // Short distances
                _midDistRangeMM,   // Mid distances
                _longDistRangeMM    // Long distances
            };
            List<int> targetMultiples = BUTTON_MULTIPLES.Values.ToList();

            //CreateAltBlocks(1, targetMultiples, distRanges);
            CreateRepBlocks(1, targetMultiples, distRanges);
        }

        private void CreateAltBlocks(int n, List<int> targetMultiples, List<Range> distRanges)
        {
            // Create n alternating blocks
            for (int i = 0; i < n; i++)
            {
                int blockId = Participant_Number * 100 + i + 1;
                Block block = Block.CreateAltBlock(blockId, targetMultiples, distRanges);
                this.TrialInfo($"Created block #{block.Id} with {block.Trials.Count} trials.");
                _blocks.Add(block);
            }

        }

        public void CreateRepBlocks(int n, List<int> targetMultiples, List<Range> distRanges)
        {
            // Create n repeating blocks
            for (int i = 0; i < n; i++)
            {
                int blockId = Participant_Number * 100 + i + 1;
                Block block = Block.CreateRepBlock(blockId, targetMultiples, distRanges, REP_TRIAL_NUM_PASS);
                this.TrialInfo($"Created block #{block.Id} with {block.Trials.Count} trials.");
                _blocks.Add(block);
            }
        }

        public int GetNumBlocks()
        {
            return _blocks.Count;
        }

        public Block GetBlock(int blockNum)
        {
            int index = blockNum - 1;
            if (index < _blocks.Count()) return _blocks[index];
            else return null;
        }

        public bool IsTechAuxCursor()
        {
            return Active_Technique == Technique.Auxursor_Swipe ||
                Active_Technique == Technique.Auxursor_Tap;
        }

        public static double GetMinTargetWidthMM()
        {
            return TARGET_WIDTHS_MM.First();
        }

        public static int GetNumGridTargetWidths()
        {
            return GRID_TARGET_WIDTHS_MM.Count;
        }

        public static List<double> GetGridTargetWidthsMM()
        {
            return GRID_TARGET_WIDTHS_MM;
        }

        public static double GetGridMinTargetWidthMM()
        {
            return GRID_TARGET_WIDTHS_MM.Min();
        }

        public static int GetStartHalfWidth()
        {
            return Utils.MM2PX(OBJ_WIDTH_MM / 2);
        }
    }
}
