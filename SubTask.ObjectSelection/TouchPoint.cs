using System;
using System.Windows;

namespace SubTask.ObjectSelection
{
    public class TouchPoint
    {
        public int Id { get; set; }  // Unique identifier for each touch point

        // For weighted center calculation
        private int _totalPressure;
        private int _weightedSumX;
        private int _weightedSumY;


        public TouchPoint()
        {

        }

        /// <summary>
        /// Add touch data to the weighted sum for center calculation
        /// </summary>
        /// <param name="x">Column</param>
        /// <param name="y">Row (from the top)</param>
        /// <param name="p">Pressure</param>
        public void AddTouchData(int x, int y, byte p)
        {
            _weightedSumX += x * p;
            _weightedSumY += y * p;
            _totalPressure += p;
        }

        public void AddColumnData(int colNum, byte[] col)
        {
            if (col == null || col.Length == 0)
            {
                throw new ArgumentException($"Column data cannot be null or empty: {colNum}");
            }

            for (int y = 0; y < col.Length; y++)
            {
                AddTouchData(colNum, y, col[y]);
            }
        }

        public Point GetCenter()
        {
            if (_totalPressure == 0)
            {
                throw new InvalidOperationException("Total pressure is zero, cannot calculate center.");
            }
            return new Point((double)_weightedSumX / _totalPressure, (double)_weightedSumY / _totalPressure);
        }

        public int GetTotalPressure()
        {
            return _totalPressure;
        }

        public double GetX()
        {
            return GetCenter().X;
        }

        public double GetY()
        {
            return GetCenter().Y;
        }
    }
}
