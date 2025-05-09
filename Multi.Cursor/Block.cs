using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        internal Trial GetNextTrial(int trialNum)
        {
            int index = trialNum - 1;
            if (index < _trials.Count() - 1) return _trials[index + 1];
            else return null; // It was the last trial
        }
    }
}
