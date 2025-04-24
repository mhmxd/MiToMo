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
using MathNet.Numerics.LinearAlgebra.Factorization;
using System.Web.UI;
using System.Windows;

namespace Multi.Cursor
{
    internal class TouchSurface
    {
        private const double MAX_MOVEMENT = 1.0; // Max movement allowed to keep the same ID
        private const int THUMB_TOP_LOWEST_ROW = 10; // Lowest row for top region (incl.) -- range: [9, 13]
        private const int MIDDLE_LEFT_LAST_COL = 8; // Last col. accepted as left -- range: [8, 10]
        private const int RING_TOP_LOWEST_ROW = 3; //  Lowest row for top region (incl.) -- range: [2, 6]
        private const int LITTLE_TOP_LOWEST_ROW = 7; //  Lowest row for top region (incl.) -- range: [9, 13]

        //private TouchFinger _thumb = new TouchFinger(0, 3);
        //private TouchFinger _index = new TouchFinger(4, 7);
        //private TouchFinger _middle = new TouchFinger(8, 10);
        //private TouchFinger _ring = new TouchFinger(11, 13);
        //private TouchFinger _little = new TouchFinger(14, 15);

        private TouchFinger[] _fingers = new TouchFinger[5]
        {
            new TouchFinger(0, 2), // Thumb
            new TouchFinger(3, 6), // Index
            new TouchFinger(7, 10), // Middle
            new TouchFinger(11, 13), // Ring
            new TouchFinger(14, 14) // Little
        };

        private Dictionary<Finger, Stopwatch> _touchTimers = new Dictionary<Finger, Stopwatch>();
        private Dictionary<Finger, Point> _downPositions = new Dictionary<Finger, Point>();
        private Dictionary<Finger, Point> _lastPositions = new Dictionary<Finger, Point>();

        private enum Finger
        {
            Thumb = 1,
            Index = 2,
            Middle = 3,
            Ring = 4,
            Little = 5
        }

        FingerTracker tracker = new FingerTracker();    

        //--- Class for each frame on the surface
        private class TouchFrame
        {
            public Dictionary<int, TouchPoint> Pointers = new Dictionary<int, TouchPoint>();
            public long Timestamp;

            public TouchFrame()
            {
                Timestamp = Stopwatch.GetTimestamp(); // in ticks
            }

            //public void AddPointer(int key, TouchPoint pointer)
            //{
            //    if (Pointers.ContainsKey(key))
            //    {
            //        if (pointer.GetMass() > Pointers[key].GetMass())
            //        {
            //            Pointers[key] = pointer;
            //        }

            //    }
            //    else
            //    {
            //        Pointers.Add(key, pointer);
            //    }
            //}

            public void AddPointer(int key, TouchPoint pointer)
            {
                // Add a new pointer
                Pointers.Add(key, pointer);
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

            public bool DoesNotIncludePointer(TouchFinger finger)
            {
                foreach (int key in Pointers.Keys)
                {
                    if (Utils.InInc(key, finger.MinCol, finger.MaxCol)) return false;
                }

                return true;
            }

            public int GetPointerCount()
            {
                return Pointers.Count;
            }

            public TouchPoint GetPointer(int keyMin, int keyMax)
            {
                foreach (var kv in Pointers)
                {
                    if (Utils.InInc(kv.Key, keyMin, keyMax)) return kv.Value;
                }

                return null;
            }

            public TouchPoint GetPointer(Finger finger)
            {
                if (Pointers.ContainsKey((int)finger)) return Pointers[(int)finger];
                return null;
            }

            public bool HasTouchPoint(Finger finger)
            {
                return Pointers.ContainsKey((int)finger);
            }

            //public TouchPoint GetPointer(int rMin, int rMax, int cMin, int cMax)
            //{
            //    foreach (var kv in Pointers)
            //    {
            //        if (Utils.InInc(kv.Key, cMin, cMax)
            //            && Utils.InInc(kv.Value.GetRow(), rMin, rMax)) return kv.Value;
            //    }

            //    return null;
            //}

            public void Clear()
            {
                Pointers.Clear();
            }
        }

        private FixedBuffer<TouchFrame> _frames = new FixedBuffer<TouchFrame>(50);

        //private Dictionary<int, Stopwatch> _timers = new Dictionary<int, Stopwatch>();

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

            _touchTimers.Add(Finger.Thumb, new Stopwatch());
            _touchTimers.Add(Finger.Index, new Stopwatch());
            _touchTimers.Add(Finger.Middle, new Stopwatch());
            _touchTimers.Add(Finger.Ring, new Stopwatch());
            _touchTimers.Add(Finger.Little, new Stopwatch());

