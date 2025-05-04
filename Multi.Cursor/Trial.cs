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

        public Point StartPosition, TargetPosition; // Relative to the respective windows

        // Trial number (not needed for now)
        //private int _number { get; set; }

        private Location _targetLocation; // Side window to show target in
        public Location TargetLocation
        {
            get => _targetLocation;
            set => _targetLocation = value;
        }

        //private int _sideWindowInd; // 0 (left), 1 (right), 2 (top) -> side window to show target in
        //public int SideWindowInd
        //{
        //    get => _sideWindowInd;
        //    set => _sideWindowInd = value;
        //}

        //private int _angle;
        //private bool _straightPath; // True (stright), false (diagonal)
        //public bool StrightPath
        //{
        //    get => _straightPath;
        //    set => _straightPath = value;
        //}

        //=========================================================================


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="targetWidthMM"> Target width in mm</param>
        /// <param name="distMM">Distance to target in mm</param>
        public Trial(int id, double targetWidthMM, double distMM, Location sideWin)
        {
            _id = id;
            _targetWidthMM = targetWidthMM;
            _distanceMM = distMM;
            _targetLocation = sideWin;
            //Location[] validDirections = { Location.Top, Location.Left, Location.Right };
            //_sideWindow = validDirections[Utils.Random.Next(validDirections.Length)];
            //_straightPath = true;
        }

        public override string ToString()
        {
            return $"Trial: [Id = {_id}, W = {_targetWidthMM}mm, D = {_distanceMM:F2}mm, Loc = {_targetLocation}]";
        }
    
        
    }
}
