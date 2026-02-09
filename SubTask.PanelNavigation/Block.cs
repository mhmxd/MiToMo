using Common.Helpers;
using Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using static Common.Constants.ExpEnums;

namespace SubTask.PanelNavigation
{
    // A block of trials in the experiment
    public class Block
    {
        private static readonly Random _random = new Random();

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

        public Technique _technique = Technique.MOUSE;

        public Technique Technique
        {
            get => _technique;
            set => _technique = value;
        }

        public int PtcNum { get; set; }

        public Block(int ptcNum, Technique technique, Complexity complexity, ExperimentType expType, int id)
        {
            this.Id = id;
            PtcNum = ptcNum;
            _technique = technique;
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
            Technique technique,
            int ptc,
            int id,
            Complexity complexity,
            ExperimentType expType)
        {

            // Create block
            Block block = new(ptc, technique, complexity, expType, id);

            //-- Create and add trials to the block
            int trialNum = 1;

            // Create Top trials for each button width
            foreach (int btnWidth in ExpLayouts.BUTTON_WIDTHS[complexity][Side.Top])
            {
                Trial trial = Trial.CreateTrial(id * 100 + trialNum,
                            technique, ptc,
                            complexity, expType,
                            Side.Top, btnWidth);

                block._trials.Add(trial);
                trialNum++;
            }

            // Create side trials (random L/R)
            foreach (int btnWidth in ExpLayouts.BUTTON_WIDTHS[complexity][Side.Left])
            {
                Side side = (Side)(_random.Next(0, 2) * 2); // Randomly select Left or Right
                Trial trial = Trial.CreateTrial(id * 100 + trialNum,
                            technique, ptc,
                            complexity, expType,
                            side, btnWidth);
                block._trials.Add(trial);
                trialNum++;
            }

            // Shuffle the trials
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

        public void ShuffleBackTrial(int trialNum)
        {
            // Validate trialNum (assuming 1-based index)
            if (trialNum < 1 || trialNum > _trials.Count)
            {
                return;
            }

            // 1. Get the target trial (using your existing method)
            Trial originalTrial = GetTrial(trialNum);
            if (originalTrial == null) return;

            // 2. Create a deep copy so data doesn't overlap
            // Note: Replace .Clone() with your actual cloning logic if different
            Trial trialCopy = originalTrial.Clone();

            // 3. Determine the range for the new insertion
            // The "remaining trials" are those from the current index + 1 to the end
            int currentIndex = trialNum - 1;
            int nextIndex = currentIndex + 1;

            if (nextIndex >= _trials.Count)
            {
                // If it's the last trial, just append it
                _trials.Add(trialCopy);
            }
            else
            {
                // Pick a random index between nextIndex and the very end of the list (inclusive)
                int randomInsertIndex = _random.Next(nextIndex, _trials.Count + 1);
                _trials.Insert(randomInsertIndex, trialCopy);
            }
        }

        //public void ShuffleBackTrial(int trialNum)
        //{
        //    // Shuffle the trial back into the block (after a failed attempt)


        //    // Shuffle the trial based on its function side
        //    //Trial trialToCopy = _trials[trialNum - 1];
        //    //if (trialNum == _trials.Count && _trials.Count > 1)
        //    //{
        //    //    _trials.Insert(trialNum, trialToCopy);
        //    //}
        //    //else if (_trials[trialNum].FuncSide == Side.Top)
        //    //{
        //    //    // Shuffle among the remaining top trials
        //    //    int insertIndex = _random.Next(trialNum + 1, _trials.Count(t => t.FuncSide == Side.Top) + 1);
        //    //    _trials.Insert(insertIndex, trialToCopy);
        //    //}
        //    //else
        //    //{
        //    //    // Shuffle among the remaining left trials
        //    //    int insertIndex = _random.Next(trialNum + 1, _trials.Count());
        //    //    _trials.Insert(insertIndex, trialToCopy);
        //    //}

        //}

        public Complexity GetComplexity()
        {
            return _complexity;
        }
    }
}
