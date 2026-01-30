namespace Common.Helpers
{
    public class MRange
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

        public MRange(double min, double max)
        {
            _min = min;
            _max = max;
        }

        public MRange(double min, double max, string label) : this(min, max)
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

        public bool ContainsExc(MRange valueRange)
        {
            return ContainsExc(valueRange.Min) && ContainsExc(valueRange.Max);
        }

        public double GetRandomValue()
        {
            return _rand.NextDouble() * (_max - _min) + _min; // Generate a random value within the range
        }

        public override string ToString()
        {
            return $"{Label} ({Min:F2}-{Max:F2})";
        }
    }
}
