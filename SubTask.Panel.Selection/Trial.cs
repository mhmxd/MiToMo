using System.Collections.Generic;
using static Common.Constants.ExpEnums;

namespace SubTask.Panel.Selection
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

        private Side _funcSide; // Side window to show target in
        public Side FuncSide
        {
            get => _funcSide;
            set => _funcSide = value;
        }

        private List<int> _functionWidths = new List<int>(); // Function widths in px (for multi-function trials)

        //=========================================================================

        public Trial(int id)
        {
            this._id = id;
        }

        public static Trial CreateTrial(
            int id, Technique tech, int ptc, Complexity complexity,
            Side side)
        {
            Trial trial = new Trial(id);
            trial.Technique = tech;
            trial.PtcNum = ptc;
            trial.Complexity = complexity;
            trial.FuncSide = side;

            trial.TaskType = TaskType.PANEL_SELECT;

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

        public int GetNumFunctions()
        {
            return _functionWidths.Count;
        }

        public string ToStr()
        {
            return $"Trial#{Id} [Target = {FuncSide.ToString()}";
        }

        public bool IsTechniqueToMo()
        {
            return Technique == Technique.TOMO || Technique == Technique.TOMO_SWIPE || Technique == Technique.TOMO_TAP;
        }


    }
}
