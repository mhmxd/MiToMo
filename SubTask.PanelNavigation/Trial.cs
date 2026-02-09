using System.Collections.Generic;
using static Common.Constants.ExpEnums;

namespace SubTask.PanelNavigation
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

        public Technique Technique { get; set; }
        public int PtcNum { get; set; }

        public TaskType TaskType { get; set; }
        public Complexity Complexity { get; set; }
        public ExperimentType ExpType { get; set; }

        private Side _funcSide; // Side window to show target in
        public Side FuncSide
        {
            get => _funcSide;
            set => _funcSide = value;
        }

        private List<int> _functionWidths = new(); // Function widths in px (for multi-function trials)

        //=========================================================================

        public Trial(int id)
        {
            this._id = id;
        }

        public static Trial CreateTrial(
            int id, Technique tech, int ptc,
            Complexity complexity, ExperimentType expType,
            Side side, int funcWidth)
        {
            Trial trial = new Trial(id);
            trial.Technique = tech;
            trial.PtcNum = ptc;
            trial.Complexity = complexity;
            trial.ExpType = expType;
            trial.FuncSide = side;
            trial.AddFunctionWidth(funcWidth);

            trial.TaskType = TaskType.PANEL_NAVIGATE;

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


        public int GetFunctionWidth(int functionIndex)
        {
            return _functionWidths[functionIndex];
        }

        public string Str()
        {
            return $"Trial#{Id} [Target = {FuncSide.ToString()}]";
        }

        public Trial Clone()
        {
            // 1. Create a shallow copy for the basic value types (Id, Side, Enums, etc.)
            Trial clone = (Trial)this.MemberwiseClone();

            // 2. Explicitly "Deep Copy" the private list 
            // This creates a NEW list so they don't share data
            clone._functionWidths = new List<int>(this._functionWidths);

            return clone;
        }


    }
}
