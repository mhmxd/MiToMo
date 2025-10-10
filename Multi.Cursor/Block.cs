using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using Seril = Serilog.Log;

namespace Multi.Cursor
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
        private int _id {  get; set; }
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

        public Technique _technique = Technique.MOUSE;

        public Technique Technique
        {
            get => _technique;
            set => _technique = value;
        }

        public int NFunctions, NObjects;

        public int PtcNum { get; set; }

        public Block(int ptcNum, Technique technique, TaskType type, int nFunc, int nObj, Complexity complexity, int id)
        {
            this.Id = id;
            PtcNum = ptcNum;
            _technique = technique;
            _taskType = type;
            _complexity = complexity;
        }

        /// <summary>
        /// One side (for repeating blocks)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="targetMultiples"></param>
        /// <param name="distsMM"></param>
        public Block(Side side, int id, List<int> targetMultiples, List<double> distsMM)
        {
            _id = id;

            //-- Create trials (dist x targetWidth x sideWindow)
            int trialNum = 1;
            foreach (int targetMultiple in targetMultiples)
            {
                foreach (double distMM in distsMM)
                {
                    Trial trial = new Trial(_id * 100 + trialNum, targetMultiple, distMM, side);
                    _trials.Add(trial);
                    trialNum++;
                }
            }


            // Shuffle the trials
            _trials.Shuffle();
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
            List<Range> distRanges,
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
                case (> 1, 1):
                    type = TaskType.MULTI_OBJ_ONE_FUNC;
                    break;
                case (> 1, > 1):
                    type = TaskType.MULTI_OBJ_MULTI_FUNC;
                    break;
                default:
                    throw new ArgumentException("Invalid number of functions or objects.");
            }

            // Create block
            Block block = new Block(ptc, technique, type, nFun, nObj, complexity, id);

            // Create and add trials to the block
            int trialNum = 1;
            for (int sInd = 0; sInd < 3; sInd++)
            {
                Side functionSide = (Side)sInd;

                // Get the function widths based on side and complexity
                List<int> buttonWidths = Experiment.BUTTON_WIDTHS[complexity][functionSide];

                foreach (Range range in distRanges)
                {
                    // For now all function Ws are the same. We may later create trials with multiple function Ws
                    foreach (int funcW in buttonWidths)
                    {
                        List<int> functionWidths = new List<int>(nFun);
                        for (int i = 0; i < nFun; i++)
                        {
                            functionWidths.Add(funcW);
                        }
                        
                        Trial trial = Trial.CreateTrial(
                            id * 100 + trialNum,
                            technique,
                            ptc,
                            type,
                            complexity,
                            functionSide,
                            range,
                            nObj,
                            functionWidths);

                        block._trials.Add(trial);
                        trialNum++;
                    }

                }
            }

            // Shuffle the trials
            block.ShuffleTrials();

            // Return the block
            return block;
        }

        //public static Block CreateSingleObjSingleFunBlock(
        //    int id,
        //    List<int> functionWidthsMX,
        //    List<Range> distRanges)
        //{
        //    Block block = new Block(TaskType.ONE_OBJ_ONE_FUNC);
        //    block.Id = id;

        //    // Create and add trials to the block
        //    int trialNum = 1;
        //    for (int sInd = 0; sInd < 3; sInd++)
        //    {
        //        Side functionSide = (Side)sInd;
        //        foreach (int targetMultiple in functionWidthsMX)
        //        {
        //            foreach (Range range in distRanges)
        //            {
        //                Trial trial = Trial.CreateSingleObjSingleFuncTrial(
        //                    id * 100 + trialNum,
        //                    functionSide,
        //                    targetMultiple,
        //                    range);
        //                block._trials.Add(trial);

        //                trialNum++;
        //            }
        //        }
        //    }

        //    return block;
        //}

        //public static Block CreateSingleObjMultiFunBlock(
        //    int id,
        //    List<int> functionWidthsMX,
        //    List<Range> distRanges)
        //{
        //    Block block = new Block(TaskType.ONE_OBJ_ONE_FUNC);
        //    block.Id = id;

        //    // Create and add trials to the block (For now, we creat 2 three functions using each widthMX)
        //    int trialNum = 1;
        //    for (int sInd = 0; sInd < 3; sInd++)
        //    {
        //        Side functionSide = (Side)sInd;
        //        foreach (int funcW in functionWidthsMX)
        //        {
        //            foreach (Range range in distRanges)
        //            {
        //                Trial trial = Trial.CreateSingleObjMultiFuncTrial(
        //                    id * 100 + trialNum,
        //                    functionSide,
        //                    funcW,
        //                    range);
        //                block._trials.Add(trial);

        //                trialNum++;
        //            }
        //        }
        //    }

        //    // Shuffle the trials
        //    block.ShuffleTrials();

        //    return block;
        //}

        //public static Block CreateMultiObjSingleFuncBlock(
        //    int id,
        //    List<int> functionWidthsMX,
        //    List<Range> distRanges)
        //{
        //    Block block = new Block(TaskType.MULTI_OBJ_ONE_FUNC);
        //    block.Id = id;

        //    // Create and add trials to the block
        //    int trialNum = 1;
        //    for (int sInd = 0; sInd < 3; sInd++)
        //    {
        //        Side side = (Side)sInd;
        //        foreach (int funcW in functionWidthsMX)
        //        {
        //            foreach (Range range in distRanges)
        //            {
        //                Trial trial = Trial.CreateMultiObjcectTrial(
        //                    id * 100 + trialNum,
        //                    side,
        //                    funcW,
        //                    range,
        //                    nPasses);

        //                block._trials.Add(trial);

        //                trialNum++;
        //            }
        //        }
        //    }
        //}


        //public static Block CreateRepBlock(
        //    int id, 
        //    List<int> targetMultiples,
        //    List<Range> distRanges,
        //    int nPasses)
        //{
        //    Block block = new Block(TaskType.MULTI_OBJ_ONE_FUNC);
        //    block.Id = id;

        //    // Create and add trials to the block
        //    block.TrialInfo($"{targetMultiples.Count}W x {distRanges.Count}D x 3Sides");
        //    int trialNum = 1;
        //    for (int sInd = 0; sInd < 3; sInd++)
        //    {
        //        Side side = (Side)sInd;
        //        foreach (int targetMultiple in targetMultiples)
        //        {
        //            foreach (Range range in distRanges)
        //            {
        //                Trial trial = Trial.CreateMultiObjcectTrial(
        //                    id * 100 + trialNum, 
        //                    side,
        //                    targetMultiple, 
        //                    range, 
        //                    nPasses);

        //                block._trials.Add(trial);

        //                trialNum++;
        //            }
        //        }
        //    }

        //    // Display all trials in the block
        //    foreach (Trial trial in block._trials)
        //    {
        //        block.TrialInfo(trial.ToString());
        //    }
        //    block._trials.Shuffle();

        //    return block;
        //}

        //public static Block CreateAltBlock(
        //    int id, 
        //    List<int> targetMultiples,
        //    List<Range> distRanges)
        //{
        //    Block block = new Block(TaskType.ONE_OBJ_ONE_FUNC);
        //    block.Id = id;

        //    // Create and add trials to the block
        //    int trialNum = 1;
        //    for (int sInd = 0; sInd < 3; sInd++)
        //    {
        //        Side side = (Side)sInd;
        //        foreach (int targetMultiple in targetMultiples)
        //        {
        //            foreach (Range range in distRanges)
        //            {
        //                Trial trial = Trial.CreateSingleObjectTrial(
        //                    id * 100 + trialNum,
        //                    side,
        //                    targetMultiple,
        //                    range);
        //                block._trials.Add(trial);

        //                trialNum++;
        //            }
        //        }

                
        //    }

        //    //-------------------- BRING BACK LATER
        //    // Shuffle until no consecutive trials have the same target multiple
        //    // This is a simple shuffle, but it may not guarantee no consecutive trials with the same target multiple
        //    //bool hasConsecutive = true;
        //    //while (hasConsecutive)
        //    //{
        //    //    block._trials.Shuffle();
        //    //    hasConsecutive = false;
        //    //    for (int i = 1; i < block._trials.Count; i++)
        //    //    {
        //    //        if (block._trials[i].TargetMultiple == block._trials[i - 1].TargetMultiple)
        //    //        {
        //    //            hasConsecutive = true;
        //    //            break;
        //    //        }
        //    //    }
        //    //}

        //    return block;
        //}

        public Block(int id, List<int> topTargetMultiples, List<int> sideTargetMultiples, List<double> distsMM, int rep)
        {
            _id = id;

            // Top trials
            int trialNum = 1;
            for (int r = 0; r < rep; r++)
            {
                foreach (int targetMultiple in topTargetMultiples)
                {
                    foreach (double distMM in distsMM)
                    {
                        Trial trial = new Trial(_id * 100 + trialNum, targetMultiple, distMM, Side.Top);
                        _trials.Add(trial);
                        trialNum++;
                    }
                }
            }

            // Left trials
            for (int r = 0; r < rep; r++)
            {
                foreach (int targetMultiple in sideTargetMultiples)
                {
                    foreach (double distMM in distsMM)
                    {
                        Trial trial = new Trial(_id * 100 + trialNum, targetMultiple, distMM, Side.Left);
                        _trials.Add(trial);
                        trialNum++;
                    }
                }
            }

            // Right trials
            for (int r = 0; r < rep; r++)
            {
                foreach (int targetMultiple in sideTargetMultiples)
                {
                    foreach (double distMM in distsMM)
                    {
                        Trial trial = new Trial(_id * 100 + trialNum, targetMultiple, distMM, Side.Right);
                        _trials.Add(trial);
                        trialNum++;
                    }
                }
            }

            // Shuffle the trials
            _trials.Shuffle();
        }

        public Block(int id, List<int> targetMultiples, List<double> distsMM, int rep)
        {
            _id = id;

            //-- Create trials (dist x targetWidth x sideWindow)
            // trial id = _id * 100 + num
            int trialNum = 1;
            for (int r = 0; r < rep; r++)
            {
                foreach (int targetMultiple in targetMultiples)
                {
                    foreach (double distMM in distsMM)
                    {
                        for (int locInd = 0; locInd < 3; locInd++)
                        {
                            Side sideWindow = (Side)locInd;
                            Trial trial = new Trial(_id * 100 + trialNum, targetMultiple, distMM, sideWindow);
                            _trials.Add(trial);
                            trialNum++;
                        }

                    }
                }
            }

            // Shuffle the trials
            _trials.Shuffle();

            // TESTING
            foreach (Trial trial in _trials)
            {
                Seril.Information(trial.ToString());
            }
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

        public string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Block: [Id = {_id}]");
            foreach (Trial trial in _trials)
            {
                sb.AppendLine(trial.ToString());
            }
            return sb.ToString();
        }

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

        public Technique GetSpecificTechnique()
        {
            return _technique;
        }

        public Complexity GetComplexity()
        {
            return _complexity;
        }
    }
}
