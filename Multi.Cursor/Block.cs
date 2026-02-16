using Common.Constants;
using Common.Helpers;
using Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using static Common.Constants.ExpEnums;

namespace Multi.Cursor
{
    // A block of trials in the experiment
    public class Block
    {
        Random _random = new Random();

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

        private TaskType _taskType = TaskType.MULTI_OBJ_ONE_FUNC;
        public TaskType TaskType
        {
            get => _taskType;
            set => _taskType = value;
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

        public int NFunctions, NObjects;

        public int PtcNum { get; set; }

        public Block(
            int ptcNum, Technique technique, TaskType type,
            int nFunc, int nObj,
            Complexity complexity,
            ExperimentType expType,
            int id)
        {
            this.Id = id;
            PtcNum = ptcNum;
            _technique = technique;
            _taskType = type;
            NFunctions = nFunc;
            NObjects = nObj;
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
        /// <param name="distRange"></param>
        /// <param name="functionWidthsMX"></param>
        /// <param name="nObj"></param>
        /// <returns></returns>
        public static Block CreateBlock(
            Technique technique,
            int ptc,
            int id,
            Complexity complexity,
            ExperimentType expType,
            MRange distRange,
            int nFun,
            int nObj)
        {

            // Set the type of the block based on nFun and nObj
            TaskType type = TaskType.ONE_OBJ_ONE_FUNC;
            switch (nObj, nFun)
            {
                case (1, 1):
                    type = TaskType.ONE_OBJ_ONE_FUNC;
                    break;
                case (1, > 1):
                    type = TaskType.ONE_OBJ_MULTI_FUNC;
                    break;
                case ( > 1, 1):
                    type = TaskType.MULTI_OBJ_ONE_FUNC;
                    break;
                case ( > 1, > 1):
                    type = TaskType.MULTI_OBJ_MULTI_FUNC;
                    break;
                default:
                    throw new ArgumentException("Invalid number of functions or objects.");
            }

            // Create block
            Block block = new(ptc, technique, type, nFun, nObj, complexity, expType, id);

            //-- Create and add trials to the block
            int trialNum = 1;

            // Create Top trials for each button width
            Side trialSide = Side.Top;
            List<int> buttonWidths = ExpLayouts.BUTTON_WIDTHS[complexity][trialSide];
            foreach (int funcW in buttonWidths)
            {
                List<int> functionWidths = new(nFun);
                for (int i = 0; i < nFun; i++)
                {
                    functionWidths.Add(funcW);
                }

                Trial trial = Trial.CreateTrial(
                            id * 100 + trialNum,
                            technique, ptc,
                            type, complexity,
                            expType, trialSide,
                            distRange,
                            nObj, functionWidths);

                block._trials.Add(trial);
                trialNum++;
            }


            // Create side trials (random L/R)
            trialSide = ExpEnums.GetRandomLR();
            buttonWidths = ExpLayouts.BUTTON_WIDTHS[complexity][trialSide];
            foreach (int funcW in buttonWidths)
            {
                List<int> functionWidths = new(nFun);
                for (int i = 0; i < nFun; i++)
                {
                    functionWidths.Add(funcW);
                }

                Trial trial = Trial.CreateTrial(
                            id * 100 + trialNum,
                            technique, ptc,
                            type, complexity,
                            expType, trialSide,
                            distRange,
                            nObj, functionWidths);

                block._trials.Add(trial);
                trialNum++;
            }


            // Shuffle the trials
            block.ShuffleTrials();

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

        public TaskType GetBlockType()
        {
            return _taskType;
        }

        public TaskType GetObjectType()
        {
            if (_taskType == TaskType.ONE_OBJ_ONE_FUNC || _taskType == TaskType.ONE_OBJ_MULTI_FUNC)
                return TaskType.ONE_OBJECT;
            else
                return TaskType.MULTI_OBJECT;
        }

        public TaskType GetFunctionType()
        {
            if (_taskType == TaskType.ONE_OBJ_ONE_FUNC || _taskType == TaskType.MULTI_OBJ_ONE_FUNC)
                return TaskType.ONE_FUNCTION;
            else
                return TaskType.MULTI_FUNCTION;
        }

        //public Technique GetTechnique()
        //{
        //    return _technique;
        //}

        public Complexity GetComplexity()
        {
            return _complexity;
        }
    }
}
