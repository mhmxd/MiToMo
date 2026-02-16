using Common.Constants;
using Common.Helpers;
using Common.Settings;
using System.Windows;
using static Common.Constants.ExpEnums;

namespace CommonUI
{
    public class TrialRecord
    {
        public int TrialId { get; set; }

        public List<TFunction> Functions;
        public List<TObject> Objects;
        public Dictionary<int, int> ObjFuncMap;
        public int Distance; // in pixels
        public double DistanceMM; // in mm
        public double AvgDistanceMM; // Average distance from different sources

        public Rect StartBtnRect; // Needed for a priori positioning
        public Rect ObjectAreaRect;

        private List<TrialEvent> Events;
        private Dictionary<string, long> Times;
        private Dictionary<string, double> Lengths; // e.g., swipe length (all in mm)

        public Result Result;

        public TrialRecord()
        {
            Functions = new List<TFunction>();
            Objects = new List<TObject>();
            ObjFuncMap = new Dictionary<int, int>();
            Events = new List<TrialEvent>();
            Times = new Dictionary<string, long>();
            Lengths = new Dictionary<string, double>();
        }

        public TrialRecord(int trialId)
        {
            TrialId = trialId;

            Functions = new List<TFunction>();
            Events = new List<TrialEvent>();
            Times = new Dictionary<string, long>();
            Lengths = new Dictionary<string, double>();
        }

        public TFunction? GetFunctionById(int id)
        {
            foreach (TFunction func in Functions)
            {
                if (func.Id == id) return func;
            }

            return null;
        }

        public List<TFunction> GetFunctions()
        {
            return Functions;
        }

        public int FindMappedFunctionId(int objectId)
        {
            if (ObjFuncMap.ContainsKey(objectId))
            {
                return ObjFuncMap[objectId];
            }
            return -1;
            // Find the first function that is mapped to the given object funcId
            //var pair = ObjFuncMap.FirstOrDefault(p => p.First == objectId);
            //return pair != null ? pair.Second : -1; // Return -1 if no mapping found
        }

        public int FindMappedObjectId(int functionId)
        {
            foreach (var kvp in ObjFuncMap)
            {
                if (kvp.Value == functionId)
                {
                    return kvp.Key;
                }
            }
            return -1;
            // Find the first object that is mapped to the given function funcId
            //var pair = ObjFuncMap.FirstOrDefault(p => p.Second == functionId);
            //return pair != null ? pair.First : -1; // Return -1 if no mapping found
        }

        public void MapObjectToFunction(int objectId, int functionId)
        {
            ObjFuncMap[objectId] = functionId;
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
            this.EventsInfo($"FuncId: {funcId}");
            TFunction func = GetFunctionById(funcId);
            if (func != null)
            {
                func.State = ButtonState.SELECTED;
            }

            // Apply to the specified object
            //if (objId != -1) ChangeObjectState(objId, ButtonState.SELECTED);

        }

        public void MarkFunction(int id)
        {
            ChangeFunctionState(id, ButtonState.MARKED);
        }

        public void UnmarkFunction(int id)
        {
            ChangeFunctionState(id, ButtonState.DEFAULT);
        }

        public bool HasFunctionState(ButtonState state)
        {
            //this.TrialInfo($"Function Id = {Functions[0].State}");
            return Functions[0]?.State == state;
        }

        public void SetFunctionAsSelected(int funcId)
        {
            ChangeFunctionState(funcId, ButtonState.SELECTED);
        }

        public void ChangeObjectState(int objId, ButtonState newState)
        {
            this.EventsInfo($"Change Obj#{objId} to {newState}");
            TObject markedObj = Objects.FirstOrDefault(o => o.Id == objId);
            if (markedObj != null)
            {
                this.EventsInfo($"Changed Obj#{objId} to {newState}");
                markedObj.State = newState;
            }
        }

        public void ChangeFunctionState(int funcId, ButtonState newState)
        {
            Functions.FirstOrDefault(f => f.Id == funcId).State = newState;
        }

        public void EnableFunction()
        {
            ChangeFunctionState(Functions[0].Id, ButtonState.ENABLED);
        }

        public void MarkAllFunctions()
        {
            foreach (TFunction func in Functions)
            {
                func.State = ButtonState.MARKED;
            }
        }

