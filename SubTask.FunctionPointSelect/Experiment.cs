using Common.Constants;
using Common.Helpers;
using Common.Settings;
using CommonUI;
using System;
using System.Collections.Generic;
using System.Linq;
using static Common.Constants.ExpEnums;

namespace SubTask.FunctionPointSelect
{
    public class Experiment
    {

        public static int START_FONT_SIZE = 12; // Font size for the start button (px)

        //--- Setting
        public Complexity Active_Complexity = Complexity.Simple; // Set in the info dialog

        //private static List<double> _distances = new List<double>(); // Generated in constructor
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

            this.PositionInfo($"Short dist range (mm): {_shortDistRangeMM}");
            this.PositionInfo($"Mid dist range (mm): {_midDistRangeMM}");
            this.PositionInfo($"Long dist range (mm): {_longDistRangeMM}");

        }

        public void Init(ExperimentType expType)
        {
            // Create factor levels
            List<MRange> distRanges = new()
            {
                _shortDistRangeMM, // Short distances
                _midDistRangeMM,   // Mid distances
                _longDistRangeMM    // Long distances
            };

            //-- For each complexity, create blocks and randomize them before adding to the list
            List<Complexity> randomizedComplexities = ExpEnums.GetRandomComplexityList();
            foreach (Complexity complexity in randomizedComplexities)
            {
                // Create blocks, then shuffle them before adding to the overall list
                List<Block> blocks = new();
                for (int i = 0; i < ExpDesign.FuncPointSelectNumBlocks; i++)
                {
                    int blockId = ExpEnvironment.PTC_NUM * 100 + i + 1;
                    blocks.Add(Block.CreateBlock(
                        ExpEnvironment.PTC_NUM,
                        blockId, complexity, expType,
                        distRanges));

                }

                // No need to shuffle blocks because they are already randomized by complexity and trials inside each block are shuffled
                //blocks.Shuffle(); 
                _blocks.AddRange(blocks);
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

        public static double GetStartWidthMM()
        {
            return ExpLayouts.OBJ_WIDTH_MM;
        }

        public static int GetStartWidth()
        {
            return UITools.MM2PX(ExpLayouts.OBJ_WIDTH_MM);
        }

        public static int GetStartHalfWidth()
        {
            return UITools.MM2PX(ExpLayouts.OBJ_WIDTH_MM / 2);
        }
    }
}
