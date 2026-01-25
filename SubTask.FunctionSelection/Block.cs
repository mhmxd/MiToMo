using Common.Constants;
using Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using static Common.Constants.ExpEnums;

namespace SubTask.FunctionSelection
{
    // A block of trials in the experiment
    public class Block
    {
        private Random _random = new Random();

        private List<Trial> _trials = new List<Trial>();
        public List<Trial> Trials
        {
            get => _trials;
            set => _trials = value;
        }
        private int _id { get; set; }
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        private Complexity _complexity = Complexity.Simple;
        public Complexity Complexity
        {
            get => _complexity;
            set => _complexity = value;
        }

        private ExperimentType _expType = ExperimentType.Practice;
        public ExperimentType ExpType
        {
            get => _expType;
            set => _expType = value;
        }

        public int NFunctions;

        public int PtcNum { get; set; }

        private Block(int ptcNum, int nFunc, Complexity complexity, ExperimentType expType, int id)
        {
            this.Id = id;
            PtcNum = ptcNum;
            NFunctions = nFunc;
            _complexity = complexity;
            _expType = expType;
        }

        public void ShuffleTrials()
        {
            _trials.Shuffle();
        }

        /// <summary>
        /// Create all types of blocks
        /// </summary>
        /// <param name="id"></param>
        /// <param name="distRanges"></param>
        /// <param name="functionWidthsMX"></param>
        /// <param name="nObj"></param>
        /// <returns></returns>
        public static Block CreateBlock(
            int ptc,
            int id,
            Complexity complexity,
            ExperimentType expType,
            int nFun)
        {

            // Create block
            Block block = new Block(ptc, nFun, complexity, expType, id);

            //-- Create and add trials to the block
            int trialNum = 1;

            //- All top trials, then left
            // Get the function widths based on side and complexity
            // For now all function Ws are the same. We may later create trials with multiple function Ws
            List<int> topButtonWs = ExpSizes.BUTTON_WIDTHS[complexity][Side.Top];
            foreach (int funcW in topButtonWs)
            {
                List<int> functionWidths = new List<int>(nFun);
                for (int i = 0; i < nFun; i++)
                {
                    functionWidths.Add(funcW);
                }

                Trial trial = Trial.CreateTrial(
                    id * 100 + trialNum,
                    ptc,
                    complexity,
                    expType,
                    Side.Top,
                    functionWidths);

                block._trials.Add(trial);
                trialNum++;
            }

            List<int> leftButtonWs = ExpSizes.BUTTON_WIDTHS[complexity][Side.Left];
            foreach (int funcW in leftButtonWs)
            {
                List<int> functionWidths = new List<int>(nFun);
                for (int i = 0; i < nFun; i++)
                {
                    functionWidths.Add(funcW);
                }

                Trial trial = Trial.CreateTrial(
                    id * 100 + trialNum,
                    ptc,
                    complexity,
                    expType,
                    Side.Left,
                    functionWidths);

                block._trials.Add(trial);
                trialNum++;
            }


            //for (int sInd = 0; sInd < 3; sInd++)
            //{
            //    Side functionSide = (Side)sInd;


            //}

            // Shuffle the trials
            //block.ShuffleTrials();

            // Return the block
            return block;
        }


        public Trial GetTrial(int trialNum)
        {
            if (trialNum <= _trials.Count()) return _trials[trialNum - 1];
            else return null;
        }

        public Trial GetTrialById(int trialId)
        {
            return _trials.FirstOrDefault(t => t.Id == trialId);

        }

        public int GetNumTrials()
        {
            return _trials.Count();
        }

        public void ShuffleBackTrial(int trialNum)
        {

            // Shuffle the trial based on its function side
            Trial trialToCopy = _trials[trialNum - 1];
            if (trialNum == _trials.Count && _trials.Count > 1)
            {
                _trials.Insert(trialNum, trialToCopy);
            }
            else if (_trials[trialNum].FuncSide == Side.Top)
            {
                // Shuffle among the remaining top trials
                int insertIndex = _random.Next(trialNum + 1, _trials.Count(t => t.FuncSide == Side.Top) + 1);
                _trials.Insert(insertIndex, trialToCopy);
            }
            else
            {
                // Shuffle among the remaining left trials
                int insertIndex = _random.Next(trialNum + 1, _trials.Count());
                _trials.Insert(insertIndex, trialToCopy);
            }
        }

        public Complexity GetComplexity()
        {
            return _complexity;
        }
    }
}
