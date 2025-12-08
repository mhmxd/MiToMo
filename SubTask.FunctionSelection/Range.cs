using System;
using static Common.Helpers.ExpUtils;

namespace SubTask.FunctionSelection
{
    public class Range
    {
        double _min, _max;
        public double Min
        {
            get => _min;
            set => _min = value;
        }
        public double Max
        {
            get => _max;
            set => _max = value;
        }

        public string Label { get; set; } = string.Empty; // Optional label for the range

        Random _rand = new Random();

        public Range(double min, double max)
        {
            _min = min;
            _max = max;
        }

        public Range(double min, double max, string label) : this(min, max)
        {
            Label = label;
        }

        public bool ContainsInc(double value)
        {
            return value >= _min && value <= _max;
        }

        public bool ContainsExc(double value)
        {
            return value > _min && value < _max;
        }

        public bool ContainsExc(Range valueRange)
        {
            return ContainsExc(valueRange.Min) && ContainsExc(valueRange.Max);
        }

        public double GetRandomValue()
        {
            return _rand.NextDouble() * (_max - _min) + _min; // Generate a random value within the range
        }

        public Range GetPx()
        {
            return new Range(MM2PX(_min), MM2PX(_max), Label);
        }

        public override string ToString()
        {
            return $"{Label} ({Min:F2}-{Max:F2})";
        }
    }
}
