using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Multi.Cursor
{
    // A trial in the experiment
    public class Trial
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
        public double TargetWidthPX => Utils.MM2PX(TargetWidthMM);

        // Distance to the target center, from start's center
        private double _distanceMM;
        public double DistanceMM
        {
            get => _distanceMM;
            set => _distanceMM = value;
        }
        public int DistancePX => Utils.MM2PX(DistanceMM);

        public List<double> Distances = new List<double>(); // Distances in px

        public Range DistRangeMM { get; set; }
        public Range DistRangePX => DistRangeMM.GetPx(); // Distance range in px

        //public Point StartPosition, TargetPosition; // Relative to the respective windows

        // Trial number (not needed for now)
        //private int _number { get; set; }

        private Side _funcSide; // Side window to show target in
        public Side FuncSide
        {
            get => _funcSide;
            set => _funcSide = value;
        }

        private List<int> _functionWidths = new List<int>(); // Function widths in px (for multi-function trials)

        private int _nObjects;
        public int NObjects
        {
            get => _nObjects;
            set => _nObjects = value;
        }

        //private int _targetMultiple; // Multiples of the target width (ref. the Experiment.TARGET_WIDTHS_MM list)
        //public int TargetMultiple
        //{
        //    get => _targetMultiple;
        //    set => _targetMultiple = value;
        //}

        //=========================================================================

        public Trial(int id)
        {
            this._id = id;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="targetWidthMM"> Target width in mm</param>
        /// <param name="distMM">Distance to target in mm</param>
        public Trial(int id, int functionWidthMX, double distMM, Side sideWin)
        {
            _id = id;
            //_targetWidthMM = targetWidthMM;
            //_targetMultiple = functionWidthMX;
            _functionWidths.Add(functionWidthMX);
            _distanceMM = distMM;
            _funcSide = sideWin;
            //Side[] validDirections = { Side.Top, Side.Left, Side.Right };
            //_sideWindow = validDirections[Utils.Random.Next(validDirections.Length)];
            //_straightPath = true;
        }

        /// <summary>
        /// Create trials
        /// What sets single/multi functions is the number of functionWidthsMX
        /// </summary>
        /// <param name="id"></param>
        /// <param name="side"></param>
        /// <param name="distRangeMM"></param>
        /// <param name="nObj"></param>
        /// <param name="functionWidthsMX"></param>
        /// <returns></returns>
        public static Trial CreateTrial(int id, Side side, Range distRangeMM, int nObj, List<int> functionWidthsMX)
        {
            Trial trial = new Trial(id);
            trial.FuncSide = side;
            trial.DistRangeMM = distRangeMM;
            trial.AddFunctionWidths(functionWidthsMX);
            trial.NObjects = nObj;

            return trial;
        }

        //public static Trial CreateSingleObjSingleFuncTrial(int id, Side side, int functionWidthMX, Range distRangeMM)
        //{
        //    Trial trial = new Trial(id);
        //    trial.FuncSide = side;
        //    trial.AddFunctionWidth(functionWidthMX);
        //    trial.DistRangeMM = distRangeMM;
        //    return trial;
        //}

        //public static Trial CreateSingleObjMultiFuncTrial(int id, Side side, List<int> functionWidthsMX, Range distRangeMM)
        //{
        //    Trial trial = new Trial(id);
        //    trial.FuncSide = side;
        //    trial.AddFunctionWidths(functionWidthsMX);
        //    trial.DistRangeMM = distRangeMM;
        //    return trial;
        //}

        //public static Trial CreateSingleObjMultiFuncTrial(int id, Side side, int funcWidthMX, Range distRangeMM)
        //{
        //    Trial trial = new Trial(id);
        //    trial.FuncSide = side;
        //    trial.AddFunctionWidths(new List<int>() { funcWidthMX, funcWidthMX, funcWidthMX });
        //    trial.DistRangeMM = distRangeMM;
        //    return trial;
        //}

        //public static Trial CreateMultiObjSingleFuncTrial(int id, Side side, int funcWidthMX, Range distRangeMM, int nObj)
        //{
        //    Trial trial = new Trial(id);
        //    trial.FuncSide = side;
        //    trial.AddFunctionWidth(funcWidthMX);
        //    trial.DistRangeMM = distRangeMM;
        //    trial.NObjects = nObj;
        //    return trial;
        //}

        //public static Trial CreateMultiObjMultiFuncTrial(int id, Side side, int funcWidthMX, Range distRangeMM, int nObj)
        //{
        //    Trial trial = new Trial(id);
        //    trial.FuncSide = side;
        //    trial.AddFunctionWidth(funcWidthMX);
        //    trial.DistRangeMM = distRangeMM;
        //    trial.NObjects = nObj;
        //    return trial;
        //}

        //public static Trial CreateMultiObjcectTrial(int id, Side side, int functionWidthMX, Range distRangeMM, int nPasses)
        //{
        //    Trial trial = new Trial(id);
        //    trial.FuncSide = side;
        //    //trial.TargetMultiple = functionWidthMX;
        //    trial._functionWidths.Add(functionWidthMX);
        //    trial.DistRangeMM = distRangeMM;
        //    //for (int i = 0; i < nPasses; i++)
        //    //{
        //    //    int randDist = Utils.MM2PX(distRange.GetRandomValue()); // Get a random distance in px
        //    //    trial.Distances.Add(randDist);
        //    //}

        //    return trial;
        //}

        //public static Trial CreateSingleObjectTrial(int id, Side side, int functionWidthMX, Range distRangeMM)
        //{
        //    Trial trial = new Trial(id);
        //    trial.FuncSide = side;
        //    //trial.TargetMultiple = targetWidthU;
        //    trial._functionWidths.Add(functionWidthMX);
        //    trial.DistanceMM = distRangeMM.GetRandomValue();
        //    return trial;
        //}

        public void AddFunctionWidth(int functionWidth)
        {
            _functionWidths.Add(functionWidth);
        }

        public void AddFunctionWidths(List<int> functionWidths)
        {
            foreach (int functionWidth in functionWidths)
            {
                AddFunctionWidth(functionWidth);
            }
        }

        public List<int> GetFunctionWidths()
        {
            return _functionWidths;
        }

        public override string ToString()
        {
            if (_distanceMM == 0)
                return $"Trial: [Id = {_id}, W = {_functionWidths.ToStr()} units, D = {DistRangeMM.Label}, Side = {_funcSide}]";
            else
                return $"Trial: [Id = {_id}, W = {_functionWidths.ToStr()} units, D = {_distanceMM:F2} mm, Side = {_funcSide}]";
        }

        public string ToStr()
        {
            return $"Trial#{Id} [Target = {FuncSide.ToString()}, " +
                $"FunctionWidths = {GetFunctionWidths().ToStr()}, Dist Range (mm) = {DistRangeMM.ToString()}]";
        }

        public string GetCacheFileName(string cachedDirectory)
        {
            // Create a unique file name based on trial parameters
            return Path.Combine(cachedDirectory, $"Cache_{FuncSide}_{_functionWidths.ToString()}_{DistRangeMM.Label}.json");
        }




    }
}