        /// <summary>
        /// If the Time stamp is gesture end, rename the previous one to match 
        /// e.g. TAP_UP -> change DOWN to TAP_DOWN 
        /// </summary>
        /// <param name="label"></param>
        public void RecordEvent(string type, string id)
        {
            TrialEvent trialEvent = new(type, id);
            Events.Add(trialEvent);
            //this.TrialInfo($"[+] {trialEvent.ToString()}");

            if (type == ExpStrs.TAP_UP)
            {
                long endTime = GetLastFingerActionTime(type);
                //this.TrialInfo($"End Time: {endTime}");
                var gestureStartTimestamp = Events.LastOrDefault(ts => ts.Type == ExpStrs.DOWN && ts.Time < endTime);
                if (gestureStartTimestamp != null)
                {
                    gestureStartTimestamp.Type = ExpStrs.TAP_DOWN;
                }
            }
        }

        public void AddLength(string label, double length)
        {
            Lengths[label] = length;
        }

        public string TrialEventsToString()
        {
            return string.Join("; ", Events.Select(ts => ts.ToString()));
        }

        public string GetLastTrialEventType()
        {
            return Events.Count > 0 ? Events.Last().Type : "No timestamps recorded";
        }

        public TrialEvent? GetLastTrialEvent()
        {
            return Events.Count > 0 ? Events.Last() : null;
        }

        public TrialEvent? GetBeforeLastTrialEvent()
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
            this.EventsInfo($"Start time ({startLabel}): {startTime}");
            long endTime = GetLastTime(endLabel);
            this.EventsInfo($"End time ({endLabel}): {endTime}");
            return MTools.GetDuration(startTime, endTime);
        }

