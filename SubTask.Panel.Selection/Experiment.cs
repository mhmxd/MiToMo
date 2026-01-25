using Common.Constants;
using Common.Settings;
using CommonUI;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using static Common.Constants.ExpEnums;

namespace SubTask.Panel.Selection
{
    public class Experiment
    {

        //--- Setting
        public Technique Active_Technique = Technique.TOMO_TAP; // Set in the info dialog
        public Complexity Active_Complexity = Complexity.Simple; // Set in the info dialog

        //-- Colors
        public static readonly Brush START_INIT_COLOR = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(ExpColors.PURPLE));

        //-- Information
        //public int Participant_Number { get; set; } // Set in the info dialog

        private readonly List<Block> _blocks = new();
        public List<Block> Blocks { get { return _blocks; } }

        public Experiment()
        {
            //Participant_Number = DEFAULT_PTC; // Default
        }

        public void Init(string tech, Complexity complexity, ExperimentType expType)
        {
            this.TrialInfo($"Participant: {ExpEnvironment.PTC_NUM}, Technique: {tech}");
            //Participant_Number = ptc;
            if (tech == ExpStrs.TAP_C)
            {
                Active_Technique = Technique.TOMO_TAP;

            }
            else if (tech == ExpStrs.SWIPE_C)
            {
                Active_Technique = Technique.TOMO_SWIPE;

            }

            Active_Complexity = complexity;

            // Create and add blocks
            for (int i = 0; i < ExpDesign.PN_N_BLOCKS; i++)
            {
                int blockId = ExpEnvironment.PTC_NUM * 100 + i + 1;
                Block block = Block.CreateBlock(
                    Active_Technique, ExpEnvironment.PTC_NUM, blockId,
                    complexity, expType, ExpDesign.PS_N_REP);
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
            return UITools.MM2PX(ExpSizes.START_BUTTON_LARGER_SIDE_MM / 2);
        }
    }
}
