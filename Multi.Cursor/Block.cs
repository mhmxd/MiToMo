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
    internal class Block
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

        private Experiment.BLOCK_TYPE _blockType = Experiment.BLOCK_TYPE.REPEATED_SIDE;
        public Experiment.BLOCK_TYPE BlockType
        {
            get => _blockType;
            set => _blockType = value;
        }

        public Block(Experiment.BLOCK_TYPE type, int id, List<double> targetWidthsMM, List<double> distsMM, int rep, Location loc = Location.Top)
        {
            _id = id;
            _blockType = type;

            if (type == Experiment.BLOCK_TYPE.REPEATED_SIDE) // One side is repeated (w/ different Targets and dists)
            {
                int trialNum = 1;
                for (int r = 0; r < rep; r++)
                {
                    foreach (double targetWidthMM in targetWidthsMM)
                    {
                        foreach (double distMM in distsMM)
                        {
                            Trial trial = new Trial(_id * 100 + trialNum, targetWidthMM, distMM, loc);
                            _trials.Add(trial);
                            trialNum++;
                        }
                    }
                }
            }

            else if (type == Experiment.BLOCK_TYPE.ALTERNATING_SIDE) // Alternating sides
            {
                int trialNum = 1;
                for (int r = 0; r < rep; r++)
                {
                    foreach (double targetWidthMM in targetWidthsMM)
                    {
                        foreach (double distMM in distsMM)
                        {
                            for (int locInd = 0; locInd < 3; locInd++)
                            {
                                Location sideWindow = (Location)locInd;
                                Trial trial = new Trial(_id * 100 + trialNum, targetWidthMM, distMM, sideWindow);
                                _trials.Add(trial);
                                trialNum++;
                            }
                        }
                    }
                }
            }

            // Shuffle the trials
            _trials.Shuffle();
        }

        public Block(int id, List<double> targetWidthsMM, List<double> distsMM, int rep)
        {
            _id = id;

            //-- Create trials (dist x targetWidth x sideWindow)
            // trial id = _id * 100 + num
            int trialNum = 1;
            for (int r = 0; r < rep; r++)
            {
                foreach (double targetWidthMM in targetWidthsMM)
                {
                    foreach (double distMM in distsMM)
                    {
                        for (int locInd = 0; locInd < 3; locInd++)
                        {
                            Location sideWindow = (Location)locInd;
                            Trial trial = new Trial(_id * 100 + trialNum, targetWidthMM, distMM, sideWindow);
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
                int insertIndex = random.Next(trialNum + 1, _trials.Count + 1);

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
