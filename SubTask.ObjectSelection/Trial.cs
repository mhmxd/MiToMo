using Common.Helpers;
using CommonUI;
using System.Collections.Generic;
using System.IO;
using static Common.Constants.ExpEnums;

namespace SubTask.ObjectSelection
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

        private int _nObjects;
        public int NObjects
        {
            get => _nObjects;
            set => _nObjects = value;
        }

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
        public static Trial CreateTrial(int id, Technique tech, ExperimentType expType, int ptc, int nObj)
        {
            Trial trial = new Trial(id);
            trial.Technique = tech;
            trial.ExpType = expType;
            trial.PtcNum = ptc;
            trial.NObjects = nObj;

            trial.TaskType = TaskType.OBJECT_SELECT;

            return trial;
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
            return $"Trial#{Id} [nObj = {NObjects}]";
        }

        public Trial Clone()
        {
            // Since all current fields (Id, Technique, PtcNum, TaskType, Complexity, ExpType, NObjects) 
            // are value types or enums, MemberwiseClone() creates a perfect independent copy.
            return (Trial)this.MemberwiseClone();
        }


    }
}