            _downPositions.Add(Finger.Thumb, new Point(-1, -1));
            _downPositions.Add(Finger.Index, new Point(-1, -1));
            _downPositions.Add(Finger.Middle, new Point(-1, -1));
            _downPositions.Add(Finger.Ring, new Point(-1, -1));
            _downPositions.Add(Finger.Little, new Point(-1, -1));

            //_touchPoints = new List<TouchPoint>();
            //_pointerDown = PointerDown;
            //_pointerUp = PointerUp;
            //_state = State.Idle;

            // Init watches for all possible fingers (from left to right)
            //for (int finger = 1; finger <= 5; finger++)
            //{
            //    _timers.Add(finger, new Stopwatch());
            //}
        }

        //private void TrackFingers(Span2D<Byte> touchFrame)
        //{
        //    FILOG.Information(Output.GetString(touchFrame));
        //    Finger[] fingers = tracker.ProcessFrame(touchFrame);

        //    foreach (Finger finger in fingers)
        //    {
        //        FILOG.Information(finger.ToString());
        //    }

        //    FILOG.Information("------------------------------");
        //}

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
            //FILOG.Information(Output.GetString(shotSpan));

            // Result
            TouchFrame activeFrame = new TouchFrame();

            //-- Process each finger partition and add data
            for (int f = 0; f < _fingers.Length; f++)
            {
                TouchFinger tf = _fingers[f];
                TouchPoint touchPoint = new TouchPoint();
                for (int x = tf.MinCol; x <= tf.MaxCol; x++)
                {
                    for (int y = 0; y < 13; y++)
                    {
                        if (shotSpan[y, x] > Config.MIN_PRESSURE)
                        {
                            touchPoint.AddTouchData(x, y, shotSpan[y, x]);
                        }
                    }
                }

                // If total pressure was above 2 X MIN_PRESSURE => finger actie
                if (touchPoint.GetTotalPressure() > 2 * Config.MIN_PRESSURE)
                {
                    //FILOG.Information($"Pressure #{f + 1} = {touchPoint.GetTotalPressure()}");
                    touchPoint.Id = f + 1;
                    activeFrame.AddPointer(touchPoint.Id, touchPoint);
                }
            }


            FILOG.Information(Output.GetKeys(activeFrame.Pointers));
            //GESTURE_LOG.Information(Output.GetKeys(activeFrame.Pointers));

            return activeFrame;

            // Convert ShotSpan to array of columns
            byte[,] columns = new byte[15, 13];
            //(byte val, int row)[] colMax = new (byte, int)[15]; // (value, row)
            //byte max;
            //int maxInd;
            //--- Thumb
            //TouchPoint thumbTouchPoint = new TouchPoint();
            //for (int colInd = 0; colInd <= 3; colInd++) // Column 0 - 3
            //{
            //    byte[] column = shotSpan.GetColumn(colInd).ToArray();
            //    if (column.Max() > Config.MIN_PRESSURE)
            //    {
            //        thumbTouchPoint.AddColumnData(colInd, column);
            //    }
            //}
            //if (thumbTouchPoint.GetTotalPressure() > Config.MIN_PRESSURE) // Check active
            //{
            //    thumbTouchPoint.Id = 0; // Thumb ID
            //    activeFrame.AddPointer(thumbTouchPoint.Id, thumbTouchPoint);
            //}

            ////--- Index
            //TouchPoint indexTouchPoint = new TouchPoint();
            //for (int colInd = 4; colInd <= 6; colInd++) // Columns 4 - 6 are certain
            //{
            //    byte[] column = shotSpan.GetColumn(colInd).ToArray();
            //    if (column.Max() > Config.MIN_PRESSURE)
            //    {
            //        indexTouchPoint.AddColumnData(colInd, column);
            //    }
            //}
            //byte[] column7 = shotSpan.GetColumn(7).ToArray();
            // Only add column 7 if all its pressures are more than adjacent cells in column 8


            //int c = 0;
            //while (c < shotSpan.Width)
            //{
            //    byte[] column = shotSpan.GetColumn(c).ToArray();
            //    max = column.Max(); maxInd = Array.IndexOf(column, max);

            //    if (c == 0)
            //    { // Only check against col. 1
            //        byte[] col1 = shotSpan.GetColumn(1).ToArray();
            //        if (max > MIN_TOUCH_VAL && max > col1.Max())
            //        {
            //            TouchPoint touchPoint = new TouchPoint
            //                (max, maxInd, c);

            //            //--- Add values around
            //            // Above
            //            if (maxInd != 0)
            //            {
            //                touchPoint.SetValue(1, column[maxInd - 1]); // Above
            //                touchPoint.SetValue(2, col1[maxInd - 1]); // Right above
            //            }

