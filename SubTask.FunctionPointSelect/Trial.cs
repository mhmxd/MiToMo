using Common.Helpers;
using CommonUI;
using System.Collections.Generic;
using System.IO;
using static Common.Constants.ExpEnums;

namespace SubTask.FunctionPointSelect
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

        public Technique Technique { get; set; } // Always MOUSE for this susbtask
        public int PtcNum { get; set; }

        public TaskType TaskType { get; set; }
        public Complexity Complexity { get; set; }
        public ExperimentType ExpType { get; set; }

        // Target circle diameter
        private double _targetWidthMM;
        public double TargetWidthMM
        {
            get => _targetWidthMM;
            set => _targetWidthMM = value;
        }
        public double TargetWidthPX => UITools.MM2PX(TargetWidthMM);

        public MRange DistRangeMM { get; set; }
        public MRange DistRangePX => new MRange(UITools.MM2PX(DistRangeMM.Min), UITools.MM2PX(DistRangeMM.Max), DistRangeMM.Label);


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

        public int NFunctions => _functionWidths.Count;

        //=========================================================================

        public Trial(int id)
        {
            this._id = id;
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
        public static Trial CreateTrial(
            int id, int ptc, Complexity complexity, ExperimentType expType,
            Side side, MRange distRangeMM, int funcWidth)
        {
            Trial trial = new Trial(id);
            trial.PtcNum = ptc;
            trial.Technique = Technique.MOUSE;
            trial.Complexity = complexity;
            trial.ExpType = expType;
            trial.FuncSide = side;
            trial.DistRangeMM = distRangeMM;
            trial.AddFunctionWidth(funcWidth);

            trial.TaskType = TaskType.FUNCTION_POINT_SELECT;

            return trial;
        }

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

        public int GetFunctionWidth(int index)
        {
            return _functionWidths[index];
        }

        public int GetFunctionWidthMM()
        {
            return _functionWidths[0] * 4; // Width is in MX (1 MX = 4 mm)
        }

        public int GetNumFunctions()
        {
            return _functionWidths.Count;
        }

        //public override string ToString()
        //{
        //    if (_distanceMM == 0)
        //        return $"Trial: [Id = {_id}, W = {_functionWidths.ToStr()} units, D = {DistRangeMM.Label}, Side = {_funcSide}]";
        //    else
        //        return $"Trial: [Id = {_id}, W = {_functionWidths.ToStr()} units, D = {_distanceMM:F2} mm, Side = {_funcSide}]";
        //}

        public string ToStr()
        {
            return $"Trial#{Id} [Target = {FuncSide.ToString()}, " +
                $"FunctionWidths = {GetFunctionWidths().ToStr()}, Dist = {DistRangeMM.ToString()}]";
        }

        public string GetCacheFileName(string cachedDirectory)
        {
            // Create a unique file name based on trial parameters
            return Path.Combine(cachedDirectory, $"Cache_{FuncSide}_{_functionWidths.ToString()}_{DistRangeMM.Label}.json");
        }

        public bool IsTechniqueToMo()
        {
            return Technique == Technique.TOMO || Technique == Technique.TOMO_SWIPE || Technique == Technique.TOMO_TAP;
        }


    }
}
