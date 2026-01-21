using Common.Constants;
using Common.Settings;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using static Common.Constants.ExpEnums;
using static Common.Helpers.ExpUtils;

namespace SubTask.FunctionSelection
{
    public class Experiment
    {

        //--- Setting
        public Technique Active_Technique = Technique.TOMO_TAP; // Set in the info dialog
        public Complexity Active_Complexity = Complexity.Simple; // Set in the info dialog

        //-- Distance Ranges
        private Range _shortDistRangeMM; // Short distances range (mm)
        private Range _midDistRangeMM; // Mid distances range (mm)
        private Range _longDistRangeMM; // Long distances range (mm)

        private double Dist_PADDING_MM = 2.5; // Padding to each side of the dist thresholds

        //-- Calculated
        public double Longest_Dist_MM;
        public double Shortest_Dist_MM;

        //-- Constants
        //public static double OBJ_WIDTH_MM = 5; // Apple Display Excel Cell H // In click experiment was 6mm
        //public static double OBJ_WIDTH_MM = Config.EXCEL_CELL_W;
        //public static double OBJ_AREA_WIDTH_MM = Config.EXCEL_CELL_W * 5; // Width of the *square* object area (mm)
        //public static double START_W_MM = Config.EXCEL_CELL_W;
        //public static double START_H_MM = Config.EXCEL_CELL_H;

        //-- Colors
        public static readonly Brush START_INIT_COLOR = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(ExpColors.PURPLE));

        //-- Information

        //public int Participant_Number { get; set; } // Set in the info dialog

        private List<Block> _blocks = new List<Block>();
        public List<Block> Blocks { get { return _blocks; } }

        public Experiment(double shortDistMM, double longDistMM)
        {
            //Participant_Number = DEFAULT_PTC; // Default
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

        public void Init(Complexity complexity, ExperimentType expType)
        {
            this.TrialInfo($"Participant: {ExpEnvironment.PTC_NUM}");

            Active_Complexity = complexity;

            // Create factor levels
            List<Range> distRanges = new List<Range>()
            {
                _shortDistRangeMM, // Short distances
                _midDistRangeMM,   // Mid distances
                _longDistRangeMM    // Long distances
            };

            // Create and add blocks
            for (int i = 0; i < ExpDesign.MFS_N_BLOCKS; i++)
            {
                int blockId = ExpEnvironment.PTC_NUM * 100 + i + 1;
                Block block = Block.CreateBlock(ExpEnvironment.PTC_NUM, blockId, complexity, expType, ExpDesign.FPS_N_FUN);
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

        public static int GetStartHalfWidth()
        {
            return MM2PX(ExpSizes.START_BUTTON_LARGER_SIDE_MM / 2);
        }
    }
}
