using Common.Helpers;
using Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using static Common.Constants.ExpEnums;
using Seril = Serilog.Log;

namespace SubTask.ObjectSelection
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

        private static Technique _technique = Technique.MOUSE;

        public static Technique Technique
        {
            get => _technique;
            set => _technique = value;
        }

        private ExperimentType _expType = ExperimentType.Practice;
        public ExperimentType ExpType
        {
            get => _expType;
            set => _expType = value;
        }

        public int PtcNum { get; set; }

        public Block(int ptc, int id, ExperimentType expType)
        {
            this.Id = id;
            PtcNum = ptc;
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
            ExperimentType expType,
            int nRep)
        {

            // Create block
            Block block = new Block(ptc, id, expType);

            // Create the same number of 3 and 5 object trials in each block
            int trialNum = 1;
            for (int rep = 0; rep < nRep; rep++)
            {
                Trial trial3 = Trial.CreateTrial(
                            id * 100 + trialNum,
                            Technique,
                            expType,
                            ExpEnvironment.PTC_NUM,
                            ExpDesign.ObjSelectNumObjects[0]);

                block._trials.Add(trial3);
                trialNum++;

                Trial trial5 = Trial.CreateTrial(
                            id * 100 + trialNum,
                            Technique,
                            expType,
                            ExpEnvironment.PTC_NUM,
                            ExpDesign.ObjSelectNumObjects[1]);

                block._trials.Add(trial5);
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
            // 1. Validate (1-based index)
            if (trialNum < 1 || trialNum > _trials.Count) return;

            // 2. Clone the current trial
            Trial trialToCopy = _trials[trialNum - 1].Clone();

            // 3. Handle Insertion Logic
            if (trialNum >= _trials.Count)
            {
                // If it's the last trial, just add it to the end
                _trials.Add(trialToCopy);
            }
            else
            {
                // Ensure at least one trial exists between the current one and the copy.
                // Current index is trialNum - 1. 
                // Next trial is at index trialNum.
                // We start our random range at trialNum + 1.
                int minInsertIndex = trialNum + 1;

                if (minInsertIndex >= _trials.Count)
                {
                    // If we are at the second-to-last trial, just add to the end.
                    _trials.Add(trialToCopy);
                }
                else
                {
                    // Pick a random spot between 'one trial away' and the very end.
                    // _random.Next max is exclusive, so +1 to include the last slot.
                    int insertIndex = _random.Next(minInsertIndex, _trials.Count + 1);
                    _trials.Insert(insertIndex, trialToCopy);
                }
            }
        }

        //public void ShuffleBackTrial(int trialNum)
        //{
        //    if (trialNum >= 1 && trialNum < _trials.Count && _trials.Count > 1)
        //    {
        //        Trial trialToCopy = _trials[trialNum - 1];
        //        int insertIndex = _random.Next(trialNum + 1, _trials.Count);

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
    }
}
