using CommunityToolkit.HighPerformance;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static Multi.Cursor.Output;
using static Multi.Cursor.Utils;
using static System.Math;
using System.Diagnostics;
using System.Windows;
using MathNet.Numerics.LinearAlgebra.Factorization;
using System.Web.UI;

namespace Multi.Cursor
{
    internal class TouchSurface
    {
        private const int MIN_TOUCH_VAL = 30; // Minimum value to consider touching the surface
        private const double MAX_MOVEMENT = 1.0; // Max movement allowed to keep the same ID
        private const int THUMB_TOP_LOWEST_ROW = 10; // Lowest row for top region (incl.) -- range: [9, 13]
        private const int MIDDLE_LEFT_LAST_COL = 9; // Last col. accepted as left -- range: [8, 10]
        private const int RING_TOP_LOWEST_ROW = 3; //  Lowest row for top region (incl.) -- range: [2, 6]
        private const int LITTLE_TOP_LOWEST_ROW = 8; //  Lowest row for top region (incl.) -- range: [9, 13]

        private Finger _thumb = new Finger(0, 3);
        private Finger _index = new Finger(4, 7);
        private Finger _middle = new Finger(8, 10);
        private Finger _ring = new Finger(11, 12);
        private Finger _little = new Finger(14, 15);

        //--- Class for each frame on the surface
        private class TouchFrame
        {
            public Dictionary<int, TouchPoint> Pointers = new Dictionary<int, TouchPoint>();
            public long Timestamp;

            public TouchFrame()
            {
                Timestamp = Stopwatch.GetTimestamp(); // in ticks
            }

            public void AddPointer(int key, TouchPoint pointer)
            {
                if (Pointers.ContainsKey(key))
                {
                    if (pointer.GetMass() > Pointers[key].GetMass())
                    {
                        Pointers[key] = pointer;
                    }

                }
                else
                {
                    Pointers.Add(key, pointer);
                }
            }

            public bool IncludePointer(int keyMin, int keyMax)
            {
                foreach (int key in Pointers.Keys)
                {
                    if (Utils.InInc(key, keyMin, keyMax)) return true;
                }

                return false;
            }

            public bool ExcludePointer(int keyMin, int keyMax)
            {
                foreach (int key in Pointers.Keys)
                {
                    if (Utils.InInc(key, keyMin, keyMax)) return false;
                }

                return true;
            }

            public bool DoesNotIncludePointer(Finger finger)
            {
                foreach (int key in Pointers.Keys)
                {
                    if (Utils.InInc(key, finger.MinCol, finger.MaxCol)) return false;
                }

                return true;
            }


            public TouchPoint GetPointer(int keyMin, int keyMax)
            {
                foreach (var kv in Pointers)
                {
                    if (Utils.InInc(kv.Key, keyMin, keyMax)) return kv.Value;
                }

                return null;
            }

            public TouchPoint GetPointer(int rMin, int rMax, int cMin, int cMax)
            {
                foreach (var kv in Pointers)
                {
                    if (Utils.InInc(kv.Key, cMin, cMax)
                        && Utils.InInc(kv.Value.GetRow(), rMin, rMax)) return kv.Value;
                }

                return null;
            }

            public void Clear()
            {
                Pointers.Clear();
            }
        }

        private FixedBuffer<TouchFrame> _frames = new FixedBuffer<TouchFrame>(50);

        private Dictionary<int, Stopwatch> _timers = new Dictionary<int, Stopwatch>();

        //private Action<int> _pointerDown;
        //private Action<int> _pointerUp;

        private int _touchId = 0;


        //--- States
        private enum State
        {
            Idle, LPActive, RPActive, TPActive
        }
        private State _state;

        private int _lastUpPointerId; // Id of the last pointer that got up
        private List<int> _upPointerIds; // List of all pointers that got up (LIFO)
        private List<int> _downPointersIds; // List of all pointers down (LIFO)