            //            // Below
            //            if (maxInd != column.Length - 1)
            //            {
            //                touchPoint.SetValue(7, column[maxInd + 1]); // Below
            //                touchPoint.SetValue(8, col1[maxInd + 1]); // Right below
            //            }

            //            // Right
            //            touchPoint.SetValue(5, col1[maxInd]);

            //            //int tpId = touchPoint.GetCol(); // Using CoM as ID
            //            int tpId = c; // Use the column as ID
            //            touchPoint.Id = tpId;
            //            activeFrame.AddPointer(tpId, touchPoint); // Add to the frame

            //            //activeTouchPoints.Add(fingerTouchPoint);
            //            //UpdatePointers(fingerTouchPoint);

            //            // Jump one column forward (1 is certainly not touch point center)
            //            c = 2;
            //        }

            //        c += 1; // Check next column
            //    }

            //    else if (c == 14)
            //    { // DO NOT check agianst col. 13 (both can have values)!
            //        byte[] col13 = shotSpan.GetColumn(13).ToArray();
            //        if (max > MIN_TOUCH_VAL)
            //        {
            //            TouchPoint touchPoint = new TouchPoint(max, maxInd, c);

            //            //--- Add values around
            //            // Above
            //            if (maxInd != 0)
            //            {
            //                touchPoint.SetValue(1, column[maxInd - 1]); // Above
            //                touchPoint.SetValue(0, col13[maxInd - 1]); // Left above
            //            }

            //            // Below
            //            if (maxInd != column.Length - 1)
            //            {
            //                touchPoint.SetValue(7, column[maxInd + 1]); // Below
            //                touchPoint.SetValue(6, col13[maxInd + 1]); // Left below
            //            }

            //            // Left
            //            touchPoint.SetValue(3, col13[maxInd]);

            //            //int tpId = touchPoint.GetCol(); // Using CoM as ID
            //            int tpId = c; // Use the column as ID
            //            touchPoint.Id = tpId;
            //            activeFrame.AddPointer(tpId, touchPoint); // Add to the frame

            //            //activeTouchPoints.Add(fingerTouchPoint);
            //            //UpdatePointers(touchPoint);

            //        }

            //        break;
            //    }

            //    else if (c == 13)
            //    { // Only check against col. 12
            //        byte[] col12 = shotSpan.GetColumn(12).ToArray();
            //        if (max > MIN_TOUCH_VAL && max > col12.Max())
            //        {
            //            TouchPoint touchPoint = new TouchPoint(max, maxInd, c);

            //            //--- Add values around
            //            // Above
            //            if (maxInd != 0)
            //            {
            //                touchPoint.SetValue(1, column[maxInd - 1]); // Above
            //                touchPoint.SetValue(0, col12[maxInd - 1]); // Left above
            //            }

            //            // Below
            //            if (maxInd != column.Length - 1)
            //            {
            //                touchPoint.SetValue(7, column[maxInd + 1]); // Below
            //                touchPoint.SetValue(6, col12[maxInd + 1]); // Left below
            //            }

            //            // Left
            //            touchPoint.SetValue(3, col12[maxInd]);

            //            //int tpId = touchPoint.GetCol(); // Using CoM as ID
            //            int tpId = c; // Use the column as ID
            //            touchPoint.Id = tpId;
            //            activeFrame.AddPointer(tpId, touchPoint); // Add to the frame

            //            //activeTouchPoints.Add(fingerTouchPoint);
            //            //UpdatePointers(touchPoint);

            //        }

            //        c += 1; // Check 14 as well
            //    }

            //    else // Rest of columns
            //    {
            //        byte[] prevCol = shotSpan.GetColumn(c - 1).ToArray();
            //        byte[] nextCol = shotSpan.GetColumn(c + 1).ToArray();
            //        //GESTURE_LOG.Information($"prevMax = {prevCol.Max()}, Max = {max}, nextMax = {nextCol.Max()}");
            //        if (max > MIN_TOUCH_VAL && max > nextCol.Max())
            //        {
            //            TouchPoint touchPoint = new TouchPoint(max, maxInd, c);
            //            touchPoint.Id = touchId++;

            //            //--- Add values around
            //            // Above
            //            if (maxInd != 0)
            //            {
            //                touchPoint.SetValue(0, prevCol[maxInd - 1]); // Left above
            //                touchPoint.SetValue(1, column[maxInd - 1]); // Above
            //                touchPoint.SetValue(2, nextCol[maxInd - 1]); // Right above
            //            }

            //            // Below
            //            if (maxInd != column.Length - 1)
            //            {
            //                touchPoint.SetValue(6, prevCol[maxInd + 1]); // Left below
            //                touchPoint.SetValue(7, column[maxInd + 1]); // Below
            //                touchPoint.SetValue(8, nextCol[maxInd + 1]); // Right below
            //            }

