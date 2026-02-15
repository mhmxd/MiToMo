using Common.Constants;
using Common.Helpers;
using Common.Settings;
using CommonUI;
using System.Collections.Generic;
using System.Linq;
using static Common.Constants.ExpEnums;

namespace Multi.Cursor
{
    public class Experiment
    {

        public static readonly int START_FONT_SIZE = 16; // For instructions and feedback

        //--- Setting
        public Technique Active_Technique = Technique.TOMO_TAP; // Set in the info dialog
        //public Complexity Active_Complexity = Complexity.Simple; // Set in the info dialog

        //private static List<double> _distances = new List<double>(); // Generated in constructor
        private MRange _shortDistRangeMM; // Short distances range (mm)
        private MRange _midDistRangeMM; // Mid distances range (mm)
        private MRange _longDistRangeMM; // Long distances range (mm)


        private double Dist_PADDING_MM = 2.5; // Padding to each side of the dist thresholds

        //-- Calculated
        public double Longest_Dist_MM;
        public double Shortest_Dist_MM;

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
            _shortDistRangeMM = new MRange(Shortest_Dist_MM, oneThird - Dist_PADDING_MM, ExpStrs.SHORT_DIST); // Short distances range
            _midDistRangeMM = new MRange(oneThird + Dist_PADDING_MM, twoThird - Dist_PADDING_MM, ExpStrs.MID_DIST); // Middle distances range (will be set later)
            _longDistRangeMM = new MRange(twoThird + Dist_PADDING_MM, Longest_Dist_MM, ExpStrs.LONG_DIST); // Long distances range

            this.TrialInfo($"Short dist range (mm): {_shortDistRangeMM.ToString()}");
            this.TrialInfo($"Mid dist range (mm): {_midDistRangeMM.ToString()}");
            this.TrialInfo($"Long dist range (mm): {_longDistRangeMM.ToString()}");

        }

        public void Init(string tech, TaskType taskType, ExperimentType expType)
        {
            //this.TrialInfo($"Participant: {ExpEnvironment.PTC_NUM}, Technique: {tech}");

            if (tech == ExpStrs.TAP_C)
            {
                Active_Technique = Technique.TOMO_TAP;
            }
            else if (tech == ExpStrs.SWIPE_C)
            {
                Active_Technique = Technique.TOMO_SWIPE;
            }
            else if (tech == ExpStrs.MOUSE_C)
            {
                Active_Technique = Technique.MOUSE;
            }

            // Set number of objects and functions based on task type
            int nObj = taskType == TaskType.MULTI_OBJ_ONE_FUNC ? ExpDesign.MainTaskNumObj : 1;
            int nFun = taskType == TaskType.ONE_OBJ_MULTI_FUNC ? ExpDesign.MainTaskNumFunc : 1;

            //Active_Complexity = complexity;

            // Create factor levels
            List<MRange> distRanges = new List<MRange>()
            {
                _shortDistRangeMM, // Short distances
                _midDistRangeMM,   // Mid distances
                _longDistRangeMM    // Long distances
            };

            //-- For each complexity, create blocks and add to the list
            List<Complexity> randomizedComplexities = ExpEnums.GetRandomComplexityList();
            foreach (Complexity complexity in randomizedComplexities)
            {
                for (int i = 0; i < ExpDesign.MainTaskNumBlocks; i++)
                {
                    int blockId = ExpEnvironment.PTC_NUM * 100 + i + 1;
                    Block block = Block.CreateBlock(
                        Active_Technique, ExpEnvironment.PTC_NUM,
                        blockId, complexity,
                        expType, distRanges,
                        nFun, nObj);

                    _blocks.Add(block);
                }
            }
        }

        public int GetNumBlocks()
        {
            return _blocks.Count;
        }

        public Block GetBlockByNum(int blockNum)
        {
            int index = blockNum - 1;
            if (index < _blocks.Count()) return _blocks[index];
            else return null;
        }

        public static int GetStartHalfWidth()
        {
            return UITools.MM2PX(ExpLayouts.START_BUTTON_LARGE_SIDE_MM / 2);
        }

        public static int GetObjWidth()
        {
            return UITools.MM2PX(ExpLayouts.OBJ_WIDTH_MM);
        }

        public static int GetObjHalfWidth()
        {
            return UITools.MM2PX(ExpLayouts.OBJ_WIDTH_MM / 2);
        }

        public static int GetObjAreaWidth()
        {
            return UITools.MM2PX(ExpLayouts.OBJ_AREA_WIDTH_MM);
        }

        public static int GetObjAreaHalfWidth()
        {
            return UITools.MM2PX(ExpLayouts.OBJ_AREA_WIDTH_MM / 2);
        }

    }
}