        private Point _prevPos = new Point(-1, -1);
        private double _moveDelta = 0; // For swipe gestures

        private ToMoGestures _gestureReceiver;

        private long _lastThumbGestureTimestamp = 0;
        private TouchPoint _lastIndexTP = null;
        private TouchPoint _lastThumbTP = null;

        public TouchSurface(ToMoGestures gestureReceiver)
        {
            _upPointerIds = new List<int>() { -1 };
            _downPointersIds = new List<int>() { -1 };

            _gestureReceiver = gestureReceiver;
            //_touchPoints = new List<TouchPoint>();
            //_pointerDown = PointerDown;
            //_pointerUp = PointerUp;
            _state = State.Idle;

            // Init watches for all possible fingers (from left to right)
            for (int finger = 1; finger <= 5; finger++)
            {
                _timers.Add(finger, new Stopwatch());
            }
        }

        /// <summary>
        /// Fill active touches
        /// </summary>
        /// <param name="shotSpan"></param>
        /// <returns></returns>
        private TouchFrame FillActiveTouches(Span2D<Byte> shotSpan)
        {
            // Reset the dictionary
            //_activeFrame.Clear();
            //Output.PrintSpan(shotSpan);

            // Result
            TouchFrame activeFrame = new TouchFrame();

            //List<TouchPoint> activeTouchPoints = new List<TouchPoint>();
            int touchId = 0;

            // Convert ShotSpan to array of columns
            byte[,] columns = new byte[15, 13];
            (byte val, int row)[] colMax = new (byte, int)[15]; // (value, row)
            byte max;
            int maxInd;
            int c = 0;
            while (c < shotSpan.Width)
            {
                byte[] column = shotSpan.GetColumn(c).ToArray();
                max = column.Max();
                maxInd = Array.IndexOf(column, max);
                if (c == 0)
                { // Only check against col. 1
                    byte[] col1 = shotSpan.GetColumn(1).ToArray();
                    if (max > MIN_TOUCH_VAL && max > col1.Max())
                    {
                        TouchPoint touchPoint = new TouchPoint
                            (max, maxInd, c);

                        //--- Add values around
                        // Above
                        if (maxInd != 0)
                        {
                            touchPoint.SetValue(1, column[maxInd - 1]); // Above
                            touchPoint.SetValue(2, col1[maxInd - 1]); // Right above
                        }

                        // Below
                        if (maxInd != column.Length - 1)
                        {
                            touchPoint.SetValue(7, column[maxInd + 1]); // Below
                            touchPoint.SetValue(8, col1[maxInd + 1]); // Right below
                        }

                        // Right
                        touchPoint.SetValue(5, col1[maxInd]);

                        //int tpId = touchPoint.GetCol(); // Using CoM as ID
                        int tpId = c; // Use the column as ID
                        touchPoint.Id = tpId;
                        activeFrame.AddPointer(tpId, touchPoint); // Add to the frame

                        //activeTouchPoints.Add(fingerTouchPoint);
                        //UpdatePointers(fingerTouchPoint);

                        // Jump one column forward (1 is certainly not touch point center)
                        c = 2;
                    }
                }
                else if (c == 14)
                { // DO NOT check agianst col. 13 (both can have values)!
                    byte[] col13 = shotSpan.GetColumn(13).ToArray();
                    if (max > MIN_TOUCH_VAL)
                    {
                        TouchPoint touchPoint = new TouchPoint(max, maxInd, c);

                        //--- Add values around
                        // Above
                        if (maxInd != 0)
                        {
                            touchPoint.SetValue(1, column[maxInd - 1]); // Above
                            touchPoint.SetValue(0, col13[maxInd - 1]); // Left above
                        }

                        // Below
                        if (maxInd != column.Length - 1)
                        {
                            touchPoint.SetValue(7, column[maxInd + 1]); // Below
                            touchPoint.SetValue(6, col13[maxInd + 1]); // Left below
                        }

                        // Left
                        touchPoint.SetValue(3, col13[maxInd]);

                        //int tpId = touchPoint.GetCol(); // Using CoM as ID
                        int tpId = c; // Use the column as ID
                        touchPoint.Id = tpId;
                        activeFrame.AddPointer(tpId, touchPoint); // Add to the frame

                        //activeTouchPoints.Add(fingerTouchPoint);
                        //UpdatePointers(touchPoint);

                    }

                    break;
                }
                else if (c == 13)
                { // Only check against col. 12
                    byte[] col12 = shotSpan.GetColumn(12).ToArray();
                    if (max > MIN_TOUCH_VAL && max > col12.Max())
                    {
                        TouchPoint touchPoint = new TouchPoint(max, maxInd, c);

                        //--- Add values around
                        // Above
                        if (maxInd != 0)
                        {
                            touchPoint.SetValue(1, column[maxInd - 1]); // Above
                            touchPoint.SetValue(0, col12[maxInd - 1]); // Left above
                        }

                        // Below
                        if (maxInd != column.Length - 1)
                        {
                            touchPoint.SetValue(7, column[maxInd + 1]); // Below
                            touchPoint.SetValue(6, col12[maxInd + 1]); // Left below
                        }

                        // Left
                        touchPoint.SetValue(3, col12[maxInd]);

                        //int tpId = touchPoint.GetCol(); // Using CoM as ID
                        int tpId = c; // Use the column as ID
                        touchPoint.Id = tpId;
                        activeFrame.AddPointer(tpId, touchPoint); // Add to the frame

                        //activeTouchPoints.Add(fingerTouchPoint);
                        //UpdatePointers(touchPoint);

                    }

                    c += 1; // Check 14 as well
                }
                else
                {
                    byte[] prevCol = shotSpan.GetColumn(c - 1).ToArray();
                    byte[] nextCol = shotSpan.GetColumn(c + 1).ToArray();
                    //GESTURE_LOG.Information($"prevMax = {prevCol.Max()}, Max = {max}, nextMax = {nextCol.Max()}");
                    if (max > MIN_TOUCH_VAL && max > nextCol.Max())
                    {
                        TouchPoint touchPoint = new TouchPoint(max, maxInd, c);
                        touchPoint.Id = touchId++;

                        //--- Add values around
                        // Above
                        if (maxInd != 0)
                        {
                            touchPoint.SetValue(0, prevCol[maxInd - 1]); // Left above
                            touchPoint.SetValue(1, column[maxInd - 1]); // Above
                            touchPoint.SetValue(2, nextCol[maxInd - 1]); // Right above
                        }

                        // Below
                        if (maxInd != column.Length - 1)
                        {
                            touchPoint.SetValue(6, prevCol[maxInd + 1]); // Left below
                            touchPoint.SetValue(7, column[maxInd + 1]); // Below
                            touchPoint.SetValue(8, nextCol[maxInd + 1]); // Right below
                        }

                        // Left and right
                        touchPoint.SetValue(3, prevCol[maxInd]);
                        touchPoint.SetValue(5, nextCol[maxInd]);

                        //int tpId = touchPoint.GetCol(); // Using CoM as ID
                        int tpId = c; // Use the column as ID
                        touchPoint.Id = tpId;
                        activeFrame.AddPointer(tpId, touchPoint); // Add to the frame

                        // Jump one column forward (next col is certainly not touch point)
                        c += 2;

                        //activeTouchPoints.Add(fingerTouchPoint);
                        //UpdatePointers(touchPoint);
                    } else // Next column should be checked
                    {
                        c += 1;
                    }

                    
                }

            }

            return activeFrame;
        }

