using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Common.Constants;
using Common.Settings;
using static System.Math;
using static Common.Constants.ExpEnums;
using static Multi.Cursor.Output;

namespace Multi.Cursor
{
    public class Experiment
    {

        //--- Setting
        public Technique Active_Technique = Technique.TOMO_TAP; // Set in the info dialog
        public Complexity Active_Complexity = Complexity.Simple; // Set in the info dialog

        //--- Variables
        //private static List<double> TARGET_WIDTHS_MM = new List<double>() { 4, 12, 20 }; // BenQ
        ////private static List<double> TARGET_WIDTHS_MM = new List<double>() { 4, 9, 18 }; // Apple Display
        //private static List<double> GRID_TARGET_WIDTHS_MM = new List<double>() { 3, 12, 30}; // BenQ

        //private static List<double> _distances = new List<double>(); // Generated in constructor
        private Range _shortDistRangeMM; // Short distances range (mm)
        private Range _midDistRangeMM; // Mid distances range (mm)
        private Range _longDistRangeMM; // Long distances range (mm)

        //public static int REP_TRIAL_NUM_PASS = 5; // Trial ends on Start
        //public static double REP_TRIAL_MAX_DIST_STARTS_MM = Config.EXCEL_CELL_W; // Max distance between Starts in a repeating trial (mm)
        //public static double REP_TRIAL_OBJ_AREA_RADIUS_MM = Config.EXCEL_CELL_W; // Radius of the object area in repeating trials (mm)
        

        private double Dist_PADDING_MM = 2.5; // Padding to each side of the dist thresholds

        //public static double Min_Target_Width_MM = TARGET_WIDTHS_MM.Min();
        //public static double Max_Target_Width_MM = TARGET_WIDTHS_MM.Max();

        //-- Calculated
        public double Longest_Dist_MM;
        public double Shortest_Dist_MM;
        //private double LONGEST_DIST_MM = 293; // BenQ = 293 mm
        //private double SHORTEST_DIST_MM = 10; // BenQ = 10 mm

        //-- Constants
        //public static double OBJ_WIDTH_MM = 5; // Apple Display Excel Cell H // In click experiment was 6mm
        //public static double OBJ_WIDTH_MM = Config.EXCEL_CELL_W;
        //public static double OBJ_AREA_WIDTH_MM = Config.EXCEL_CELL_W * 5; // Width of the *square* object area (mm)
        //public static double START_WIDTH_MM = OBJ_WIDTH_MM;



        //-- Information

        //public int Participant_Number { get; set; } // Set in the info dialog
        
        private List<Block> _blocks = new List<Block>();
        public List<Block> Blocks { get { return _blocks; } }

        public Experiment(double shortDistMM, double longDistMM)
        {
            //Participant_Number = ExpPtc.PTC_NUM; // Default
            Shortest_Dist_MM = shortDistMM;
            Longest_Dist_MM = longDistMM;

            //--- Generate the distances
            double distDiff = Longest_Dist_MM - Shortest_Dist_MM;
            //Dist_PADDING_MM = 0.1 * distDiff;
            double oneThird = Shortest_Dist_MM + distDiff / 3;
            double twoThird = Shortest_Dist_MM + distDiff * 2 / 3;

            // Set the distRanges
            _shortDistRangeMM = new Range(Shortest_Dist_MM, oneThird - Dist_PADDING_MM, ExpStrs.SHORT_DIST); // Short distances range
            _midDistRangeMM = new Range(oneThird + Dist_PADDING_MM, twoThird - Dist_PADDING_MM, ExpStrs.MID_DIST); // Middle distances range (will be set later)
            _longDistRangeMM = new Range(twoThird + Dist_PADDING_MM, Longest_Dist_MM, ExpStrs.LONG_DIST); // Long distances range

            this.TrialInfo($"Short dist range (mm): {_shortDistRangeMM.ToString()}");
            this.TrialInfo($"Mid dist range (mm): {_midDistRangeMM.ToString()}");
            this.TrialInfo($"Long dist range (mm): {_longDistRangeMM.ToString()}");

        }

        public void Init(string tech, Complexity complexity)
        {
            this.TrialInfo($"Participant: {ExpPtc.PTC_NUM}, Technique: {tech}");
            //Participant_Number = ptc;
            if (tech == ExpStrs.TAP_C)
            {
                Active_Technique = Technique.TOMO_TAP;
                Config.SetMode(0);
            }
            else if (tech == ExpStrs.SWIPE_C)
            {
                Active_Technique = Technique.TOMO_SWIPE;
                Config.SetMode(1);
            }
            else if (tech == ExpStrs.MOUSE_C)
            {
                Active_Technique = Technique.MOUSE;
            }

            Active_Complexity = complexity;

            // Create factor levels
            List<Range> distRanges = new List<Range>()
            {
                _shortDistRangeMM, // Short distances
                _midDistRangeMM,   // Mid distances
                _longDistRangeMM    // Long distances
            };
            
            //List<int> targetMultiples = BUTTON_MULTIPLES.Values.ToList();
            // Create and add blocks
            for (int i = 0; i < ExpDesign.LT_N_BLOCKS; i++)
            {
                int blockId = ExpPtc.PTC_NUM * 100 + i + 1;
                Block block = Block.CreateBlock(
                    Active_Technique, ExpPtc.PTC_NUM, 
                    blockId, complexity, distRanges, 
                    ExpDesign.LT_N_FUN, ExpDesign.LT_N_OBJ);
                _blocks.Add(block);
            }

            //CreateAltBlocks(1, targetMultiples, distRanges);
            //CreateRepBlocks(1, targetMultiples, distRanges);
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

        public static int GetStartHalfWidth()
        {
            return Utils.MM2PX(ExpSizes.START_BUTTON_LARGER_SIDE_MM / 2);
        }
    }
}
