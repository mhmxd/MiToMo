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
        private static List<double> TARGET_WIDTHS_MM = new List<double>() { 4, 12, 20 };
        private static List<double> _distances = new List<double>(); // Generated in constructor
        private double LONGEST_DIST_MM = 293; // BenQ = 293 mm
        private double SHORTEST_DIST_MM = 10; // BenQ = 10 mm
        private double _distPaddingMM; // Padding to keep random dists from being too close (set to 0.1 Total Dist)
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

            //--- Generate the distances
            double distDiff = LONGEST_DIST_MM - SHORTEST_DIST_MM;
            _distPaddingMM = 0.1 * distDiff;
            double oneThird = distDiff / 3;
            double twoThird = distDiff * 2 / 3;
            double midDist = Utils.RandDouble(oneThird, twoThird); // Middle distance
            double shortDist = Utils.RandDouble(
                SHORTEST_DIST_MM,
                Min(oneThird, midDist - _distPaddingMM)); // Shortest distance
            _distances.Add(shortDist);
            _distances.Add(midDist);
            double longDist = Utils.RandDouble(
                Max(midDist + _distPaddingMM, twoThird),
                LONGEST_DIST_MM); // Longest distance
            _distances.Add(longDist);

            //-- Create blocks of trials
            for (int i = 0; i < 1; i++)
            {
                Block block = new Block(ParticipantNumber * 100 + i, TARGET_WIDTHS_MM, _distances);
                _blocks.Add(block);
            }

            //-- Init
            _activeBlockNum = 1;
            _activeBlockInd = 0;
            _activeTrialNum = 1;
            _activeTrialInd = 0;
            _activeBlock = _blocks[0];

            
        }

        public Experiment(int ptc, int nBlocks)
        {
            ParticipantNumber = ptc;

            //-- Create blocks of trials
            for (int i = 0; i < nBlocks; i++)
            {
                Block block = new Block(ParticipantNumber * 100 + i, TARGET_WIDTHS_MM, _distances);
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

        public static double GetMinTargetW()
        {
            return TARGET_WIDTHS_MM.First();
        }

        private string ListToString<T>(List<T> list)
        {
            return "{" + string.Join(", ", list) + "}";
        }
    }
}
