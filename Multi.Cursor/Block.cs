using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public Block(int id, List<double> targetWidthsMM, List<double> distsMM) 
        {
            _id = id;

            //-- Create trials (trial id = _id * 100 + num)
            int trialNum = 1;
            foreach (double targetWidthMM in targetWidthsMM)
            {
                foreach (double distMM in distsMM)
                {
                    Trial trial = new Trial(_id * 100 + trialNum, targetWidthMM, distMM);
                    _trials.Add(trial);
                    trialNum++;
                }
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
