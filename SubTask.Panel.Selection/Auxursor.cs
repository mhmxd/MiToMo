using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using static SubTask.Panel.Selection.Output;
using static System.Math;

namespace SubTask.Panel.Selection
{
    internal class Auxursor
    {
        private bool _active;
        private bool _freezed; // For setting when mouse moves
        private bool _initMove;

        private Stopwatch _stopWatch;
        private List<TouchPoint> _touchFrames;

        private Point _initPosition = new Point(-1, -1);
        private Point _prevPosition = new Point(-1, -1);
        private Point _landPosition = new Point(-1, -1);
        private Point _prevKFEPosition = new Point(-1, -1); // Previous estimated position

        public (double x, double y) prevV = (-1, -1); // Velocity
        public (double x, double y) prevA = (0, 0); // Acceleration

        //public KalmanFilter _kf;
        private KalmanVeloFilter _kvf;
        //public int kfSkips = 5;


        public Auxursor(double dT)
        {
            _active = false;
            _freezed = false;
            _initMove = true;
            _stopWatch = new Stopwatch();
            _touchFrames = new List<TouchPoint>();

            //_kf = new KalmanFilter(dT);
            _kvf = new KalmanVeloFilter(ExpEnvironment.VKF_PROCESS_NOISE, ExpEnvironment.VKF_MEASURE_NOISE);

        }

        public void Activate()
        {
            _active = true;
            // _firstTouch is changed when moving
            _stopWatch.Restart();
        }

        public void Deactivate()
        {
            _active = false;
            _initMove = true;
            _stopWatch.Reset(); // Also stops
        }

        /// <summary>
        /// FullFinger lifted up or auxursor deactivated
        /// </summary>
        public void Stop()
        {
            _initMove = true;
        }

        public (double dX, double dY) Update(TouchPoint tp)
        {
            if (!_active) return (0, 0); // Not active

            Point currentPosition = tp.GetCenter();
            if (_initMove)
            {
                // First move: Don't move the cursor, just initialize
                _prevPosition = currentPosition;
                _kvf.Initialize(0, 0); // Start at zero VELOCITY!
                _initMove = false;
            }
            else
            {
                // Compute velocity
                _stopWatch.Stop();
                double dT = _stopWatch.ElapsedMilliseconds / 1000.0; // seconds
                double dX_raw = currentPosition.X - _prevPosition.X;
                double dY_raw = currentPosition.Y - _prevPosition.Y;

                // dT may become zero! => NaN
                if (dT > 1e-9)
                {
                    double rawVX = dX_raw / dT;
                    double rawVY = dY_raw / dT;
                    FILOG.Debug($"Raw V: {rawVX:F2}, {rawVY:F2}");

                    // Use Kalman filter for velocity
                    _kvf.Predict(dT);
                    _kvf.Update(rawVX, rawVY);

                    (double fvX, double fvY) filteredV = _kvf.GetEstVelocity();
                    FILOG.Debug($"KvF V: {filteredV.fvX:F2}, {filteredV.fvY:F2}");
                    // Compute speed and apply dynamic gain
                    double speed = Sqrt(Pow(filteredV.fvX, 2) + Pow(filteredV.fvY, 2));
                    double gain =
                        ExpEnvironment.BASE_GAIN +
                        ExpEnvironment.SCALE_FACTOR * Tanh(speed * ExpEnvironment.SENSITIVITY);

                    double dX = filteredV.fvX * dT * gain;
                    double dY = filteredV.fvY * dT * gain;

                    // Update previous state
                    _prevPosition = currentPosition;
                    _stopWatch.Restart();

                    return (dX, dY); // Return resulting movement
                }
                else // dT next to zero or zero => skip calculations
                {
                    _stopWatch.Restart();
                    return (0, 0);
                }


            }

            return (0, 0); // Default
        }

    }


}