            //            // Left and right
            //            touchPoint.SetValue(3, prevCol[maxInd]);
            //            touchPoint.SetValue(5, nextCol[maxInd]);

            //            //int tpId = touchPoint.GetCol(); // Using CoM as ID
            //            int tpId = c; // Use the column as ID
            //            touchPoint.Id = tpId;
            //            activeFrame.AddPointer(tpId, touchPoint); // Add to the frame

            //            // Jump one column forward (next col is certainly not touch point)
            //            c += 2;

            //            //activeTouchPoints.Add(fingerTouchPoint);
            //            //UpdatePointers(touchPoint);
            //        } else // Next column should be checked
            //        {
            //            c += 1;
            //        }


            //    }

            //}

            //return activeFrame;
        }

        /// <summary>
        /// Track touches using the shots
        /// </summary>
        /// <param name="shotSpan"></param>
        public void Track(Span2D<Byte> shotSpan)
        {
            //TrackFingers(shotSpan);
            // First, get the current frame
            TouchFrame activeFrame = FillActiveTouches(shotSpan);

            //--- Print
            //GESTURE_LOG.Information(JsonSerializer.Serialize(_activeTouches));

            _frames.Add(activeFrame);
            //GESTURE_LOG.Verbose($"nFrames = {_frames.Count}");
            if (_frames.Count > 1) // Need at least two frames
            {
                //TouchFrame lastFrame = _frames.Last;
                //TouchFrame beforeLastFrame = _frames.BeforeLast;

                //GESTURE_LOG.Information(Output.GetKeys(lastFrame.Pointers));
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
                TrackLittle();

            }

        }


