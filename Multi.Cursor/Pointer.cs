using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using static Multi.Cursor.Output;
using static System.Math;

namespace Multi.Cursor
{
    internal class Pointer
    {
        private bool _active;
        private bool _initMove;

        private Point _prevPos;
        private Stopwatch _stopWatch;
        private List<TouchPoint> _frames;

        public KalmanFilter _kf;

        public Pointer()
        {
            _active = false;
            _prevPos = new Point(-1, -1);
            _stopWatch = new Stopwatch();
            _frames = new List<TouchPoint>();
            _initMove = true;

            _kf = new KalmanFilter(Config.FRAME_DUR_MS / 1000.0); // dT in seconds
        }

        public (double dX, double dY) Update(TouchPoint tp)
        {
            if (!_stopWatch.IsRunning) _stopWatch.Start();

            if (_stopWatch.ElapsedMilliseconds < Config.FRAME_DUR_MS)
            { // Still collecting frames
                _frames.Add(tp);

            } else
            {
                // Compute movement delta (ignore absolute positions!)
                double dX_raw = 0, dY_raw = 0;
                if (_frames.Count > 1)
                {
                    // Compute delta from oldest to newest frame
                    TouchPoint first = _frames.First();
                    TouchPoint last = _frames.Last();

                    dX_raw = last.GetCenterOfMass().X - first.GetCenterOfMass().X;
                    dY_raw = last.GetCenterOfMass().Y - first.GetCenterOfMass().Y;

                    TRACK_LOG.Information($"KF Delta Raw: {dX_raw:F3}, {dY_raw:F3}");
                }

                if (_initMove)
                {
                    // First touch: Don't move the cursor, just initialize
                    _prevPos = tp.GetCenterOfMass();
                    _kf.Initialize(new Point(0, 0)); // Start at zero movement
                    _initMove = false;
                }
                else
                {
                    // Compute velocity
                    _stopWatch.Stop();
                    double dT = _stopWatch.ElapsedMilliseconds / 1000.0; // seconds
                    double vX_raw = dX_raw / dT;
                    double vY_raw = dY_raw / dT;

                    // Kalman filter for velocity
                    _kf.Predict();
                    _kf.Update(new Point(vX_raw, vY_raw));

                    (double fvX, double fvY) filteredV = _kf.GetEstVelocity();

                    // Compute speed and apply dynamic gain
                    double speed = Sqrt(Pow(filteredV.fvX, 2) + Pow(filteredV.fvY, 2));
                    double gain = Config.BASE_GAIN +
                        Config.SCALE_FACTOR * Tanh(speed * Config.SENSITIVITY);

                    double dX = filteredV.fvX * dT * gain;
                    double dY = filteredV.fvY * dT * gain;

                    TRACK_LOG.Information($"KF Vel.: {filteredV.fvX:F3}, {filteredV.fvY:F3}");
                    TRACK_LOG.Information($"KF dX, dY: {dX:F3}, {dY:F3}");
                    TRACK_LOG.Information(Str.MINOR_LINE);

                    // Update previous state
                    _prevPos = tp.GetCenterOfMass();
                    _frames.Clear();
                    _stopWatch.Restart();

                    return (dX, dY);
                }
            }

            

            // No movemet (not enough frames yet)
            return (0, 0);

        }
    }
}
