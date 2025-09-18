using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static Multi.Cursor.BlockHandler;
using static Multi.Cursor.Utils;

namespace Multi.Cursor
{
    public class TrialRecord
    {
        // Classes
        public enum ButtonState
        {
            DEFAULT = 0, MARKED = 1, APPLIED = 2
        }

        public class TObject
        {
            public int Id { get; set; }
            public Point Position { get; set; }
            public ButtonState State { get; set; }

            public TObject(int id, Point position)
            {
                Id = id;
                Position = position;
                State = ButtonState.DEFAULT;
            }
        }

        public class TFunction
        {
            public int Id { get; set; }
            public int WidthInUnits { get; set; }
            public Point Center { get; set; }
            public Point Position { get; set; } // Top-left corner of the button
            public ButtonState State { get; set; }

            public TFunction(int id, int widthInUnits, Point center, Point position)
            {
                Id = id;
                Center = center;
                Position = position;
                WidthInUnits = widthInUnits;
                State = ButtonState.DEFAULT;
            }

        }

        //public int FunctionId;
        public List<TFunction> Functions;
        public List<TObject> Objects;
        public List<Pair> ObjFuncMap;

        public Rect ObjectAreaRect;
        public Dictionary<string, int> EventCounts;
        private List<Timestamp> Timestamps;
        private Dictionary<string, double> Times;

        public TrialRecord()
        {
            Functions = new List<TFunction>();
            Objects = new List<TObject>();
            ObjFuncMap = new List<Pair>();
            ObjectAreaRect = new Rect();
            EventCounts = new Dictionary<string, int>();
            Timestamps = new List<Timestamp>();
            Times = new Dictionary<string, double>();
        }

        public void MapObjectToFunction(int objectId, int functionId)
        {
            var pair = new Pair(objectId, functionId);
            if (!ObjFuncMap.Contains(pair))
            {
                ObjFuncMap.Add(pair);
            }
        }

        public TFunction GetFunctionById(int id)
        {
            foreach (TFunction func in Functions)
            {
                if (func.Id == id) return func;
            }

            return null;
        }

        public int FindMappedFunctionId(int objectId)
        {
            // Find the first function that is mapped to the given object funcId
            var pair = ObjFuncMap.FirstOrDefault(p => p.First == objectId);
            return pair != null ? pair.Second : -1; // Return -1 if no mapping found
        }

        public int FindMappedObjectId(int functionId)
        {
            // Find the first object that is mapped to the given function funcId
            var pair = ObjFuncMap.FirstOrDefault(p => p.Second == functionId);
            return pair != null ? pair.First : -1; // Return -1 if no mapping found
        }

        public bool IsEnabledFunction(int id)
        {
            // Check if it's the funcId of a function and its newState is enabled
            TFunction func = GetFunctionById(id);
            return (func != null) && (func.State == ButtonState.MARKED);
        }

        public bool AreAllFunctionsApplied()
        {
            foreach (TFunction func in Functions)
            {
                if (func.State != ButtonState.APPLIED)
                {
                    return false; // If any function is not selected, return false
                }
            }

            return true; // All functions are selected
        }

        public bool AreAllObjectsApplied()
        {
            foreach (TObject obj in Objects)
            {
                if (obj.State != ButtonState.APPLIED)
                {
                    return false; // If any object is not applied, return false
                }
            }
            return true; // All objects are applied
        }

        public bool IsAnyFunctionEnabled()
        {
            foreach (TFunction func in Functions)
            {
                if (func.State == ButtonState.MARKED)
                {
                    return true; // If any function is enabled, return true
                }
            }
            return false; // No functions are enabled
        }

        public void MarkObject(int id)
        {
            TObject obj = Objects.FirstOrDefault(o => o.Id == id);
            if (obj != null)
            {
                obj.State = ButtonState.MARKED;
            }
        }

