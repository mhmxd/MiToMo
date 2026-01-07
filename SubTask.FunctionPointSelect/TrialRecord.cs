using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Common.Constants;
using static SubTask.FunctionPointSelect.BlockHandler;
using static SubTask.FunctionPointSelect.Utils;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static Common.Constants.ExpEnums;

namespace SubTask.FunctionPointSelect
{
    public class TrialRecord
    {

        public class TObject
        {
            public int Id { get; set; }
            public Point Position { get; set; }
            public Point Center { get; set; }
            public ButtonState State { get; set; }

            public TObject(int id, Point position, Point center)
            {
                Id = id;
                Position = position;
                Center = center;
                State = ButtonState.DEFAULT;
            }

        }

        public class TFunction
        {
            public int Id { get; set; }
            public int WidthInUnits { get; set; }
            public Point Center { get; set; }
            public Point Position { get; set; } // Top-left corner of the button
            public int DistanceToObjArea; // in pixels
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
        public double DistanceMM; // in mm

        public Rect StartBtnRect;
        //public Rect ObjectAreaRect;
        //public Dictionary<string, int> EventCounts;
        private List<TrialEvent> Events;
        private Dictionary<string, double> Times;

        public Result Result;

        public TrialRecord()
        {
            Functions = new List<TFunction>();
            Objects = new List<TObject>();
            ObjFuncMap = new List<Pair>();
            StartBtnRect = new Rect(0, 0, ExpSizes.START_BUTTON_LARGER_SIDE_MM, ExpSizes.START_BUTTON_LARGER_SIDE_MM);
            //ObjectAreaRect = new Rect();
            //EventCounts = new Dictionary<string, int>();
            Events = new List<TrialEvent>();
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

        public int FindFunctionIndex(int funId)
        {
            for (int i = 0; i < Functions.Count; i++)
            {
                if (Functions[i].Id == funId) return i;
            }

            return -1;
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
                if (func.State != ButtonState.SELECTED)
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
                if (obj.State != ButtonState.SELECTED)
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

        public void UnmarkObject(int id)
        {
            TObject obj = Objects.FirstOrDefault(o => o.Id == id);
            if (obj != null)
            {
                obj.State = ButtonState.DEFAULT;
            }
        }

        public void ApplyFunction(int funcId, int objId)
        {
            this.TrialInfo($"FuncId: {funcId}");
            TFunction func = GetFunctionById(funcId);
            if (func != null)
            {
                func.State = ButtonState.SELECTED;
            }

            // Apply to the specified object
            //if (objId != -1) ChangeObjectState(objId, ButtonState.APPLIED);

            //int nFuncs = Functions.Count;
            //int nObjs = Objects.Count;

            ////this.TrialInfo($"nFunc: {nFuncs}; nObj: {nObjs}");

            //switch (nFuncs, nObjs) 
            //{
            //    case (1, 1): // One function and one object => apply the function to the object
            //        ChangeObjectState(1, ButtonState.APPLIED);
            //        break;
            //    case (1, _): // One function and multiple objects => apply the function to the marked/enabled object
            //        int markedObjId = Objects.FirstOrDefault(o => o.State == ButtonState.MARKED)?.Id ?? -1;
            //        ChangeObjectState(markedObjId, ButtonState.APPLIED);
            //        break;
            //    case (_, 1): // Multiple functions and one object => apply the function to the single object
            //        //ChangeObjectState(1, ButtonState.APPLIED);
            //        break;
            //    default: // Multiple functions and multiple objects => apply the function to the object mapped to the function
            //        int mappedObjId = FindMappedObjectId(funcId);
            //        ChangeObjectState(mappedObjId, ButtonState.APPLIED);
            //        break;
            //}

        }

        public void MarkFunction(int id)
        {
            ChangeFunctionState(id, ButtonState.MARKED);
            this.TrialInfo($"Function#{id} marked.");
        }

        public void UnmarkFunction(int id)
        {
            ChangeFunctionState(id, ButtonState.DEFAULT);
            this.TrialInfo($"Function#{id} demarked.");
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

        public void UnmarkAllObjects()
        {
            foreach (TObject obj in Objects)
            {
                obj.State = ButtonState.DEFAULT;
            }
        }

        public void SetFunctionAsApplied(int funcId)
        {
            ChangeFunctionState(funcId, ButtonState.SELECTED);
        }

        public void EnableFunction()
        {
            ChangeFunctionState(Functions[0].Id, ButtonState.ENABLED);
        }

        public void MarkMappedObject(int funcId)
        {
            int objId = FindMappedObjectId(funcId);
            if (objId != -1)
            {
                ChangeObjectState(objId, ButtonState.MARKED);
            }
        }

        public void ChangeObjectState(int objId, ButtonState newState)
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
        /// If the Time stamp is gesture end, rename the previous one to match 
        /// e.g. TAP_UP -> change DOWN to TAP_DOWN 
        /// </summary>
        /// <param name="label"></param>
        public void RecordEvent(string type, string id)
        {
            TrialEvent trialEvent = new TrialEvent(type, id);
            Events.Add(trialEvent);
            this.TrialInfo($"[+] {trialEvent.ToString()}");

            if (type == ExpStrs.TAP_UP)
            {
                long endTime = GetLastFingerActionTime(type);
                this.TrialInfo($"End Time: {endTime}");
                var gestureStartTimestamp = Events.LastOrDefault(ts => ts.Type == ExpStrs.DOWN && ts.Time < endTime);
                if (gestureStartTimestamp != null)
                {
                    gestureStartTimestamp.Type = ExpStrs.TAP_DOWN;
                }
            }
        }

        public string TrialEventsToString()
        {
            return string.Join("; ", Events.Select(ts => ts.ToString()));
        }

        public string GetLastTrialEventType()
        {
            return Events.Count > 0 ? Events.Last().Type : "No timestamps recorded";
        }

        public TrialEvent GetLastTrialEvent()
        {
            return Events.Count > 0 ? Events.Last() : null;
        }

        public TrialEvent GetBeforeLastTrialEvent()
        {
            return Events.Count > 1 ? Events[Events.Count - 2] : null;
        }

        public int GetEventIndex(string type)
        {
            return Events.FindIndex(ts => ts.Type == type);
        }

        public long GetFirstTime(string type)
        {
            var timestamp = Events.FirstOrDefault(ts => ts.Type == type);
            if (timestamp != null)
            {
                return timestamp.Time;
            }
            return -1; // Return -1 if the type is not found
        }

        public long GetFirstAfterLast(string beforeType, string type)
        {
            if (Events == null || Events.Count == 0)
                return -1;

            var before = Events.LastOrDefault(t => t.Type == beforeType);
            if (before == null)
                return -1;

            var after = Events
                .Where(t => t.Type == type && t.Time > before.Time)
                .OrderBy(t => t.Time)
                .FirstOrDefault();

            return after == null ? -1 : after.Time;
        }

        public long GetImmediateBeforeLast(string afterLabel)
        {
            if (Events == null || Events.Count == 0)
                return -1;

            int afterIndex = Events.FindLastIndex(t => t.Type == afterLabel);
            if (afterIndex > 0)
            {
                return Events[afterIndex - 1].Time;
            }

            return -1; // not found or nothing before it
        }

        public long GetLastTime(string type)
        {
            var trialEvent = Events.LastOrDefault(ts => ts.Type == type);
            if (trialEvent != null)
            {
                return trialEvent.Time;
            }

            return -1; // Return -1 if the type is not found
        }

        public long GetLastFingerActionTime(string action)
        {
            var trialEvent = Events.LastOrDefault(ts => ts.Type == action);
            if (trialEvent != null)
            {
                return trialEvent.Time;
            }

            return -1; // Return -1 if the type is not found
        }

        public long GetFingerTimeBefore(string type, long endTime)
        {
            // Return the last trialEvent with type before the endType
            var timestamp = Events.LastOrDefault(ts => ts.Type == type && ts.Time < endTime);
            if (timestamp != null)
            {
                return timestamp.Time;
            }

            return -1; // Return -1 if the type is not found

        }

        public int GetDuration(string startLabel, string endLabel)
        {
            long startTime = GetLastTime(startLabel);
            this.TrialInfo($"Start time ({startLabel}): {startTime}");
            long endTime = GetLastTime(endLabel);
            this.TrialInfo($"End time ({endLabel}): {endTime}");
            return Utils.GetDuration(startTime, endTime);
        }

        public int GetDurtionToFirstAfter(string startLabel, string endLabel)
        {
            long startTime = GetLastTime(startLabel);
            this.TrialInfo($"Start time ({startLabel}): {startTime}");
            long endTime = GetFirstAfterLast(startLabel, endLabel);
            this.TrialInfo($"End time ({endLabel}): {endTime}");
            return Utils.GetDuration(startTime, endTime);
        }

        public int GetFirstSeqDuration(string startType, string endType)
        {
            if (Events == null || Events.Count == 0)
                return -1;

            for (int i = 0; i < Events.Count; i++)
            {
                if (Events[i].Type == startType)
                {
                    // found the start, look forward for the end
                    for (int j = i + 1; j < Events.Count; j++)
                    {
                        if (Events[j].Type == endType)
                        {
                            return Utils.GetDuration(Events[i].Time, Events[j].Time);
                        }
                    }
                }
            }

            return -1; // not found
        }

        /// <summary>
        /// Calculates the duration between the N-th occurrence of a startType event 
        /// and the first subsequent endType event.
        /// </summary>
        /// <param name="startType">The type of the starting event (e.g., "Pressed").</param>
        /// <param name="endType">The type of the ending event (e.g., "Released").</param>
        /// <param name="n">The 1-based index (occurrence) to find (e.g., 3 for the third time).</param>
        /// <returns>The duration in a suitable unit (depending on Utils.GetDuration), or -1 if the N-th sequence is not found.</returns>
        public int GetNthSeqDuration(string startType, string endType, int n)
        {
            // 1. Handle edge cases for empty list or invalid index
            if (Events == null || Events.Count == 0 || n <= 0)
                return -1;

            int occurrenceCount = 0;

            // Iterate through all events to find the N-th start event
            for (int i = 0; i < Events.Count; i++)
            {
                // Check for the desired start event type
                if (Events[i].Type == startType)
                {
                    occurrenceCount++;
                    this.TrialInfo($"nOccurences of {startType}: {occurrenceCount}");
                    // 2. Check if this is the N-th occurrence we are looking for
                    if (occurrenceCount == n)
                    {
                        var startTime = Events[i].Time; // Capture the start time
                        this.TrialInfo($"Start time of {n}th {startType}: {startTime}");

                        // 3. Found the N-th start. Now, look *forward* for the first end event
                        for (int j = i + 1; j < Events.Count; j++)
                        {
                            if (Events[j].Type == endType)
                            {
                                // Found the corresponding end event
                                var endTime = Events[j].Time;
                                this.TrialInfo($"End time of {n}th {endType}: {endTime}");
                                // 4. Return the calculated duration
                                return Utils.GetDuration(startTime, endTime);
                            }
                            // Optimization: If the sequence is [Press, Press, Release], 
                            // we are only looking for the *first* Release after the N-th Press.
                        }

                        // If the loop finishes without finding an endType, the sequence is incomplete.
                        // Since we found the N-th start, we immediately stop and return -1.
                        return -1;
                    }
                }
            }

            // 5. If the main loop finishes, the N-th start event was never found.
            return -1; // N-th occurrence of startType not found
        }

        public int GetLastSeqDuration(string startLabel, string endLabel)
        {
            this.TrialInfo($"From {startLabel} to {endLabel}");
            if (Events == null || Events.Count == 0)
                return -1;

            // find the last occurrence of endType
            int afterIndex = Events.FindLastIndex(t => t.Type == endLabel);
            this.TrialInfo($"Index of {endLabel}: {afterIndex}");
            if (afterIndex < 0)
                return -1;

            // scan backwards from that point to find the last startType
            for (int i = afterIndex - 1; i >= 0; i--)
            {
                if (Events[i].Type == startLabel)
                {
                    this.TrialInfo($"Start time {startLabel}: {Events[i].Time}");
                    this.TrialInfo($"End time {endLabel}: {Events[afterIndex].Time}");
                    return Utils.GetDuration(
                        Events[i].Time,
                        Events[afterIndex].Time
                    );
                }
            }

            return -1; // not found
        }

        public int GetDurationToGestureStart(string startLabel, Technique technique)
        {
            long startTime = GetLastTime(startLabel);
            this.TrialInfo($"StartTime {startLabel}: {startTime}");
            long endTime = GetGestureStartTime(technique);
            this.TrialInfo($"End time {technique}: {endTime}");
            return Utils.GetDuration(startTime, endTime);
        }

        public int GetDurationFromGestureEnd(Technique technique, string endLabel)
        {
            long startTime = GetGestureEndTimestamp(technique);
            this.TrialInfo($"Start time {technique}: {startTime}");
            long endTime = GetLastTime(endLabel);
            this.TrialInfo($"End time {endLabel}: {endTime}");
            return Utils.GetDuration(startTime, endTime);
        }

        public int GetDurationToFingerAction(string type, string action)
        {
            long startTime = GetLastTime(type);
            this.TrialInfo($"Start time {type}: {startTime}");
            long endTime = GetFirstAfterLast(type, action);
            this.TrialInfo($"End time {action}: {endTime}");
            return Utils.GetDuration(startTime, endTime);
        }

        public int GetDurationFromFingerAction(string action, string endLabel)
        {
            long startTime = GetLastFingerActionTime(action);
            this.TrialInfo($"Start time {action}: {startTime}");
            long endTime = GetLastTime(endLabel);
            this.TrialInfo($"End time {endLabel}: {endTime}");
            return Utils.GetDuration(startTime, endTime);
        }

        public int GetGestureDuration(Technique gesture)
        {
            switch (gesture)
            {
                case Technique.TOMO_TAP:
                    //long tapEndTime = GetLastFingerActionTime(ExpStrs.TAP_UP);
                    //long tapStartTime = GetFingerTimeBefore(ExpStrs.DOWN, tapEndTime);
                    //return Utils.GetDuration(tapStartTime, tapEndTime);
                    return GetLastSeqDuration(ExpStrs.TAP_DOWN, ExpStrs.TAP_UP);

                case Technique.TOMO_SWIPE:
                    //long swipeEndTime = GetLastFingerActionTime(ExpStrs.SWIPE_END);
                    //long swipeStartTime = GetFingerTimeBefore(ExpStrs.SWIPE_START, swipeEndTime);
                    //return Utils.GetDuration(swipeStartTime, swipeEndTime);
                    return GetLastSeqDuration(ExpStrs.SWIPE_START, ExpStrs.SWIPE_END);
            }

            return -1;
        }

        public long GetGestureStartTime(Technique technique)
        {
            switch (technique)
            {
                case Technique.TOMO_TAP:
                    return GetLastFingerActionTime(ExpStrs.TAP_DOWN);

                case Technique.TOMO_SWIPE:
                    return GetLastFingerActionTime(ExpStrs.SWIPE_START);
            }

            return -1;
        }

        public long GetGestureEndTimestamp(Technique technique)
        {
            switch (technique)
            {
                case Technique.TOMO_TAP:
                    return GetLastFingerActionTime(ExpStrs.TAP_UP);

                case Technique.TOMO_SWIPE:
                    return GetLastFingerActionTime(ExpStrs.SWIPE_END);
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
            return Events.Any(ts => ts.Type == label);
        }

        public int GetMarketObjectId()
        {
            return Objects.FirstOrDefault(o => o.State == ButtonState.MARKED)?.Id ?? -1;
        }

        public void ClearTimestamps()
        {
            Events.Clear();
        }

        public void ResetStates()
        {
            foreach (TFunction func in Functions)
            {
                func.State = ButtonState.DEFAULT;
            }

            foreach (TObject obj in Objects)
            {
                obj.State = ButtonState.DEFAULT;
            }
        }

        public void Reset()
        {
            Functions.Clear();
            Objects.Clear();
            ObjFuncMap.Clear();
            Events.Clear();
            Times.Clear();
        }

        public List<int> GetFunctionIds()
        {
            return Functions.Select(f => f.Id).ToList();
        }

        public List<Point> GetFunctionCenters()
        {
            return Functions.Select(f => f.Center).ToList();
        }

        public void AddTime(string label, double time)
        {
            Times[label] = time;
        }

        internal void RemoveLastTimestamp()
        {
            Events.RemoveAt(Events.Count - 1);
        }

        public int CountEvent(string type)
        {
            return Events.Count(ts => ts.Type == type);
        }


    }
}
