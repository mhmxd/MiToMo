using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tensorflow.Gradients;

namespace Multi.Cursor
{
    internal class GestureDetector
    {
        private const int TAP_TIME = 200; // Maximum time in milliseconds for a tap
        private const double TAP_RAD = 10.0; // Maximum movement radius for a tap

        private readonly Stopwatch _watch;
        private TouchPoint? _initialTouch;
        private long _touchStartTime;
        private int _prevCount;

        public GestureDetector()
        {
            _watch = Stopwatch.StartNew();
        }

        public void Detect(List<TouchPoint> touchPoints, Action<double, double> OnTap)
        {
            long currentTime = _watch.ElapsedMilliseconds;

            // Finger added (could be a returning finger) => set time
            if (touchPoints.Count > _prevCount)
            {
                _touchStartTime = currentTime;
               
            }

            // Finger lifted => Was it added shortly? Yes => Tap!
            if (touchPoints.Count < _prevCount)
            {
                if (currentTime - _touchStartTime < TAP_TIME)
                {
                    OnTap?.Invoke(_initialTouch.GetX(), _initialTouch.GetY());
                }
            }


            if (touchPoints.Count == 1)
            {
                var touch = touchPoints[0];
                if (_initialTouch == null)
                {
                    // First touch detected
                    _initialTouch = touch;
                    _touchStartTime = currentTime;
                }
                else
                {
                    // Check if the touch has moved significantly
                    double dx = touch.GetX() - _initialTouch.GetX();
                    double dy = touch.GetY() - _initialTouch.GetY();
                    double distance = Math.Sqrt(dx * dx + dy * dy);
                    if (distance > TAP_RAD)
                    {
                        // Movement exceeded tap threshold, reset
                        _initialTouch = null;
                    }
                }
            }
            else if (touchPoints.Count == 0 && _initialTouch != null)
            {
                // Touch has been released
                if (currentTime - _touchStartTime <= TAP_TIME)
                {
                    // Valid tap detected
                    OnTap?.Invoke(_initialTouch.GetX(), _initialTouch.GetY());
                }
                _initialTouch = null;
            }
        }
    }
}