        public void ApplyFunction(int funcId)
        {
            TFunction func = GetFunctionById(funcId);
            if (func != null)
            {
                func.State = ButtonState.APPLIED;
            }

            int nFuncs = Functions.Count;
            int nObjs = Objects.Count;

            //this.TrialInfo($"nFunc: {nFuncs}; nObj: {nObjs}");

            switch (nFuncs, nObjs) 
            {
                case (1, 1): // One function and one object => apply the function to the object
                    ChangeObjectState(1, ButtonState.APPLIED);
                    break;
                case (1, _): // One function and multiple objects => apply the function to the marked/enabled object
                    int markedObjId = Objects.FirstOrDefault(o => o.State == ButtonState.MARKED)?.Id ?? -1;
                    ChangeObjectState(markedObjId, ButtonState.APPLIED);
                    break;
                case (_, 1): // Multiple functions and one object => apply the function to the single object
                    ChangeObjectState(1, ButtonState.APPLIED);
                    break;
                default: // Multiple functions and multiple objects => apply the function to the object mapped to the function
                    int mappedObjId = FindMappedObjectId(funcId);
                    ChangeObjectState(mappedObjId, ButtonState.APPLIED);
                    break;
            }

        }

        public void MarkFunction(int id)
        {
            ChangeFunctionState(id, ButtonState.MARKED);
        }

        public void EnableAllFunctions()
        {
            foreach (TFunction func in Functions)
            {
                func.State = ButtonState.MARKED;
            }
        }

        public void MarkAllObjects()
        {
            foreach (TObject obj in Objects)
            {
                obj.State = ButtonState.MARKED;
            }
        }

        public void SetFunctionAsApplied(int funcId)
        {
            ChangeFunctionState(funcId, ButtonState.APPLIED);
        }

        public void MarkMappedObject(int funcId)
        {
            int objId = FindMappedObjectId(funcId);
            if (objId != -1)
            {
                ChangeObjectState(objId, ButtonState.MARKED);
            }
        }

        private void ChangeObjectState(int objId, ButtonState newState)
        {
            this.TrialInfo($"Change Obj#{objId} to {newState}");
            TObject markedObj = Objects.FirstOrDefault(o => o.Id == objId);
            if (markedObj != null)
            {
                this.TrialInfo($"Changed Obj#{objId} to {newState}");
                markedObj.State = newState;
            }
        }

        public void ChangeFunctionState(int funcId, ButtonState newState)
        {
            TFunction func = GetFunctionById(funcId);
            if (func != null)
            {
                func.State = newState;
            }
        }

        /// <summary>
        /// If the time stamp is gesture end, rename the previous one to match 
        /// e.g. TAP_UP -> change DOWN to TAP_DOWN 
        /// </summary>
        /// <param name="label"></param>
        public void AddTimestamp(string label)
        {
            Timestamp timestamp = new Timestamp(label);
            Timestamps.Add(timestamp);
            this.TrialInfo($"Added timestamp: {timestamp.ToString()}");
            if (label.EndsWith("_tap_up"))
            {
                long endTime = GetLastFingerActionTime(label);
                this.TrialInfo($"End time: {endTime}");
                var gestureStartTimestamp = Timestamps.LastOrDefault(ts => ts.label.EndsWith(Str.DOWN) && ts.time < endTime);
                if (gestureStartTimestamp != null)
                {
                    gestureStartTimestamp.label = gestureStartTimestamp.label.Replace(Str.DOWN, Str.TAP_DOWN);
                }
            }
        }

        public void AddTimestamp(string label, long time)
        {
            Timestamp timestamp = new Timestamp(label, time);
            Timestamps.Add(timestamp);
            this.TrialInfo($"Added timestamp: {timestamp.ToString()}");
        }

        public string TimestampsToString()
        {
            return string.Join(", ", Timestamps.Select(ts => $"{ts.label}: {ts.time}"));
        }

        public string GetLastTimestamp()
        {
            return Timestamps.Count > 0 ? Timestamps.Last().label : "No timestamps recorded";
        }

