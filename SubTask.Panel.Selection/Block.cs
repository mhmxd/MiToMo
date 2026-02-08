using Common.Helpers;
using Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using static Common.Constants.ExpEnums;
using Seril = Serilog.Log;

namespace SubTask.Panel.Selection
{
    // A block of trials in the experiment
    public class Block
    {
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
            Id = id;
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

            int trialNum = 1;
            // Create Top trials for each button width
            Side side = Side.Top;
            foreach (int btnWidth in ExpLayouts.BUTTON_WIDTHS[complexity][side])
            {
                Trial trial = Trial.CreateTrial(id * 100 + trialNum,
                            technique, ptc,
                            complexity, expType,
                            side, btnWidth);

                block._trials.Add(trial);
                trialNum++;
            }

            // Create Left trials
            side = Side.Left;
            foreach (int btnWidth in ExpLayouts.BUTTON_WIDTHS[complexity][side])
            {
                Trial trial = Trial.CreateTrial(id * 100 + trialNum,
                            technique, ptc,
                            complexity, expType,
                            side, btnWidth);
                block._trials.Add(trial);
                trialNum++;
            }

            // Create Right trials
            side = Side.Right;
            foreach (int btnWidth in ExpLayouts.BUTTON_WIDTHS[complexity][side])
            {
                Trial trial = Trial.CreateTrial(id * 100 + trialNum,
                            technique, ptc,
                            complexity, expType,
                            side, btnWidth);
                block._trials.Add(trial);
                trialNum++;
            }

            block.ShuffleTrials();

            //bool wasLeft = false;
            // One Top in reach rep
            //Trial trial = Trial.CreateTrial(
            //            id * 100 + trialNum,
            //            technique,
            //            ptc,
            //            complexity,
            //            expType,
            //            Side.Top);

            //block._trials.Add(trial);
            //trialNum++;

            //// One Left/Right in reach rep (equal number of left and right in all blocks together)
            //if (wasLeft)
            //{
            //    trial = Trial.CreateTrial(
            //            id * 100 + trialNum,
            //            technique,
            //            ptc,
            //            complexity,
            //            expType,
            //            Side.Right);
            //    wasLeft = false;
            //}
            //else
            //{
            //    trial = Trial.CreateTrial(
            //            id * 100 + trialNum,
            //            technique,
            //            ptc,
            //            complexity,
            //            expType,
            //            Side.Left);
            //    wasLeft = true;
            //}

            //block._trials.Add(trial);
            //trialNum++;

            //// Shuffle the trials
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
            if (trialNum >= 1 && trialNum < _trials.Count && _trials.Count > 1)
            {
                Trial trialToCopy = _trials[trialNum - 1];
                Random random = new Random();
                int insertIndex = random.Next(trialNum + 1, _trials.Count);

                _trials.Insert(insertIndex, trialToCopy);
            }
            else if (trialNum == _trials.Count && _trials.Count > 1)
            {
                _trials.Insert(trialNum, _trials[trialNum - 1]);
            }
            else if (_trials.Count <= 1)
            {
                Seril.Information("Not enough trials to shuffle back with at least one trial in between.");
            }
            else
            {
                Seril.Error($"Invalid trial number: {trialNum}. Trial number must be between 1 and {_trials.Count}.");
            }
        }

        public Complexity GetComplexity()
        {
            return _complexity;
        }
    }
}
