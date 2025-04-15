using CommunityToolkit.HighPerformance;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using Tensorflow;

namespace Multi.Cursor
{
    public class TouchPoint
    {
        public int Id { get; set; }  // Unique identifier for each touch point

        // Col (X) and Row (Y) of the center value (in overall surface)
        private Point _center = new Point(-1, -1);

        /// <summary>
        /// 3x3 value square
        /// 0 1 2
        /// 3 4 5
        /// 6 7 8
        /// </summary>
        private byte[] _values = new byte[9];

        public TouchPoint()
        {
            // Intentionally empty
        }

        // Create FingerTouchPoint with the center value
        public TouchPoint(byte value, int r, int c)
        {
            _values[4] = value;
            _center = new Point(c, r);
        }

        public TouchPoint Clone()
        {
            return new TouchPoint
            {
                Id = this.Id,
                _center = this._center, // Structs like Point are copied by value
                _values = (byte[])this._values.Clone() // Create a new array copy
            };
        }

        public void SetValue(int ind, byte val) 
        {
            _values[ind] = val;
        }

        public int GetMassCenterCol()
        {
            return _values[1] + _values[4] + _values[7];
        }

        public int GetMassCenterRow()
        {
            return _values[3] + _values[4] + _values[5];
        }

        public int GetMass()
        {
            int totalMass = 0;
            foreach (byte b in _values) {
                totalMass += b;
            }

            return totalMass;
        }

        public double GetX()
        {
            return GetCenterOfMass().X;
        }

        /// <summary>
        /// Same as GetX but with int
        /// </summary>
        /// <returns></returns>
        public int GetCol()
        {
            return (int)GetCenterOfMass().X;
        }

        public double GetY()
        {
            return GetCenterOfMass().Y;
        }

        public int GetRow()
        {
            return (int)GetCenterOfMass().Y;
        }

        public bool IsCenterRowInRange(int min, int max)
        {
            return Utils.InInc(_center.Y, min, max);
        }

        public bool IsCenterColInRange(int min, int max)
        {
            return Utils.InInc(_center.X, min, max);
        }

        public bool IsToTheLeftOf(TouchPoint tp)
        {
            return this.GetX() < tp.GetX();
        }

        // Get the center of mass
        public Point GetCenterOfMass()
        {
            Point centerOfMass = new Point(GetRelCenterOfMass().centerX, GetRelCenterOfMass().centerY);
            centerOfMass.Offset(_center.X, _center.Y);

            return centerOfMass;
        }

        // Get center of mass (relative to the center)
        public (double centerX, double centerY) GetRelCenterOfMass()
        {

            double totalMass = GetMass();
            double sumR = 0; // Weighted sum of row indices
            double sumC = 0; // Weighted sum of column indices

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    byte mass = _values[i * 3 + j];
                    sumR += (i - 1) * mass; // Weight row index by mass
                    sumC += (j - 1) * mass; // Weight column index by mass
                }
            }

            if (totalMass == 0)
            {
                // Avoid division by zero
                throw new InvalidOperationException("Total mass is zero, cannot calculate center of mass.");
            }

            double centerX = sumC / totalMass; // Weighted average of row indices
            double centerY = sumR / totalMass; // Weighted average of column indices

            return (centerX, centerY);
        }

        public int GetMin()
        {
            int min = _values[4]; // Center is max
            foreach (byte b in _values)
            {
                if (b < min) min = b;
            }

            return min;
        }

        public double GetMean()
        {
            return GetMass() / 9.0;
        }

        public double GetStd()
        {
            double mean = GetMean();

            // Compute the variance
            double varianceSum = 0;
            foreach (byte b in _values)
            {
                double diff = b - mean;
                varianceSum += diff * diff;
            }

            double variance = varianceSum / 9;

            // Return std
            return Math.Sqrt(variance);
        }

        public Dictionary<string, double> ExtractFeatures()
        {
            return new Dictionary<string, double>
            {
            { "Mass", GetMass() },
            { "MassCenterRow", GetMassCenterRow() },
            { "MassCenterCol", GetMassCenterCol() },
            { "Max", _values[4] }, // Center is always max
            { "Min", GetMin() },
            { "Mean", GetMean() },
            { "Std", GetStd() },
            { "CoMX", GetRelCenterOfMass().centerX },
            { "CoMY", GetRelCenterOfMass().centerY }
            };
        }

        public string GetFeatureNames()
        {
            Dictionary<string, double> features = ExtractFeatures();
            return string.Join(";", features.Keys);
        }

        public string GetFeautureValues()
        {
            Dictionary<string, double> features = ExtractFeatures();
            return string.Join(";", features.Values.Select(v => v.ToString("F3")));
        }

        public string CoMToString()
        {
            return $"({GetX():F3}, {GetY():F3})";
        }

        public void Print()
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Console.Write(_values[i * 3 + j] + "\t"); // Use tab (\t) to separate columns
                }
                Console.WriteLine(); // New line after each row
            }
        }

        public void PrintFeatures()
        {
            Dictionary<string, double> features = ExtractFeatures();
            foreach (var kvp in features)
            {
                Console.Write($"{kvp.Key} : {kvp.Value} | ");
            }

            Console.WriteLine();
        }

        public void PrintFeatureValues()
        {
            Console.WriteLine(GetFeautureValues());
        }

        public void PrintCenterOfMass()
        {
            Point comP = GetCenterOfMass();
            Console.WriteLine($"Center of Mass: ({comP.X:F3}, {comP.Y:F3})");
        }

        public void PrintMass()
        {
            Console.WriteLine($"Mass = ${GetMass()}");
        }

    }
}