        /// <summary>
        /// Track touches using the shots
        /// </summary>
        /// <param name="shotSpan"></param>
        public void Track(Span2D<Byte> shotSpan)
        {
            // First, get the current frame
            TouchFrame activeFrame = FillActiveTouches(shotSpan);

            //--- Print
            //GESTURE_LOG.Information(JsonSerializer.Serialize(_activeTouches));

            _frames.Add(activeFrame);
            //GESTURE_LOG.Verbose($"nFrames = {_frames.Count}");
            if (_frames.Count > 1) // Need at least two frames
            {
                TouchFrame lastFrame = _frames.Last;
                TouchFrame beforeLastFrame = _frames.BeforeLast;

                GESTURE_LOG.Verbose(Output.GetKeys(lastFrame.Pointers));
                //int frameWithThumb = 0;
                //foreach (TouchFrame frame in _frames.GetFrames())
                //{
                //    if (frame.IncludePointer(0, 3))
                //    {
                //        frameWithThumb++;
                //    }
                //}

                // Debug...
                //--- REMOVED: Only track multiple fingers if multiple fingers are present!
                // Presence checks are done inside the methods
                TrackThumb();
                TrackIndex();
                TrackMiddle();
                TrackRing();
                //TrackLittle();

            }

        }


        /// <summary>
        /// Tracking of the thumb finger
        /// </summary>
        private void TrackThumb()
        {
            // Get the last frame
            TouchFrame lastFrame = _frames.Last;
            TouchFrame beforeLastFrame = _frames.BeforeLast;
            TouchPoint thumbPoint = lastFrame.GetPointer(_thumb.MinCol, _thumb.MaxCol);

            if (thumbPoint == null) // No thumb present in the last frame
            {
                _lastThumbTP = null;

                if (_thumb.IsDown)
                {
                    if (_timers[1].ElapsedMilliseconds < Config.TAP_TIME_MS) // Tap!
                    {
                        // Distinguish where the tap was...
                        Direction tapDir = Direction.Up;
                        if (_thumb.GetDownRow() > THUMB_TOP_LOWEST_ROW) tapDir = Direction.Down;
                        _gestureReceiver.ThumbTap(tapDir);
                    }

                    _gestureReceiver.ThumbUp(lastFrame.GetPointer(0, 3)); // DON'T DELETE! (Needed for Radiusor)

                    _thumb.LiftUp();
                }

                //if (beforeLastFrame.IncludePointer(0, 3)) // Thumb was present before => Check for Tap
                //{

                //    if (_timers[1].ElapsedMilliseconds < Config.TAP_TIME_MS) // Tap!
                //    {
                //        _gestureReceiver.ThumbTap();
                //    }

                //    _gestureReceiver.ThumbUp(lastFrame.GetPointer(0, 3)); // DON'T DELETE! (Needed for Radiusor)
                //}

                return;
            }

            //--- Thumb present ----------------------------------------
            TouchPoint thumbTP = lastFrame.GetPointer(0, 3);

            if (_thumb.IsUp)
            {
                // Thumb Down => Start Tap-watch
                _thumb.TouchDown(thumbPoint.GetRow(), thumbPoint.GetCol());
                _timers[1].Restart();
            }

            //if (beforeLastFrame.ExcludePointer(0, 3)) // Thumb wasn't present => Start Tap-watch
            //{
            //    _timers[1].Restart();
            //}

            // Send the point for analyzing the movement
            _gestureReceiver.ThumbMove(thumbTP); // Managed where implemented (could be 0)

            //--- Track gestures 
            // First and last frames
            TouchFrame historyFirst = _frames.First;
            long historyStartTimestamp = historyFirst.Timestamp;

            TouchFrame historyLast = _frames.Last;
            long historyEndTimestamp = historyLast.Timestamp;

            // History duration
            double historyDT =
                (historyEndTimestamp - historyStartTimestamp)
                / (double)Stopwatch.Frequency;

            // Not enough history
            if (historyDT < Config.SWIPE_TIME_MIN) return;

            //--- Long hisotry

            // Check the last window of frames that fits the time duration
            for (int i = _frames.Count - 2; i >= 0; i--)
            {
                TouchFrame firstFrame = _frames[i];
                long firstTimestamp = firstFrame.Timestamp;
                double duration = (historyEndTimestamp - firstTimestamp) / (double)Stopwatch.Frequency;

                if (firstTimestamp <= _lastThumbGestureTimestamp) continue;
                if (duration < Config.SWIPE_TIME_MIN) continue; // Too fast, keep looking
                if (duration > Config.SWIPE_TIME_MAX) break; // Too slow

                //--- Frames found ------------------------------------------

                // Check for thumb...
                if (!firstFrame.IncludePointer(0, 3) || !historyLast.IncludePointer(0, 3)) break;

                //- Thumb found
                TouchPoint gestureStart = firstFrame.GetPointer(0, 3);
                TouchPoint gestureEnd = historyLast.GetPointer(0, 3);

                double dX = gestureEnd.GetX() - gestureStart.GetX();
                double dY = gestureEnd.GetY() - gestureStart.GetY();

                //-- Check against thresholds
                if (Abs(dX) > Config.MOVE_THRESHOLD) // Swipe left/right
                {
                    _lastThumbGestureTimestamp = historyEndTimestamp;

                    if (Abs(dY) > Config.MOVE_LIMIT)
                    {
                        //_frames = new FixedBuffer<TouchFrame>(_frames.Count); // Reset
                        return;
                    }
                    else
                    {
                        if (dX > Config.MOVE_THRESHOLD) // Right
                        {
                            _gestureReceiver.ThumbSwipe(Direction.Right);
                            return;
                            //_frames = new FixedBuffer<TouchFrame>(_frames.Count); // Reset
                        }
                        else // Left
                        {
                            _gestureReceiver.ThumbSwipe(Direction.Left);
                            return;
                            //_frames = new FixedBuffer<TouchFrame>(_frames.Count); // Reset
                        }

                    }
                }

                if (Abs(dY) > Config.HIGHT_MOVE_THRESHOLD) // Swipe up/down
                {
                    _lastThumbGestureTimestamp = historyEndTimestamp;

                    if (Abs(dX) > Config.MOVE_LIMIT) // dX exceeded limits
                    {
                        //_frames = new FixedBuffer<TouchFrame>(_frames.Count); // Reset
                        return;
                    }
                    else
                    {
                        if (dY > Config.HIGHT_MOVE_THRESHOLD) // Down
                        {
                            _gestureReceiver.ThumbSwipe(Direction.Down);
                            return;
                            //_frames = new FixedBuffer<TouchFrame>(_frames.Count); // Reset
                        }
                        else // Up
                        {
                            _gestureReceiver.ThumbSwipe(Direction.Up);
                            return;
                            //_frames = new FixedBuffer<TouchFrame>(_frames.Count); // Reset
                        }

                    }
                }
            }
        }

