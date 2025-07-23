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
        //public int FunctionId;
        public List<TFunction> Functions;
        public List<TObject> Objects;
        public List<Pair> ObjFuncMap;

        public Rect ObjectAreaRect;
        public Dictionary<string, int> EventCounts;
        private List<Timestamp> Timestamps;

        public TrialRecord()
        {
            Functions = new List<TFunction>();
            Objects = new List<TObject>();
            ObjFuncMap = new List<Pair>();
            ObjectAreaRect = new Rect();
            EventCounts = new Dictionary<string, int>();
            Timestamps = new List<Timestamp>();
        }

        public void MapObjectToFunction(int objectId, int functionId)
        {
            var pair = new Pair(objectId, functionId);
            if (!ObjFuncMap.Contains(pair))
            {
                ObjFuncMap.Add(pair);
            }
        }

        public void AddTimestamp(string label)
        {
            Timestamps.Add(new Timestamp(label));
        }

        public string TimestampsToString()
        {
            return string.Join(", ", Timestamps.Select(ts => $"{ts.label}: {ts.time}"));
        }

        public string GetLastTimestamp()
        {
            return Timestamps.Count > 0 ? Timestamps.Last().label : "No timestamps recorded";
        }

        public long GetTime(string label)
        {
            var timestamp = Timestamps.FirstOrDefault(ts => ts.label == label);
            if (timestamp != null)
            {
                return timestamp.time;
            }
            return -1; // Return -1 if the label is not found
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
    }
}
