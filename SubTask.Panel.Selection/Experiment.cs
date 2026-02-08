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

            //-- For each complexity, create blocks and randomize them before adding to the list
            foreach (Complexity complexity in ExpEnums.GetRandomComplexityList())
            {
                // Create blocks, then shuffle them before adding to the overall list
                List<Block> blocks = new();
                for (int i = 0; i < ExpDesign.PaneSelectNumBlocks; i++)
                {
                    int blockId = ExpEnvironment.PTC_NUM * 100 + i + 1;
                    blocks.Add(Block.CreateBlock(
                        ActiveTechnique, ExpEnvironment.PTC_NUM,
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

        public static int GetStartHalfWidth()
        {
            return UITools.MM2PX(ExpLayouts.START_BUTTON_LARGE_SIDE_MM / 2);
        }
    }
}
