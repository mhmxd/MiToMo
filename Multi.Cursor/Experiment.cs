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
        private static int N_BLOCKS = 1; // Number of blocks in the experiment
        private static int N_REPS_IN_BLOCK = 1;
        private double _distPaddingMM; // Padding to each side of the dist thresholds
        

        public static double Min_Target_Width_MM = TARGET_WIDTHS_MM.Min();
        public static double Max_Target_Width_MM = TARGET_WIDTHS_MM.Max();

        //-- Calculated
        public double Longest_Dist_MM;
        public double Shortest_Dist_MM;
        //private double LONGEST_DIST_MM = 293; // BenQ = 293 mm
        //private double SHORTEST_DIST_MM = 10; // BenQ = 10 mm

        public enum Technique { Auxursor_Swipe, Auxursor_Tap, Radiusor, Mouse }

        //-- Constants
        public static double START_WIDTH_MM = 6; // Same as our click experiment

        //-- Current state
        
        public enum RESULT { MISS, HIT, NO_START }

        //-- Information
        public Technique Active_Technique = Technique.Auxursor_Tap; // Set in the info dialog
        public int Participant_Number { get; set; } // Set in the info dialog
        private int _activeBlockNum;
        private int _activeBlockInd;
        private int _activeTrialNum;
        private int _activeTrialInd;
        //private Block _activeBlock;
        private List<Block> _blocks = new List<Block>();

        public Experiment(double shortDistMM, double longDistMM)
        {
            Participant_Number = 100; // Default
            Shortest_Dist_MM = shortDistMM;
            Longest_Dist_MM = longDistMM;

            //--- Generate the distances
            double distDiff = Longest_Dist_MM - Shortest_Dist_MM;
            //_distPaddingMM = 0.1 * distDiff;
            _distPaddingMM = 2.5; // 2.5 mm padding
            double oneThird = distDiff / 3;
            double twoThird = distDiff * 2 / 3;
            double midDist = Utils.RandDouble(
                oneThird + _distPaddingMM,
                twoThird - _distPaddingMM); // Middle distance
            double shortDist = Utils.RandDouble(
                Shortest_Dist_MM,
                oneThird - _distPaddingMM); // Shortest distance
            double longDist = Utils.RandDouble(
                twoThird + _distPaddingMM,
                Longest_Dist_MM); // Longest distance
            _distances.Add(shortDist);
            _distances.Add(midDist);
            _distances.Add(longDist);

            //-- Create blocks of trials
            for (int i = 0; i < 1; i++)
            {
                int blockId = Participant_Number * 100 + i;
                Block block = new Block(blockId, TARGET_WIDTHS_MM, _distances, N_REPS_IN_BLOCK);
                _blocks.Add(block);
            }

            //-- Init
            _activeBlockNum = 1;
            _activeBlockInd = 0;
            _activeTrialNum = 1;
            _activeTrialInd = 0;
            //_activeBlock = _blocks[0];

            Outlog<Experiment>().Information(ListToString(_distances));
        }

        public void Init(int ptc, string tech)
        {
            Participant_Number = ptc;
            if (tech == Str.TOUCH_MOUSE_TAP) Active_Technique = Technique.Auxursor_Tap;
            else if (tech == Str.TOUCH_MOUSE_SWIPE) Active_Technique = Technique.Auxursor_Swipe;
            else if (tech == Str.MOUSE) Active_Technique = Technique.Mouse;

            //-- Create blocks of trials
            for (int i = 0; i < N_BLOCKS; i++)
            {
                int blockId = Participant_Number * 100 + i;
                Block block = new Block(blockId, TARGET_WIDTHS_MM, _distances, N_REPS_IN_BLOCK);
                _blocks.Add(block);
            }

            //-- Init
            _activeBlockNum = 1;
            _activeBlockInd = 0;
            _activeTrialNum = 1;
            _activeTrialInd = 0;
            //_activeBlock = _blocks[0];
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

        public bool IsTechAuxCursor()
        {
            return Active_Technique == Technique.Auxursor_Swipe ||
                Active_Technique == Technique.Auxursor_Tap;
        }

        public bool IsTechRadiusor()
        {
            return Active_Technique == Technique.Radiusor;
        }

        public static double GetMinTargetWidthMM()
        {
            return TARGET_WIDTHS_MM.First();
        }

        private string ListToString<T>(List<T> list)
        {
            return "{" + string.Join(", ", list) + "}";
        }
    }
}
