using Common.Helpers;
using Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using static Common.Constants.ExpEnums;

namespace SubTask.FunctionPointSelect
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

        public int NFunctions;

        public int PtcNum { get; set; }

        public Block(int ptcNum, Complexity complexity, ExperimentType expType, int id)
        {
            this.Id = id;
            PtcNum = ptcNum;
            _complexity = complexity;
            _expType = expType;
        }

        /// <summary>
        /// One side (for repeating blocks)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="targetMultiples"></param>
        /// <param name="distsMM"></param>
        //public Block(Side side, int id, List<int> targetMultiples, List<double> distsMM)
        //{
        //    _id = id;

        //    //-- Create trials (dist x targetWidth x sideWindow)
        //    int trialNum = 1;
        //    foreach (int targetMultiple in targetMultiples)
        //    {
        //        foreach (double distMM in distsMM)
        //        {
        //            Trial trial = new Trial(_id * 100 + trialNum, targetMultiple, distMM, side);
        //            _trials.Add(trial);
        //            trialNum++;
        //        }
        //    }


        //    // Shuffle the trials
        //    _trials.Shuffle();
        //}

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
            trialSide = (Side)(new Random().Next(0, 2)); // Randomly select Left (0) or Right (1)
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

        //public Block(int id, List<int> topTargetMultiples, List<int> sideTargetMultiples, List<double> distsMM, int rep)
        //{
        //    _id = id;

        //    // Top trials
        //    int trialNum = 1;
        //    for (int r = 0; r < rep; r++)
        //    {
        //        foreach (int targetMultiple in topTargetMultiples)
        //        {
        //            foreach (double distMM in distsMM)
        //            {
        //                Trial trial = new Trial(_id * 100 + trialNum, targetMultiple, distMM, Side.Top);
        //                _trials.Add(trial);
        //                trialNum++;
        //            }
        //        }
        //    }

        //    // Left trials
        //    for (int r = 0; r < rep; r++)
        //    {
        //        foreach (int targetMultiple in sideTargetMultiples)
        //        {
        //            foreach (double distMM in distsMM)
        //            {
        //                Trial trial = new Trial(_id * 100 + trialNum, targetMultiple, distMM, Side.Left);
        //                _trials.Add(trial);
        //                trialNum++;
        //            }
        //        }
        //    }

        //    // Right trials
        //    for (int r = 0; r < rep; r++)
        //    {
        //        foreach (int targetMultiple in sideTargetMultiples)
        //        {
        //            foreach (double distMM in distsMM)
        //            {
        //                Trial trial = new Trial(_id * 100 + trialNum, targetMultiple, distMM, Side.Right);
        //                _trials.Add(trial);
        //                trialNum++;
        //            }
        //        }
        //    }

        //    // Shuffle the trials
        //    _trials.Shuffle();
        //}

        //public Block(int id, List<int> targetMultiples, List<double> distsMM, int rep)
        //{
        //    _id = id;

        //    //-- Create trials (dist x targetWidth x sideWindow)
        //    // trial id = _id * 100 + num
        //    int trialNum = 1;
        //    for (int r = 0; r < rep; r++)
        //    {
        //        foreach (int targetMultiple in targetMultiples)
        //        {
        //            foreach (double distMM in distsMM)
        //            {
        //                for (int locInd = 0; locInd < 3; locInd++)
        //                {
        //                    Side sideWindow = (Side)locInd;
        //                    Trial trial = new Trial(_id * 100 + trialNum, targetMultiple, distMM, sideWindow);
        //                    _trials.Add(trial);
        //                    trialNum++;
        //                }

        //            }
        //        }
        //    }

        //    // Shuffle the trials
        //    _trials.Shuffle();

        //    // TESTING
        //    foreach (Trial trial in _trials)
        //    {
        //        Seril.Information(trial.ToString());
        //    }
        //}

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
        //    if (trialNum >= 1 && trialNum < _trials.Count && _trials.Count > 1)
        //    {
        //        Trial trialToCopy = _trials[trialNum - 1];
        //        Random random = new Random();
        //        int insertIndex = random.Next(trialNum + 1, _trials.Count);

        //        _trials.Insert(insertIndex, trialToCopy);
        //    }
        //    else if (trialNum == _trials.Count && _trials.Count > 1)
        //    {
        //        _trials.Insert(trialNum, _trials[trialNum - 1]);
        //    }
        //    else if (_trials.Count <= 1)
        //    {
        //        Seril.Information("Not enough trials to shuffle back with at least one trial in between.");
        //    }
        //    else
        //    {
        //        Seril.Error($"Invalid trial number: {trialNum}. Trial number must be between 1 and {_trials.Count}.");
        //    }
        //}

        public void ShuffleBackTrial(int trialNum)
        {
            int totalCount = _trials.Count;

            // 1. Basic Validation
            if (trialNum < 1 || trialNum > totalCount)
            {
                // Use Serilog (fixing the typo 'Seril' to 'Log' or 'Serilog' as per your project)
                Serilog.Log.Error($"Invalid trial number: {trialNum}. Must be between 1 and {totalCount}.");
                return;
            }

            // 2. Logic for insufficient trials
            if (totalCount <= 1)
            {
                Serilog.Log.Information("Not enough trials to shuffle back with at least one trial in between.");
                return;
            }

            // 3. Perform the Clone
            Trial trialToCopy = _trials[trialNum - 1].Clone();

            // 4. Calculate insertion range
            // We want at least one trial between the current one and the copy.
            // Current trial is at trialNum - 1.
            // Next trial is at trialNum.
            // Minimum insert index is trialNum + 1.
            int minInsertIndex = trialNum + 1;

            if (minInsertIndex >= totalCount)
            {
                // If it's the last or second-to-last trial, the copy simply becomes the new last trial.
                _trials.Add(trialToCopy);
            }
            else
            {
                // Random.Next(min, max) -> max is exclusive. 
                // We use totalCount + 1 to allow it to be placed at the very end.
                int insertIndex = _random.Next(minInsertIndex, totalCount + 1);
                _trials.Insert(insertIndex, trialToCopy);
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