        /// <summary>
        /// Tracking of the thumb finger
        /// </summary>
        private void TrackThumb()
        {
            // Get the last frame
            TouchFrame currentFrame = _frames.Last;
            TouchFrame beforeLastFrame = _frames.BeforeLast;
            //TouchPoint thumbPoint = currentFrame.GetPointer(Finger.Thumb);

            if (currentFrame.HasTouchPoint(Finger.Thumb)) // middle present
            {
                Point center = currentFrame.GetPointer(Finger.Thumb).GetCenter();
                if (_touchTimers[Finger.Thumb].IsRunning) // Already active => update position (move)
                {
                    _lastPositions[Finger.Thumb] = center;
                    // Mange the Swipe (later)
                }
                else // First touch
                {
                    _downPositions[Finger.Thumb] = center;
                    _lastPositions[Finger.Thumb] = center;
                    _touchTimers[Finger.Thumb].Restart(); // Start the timer
                }
            }
            else // No middle present in the current frame
            {
                Point downPosition = _downPositions[Finger.Thumb];
                Point lastPosition = _lastPositions[Finger.Thumb];
                if (_touchTimers[Finger.Thumb].IsRunning) // Was active => Lifted up
                {
                    // Check Tap time and movement conditions
                    if (_touchTimers[Finger.Thumb].ElapsedMilliseconds < Config.TAP_TIME_MS
                        && Abs(lastPosition.X - downPosition.X) < Config.TAP_X_MOVE_LIMIT
                        && Abs(lastPosition.Y - downPosition.Y) < Config.TAP_Y_MOVE_LIMIT)
                    {
                        // Find the Tap position (Top or Bottom)
                        if (lastPosition.Y < THUMB_TOP_LOWEST_ROW) // Top
                        {
                            _gestureReceiver.ThumbTap(Location.Top);
                            FILOG.Information("Thumb tapped! Top.");
                        }
                        else // Bottom
                        {
                            _gestureReceiver.ThumbTap(Location.Bottom);
                            FILOG.Information("Thumb tapped! Bottom.");
                        }
                    }

                    _touchTimers[Finger.Thumb].Stop();
                }
            }

            //if (thumbPoint == null) // Thumb not present in the current frame
            //{
            //    _lastThumbTP = null;

            //    if (_thumb.IsDown)
            //    {
            //        if (_timers[1].ElapsedMilliseconds < Config.TAP_TIME_MS) // Tap!
            //        {
            //            // Distinguish where the tap was...
            //            Location tapLoc = Location.Top;
            //            if (_thumb.GetDownRow() > THUMB_TOP_LOWEST_ROW) tapLoc = Location.Bottom;
            //            _gestureReceiver.ThumbTap(tapLoc);
            //        }

            //        _gestureReceiver.ThumbUp(currentFrame.GetPointer(0, 3)); // DON'T DELETE! (Needed for Radiusor)

            //        _thumb.LiftUp();
            //    }

            //    //if (beforeLastFrame.IncludePointer(0, 3)) // Thumb was present before => Check for Tap
            //    //{

            //    //    if (_timers[1].ElapsedMilliseconds < Config.TAP_TIME_MS) // Tap!
            //    //    {
            //    //        _gestureReceiver.ThumbTap();
            //    //    }

            //    //    _gestureReceiver.ThumbUp(lastFrame.GetPointer(0, 3)); // DON'T DELETE! (Needed for Radiusor)
            //    //}

            //    return;
            //}

            //--- Thumb present ----------------------------------------

            //if (_thumb.IsUp)
            //{
            //    // Thumb Down => Start Tap-watch
            //    //_thumb.TouchDown(thumbPoint.GetRow(), thumbPoint.GetCol());

            //    _timers[1].Restart();
            //}

            //if (beforeLastFrame.ExcludePointer(0, 3)) // Thumb wasn't present => Start Tap-watch
            //{
            //    _timers[1].Restart();
            //}

            // Send the point for analyzing the movement
            //_gestureReceiver.ThumbMove(thumbPoint); // Managed where implemented (could be 0)

            //--- Track gestures 
            // First and last frames
            //TouchFrame historyFirst = _frames.First;
            //long historyStartTimestamp = historyFirst.Timestamp;

            //TouchFrame historyLast = _frames.Last;
            //long historyEndTimestamp = historyLast.Timestamp;

            //// History duration
            //double historyDT =
            //    (historyEndTimestamp - historyStartTimestamp)
            //    / (double)Stopwatch.Frequency;

            //// Not enough history
            //if (historyDT < Config.SWIPE_TIME_MIN) return;

            ////--- Long hisotry

            //// Check the last window of frames that fits the time duration
            //for (int i = _frames.Count - 2; i >= 0; i--)
            //{
            //    TouchFrame firstFrame = _frames[i];
            //    long firstTimestamp = firstFrame.Timestamp;
            //    double duration = (historyEndTimestamp - firstTimestamp) / (double)Stopwatch.Frequency;

            //    if (firstTimestamp <= _lastThumbGestureTimestamp) continue;
            //    if (duration < Config.SWIPE_TIME_MIN) continue; // Too fast, keep looking
            //    if (duration > Config.SWIPE_TIME_MAX) break; // Too slow

            //    //--- Frames found ------------------------------------------

            //    // Check for thumb...
            //    if (!firstFrame.IncludePointer(0, 3) || !historyLast.IncludePointer(0, 3)) break;

            //    //- Thumb found
            //    TouchPoint gestureStart = firstFrame.GetPointer(0, 3);
            //    TouchPoint gestureEnd = historyLast.GetPointer(0, 3);

            //    //double dX = gestureEnd.GetX() - gestureStart.GetX();
            //    //double dY = gestureEnd.GetY() - gestureStart.GetY();
            //    double dX = gestureEnd.GetCenter().X - gestureStart.GetCenter().X;
            //    double dY = gestureEnd.GetCenter().Y - gestureStart.GetCenter().Y;

            //    //-- Check against thresholds
            //    if (Abs(dX) > Config.MOVE_THRESHOLD) // Swipe left/right
            //    {
            //        _lastThumbGestureTimestamp = historyEndTimestamp;

            //        if (Abs(dY) > Config.MOVE_LIMIT)
            //        {
            //            //_frames = new FixedBuffer<TouchFrame>(_frames.Count); // Reset
            //            return;
            //        }
            //        else
            //        {
            //            if (dX > Config.MOVE_THRESHOLD) // Right
            //            {
            //                _gestureReceiver.ThumbSwipe(Location.Right);
            //                return;
            //                //_frames = new FixedBuffer<TouchFrame>(_frames.Count); // Reset
            //            }
            //            else // Left
            //            {
            //                _gestureReceiver.ThumbSwipe(Location.Left);
            //                return;
            //                //_frames = new FixedBuffer<TouchFrame>(_frames.Count); // Reset
            //            }

            //        }
            //    }

            //    if (Abs(dY) > Config.HIGHT_MOVE_THRESHOLD) // Swipe up/down
            //    {
            //        _lastThumbGestureTimestamp = historyEndTimestamp;

            //        if (Abs(dX) > Config.MOVE_LIMIT) // dX exceeded limits
            //        {
            //            //_frames = new FixedBuffer<TouchFrame>(_frames.Count); // Reset
            //            return;
            //        }
            //        else
            //        {
            //            if (dY > Config.HIGHT_MOVE_THRESHOLD) // Down
            //            {
            //                _gestureReceiver.ThumbSwipe(Location.Bottom);
            //                return;
            //                //_frames = new FixedBuffer<TouchFrame>(_frames.Count); // Reset
            //            }
            //            else // Up
            //            {
            //                _gestureReceiver.ThumbSwipe(Location.Top);
            //                return;
            //                //_frames = new FixedBuffer<TouchFrame>(_frames.Count); // Reset
            //            }

            //        }
            //    }
            //}
        }

