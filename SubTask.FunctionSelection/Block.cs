using Common.Helpers;
using Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using static Common.Constants.ExpEnums;

namespace SubTask.FunctionSelection
{
    // A block of trials in the experiment
    public class Block
    {
        private static readonly Random _random = new();

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
            Block block = new(ptc, nFun, complexity, expType, id);

            //-- Create and add trials to the block
            int trialNum = 1;

            //-- Add top trials
            List<int> topButtonWs = ExpLayouts.BUTTON_WIDTHS[complexity][Side.Top];
            foreach (int funcW in topButtonWs)
            {
                List<int> functionWidths = new(nFun);
                for (int i = 0; i < nFun; i++)
                {
                    functionWidths.Add(funcW);
                }

                Trial trial = Trial.CreateTrial(
                    id * 100 + trialNum, ptc,
                    complexity, expType,
                    Side.Top, functionWidths);

                block._trials.Add(trial);
                trialNum++;
            }

            //-- Add random L or R
            List<int> sideButtonWs = ExpLayouts.BUTTON_WIDTHS[complexity][Side.Left]; // Same for L or R
            foreach (int funcW in sideButtonWs)
            {
                List<int> functionWidths = new List<int>(nFun);
                for (int i = 0; i < nFun; i++)
                {
                    functionWidths.Add(funcW);
                }

                // Choose a ranodm side
                Side side = (Side)(_random.Next(0, 2) * 2); // Randomly select Left or Right
                Trial trial = Trial.CreateTrial(
                    id * 100 + trialNum, ptc,
                    complexity, expType,
                    side, functionWidths);

                block._trials.Add(trial);
                trialNum++;
            }

            // Shuffle trials
            block.ShuffleTrials();

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

        //public void ShuffleBackTrial(int trialNum)
        //{

        //    // Shuffle the trial based on its function side
        //    Trial trialToCopy = _trials[trialNum - 1];
        //    if (trialNum == _trials.Count && _trials.Count > 1)
        //    {
        //        _trials.Insert(trialNum, trialToCopy);
        //    }
        //    else if (_trials[trialNum].FuncSide == Side.Top)
        //    {
        //        // Shuffle among the remaining top trials
        //        int insertIndex = _random.Next(trialNum + 1, _trials.Count(t => t.FuncSide == Side.Top) + 1);
        //        _trials.Insert(insertIndex, trialToCopy);
        //    }
        //    else
        //    {
        //        // Shuffle among the remaining left trials
        //        int insertIndex = _random.Next(trialNum + 1, _trials.Count());
        //        _trials.Insert(insertIndex, trialToCopy);
        //    }
        //}

        public void ShuffleBackTrial(int trialNum)
        {
            // 1. Basic Validation (trialNum is 1-based)
            if (trialNum < 1 || trialNum > _trials.Count)
            {
                return;
            }

            // 2. Deep Clone the trial so data stays independent
            Trial trialToCopy = _trials[trialNum - 1].Clone();

            // 3. Handle Insertion
            if (trialNum >= _trials.Count)
            {
                // If it's the last trial, the only place to go is the end.
                _trials.Add(trialToCopy);
            }
            else
            {
                // To ensure at least one trial exists between the current one and the copy:
                // Current index is trialNum - 1. 
                // Next trial is at index trialNum.
                // We start our random range at trialNum + 1.
                int minInsertIndex = trialNum + 1;

                // If we are at the second to last trial, minInsertIndex might exceed Count.
                // We cap it or use Add().
                if (minInsertIndex >= _trials.Count)
                {
                    _trials.Add(trialToCopy);
                }
                else
                {
                    // random.Next(min, max) -> max is exclusive.
                    // _trials.Count + 1 allows the trial to potentially land at the very end.
                    int insertIndex = _random.Next(minInsertIndex, _trials.Count + 1);
                    _trials.Insert(insertIndex, trialToCopy);
                }
            }
        }

        public Complexity GetComplexity()
        {
            return _complexity;
        }
    }
}
