using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Multi.Cursor
{
    // A trial in the experiment
    internal class Trial
    {
        // Trial Id
        private int _id { get; set; }
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        // Target circle diameter
        private double _targetWidthMM;
        public double TargetWidthMM 
        {
            get => _targetWidthMM;
            set => _targetWidthMM = value;
        }
        public int TargetWidthPX => Utils.MM2PX(TargetWidthMM);

        // Distance to the target center, from start's center
        private double _distanceMM;
        public double DistanceMM
        {
            get => _distanceMM;
            set => _distanceMM = value;
        }
        public int DistancePX => Utils.MM2PX(DistanceMM);

        //public Point StartPosition, TargetPosition; // Relative to the respective windows

        // Trial number (not needed for now)
        //private int _number { get; set; }

        private Side _targetSide; // Side window to show target in
        public Side TargetSide
        {
            get => _targetSide;
            set => _targetSide = value;
        }

        private int _targetMultiple; // Multiples of the target width (ref. the Experiment.TARGET_WIDTHS_MM list)
        public int TargetMultiple
        {
            get => _targetMultiple;
            set => _targetMultiple = value;
        }

        //=========================================================================


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="targetWidthMM"> Target width in mm</param>
        /// <param name="distMM">Distance to target in mm</param>
        public Trial(int id, int targetMultiple, double distMM, Side sideWin)
        {
            _id = id;
            //_targetWidthMM = targetWidthMM;
            _targetMultiple = targetMultiple;
            _distanceMM = distMM;
            _targetSide = sideWin;
            //Side[] validDirections = { Side.Top, Side.Left, Side.Right };
            //_sideWindow = validDirections[Utils.Random.Next(validDirections.Length)];
            //_straightPath = true;
        }

        public override string ToString()
        {
            return $"Trial: [Id = {_id}, W = {_targetMultiple} units, D = {_distanceMM:F2} mm, Loc = {_targetSide}]";
        }
    
        
    }
}