        /// <summary>
        /// Tracking the index finger
        /// </summary>
        private void TrackIndex()
        {
            // Get the last frame
            //TouchFrame beforeLastFrame = _frames.BeforeLast;
            //TouchPoint indexLastDown = beforeLastFrame.GetPointer(_index.MinCol, _index.MaxCol);
            TouchFrame currentFrame = _frames.Last;
            //TouchPoint indexTouchPoint = lastFrame.GetPointer(_index.MinCol, _index.MaxCol);
            //TouchPoint indexTouchPoint = lastFrame.GetPointer(Finger.Index);
            Finger finger = Finger.Index;

            if (currentFrame.HasTouchPoint(finger)) // Finger present
            {
                Point center = currentFrame.GetPointer(finger).GetCenter();
                if (_touchTimers[finger].IsRunning) // Already active => update position (move)
                {
                    _lastPositions[finger] = center;
                    // Mange the Swipe (later)
                }
                else // First touch
                {
                    _downPositions[finger] = center;
                    _lastPositions[finger] = center;
                    _touchTimers[finger].Restart(); // Start the timer
                }
            }
            else // Finger not present in the current frame
            {
                Point downPosition = _downPositions[finger];
                Point lastPosition = _lastPositions[finger];
                if (_touchTimers[finger].IsRunning) // Was active => Lifted up
                {
                    FILOG.Information($"Index Up: {_touchTimers[finger].ElapsedMilliseconds}" +
                        $" | dX = {Abs(lastPosition.X - downPosition.X):F3}" +
                        $" | dY = {Abs(lastPosition.Y - downPosition.Y):F3}");
                    if (_touchTimers[finger].ElapsedMilliseconds < Config.TAP_TIME_MS
                        && Abs(lastPosition.X - downPosition.X) < Config.TAP_X_MOVE_LIMIT
                        && Abs(lastPosition.Y - downPosition.Y) < Config.TAP_Y_MOVE_LIMIT)
                    {
                        FILOG.Information("Index tapped!");
                        _gestureReceiver.IndexTap();
                    }

                    _touchTimers[finger].Stop();
                }
            }

            //if (indexTouchPoint == null) // No index finger present in the frame => reset the last position
            //{
            //    if (_index.IsDown) // It was previously down
            //    {
            //        FILOG.Information($"{ShowPointers(lastFrame),-18} Index up: down time = {_index.GetDownTime()} | " +
            //            $"travel dist = {_index.GetTravelDistX():F2}, {_index.GetTravelDistY():F2}");
            //        // If the index was down and hasn't moved outside of Tap limit => Tap!
            //        if (_index.GetDownTime() < Config.TAP_TIME_MS 
            //            && _index.GetTravelDistX() < Config.TAP_X_MOVE_LIMIT
            //            && _index.GetTravelDistY() < Config.TAP_Y_MOVE_LIMIT) 
            //        {
            //            FILOG.Information("Index tapped!");
            //            _gestureReceiver.IndexTap();
            //        }

            //        _index.LiftUp();
            //        _gestureReceiver.IndexUp();
            //        _lastIndexTP = null;
            //    }

            //    return;
            //} 
            //else // Index is present
            //{
            //    _index.TouchMove(indexTouchPoint.GetCenter());

            //    if (_index.IsUp) // Index was up before
            //    {
            //        FILOG.Information($"{ShowPointers(lastFrame),-18} Index down");
            //        _index.TouchDown(indexTouchPoint.GetCenter());
            //        _index.RestartTimer(); // Index Down => Start Tap-watch
            //    }
            //    else // Index is still down => movement
            //    {
            //        // Send the point for analyzing the movement
            //        _gestureReceiver.IndexMove(indexTouchPoint);
            //    }
            //}
        }