        /// <summary>
        /// Tracking the index finger
        /// </summary>
        private void TrackIndex()
        {
            // Get the last frame
            TouchFrame lastFrame = _frames.Last;

            // No index finger present in the frame => reset the last positoin
            if (lastFrame.ExcludePointer(4, 7))
            {
                _gestureReceiver.IndexPointUp();
                _lastIndexTP = null;
                return;
            }

            //--- Index finger present ------------------
            TouchPoint indexTP = lastFrame.GetPointer(4, 7);

            _gestureReceiver.IndexPointMove(indexTP);
        }

        /// <summary>
        /// Tracking the middle finger
        /// </summary>
        private void TrackMiddle()
        {
            // Get the last frame
            TouchFrame lastFrame = _frames.Last;
            TouchFrame beforeLastFrame = _frames.BeforeLast;
            TouchPoint middleTouchPoint = lastFrame.GetPointer(_middle.MinCol, _middle.MaxCol);

            if (middleTouchPoint == null) // No middle finger present
            {

                if (_middle.IsDown)
                {
                    if (_middle.GetDownTime() < Config.TAP_TIME_MS) // Tap!
                    {
                        Direction tapDir = Direction.Left;
                        if (_middle.GetDownCol() > MIDDLE_LEFT_LAST_COL) tapDir = Direction.Right;
                        _gestureReceiver.MiddleTap(tapDir); // Bc of ring, middle always activates left
                    }

                    _middle.LiftUp();
                }

                return;

            }
            else // Middle present
            {

                if (_middle.IsUp)
                {
                    // Thumb Down => Start Tap-watch
                    _middle.TouchDown(middleTouchPoint.GetRow(), middleTouchPoint.GetCol());
                    _middle.RestartTimer();
                }

            }


        }