        public long GetFirstTimestamp(string label)
        {
            var timestamp = Timestamps.FirstOrDefault(ts => ts.label == label);
            if (timestamp != null)
            {
                return timestamp.time;
            }
            return -1; // Return -1 if the label is not found
        }

        public long GetLastTime(string label)
        {
            var timestamp = Timestamps.LastOrDefault(ts => ts.label == label);
            if (timestamp != null)
            {
                return timestamp.time;
            }

            return -1; // Return -1 if the label is not found
        }

        public long GetLastFingerActionTime(string action)
        {
            var timestamp = Timestamps.LastOrDefault(ts => ts.label.EndsWith(action));
            if (timestamp != null)
            {
                return timestamp.time;
            }

            return -1; // Return -1 if the label is not found
        }

        public long GetFingerTimeBefore(string label, long endTime)
        {
            // Return the last timestamp with label before the endLabel
            var timestamp = Timestamps.LastOrDefault(ts => ts.label.EndsWith(label) && ts.time < endTime);
            if (timestamp != null)
            {
                return timestamp.time;
            }

            return -1; // Return -1 if the label is not found

        }

        public int GetDuration(string startLabel, string endLabel)
        {
            long startTime = GetLastTime(startLabel);
            //this.TrialInfo($"{startLabel}: {tapStartTime}");
            long endTime = GetLastTime(endLabel);
            //this.TrialInfo($"{endLabel}: {tapEndTime}");
            return Utils.GetDuration(startTime, endTime);
        }

        public int GetDurationToFingerAction(string startLabel, string action)
        {
            this.TrialInfo($"Timestamps: {TimestampsToString()}");
            long startTime = GetLastTime(startLabel);
            this.TrialInfo($"startTime: {startTime}");
            long endTime = GetLastFingerActionTime(action);
            this.TrialInfo($"endTime: {endTime}");
            return Utils.GetDuration(startTime, endTime);
        }

        public int GetDurationFromFingerAction(string action, string endLabel)
        {
            this.TrialInfo($"Timestamps: {TimestampsToString()}");
            long startTime = GetLastFingerActionTime(action);
            long endTime = GetLastTime(endLabel);
            return Utils.GetDuration(startTime, endTime);
        }

        public int GetGestureDuration(Technique gesture)
        {
            switch (gesture)
            {
                case Technique.TOMO_TAP:
                    long tapEndTime = GetLastFingerActionTime(Str.TAP_UP);
                    long tapStartTime = GetFingerTimeBefore(Str.DOWN, tapEndTime);
                    return Utils.GetDuration(tapStartTime, tapEndTime);
                    break;
                
                case Technique.TOMO_SWIPE:
                    long swipeEndTime = GetLastFingerActionTime(Str.SWIPE_END);
                    long swipeStartTime = GetFingerTimeBefore(Str.SWIPE_START, swipeEndTime);
                    return Utils.GetDuration(swipeStartTime, swipeEndTime);
                    break;
            }

            return -1;
        }

        public double GetTime(string label)
        {
            if (Times.Any(t => t.Key == label))
            {
                return Times[label];
            }

            return -1;
        }

        public bool HasTime(string label)
        {
            return Times.Any(t => t.Key == label);
        }

        public bool HasTimestamp(string label)
        {
            return Timestamps.Any(ts => ts.label == label);
        }

        public void ClearTimestamps()
        {
            Timestamps.Clear();
        }

        public List<int> GetFunctionIds()
        {
            return Functions.Select(f => f.Id).ToList();
        }

        public List<Point> GetFunctionCenters()
        {
            return Functions.Select(f => f.Center).ToList();
        }

        public bool IsObjectPressed(int objId)
        {
            //this.TrialInfo($"Timestamps: {TimestampsToString()}");
            // Check if timestamps contains Str.OBJ_objId_Str.PRESS
            return Timestamps.Any(ts => ts.label.Contains(Str.Join(Str.OBJ, objId.ToString(), Str.PRESS)));
        }

        public void AddTime(string label, double time)
        {
            Times[label] = time;
        }
    }
}
