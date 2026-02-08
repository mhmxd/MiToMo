using Common.Settings;
using System.Collections.Generic;
using System.Linq;
using static Common.Constants.ExpEnums;

namespace SubTask.ObjectSelection
{
    public class Experiment
    {

        public static readonly int START_FONT_SIZE = 16; // For instructions and feedback

        //--- Setting
        public Technique Active_Technique = Technique.MOUSE; // Set in the info dialog

        //-- Information
        private List<Block> _blocks = new List<Block>();
        public List<Block> Blocks { get { return _blocks; } }

        public Experiment()
        {
            //Participant_Number = DEFAULT_PTC; // Default
        }

        public void Init(ExperimentType expType)
        {

            // Create and add blocks
            for (int i = 0; i < ExpDesign.ObjectSelectNumBlocks; i++)
            {
                int blockId = ExpEnvironment.PTC_NUM * 100 + i + 1;
                Block block = Block.CreateBlock(ExpEnvironment.PTC_NUM, blockId, expType, ExpDesign.ObjectSelectNumRep);
                _blocks.Add(block);
            }

            // No need to shuffle: 3 and 5 objects are already shuffled inside blocks
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
    }
}