        /// <summary>
        /// Tracking the ring finger
        /// </summary>
        private void TrackRing()
        {

            // Get the last frame
            TouchFrame lastFrame = _frames.Last;
            TouchFrame beforeLastFrame = _frames.BeforeLast;
            TouchPoint ringTouchPoint = lastFrame.GetPointer(0, 10, _ring.MinCol, _ring.MaxCol);

            if (ringTouchPoint == null) // No ring finger present
            {
                if (_ring.IsDown)
                {
                    if (_ring.GetDownTime() < Config.TAP_TIME_MS) // Tap!
                    {
                        Direction tapDir = Direction.Up;
                        if (_ring.GetDownRow() > RING_TOP_LOWEST_ROW) tapDir = Direction.Down;
                        _gestureReceiver.RingTap(tapDir); // Ring also activates cursor in top
                    }

                    _ring.LiftUp();
                }

                return;

            }
            else // Ringer finger is present in the last frame
            {
                if (_ring.IsUp) // Ring was up before
                {
                    // Ring Down => Start Tap-watch
                    _ring.RestartTimer();
                    _ring.TouchDown(ringTouchPoint.GetRow(), ringTouchPoint.GetCol());
                }
            }

        }

        /// <summary>
        /// Tracking the pinky finger
        /// </summary>
        private void TrackLittle()
        {

            // Get the last frame
            TouchFrame lastFrame = _frames.Last;
            TouchFrame beforeLastFrame = _frames.BeforeLast;
            TouchPoint littleTouchPoint = lastFrame.GetPointer(_little.MinCol, _little.MaxCol);

            if (littleTouchPoint == null) // No ring finger present
            {
                if (_little.IsDown) // Little finger was down
                {
                    if (_little.GetDownTime() < Config.TAP_TIME_MS) // Tap!
                    {
                        Direction tapDir = Direction.Up;
                        if (_ring.GetDownRow() > LITTLE_TOP_LOWEST_ROW) tapDir = Direction.Down;
                        _gestureReceiver.LittleTap(tapDir);
                    }

                    _little.LiftUp();
                }

                return;

            }
            else // Ringer finger is present in the last frame
            {
                if (_little.IsUp)
                {
                    // Ring Down => Start Tap-watch
                    _little.RestartTimer();
                    _little.TouchDown(littleTouchPoint.GetRow(), littleTouchPoint.GetCol());
                }
            }

        }

        private string ShowPointers(TouchFrame frame)
        {
            if (frame == null || frame.Pointers == null)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("{");

            foreach (var pair in frame.Pointers)
            {
                sb.Append(pair.Key).Append(", ");
            }
            if (sb.Length >= 2) sb.Remove(sb.Length - 2, 2);
            sb.Append("}");

            return sb.ToString();
        }

        private string ShowHistory()
        {
            int nHistory = _frames.Count;
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            for (int i = nHistory - 1; i > nHistory - 4; i--)
            {
                sb.Append(ShowPointers(_frames[i])).Append("||");
            }
            if (sb.Length >= 2) sb.Remove(sb.Length - 2, 2);
            sb.Append("}");
            return sb.ToString();
        }
    }
}
