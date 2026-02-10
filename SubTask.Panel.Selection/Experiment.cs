using Common.Constants;
using Common.Helpers;
using Common.Settings;
using CommonUI;
using System.Collections.Generic;
using System.Linq;
using static Common.Constants.ExpEnums;

namespace SubTask.Panel.Selection
{
    public class Experiment
    {

        public static readonly int START_FONT_SIZE = 18;

        //--- Setting
        public Technique ActiveTechnique = Technique.TOMO_TAP; // Set in the info dialog
        public Complexity ActiveComplexity = Complexity.Simple; // Set in the info dialog

        private readonly List<Block> _blocks = new();
        public List<Block> Blocks { get { return _blocks; } }

        public Experiment()
        {
            //Participant_Number = DEFAULT_PTC; // Default
        }

        public void Init(string tech, ExperimentType expType)
        {
            this.TrialInfo($"Participant: {ExpEnvironment.PTC_NUM}, Technique: {tech}");

            if (tech == ExpStrs.TAP_C)
            {
                ActiveTechnique = Technique.TOMO_TAP;

            }
            else if (tech == ExpStrs.SWIPE_C)
            {
                ActiveTechnique = Technique.TOMO_SWIPE;

            }

            //-- For each complexity, create blocks and add them
            List<Complexity> randomComplexities = ExpEnums.GetRandomComplexityList();
            foreach (Complexity complexity in randomComplexities)
            {
                for (int i = 0; i < ExpDesign.PaneSelectNumBlocks; i++)
                {
                    int blockId = ExpEnvironment.PTC_NUM * 100 + i + 1;
                    _blocks.Add(Block.CreateBlock(
                        ActiveTechnique, ExpEnvironment.PTC_NUM,
                        blockId, complexity, expType));

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

        public static int GetStartSize()
        {
            return UITools.MM2PX(ExpLayouts.OBJ_AREA_WIDTH_MM);
        }

    }
}
