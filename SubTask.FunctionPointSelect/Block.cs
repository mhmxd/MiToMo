using Common.Constants;
using Common.Helpers;
using Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using static Common.Constants.ExpEnums;
using Seril = Serilog.Log;

namespace SubTask.FunctionPointSelect
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

        public int NFunctions;

        public int PtcNum { get; set; }

        public Block(int ptcNum, Complexity complexity, ExperimentType expType, int id)
        {
            this.Id = id;
            PtcNum = ptcNum;
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
            List<MRange> distRanges)
        {

            // Create block
            Block block = new(ptc, complexity, expType, id);

            int trialNum = 1;
            Side trialSide;

            //-- Create Top trials
            trialSide = Side.Top;
            // Get the function widths based on side and complexity
            List<int> topButtonWidths = ExpLayouts.BUTTON_WIDTHS[complexity][trialSide];
            foreach (int buttonWidth in topButtonWidths)
            {
                foreach (MRange range in distRanges)
                {
                    Trial trial = Trial.CreateTrial(
                        id * 100 + trialNum, ptc,
                        complexity, expType,
                        trialSide, range,
                        buttonWidth);

                    block._trials.Add(trial);
                    trialNum++;
                }
            }

            //-- Create side trials
            trialSide = ExpEnums.GetRandomLR();
            // Get the function widths based on side and complexity
            List<int> sideButtonWidths = ExpLayouts.BUTTON_WIDTHS[complexity][trialSide];
            foreach (int buttonWidth in sideButtonWidths)
            {
                foreach (MRange range in distRanges)
                {
                    Trial trial = Trial.CreateTrial(
                        id * 100 + trialNum, ptc,
                        complexity, expType,
                        trialSide, range,
                        buttonWidth);

                    block._trials.Add(trial);
                    trialNum++;
                }
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

        //public string ToString()
        //{
        //    StringBuilder sb = new StringBuilder();
        //    sb.AppendLine($"Block: [Id = {_id}]");
        //    foreach (Trial trial in _trials)
        //    {
        //        sb.AppendLine(trial.ToString());
        //    }
        //    return sb.ToString();
        //}

        public Complexity GetComplexity()
        {
            return _complexity;
        }
    }
}
