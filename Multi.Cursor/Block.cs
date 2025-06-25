using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using Seril = Serilog.Log;

namespace Multi.Cursor
{
    // A block of trials in the experiment
    public class Block
    {
        public enum BLOCK_TYPE
        {
            REPEATING = 0,
            ALTERNATING = 1
        }

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

        private BLOCK_TYPE _blockType = BLOCK_TYPE.REPEATING;
        public BLOCK_TYPE BlockType
        {
            get => _blockType;
            set => _blockType = value;
        }

        public Block(BLOCK_TYPE type)
        {
            this.BlockType = type;
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

        public static Block CreateRepBlock(
            int id, 
            List<int> targetMultiples,
            List<Range> distRanges,
            int nPasses)
        {
            Block block = new Block(BLOCK_TYPE.REPEATING);
            block.Id = id;

            // Create and add trials to the block
            int trialNum = 1;
            for (int sInd = 0; sInd < 3; sInd++)
            {
                Side side = (Side)sInd;
                foreach (int targetMultiple in targetMultiples)
                {
                    foreach (Range range in distRanges)
                    {
                        Trial trial = Trial.CreateRepetingTrial(
                            id * 100 + trialNum, 
                            side,
                            targetMultiple, 
                            range, 
                            nPasses);
                        block._trials.Add(trial);

                        trialNum++;
                    }
                }
            }
            block._trials.Shuffle();

            return block;
        }

        public static Block CreateAltBlock(
            int id, 
            List<int> targetMultiples,
            List<Range> distRanges)
        {
            Block block = new Block(BLOCK_TYPE.ALTERNATING);
            block.Id = id;

            // Create and add trials to the block
            int trialNum = 1;
            for (int sInd = 0; sInd < 3; sInd++)
            {
                Side side = (Side)sInd;
                foreach (int targetMultiple in targetMultiples)
                {
                    foreach (Range range in distRanges)
                    {
                        Trial trial = Trial.CreateAlternatingTrial(
                            id * 100 + trialNum,
                            side,
                            targetMultiple,
                            range);
                        block._trials.Add(trial);

                        trialNum++;
                    }
                }

                
            }

            // Shuffle until no consecutive trials have the same target multiple
            // This is a simple shuffle, but it may not guarantee no consecutive trials with the same target multiple
            bool hasConsecutive = true;
            while (hasConsecutive)
            {
                block._trials.Shuffle();
                hasConsecutive = false;
                for (int i = 1; i < block._trials.Count; i++)
                {
                    if (block._trials[i].TargetMultiple == block._trials[i - 1].TargetMultiple)
                    {
                        hasConsecutive = true;
                        break;
                    }
                }
            }

            return block;
        }

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

        public int GetNumTrials()
        {
            return _trials.Count();
        }

        public void ShuffleBackTrial(int trialNum)
        {
            if (trialNum >= 1 && trialNum <= _trials.Count && _trials.Count > 1)
            {
                Trial trialToCopy = _trials[trialNum - 1];
                Random random = new Random();
                int insertIndex = random.Next(trialNum + 1, _trials.Count);

                _trials.Insert(insertIndex, trialToCopy);
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
    }
}
