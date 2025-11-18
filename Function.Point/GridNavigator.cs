using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static Function.Point.Output;
using static System.Math;

namespace Function.Point
{
    public class GridNavigator
    {
        private bool _active;
        private bool _initMove;
        private Point _prevPosition;

        // --- New State for Grid Navigation ---
        private int _currentRow;
        private int _currentCol;
        private double _accumulatedX_displacement = 0.0;
        private double _accumulatedY_displacement = 0.0;

        private Stopwatch _stopWatch;

        public (double x, double y) prevV = (-1, -1); // Velocity
        public (double x, double y) prevA = (0, 0); // Acceleration

        private KalmanVeloFilter _kvf;

        public GridNavigator(double dT)
        {
            _initMove = true;
            _stopWatch = new Stopwatch();
            _kvf = new KalmanVeloFilter(Config.VKF_PROCESS_NOISE, Config.VKF_MEASURE_NOISE);
        }

        public void Activate()
        {
            _active = true;
            _stopWatch.Restart();
        }

        public void Deactivate()
        {
            _active = false;
            _initMove = true;
            _stopWatch.Reset(); // Also stops
        }

        public void Stop()
        {
            _initMove = true;
        }

        /// <summary>
        /// Processes touch input to update the grid selection.
        /// </summary>
        /// <param name="tp">The current TouchPoint.</param>
        /// <returns>True if the grid position changed, false otherwise.</returns>
        public (int dGridX, int dGridY) Update(TouchPoint tp)
        {
            if (!_active) return (0, 0); // Not active

            Point currentPosition = tp.GetCenter();

            if (_initMove)
            {
                // First move: Don't move the cursor, just initialize
                _prevPosition = currentPosition;
                _kvf.Initialize(0, 0); // Start at zero VELOCITY!
                _initMove = false;
                return (0, 0); // No movement yet
            }

            // Compute velocity
            _stopWatch.Stop();
            double dT = _stopWatch.ElapsedMilliseconds / 1000.0; // seconds

            // Handle dT being zero or very small to avoid division by zero/NaN
            if (dT < 1e-9) // Using a small epsilon instead of just 0
            {
                // If no Time has passed or negligible, just skip velocity calculation
                // The Kalman filter will maintain its last state, and we won't accumulate displacement
                return (0, 0);
            }

            double dX_raw = currentPosition.X - _prevPosition.X;
            double dY_raw = currentPosition.Y - _prevPosition.Y;

            // Use Kalman filter for velocity
            _kvf.Predict(dT);
            _kvf.Update(dX_raw / dT, dY_raw / dT); // Pass raw velocities to KVF

            (double fvX, double fvY) filteredV = _kvf.GetEstVelocity();
            //GestInfo<GridNavigator>($"Velocity = {filteredV.fvX}, {filteredV.fvY}");
            // Compute speed and apply dynamic gain
            double speed = Sqrt(Pow(filteredV.fvX, 2) + Pow(filteredV.fvY, 2));
            double gain =
                Config.BASE_GAIN +
                Config.SCALE_FACTOR * Tanh(speed * Config.SENSITIVITY);

            // Apply gain to the filtered velocity
            filteredV.fvX *= gain;
            filteredV.fvY *= gain;

            // --- Accumulate Displacement based on Filtered Velocity and dT ---
            _accumulatedX_displacement += filteredV.fvX * dT;
            _accumulatedY_displacement += filteredV.fvY * dT;

            // Update previous position for the next raw dX/dY calculation
            _prevPosition = currentPosition;

            // --- Calculate Grid Movement to return ---
            int dGridX = 0;
            int dGridY = 0;

            // Process Horizontal Movement
            while (_accumulatedX_displacement >= Config.CELL_WIDTH_THRESHOLD)
            {
                dGridX++;
                _accumulatedX_displacement -= Config.CELL_WIDTH_THRESHOLD;
            }
            while (_accumulatedX_displacement <= -Config.CELL_WIDTH_THRESHOLD)
            {
                dGridX--;
                _accumulatedX_displacement += Config.CELL_WIDTH_THRESHOLD;
            }

            // Process Vertical Movement
            while (_accumulatedY_displacement >= Config.CELL_HEIGHT_THRESHOLD)
            {
                dGridY++;
                _accumulatedY_displacement -= Config.CELL_HEIGHT_THRESHOLD;
            }
            while (_accumulatedY_displacement <= -Config.CELL_HEIGHT_THRESHOLD)
            {
                dGridY--;
                _accumulatedY_displacement += Config.CELL_HEIGHT_THRESHOLD;
            }

            return (dGridX, dGridY);
        }
    }
}