        /// <summary>
        /// Tracking the middle finger
        /// </summary>
        private void TrackMiddle()
        {
            // Get the last frame
            TouchFrame currentFrame = _frames.Last;
            //TouchPoint middleTouchPoint = lastFrame.GetPointer(Finger.Middle);
            Finger finger = Finger.Middle;

            if (currentFrame.HasTouchPoint(finger)) // middle present
            {
                Point center = currentFrame.GetPointer(finger).GetCenter();
                if (_touchTimers[finger].IsRunning) // Already active => update position (move)
                {
                    _lastPositions[finger] = center;
                    // Mange the Swipe (later)
                }
                else // First touch
                {
                    _downPositions[finger] = center;
                    _lastPositions[finger] = center;
                    _touchTimers[finger].Restart(); // Start the timer
                }
            }
            else // No middle present in the current frame
            {
                Point downPosition = _downPositions[finger];
                Point lastPosition = _lastPositions[finger];
                if (_touchTimers[finger].IsRunning) // Was active => Lifted up
                {
                    FILOG.Information($"Middle Up: {_touchTimers[finger].ElapsedMilliseconds}" +
                        $" | dX = {Abs(lastPosition.X - downPosition.X):F3}" +
                        $" | dY = {Abs(lastPosition.Y - downPosition.Y):F3}");
                    if (_touchTimers[finger].ElapsedMilliseconds < Config.TAP_TIME_MS
                        && Abs(lastPosition.X - downPosition.X) < Config.TAP_X_MOVE_LIMIT
                        && Abs(lastPosition.Y - downPosition.Y) < Config.TAP_Y_MOVE_LIMIT)
                    {
                        _gestureReceiver.MiddleTap();
                        FILOG.Information("Middle tapped!");
                    }

                    _touchTimers[finger].Stop();
                }
            }

            //if (middleTouchPoint == null) // No middle finger present
            //{

            //    if (_middle.IsDown) // It was previously down
            //    {
            //        FILOG.Information($"{ShowPointers(lastFrame),-18} Middle up: down time = {_middle.GetDownTime()} | " +
            //            $"travel dist = {_middle.GetTravelDistX():F2}, {_middle.GetTravelDistY():F2}");
            //        // If the middle was down and hasn't moved outside of Tap limit => Tap!
            //        if (_middle.GetDownTime() < Config.TAP_TIME_MS
            //            && _middle.GetTravelDistX() < Config.TAP_X_MOVE_LIMIT
            //            && _middle.GetTravelDistY() < Config.TAP_Y_MOVE_LIMIT)
            //        {
            //            FILOG.Information("Middle tapped!");
            //            _gestureReceiver.MiddleTap();
            //        }

            //        _middle.LiftUp();
            //    }

            //    return;

            //    //if (_middle.GetDownTime() < Config.TAP_TIME_MS) // Tap!
            //    //{
            //    //    //Location tapLoc = Location.Left;
            //    //    //if (_middle.GetDownCol() > MIDDLE_LEFT_LAST_COL) tapLoc = Location.Right;
            //    //    _gestureReceiver.MiddleTap(); // Bc of ring, middle always activates left
            //    //}

            //    //_middle.LiftUp();

            //}
            //else // Middle present
            //{

            //    if (_middle.IsUp)
            //    {
            //        FILOG.Information($"{ShowPointers(lastFrame),-18} Middle down");
            //        // Finger Down => Start Tap-watch
            //        _middle.TouchDown(middleTouchPoint.GetCenter());
            //        _middle.RestartTimer();
            //    } else
            //    {
            //        _middle.TouchMove(middleTouchPoint.GetCenter());
            //    }

            //}


        }

        /// <summary>
        /// Tracking the ring finger
        /// </summary>
        private void TrackRing()
        {

            // Get the last frame
            TouchFrame currentFrame = _frames.Last;
            //TouchPoint ringTouchPoint = lastFrame.GetPointer(Finger.Ring);

            Finger finger = Finger.Ring;

            if (currentFrame.HasTouchPoint(finger)) // middle present
            {
                Point center = currentFrame.GetPointer(finger).GetCenter();
                if (_touchTimers[finger].IsRunning) // Already active => update position (move)
                {
                    _lastPositions[finger] = center;
                    // Mange the Swipe (later)
                }
                else // First touch
                {
                    _downPositions[finger] = center;
                    _lastPositions[finger] = center;
                    _touchTimers[finger].Restart(); // Start the timer
                }
            }
            else // No middle present in the current frame
            {
                Point downPosition = _downPositions[finger];
                Point lastPosition = _lastPositions[finger];
                if (_touchTimers[finger].IsRunning) // Was active => Lifted up
                {
                    FILOG.Information($"Ring Up: {_touchTimers[finger].ElapsedMilliseconds}" +
                        $" | dX = {Abs(lastPosition.X - downPosition.X):F3}" +
                        $" | dY = {Abs(lastPosition.Y - downPosition.Y):F3}");
                    if (_touchTimers[finger].ElapsedMilliseconds < Config.TAP_TIME_MS
                        && Abs(lastPosition.X - downPosition.X) < Config.TAP_X_MOVE_LIMIT
                        && Abs(lastPosition.Y - downPosition.Y) < Config.TAP_Y_MOVE_LIMIT)
                    {
                        _gestureReceiver.RingTap();
                        FILOG.Information("Ring tapped!");
                    }

                    _touchTimers[finger].Stop();
                }
            }

            //if (ringTouchPoint == null) // No ring finger present
            //{
            //    if (_ring.IsDown)
            //    {
            //        FILOG.Information($"{ShowPointers(lastFrame),-18} Ring up: down time = {_ring.GetDownTime()} | " +
            //            $"travel dist = {_ring.GetTravelDistX():F2}, {_ring.GetTravelDistY():F2}");
            //        // If the finger was down and hasn't moved outside of Tap limit => Tap!
            //        if (_ring.GetDownTime() < Config.TAP_TIME_MS
            //            && _ring.GetTravelDistX() < Config.TAP_X_MOVE_LIMIT
            //            && _ring.GetTravelDistY() < Config.TAP_Y_MOVE_LIMIT)
            //        {
            //            FILOG.Information("Ring tapped!");
            //            _gestureReceiver.RingTap();
            //        }

            //        _ring.LiftUp();
            //    }

            //    return;

            //}
            //else // Ring finger is present in the last frame
            //{
            //    if (_ring.IsUp)
            //    {
            //        FILOG.Information($"{ShowPointers(lastFrame),-18} Ring down");
            //        // Finger Down => Start Tap-watch
            //        _ring.TouchDown(ringTouchPoint.GetCenter());
            //        _ring.RestartTimer();
            //    }
            //    else
            //    {
            //        _ring.TouchMove(ringTouchPoint.GetCenter());
            //    }
            //}

        }

