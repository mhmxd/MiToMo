using Common.Constants;
using Common.Helpers;
using Common.Settings;
using CommonUI;
using System.Collections.Generic;
using System.Linq;
using static Common.Constants.ExpEnums;

namespace SubTask.PanelNavigation
{
    public class Experiment
    {

        public static readonly int START_FONT_SIZE = 16;

        //--- Setting
        public Technique Active_Technique = Technique.TOMO; // Set in the intro dialog
        //public Complexity Active_Complexity = Complexity.Simple; // Set in the intro dialog
        public ExperimentType Active_Type = ExperimentType.Practice; // Set from the intro dialog

        private readonly List<Block> _blocks = new();
        public List<Block> Blocks { get { return _blocks; } }

        public Experiment()
        {
            //Participant_Number = DEFAULT_PTC; // Default
        }

        public void Init(ExperimentType expType)
        {
            this.TrialInfo($"Participant: {ExpEnvironment.PTC_NUM}");

            //Active_Complexity = complexity;
            Active_Type = expType;

            //-- For each complexity, create blocks and randomize them before adding to the list
            List<Complexity> randomizedComplexities = ExpEnums.GetRandomComplexityList();
            foreach (Complexity complexity in randomizedComplexities)
            {
                // Create blocks, then shuffle them before adding to the overall list
                List<Block> blocks = new List<Block>();
                for (int i = 0; i < ExpDesign.PaneNavNumBlocks; i++)
                {
                    int blockId = ExpEnvironment.PTC_NUM * 100 + i + 1;
                    blocks.Add(Block.CreateBlock(
                        Active_Technique, ExpEnvironment.PTC_NUM,
                        blockId, complexity, expType));

                }

                // Shuffle blocks inside the complexity
                blocks.Shuffle();
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

        public static int GetStartSize()
        {
            return UITools.MM2PX(ExpLayouts.START_BUTTON_IN_SIDE_MM.W);
        }

        public static int GetLargerStartSize()
        {
            return UITools.MM2PX(ExpLayouts.START_BUTTON_LARGE_SIDE_MM);
        }

        public static int GetStartHalfWidth()
        {
            return UITools.MM2PX(ExpLayouts.START_BUTTON_IN_SIDE_MM.W / 2);
        }
    }
}
