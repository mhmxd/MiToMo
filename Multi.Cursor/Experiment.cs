using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Math;
using static Multi.Cursor.Output;

namespace Multi.Cursor
{
    internal class Experiment
    {
        //-- Variables
        public List<double> TARGET_WIDTHS_MM = new List<double>() { 4, 12, 20 };
        private List<double> DISTANCES_MM = new List<double>() { 100, 200 };
        private double _longestDistMM; // BenQ = 568 mm
        private double _sortestDistMM; // BenQ = 10 mm
        private double DISTANCE_PADDING_MM = 10; // Padding to keep random dists from being too close
        public enum Technique { Auxursor_Swipe, Auxursor_Tap, Radiusor, Mouse }

        //-- Constants
        public static double START_WIDTH_MM = 6; // Same as our click experiment

        //-- States
        public enum RESULT { MISS, HIT }

        //-- Information
        public int ParticipantNumber { get; set; }
        public Technique Active_Technique = Technique.Auxursor_Tap;
        private int _activeBlockNum;
        private int _activeBlockInd;
        private int _activeTrialNum;
        private int _activeTrialInd;
        private Block _activeBlock;
        private List<Block> _blocks = new List<Block>();

        public Experiment()
        {
            ParticipantNumber = 100;

            //-- Create blocks of trials
            for (int i = 0; i < 1; i++)
            {
                Block block = new Block(ParticipantNumber * 100 + i, TARGET_WIDTHS_MM, DISTANCES_MM);
                _blocks.Add(block);
            }

            //-- Init
            _activeBlockNum = 1;
            _activeBlockInd = 0;
            _activeTrialNum = 1;
            _activeTrialInd = 0;
            _activeBlock = _blocks[0];

            //--- Generate the distances
            double oneThird = DISTANCES_MM.Max() / 3;
            double twoThird = DISTANCES_MM.Max() * 2 / 3;
            double midDist = Utils.RandDouble(oneThird, twoThird); // Middle distance
            double shortDist = Utils.RandDouble(
                _sortestDistMM, 
                Min(oneThird, midDist - DISTANCE_PADDING_MM)); // Shortest distance
            double longDist = Utils.RandDouble(
                Max(midDist + DISTANCE_PADDING_MM, twoThird),
                _longestDistMM); // Longest distance

            TRIAL_LOG.Information($"Distances: {shortDist}, {midDist}, {longDist}");
        }

        public Experiment(int ptc, int nBlocks)
        {
            ParticipantNumber = ptc;

            //-- Create blocks of trials
            for (int i = 0; i < nBlocks; i++)
            {
                Block block = new Block(ParticipantNumber * 100 + i, TARGET_WIDTHS_MM, DISTANCES_MM);
                _blocks.Add(block);
            }

            //-- Init
            _activeBlockNum = 1;
            _activeBlockInd = 0;
            _activeTrialNum = 1;
            _activeTrialInd = 0;
            _activeBlock = _blocks[0];
        }

        public int GetNumBlocks()
        {
            return _blocks.Count;
        }

        public Block GetBlock(int blockNum)
        {
            int index = blockNum - 1;
            if (index < _blocks.Count()) return _blocks[index];
            else return null;
        }

        public Trial GetFirstTrial()
        {
            if (_blocks.Count() > 0) return _activeBlock.GetTrial(_activeTrialNum);
            else return null;
        }

        //public bool IsLastTrialInBlock()
        //{
        //    return _activeTrialNum == _activeBlock.GetNumTrials();
        //}

        //public Trial GetNextTrial()
        //{
        //    if (_activeTrialNum < _activeBlock.GetNumTrials())
        //    {
        //        _activeTrialNum++;
        //        return _activeBlock.GetTrial(_activeTrialNum);
        //    } else
        //    {
        //        return null;
        //    }
        //}

        public bool IsTechAuxCursor()
        {
            return Active_Technique == Technique.Auxursor_Swipe ||
                Active_Technique == Technique.Auxursor_Tap;
        }

        public bool IsTechRadiusor()
        {
            return Active_Technique == Technique.Radiusor;
        }

    }
}
