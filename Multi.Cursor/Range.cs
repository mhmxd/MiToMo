using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multi.Cursor
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

        Random _rand = new Random();

        public Range(double min, double max)
        {
            _min = min;
            _max = max;
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

        public string ToString()
        {
            return $"Min: {Min:F2} -- Max: {Max:F2}";
        }
    }
}
