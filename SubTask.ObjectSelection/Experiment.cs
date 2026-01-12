using Common.Constants;
using Common.Settings;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using static Common.Constants.ExpEnums;

namespace SubTask.ObjectSelection
{
    public class Experiment
    {

        //--- Setting
        public Technique Active_Technique = Technique.MOUSE; // Set in the info dialog

        //-- Colors
        public static readonly Brush START_INIT_COLOR = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(ExpColors.PURPLE));

        //-- Information

        //public int Participant_Number { get; set; } // Set in the info dialog

        private List<Block> _blocks = new List<Block>();
        public List<Block> Blocks { get { return _blocks; } }

        public Experiment()
        {
            //Participant_Number = DEFAULT_PTC; // Default
        }

        public void Init()
        {
            this.TrialInfo($"Participant: {ExpPtc.PTC_NUM}");
            //Participant_Number = ptc;

            // Create and add blocks
            for (int i = 0; i < ExpDesign.OS_N_BLOCKS; i++)
            {
                int blockId = ExpPtc.PTC_NUM * 100 + i + 1;
                Block block = Block.CreateBlock(Active_Technique, ExpPtc.PTC_NUM, blockId, ExpDesign.OS_N_REP);
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
    }
}
