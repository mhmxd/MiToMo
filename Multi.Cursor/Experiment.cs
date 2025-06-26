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

        private static List<double> _distances = new List<double>(); // Generated in constructor
        private Range _shortDistRangeMM; // Short distances range (mm)
        private Range _midDistRangeMM; // Mid distances range (mm)
        private Range _longDistRangeMM; // Long distances range (mm)


        private static int N_BLOCKS = 3; // Number of blocks in the experiment
        private static int REP_TRIALS_NUM_PASSES = 3; // Must be even number (trial ends on Start)
        public static double REP_TRIAL_MAX_DIST_STARTS_MM = 30; // Max distance between Starts in a repeating trial (mm)

        private double _distPaddingMM; // Padding to each side of the dist thresholds

        public static double Min_Target_Width_MM = TARGET_WIDTHS_MM.Min();
        public static double Max_Target_Width_MM = TARGET_WIDTHS_MM.Max();

        //-- Calculated
        public double Longest_Dist_MM;
        public double Shortest_Dist_MM;
        //private double LONGEST_DIST_MM = 293; // BenQ = 293 mm
        //private double SHORTEST_DIST_MM = 10; // BenQ = 10 mm

        public enum Technique { Auxursor_Swipe, Auxursor_Tap, Radiusor, Mouse }

        //-- Constants
        public static double START_WIDTH_MM = 5; // Apple Display Excel Cell H // In click experiment was 6mm

        //-- Current state
        
        public enum Result { MISS, HIT, NO_START }

        //-- Information
        public Technique Active_Technique = Technique.Auxursor_Tap; // Set in the info dialog
        public int Participant_Number { get; set; } // Set in the info dialog
        
        private List<Block> _blocks = new List<Block>();
        public List<Block> Blocks { get { return _blocks; } }


        public Experiment(double shortDistMM, double longDistMM)
        {
            this.TrialInfo($"Experiment dist range (px): {Utils.MmToDips(shortDistMM):F2} - {Utils.MmToDips(longDistMM):F2}");
            Participant_Number = 100; // Default
            Shortest_Dist_MM = shortDistMM;
            Longest_Dist_MM = longDistMM;

            //--- Generate the distances
            double distDiff = Longest_Dist_MM - Shortest_Dist_MM;
            //_distPaddingMM = 0.1 * distDiff;
            _distPaddingMM = 2.5; // 2.5 mm padding
            double oneThird = Shortest_Dist_MM + distDiff / 3;
            double twoThird = Shortest_Dist_MM + distDiff * 2 / 3;

            // Set the distRanges
            _shortDistRangeMM = new Range(Shortest_Dist_MM, oneThird - _distPaddingMM); // Short distances range
            _midDistRangeMM = new Range(oneThird + _distPaddingMM, twoThird - _distPaddingMM); // Middle distances range (will be set later)
            _longDistRangeMM = new Range(twoThird + _distPaddingMM, Longest_Dist_MM); // Long distances range

            // Find random distances in the distRanges
            double midDist = Utils.RandDouble(oneThird + _distPaddingMM, twoThird - _distPaddingMM); // Middle distance
            double shortDist = Utils.RandDouble(Shortest_Dist_MM, oneThird - _distPaddingMM); // Shortest distance
            double longDist = Utils.RandDouble(twoThird + _distPaddingMM, Longest_Dist_MM); // Longest distance

            _distances.Add(shortDist);
            _distances.Add(midDist);
            _distances.Add(longDist);
            this.TrialInfo($"Experiment distances (mm): {ListToString(_distances)}");
            //for (int i = 0; i < N_BLOCKS; i++)
            //{
            //    int blockId = Participant_Number * 100 + i;
            //    //Block block = new Block(blockId, TARGET_WIDTHS_MM, _distances, N_REPS_IN_BLOCK);
            //    Block block = new Block(BLOCK_TYPE.ALTERNATING, blockId, _distances, N_REPS_IN_BLOCK);
            //    _blocks.Add(block);
            //}

            //-- Init
            //_activeBlockNum = 1;
            //_activeBlockInd = 0;
            //_activeTrialNum = 1;
            //_activeTrialInd = 0;
            //_activeBlock = _blocks[0];

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

            //CreateAltBlocks(3, targetMultiples, distRanges);
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
                Block block = Block.CreateRepBlock(blockId, targetMultiples, distRanges, REP_TRIALS_NUM_PASSES);
                this.TrialInfo($"Created block #{block.Id} with {block.Trials.Count} trials.");
                _blocks.Add(block);
            }
        }

        public int GetNumBlocks()
        {
            return N_BLOCKS;
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

        public bool IsTechRadiusor()
        {
            return Active_Technique == Technique.Radiusor;
        }

        public static double GetMinTargetWidthMM()
        {
            return TARGET_WIDTHS_MM.First();
        }

        private string ListToString<T>(List<T> list)
        {
            return "{" + string.Join(", ", list) + "}";
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
    }
}
