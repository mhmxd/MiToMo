using Common.Constants;
using Common.Helpers;
using Common.Settings;
using CommonUI;
using System.Collections.Generic;
using System.Linq;
using static Common.Constants.ExpEnums;

namespace SubTask.FunctionSelection
{
    public class Experiment
    {
        public static readonly int START_FONT_SIZE = 18; // Font size for the start button (px)

        //--- Setting
        public Technique Active_Technique = Technique.MOUSE; // Set in the info dialog
        public Complexity Active_Complexity = Complexity.Simple; // Set in the info dialog
        public ExperimentType Active_Type = ExperimentType.Practice; // Set from the intro dialog

        //-- Distance Ranges
        private MRange _shortDistRangeMM; // Short distances range (mm)
        private MRange _midDistRangeMM; // Mid distances range (mm)
        private MRange _longDistRangeMM; // Long distances range (mm)

        private double Dist_PADDING_MM = 2.5; // Padding to each side of the dist thresholds

        //-- Calculated
        public double Longest_Dist_MM;
        public double Shortest_Dist_MM;

        //-- Information
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
            _shortDistRangeMM = new MRange(Shortest_Dist_MM, oneThird - Dist_PADDING_MM, ExpStrs.SHORT_DIST); // Short distances range
            _midDistRangeMM = new MRange(oneThird + Dist_PADDING_MM, twoThird - Dist_PADDING_MM, ExpStrs.MID_DIST); // Middle distances range (will be set later)
            _longDistRangeMM = new MRange(twoThird + Dist_PADDING_MM, Longest_Dist_MM, ExpStrs.LONG_DIST); // Long distances range

            this.TrialInfo($"Short dist range (mm): {_shortDistRangeMM.ToString()}");
            this.TrialInfo($"Mid dist range (mm): {_midDistRangeMM.ToString()}");
            this.TrialInfo($"Long dist range (mm): {_longDistRangeMM.ToString()}");

        }

        public void Init(ExperimentType expType)
        {
            Active_Type = expType;

            //-- For each complexity, create blocks and randomize them before adding to the list
            List<Complexity> randomizedComplexities = ExpEnums.GetRandomComplexityList();
            foreach (Complexity complexity in randomizedComplexities)
            {
                for (int i = 0; i < ExpDesign.MultiFuncSelectNumBlocks; i++)
                {
                    int blockId = ExpEnvironment.PTC_NUM * 100 + i + 1;
                    _blocks.Add(Block.CreateBlock(
                        ExpEnvironment.PTC_NUM,
                        blockId, complexity, expType,
                        ExpDesign.MultiFuncSelectNumFunc));

                }
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

        public static int GetStartButtonWidth()
        {
            return UITools.MM2PX(ExpLayouts.OBJ_AREA_WIDTH_MM);
        }

        public static int GetStartHalfWidth()
        {
            return UITools.MM2PX(ExpLayouts.OBJ_AREA_WIDTH_MM / 2);
        }
    }
}