        /// <summary>
        /// Tracking the pinky finger
        /// </summary>
        private void TrackLittle()
        {

            // Get the last frame
            TouchFrame currentFrame = _frames.Last;
            //TouchFrame beforeLastFrame = _frames.BeforeLast;
            //TouchPoint littleTouchPoint = lastFrame.GetPointer(Finger.Little);

            Finger finger = Finger.Little;

            if (currentFrame.HasTouchPoint(finger)) // middle present
            {
                Point center = currentFrame.GetPointer(finger).GetCenter();
                if (_touchTimers[finger].IsRunning) // Already active => update position (move)
                {
                    _lastPositions[finger] = center;
                    // Mange the Swipe (later)
                }
                else // First touch
                {
                    _downPositions[finger] = center;
                    _lastPositions[finger] = center;
                    _touchTimers[finger].Restart(); // Start the timer
                }
            }
            else // No middle present in the current frame
            {
                Point downPosition = _downPositions[finger];
                Point lastPosition = _lastPositions[finger];
                if (_touchTimers[finger].IsRunning) // Was active => Lifted up
                {
                    FILOG.Information($"Ring Up: {_touchTimers[finger].ElapsedMilliseconds}" +
                        $" | dX = {Abs(lastPosition.X - downPosition.X):F3}" +
                        $" | dY = {Abs(lastPosition.Y - downPosition.Y):F3}");
                    if (_touchTimers[finger].ElapsedMilliseconds < Config.TAP_TIME_MS
                        && Abs(lastPosition.X - downPosition.X) < Config.TAP_X_MOVE_LIMIT
                        && Abs(lastPosition.Y - downPosition.Y) < Config.TAP_Y_MOVE_LIMIT)
                    {
                        // Find the Tap position (Top or Bottom)
                        if (lastPosition.Y < LITTLE_TOP_LOWEST_ROW) // Top
                        {
                            _gestureReceiver.LittleTap(Location.Top);
                            FILOG.Information("Little tapped! Top.");
                        }
                        else // Bottom
                        {
                            _gestureReceiver.LittleTap(Location.Bottom);
                            FILOG.Information("Little tapped! Bottom.");
                        }
                    }

                    _touchTimers[finger].Stop();
                }
            }

            //if (littleTouchPoint == null) // No ring finger present
            //{
            //    if (_little.IsDown) // Little finger was down
            //    {
            //        if (_little.GetDownTime() < Config.TAP_TIME_MS) // Tap!
            //        {
            //            Location tapLoc = Location.Top;
            //            if (_little.GetDownRow() > LITTLE_TOP_LOWEST_ROW) tapLoc = Location.Bottom;
            //            _gestureReceiver.LittleTap(tapLoc);
            //        }

            //        _little.LiftUp();
            //    }

            //    return;

            //}
            //else // Ringer finger is present in the last frame
            //{
            //    if (_little.IsUp)
            //    {
            //        // Ring Down => Start Tap-watch
            //        _little.RestartTimer();
            //        //_little.TouchDown(littleTouchPoint.GetRow(), littleTouchPoint.GetCol());
            //        _little.TouchDown(littleTouchPoint.GetCenter());
            //    }
            //}

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

        private string Dig2(double d)
        {
            return $"{d:F2}";
        }
    }
}