        public int GetDurtionToFirstAfter(string startLabel, string endLabel)
        {
            long startTime = GetLastTime(startLabel);
            //this.TrialInfo($"Start time ({startLabel}): {startTime}");
            long endTime = GetFirstAfterLast(startLabel, endLabel);
            //this.TrialInfo($"End time ({endLabel}): {endTime}");
            return MTools.GetDuration(startTime, endTime);
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
                            return MTools.GetDuration(Events[i].Time, Events[j].Time);
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
        /// <returns>The duration in a suitable unit (depending on Tools.GetDuration), or -1 if the N-th sequence is not found.</returns>
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
                    //this.TrialInfo($"nOccurences of {startType}: {occurrenceCount}");
                    // 2. Check if this is the N-th occurrence we are looking for
                    if (occurrenceCount == n)
                    {
                        var startTime = Events[i].Time; // Capture the start time
                        //this.TrialInfo($"Start time of {n}th {startType}: {startTime}");

                        // 3. Found the N-th start. Now, look *forward* for the first end event
                        for (int j = i + 1; j < Events.Count; j++)
                        {
                            if (Events[j].Type == endType)
                            {
                                // Found the corresponding end event
                                var endTime = Events[j].Time;
                                //this.TrialInfo($"End time of {n}th {endType}: {endTime}");
                                // 4. Return the calculated duration
                                return MTools.GetDuration(startTime, endTime);
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
            //this.TrialInfo($"From {startLabel} to {endLabel}");
            if (Events == null || Events.Count == 0)
                return -1;

            // find the last occurrence of endType
            int afterIndex = Events.FindLastIndex(t => t.Type == endLabel);
            //this.TrialInfo($"Index of {endLabel}: {afterIndex}");
            if (afterIndex < 0)
                return -1;

            // scan backwards from that point to find the last startType
            for (int i = afterIndex - 1; i >= 0; i--)
            {
                if (Events[i].Type == startLabel)
                {
                    this.EventsInfo($"Start time {startLabel}: {Events[i].Time}");
                    this.EventsInfo($"End time {endLabel}: {Events[afterIndex].Time}");
                    return MTools.GetDuration(
                        Events[i].Time,
                        Events[afterIndex].Time
                    );
                }
            }

            return -1; // not found
        }

        public int GetDurationFromFirstToLast(string startLabel, string endLabel)
        {
            if (Events == null || Events.Count == 0)
                return -1;

            // Find the first occurrence of startLabel
            int startIndex = Events.FindIndex(t => t.Type == endLabel);
            this.LogsInfo($"Index of {startLabel}: {startIndex}");
            if (startIndex < 0)
                return -1;

            // Find the last occurence of endLabel
            int endIndex = Events.FindLastIndex(t => t.Type == endLabel);
            this.LogsInfo($"Index of {endLabel}: {endIndex}");
            if (endIndex < 0)
                return -1;

            return MTools.GetDuration(Events[startIndex].Time, Events[endIndex].Time);
        }

        public int GetDurationToGestureStart(string startLabel, Technique technique)
        {
            long startTime = GetLastTime(startLabel);
            //this.TrialInfo($"StartTime {startLabel}: {startTime}");
            long endTime = GetGestureStartTime(technique);
            //this.TrialInfo($"End time {technique}: {endTime}");
            return MTools.GetDuration(startTime, endTime);
        }

        public int GetDurationFromGestureEnd(Technique technique, string endLabel)
        {
            long startTime = GetGestureEndTimestamp(technique);
            //this.TrialInfo($"Start time {technique}: {startTime}");
            long endTime = GetLastTime(endLabel);
            //this.TrialInfo($"End time {endLabel}: {endTime}");
            return MTools.GetDuration(startTime, endTime);
        }

        public int GetDurationToFingerAction(string type, string action)
        {
            long startTime = GetLastTime(type);
            //this.TrialInfo($"Start time {type}: {startTime}");
            long endTime = GetFirstAfterLast(type, action);
            //this.TrialInfo($"End time {action}: {endTime}");
            return MTools.GetDuration(startTime, endTime);
        }

        public int GetDurationFromFingerAction(string action, string endLabel)
        {
            long startTime = GetLastFingerActionTime(action);
            //this.TrialInfo($"Start time {action}: {startTime}");
            long endTime = GetLastTime(endLabel);
            //this.TrialInfo($"End time {endLabel}: {endTime}");
            return MTools.GetDuration(startTime, endTime);
        }

        public int GetGestureDuration(Technique gesture)
        {
            switch (gesture)
            {
                case Technique.TOMO_TAP:
                    return GetLastSeqDuration(ExpStrs.TAP_DOWN, ExpStrs.TAP_UP);

                case Technique.TOMO_SWIPE:
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

        public int GetSequenceDuration(string label)
        {
            long startTime = GetFirstTime(label);
            this.EventsInfo($"Start time {label}: {startTime}");
            long endTime = GetLastTime(label);
            this.EventsInfo($"End time {label}: {startTime}");
            return MTools.GetDuration(startTime, endTime);
        }

        public double GetTime(string label)
        {
            if (Times.Any(t => t.Key == label))
            {
                return Times[label];
            }

            return -1;
        }

        public List<TrialEvent> GetTrialEvents()
        {
            return Events;
        }

        public TObject? GetObjectById(int id)
        {
            return Objects.FirstOrDefault(o => o.Id == id);
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

        public List<int> GetFunctionIds()
        {
            return Functions.Select(f => f.Id).ToList();
        }

        public int GetFunctionId()
        {
            return Functions[0].Id;
        }

        public int GetFunctionWidthInUnits(int index)
        {
            if (index < 0 || index >= Functions.Count)
            {
                return -1; // Invalid index
            }

            return Functions[index].WidthInUnits;
        }

        public List<Point> GetFunctionCenters()
        {
            return Functions.Select(f => f.Center).ToList();
        }

        public void AddTime(string label, long time)
        {
            Times[label] = time;
        }

        public void RemoveLastTimestamp()
        {
            Events.RemoveAt(Events.Count - 1);
        }

        public int CountEvent(string type)
        {
            return Events.Count(ts => ts.Type == type);
        }

        public double GetLength(string label)
        {
            if (Lengths.Any(l => l.Key == label))
            {
                return Lengths[label];
            }
            return -1;
        }

        public bool AreAllObjectsApplied()
        {
            return Objects.All(o => o.State == ButtonState.SELECTED);
        }

        public bool AreAllFunctionsSelected()
        {
            foreach (TFunction func in Functions)
            {
                this.TrialInfo($"Function#{func.Id} state: {func.State}");
                if (func.State != ButtonState.SELECTED)
                {
                    return false; // If any function is not selected, return false
                }
            }

            return true; // All functions are selected
        }

        public void AddAllFunctions(List<TFunction> functions)
        {
            Functions.AddRange(functions);
        }

        public bool IsObjectClicked(int objId)
        {
            return Events.Any(ts => ts.Type == ExpStrs.OBJ_RELEASE && ts.Id == objId.ToString());
        }

        public void MarkObject(int id)
        {
            TObject obj = Objects.FirstOrDefault(o => o.Id == id);
            if (obj != null)
            {
                obj.State = ButtonState.MARKED;
            }
        }


        public bool IsFunctionSelected(int funcId)
        {
            TFunction func = GetFunctionById(funcId);
            return func != null && func.State == ButtonState.SELECTED;
        }

        public void MakeAllObjectsAvailable(ButtonState newState)
        {
            this.LogsInfo($"Change all objects to {newState}");
            foreach (TObject obj in Objects)
            {
                obj.State = newState;
            }
        }

        public void MarkMappedObject(int funcId)
        {
            int objId = FindMappedObjectId(funcId);
            if (objId != -1)
            {
                ChangeObjectState(objId, ButtonState.MARKED);
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

        public void SelectObject(int id)
        {
            TObject obj = Objects.FirstOrDefault(o => o.Id == id);
            if (obj != null)
            {
                obj.State = ButtonState.SELECTED;
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


    }
}
