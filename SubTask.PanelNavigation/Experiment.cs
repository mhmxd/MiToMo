using Common.Settings;
using CommonUI;
using System.Collections.Generic;
using System.Linq;
using static Common.Constants.ExpEnums;

namespace SubTask.PanelNavigation
{
    public class Experiment
    {

        //--- Setting
        public Technique Active_Technique = Technique.TOMO; // Set in the intro dialog
        public Complexity Active_Complexity = Complexity.Simple; // Set in the intro dialog
        public ExperimentType Active_Type = ExperimentType.Practice; // Set from the intro dialog

        private readonly List<Block> _blocks = new();
        public List<Block> Blocks { get { return _blocks; } }

        public Experiment()
        {
            //Participant_Number = DEFAULT_PTC; // Default
        }

        public void Init(Complexity complexity, ExperimentType expType)
        {
            this.TrialInfo($"Participant: {ExpEnvironment.PTC_NUM}");

            Active_Complexity = complexity;
            Active_Type = expType;

            // Create and add blocks
            for (int i = 0; i < ExpDesign.PN_N_BLOCKS; i++)
            {
                int blockId = ExpEnvironment.PTC_NUM * 100 + i + 1;
                Block block = Block.CreateBlock(
                    Active_Technique, ExpEnvironment.PTC_NUM,
                    blockId, complexity, expType,
                    ExpDesign.PN_N_REP);
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

        public static int GetStartSize()
        {
            return UITools.MM2PX(ExpLayouts.START_BUTTON_IN_SIDE_MM.W);
        }

        public static int GetStartHalfWidth()
        {
            return UITools.MM2PX(ExpLayouts.START_BUTTON_IN_SIDE_MM.W / 2);
        }
    }
}
