﻿using CommunityToolkit.HighPerformance;
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
using Serilog.Core;

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
        private FixedBuffer<TouchFrame> _thumbGestureFrames = new FixedBuffer<TouchFrame>(100);
        private TouchFrame _thumbGestureStart;

        private Stopwatch debugWatch = new Stopwatch();

        private enum Finger
        {
            Thumb = 1,
            Index = 2,
            Middle = 3,
            Ring = 4,
            Little = 5
        }

        FingerTracker tracker = new FingerTracker();

        private Experiment.Technique _activeTechnique = Experiment.Technique.Auxursor_Tap; // Set in the constructor

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

        public TouchSurface(ToMoGestures gestureReceiver, Experiment.Technique activeTechnique)
        {
            _gestureReceiver = gestureReceiver;

            _upPointerIds = new List<int>() { -1 };
            _downPointersIds = new List<int>() { -1 };

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

            _activeTechnique = activeTechnique;

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
        //    FILOG.Debug(Output.GetString(touchFrame));
        //    Finger[] fingers = tracker.ProcessFrame(touchFrame);

        //    foreach (Finger finger in fingers)
        //    {
        //        FILOG.Debug(finger.ToString());
        //    }

        //    FILOG.Debug("------------------------------");
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
            //FILOG.Debug(Output.GetString(shotSpan));

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
                    //FILOG.Debug($"Pressure #{f + 1} = {touchPoint.GetTotalPressure()}");
                    touchPoint.Id = f + 1;
                    activeFrame.AddPointer(touchPoint.Id, touchPoint);
                }
            }

            //FILOG.Debug(Output.GetKeys(activeFrame.Pointers));
            //GESTURE_LOG.Information(Output.GetKeys(activeFrame.Pointers));

            return activeFrame;
        }

        /// <summary>
        /// Track touches using the shots
        /// </summary>
        /// <param name="shotSpan"></param>
        public void Track(Span2D<Byte> shotSpan)
        {
            //--- Debug
            if (debugWatch.IsRunning)
            {
                debugWatch.Stop();
                FILOG.Debug($"Track time = {debugWatch.ElapsedMilliseconds} ms");
            }
            else
            {
                debugWatch.Restart();
            }
            //TrackFingers(shotSpan);
            // First, get the current frame
            TouchFrame activeFrame = FillActiveTouches(shotSpan);

            _frames.Add(activeFrame);
            
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

                if (_activeTechnique == Experiment.Technique.Auxursor_Tap)
                {
                    TapTrackThumb();
                    TapTrackIndex();
                    TapTrackMiddle();
                    TapTrackRing();
                    TapTrackLittle();
                }

                if (_activeTechnique == Experiment.Technique.Auxursor_Swipe)
                {
                    SwipeTechTrackThumb();
                    SwipeTechTrackIndex();
                    SwipeTechTrackMiddle();
                    SwipeTechTrackRing();
                    SwipeTechTrackLittle();
                }


            }

        }


        //========= Tap tech tracking ==========================

        /// <summary>
        /// Tracking of the thumb finger
        /// </summary>
        private void TapTrackThumb()
        {
            // Get the last frame
            TouchFrame currentFrame = _frames.Last;
            //TouchFrame beforeLastFrame = _frames.BeforeLast;
            //TouchPoint thumbPoint = currentFrame.GetPointer(Finger.Thumb);
            Finger finger = Finger.Thumb;

            if (currentFrame.HasTouchPoint(finger)) // Finger present
            {
                Point center = currentFrame.GetPointer(finger).GetCenter();
                if (_touchTimers[finger].IsRunning) // Already active => update position
                {
                    _lastPositions[finger] = center; // Update position
                }
                else // First touch
                {
                    GestInfo<TouchSurface>($"{finger.ToString()} Down: {currentFrame.Timestamp}");
                    _downPositions[finger] = center;
                    _lastPositions[finger] = center;
                    _touchTimers[finger].Restart(); // Start the timer
                    _thumbGestureStart = currentFrame;
                }
            }
            else // Finger NOT present in the current frame
            {
                if (_touchTimers[finger].IsRunning) // Was active => Lifted up
                {
                    Point downPosition = _downPositions[finger];
                    Point lastPosition = _lastPositions[finger];

                    // Check Tap time and movement conditions
                    GestInfo<TouchSurface>($"{finger.ToString()} Up: {_touchTimers[finger].ElapsedMilliseconds}" +
                        $" | dX = {Abs(lastPosition.X - downPosition.X):F2}" +
                        $" | dY = {Abs(lastPosition.Y - downPosition.Y):F2}");
                    if (_touchTimers[finger].ElapsedMilliseconds < Config.TAP_TIME_MS
                        && Abs(lastPosition.X - downPosition.X) < Config.TAP_X_MOVE_LIMIT
                        && Abs(lastPosition.Y - downPosition.Y) < Config.TAP_Y_MOVE_LIMIT)
                    {
                        // Find the Tap position (Top or Bottom)
                        if (lastPosition.Y < THUMB_TOP_LOWEST_ROW) // Top
                        {
                            GestInfo<TouchSurface>($"{finger.ToString()} Tapped! Top.");
                            _gestureReceiver.ThumbTap(Location.Top);
                            
                        }
                        else // Bottom
                        {
                            GestInfo<TouchSurface>($"{finger.ToString()} Tapped! Bottom.");
                            _gestureReceiver.ThumbTap(Location.Bottom);
                            
                        }
                    }

                    _gestureReceiver.ThumbUp();
                    _touchTimers[finger].Stop();
                }
                
            }
        }

        /// <summary>
        /// Tracking the index finger
        /// </summary>
        private void TapTrackIndex()
        {
            TouchFrame currentFrame = _frames.Last; // Get current frame
            Finger finger = Finger.Index;

            if (currentFrame.HasTouchPoint(finger)) // Finger present
            {
                TouchPoint touchPoint = currentFrame.GetPointer(finger);
                Point tpCenter = touchPoint.GetCenter();
                if (_touchTimers[finger].IsRunning) // Already active => update position (move)
                {
                    _lastPositions[finger] = tpCenter;

                    // Index moving
                    _gestureReceiver.IndexMove(touchPoint);
                }
                else // First touch
                {
                    GestInfo<TouchSurface>($"{finger.ToString()} Down: {currentFrame.Timestamp}");
                    _downPositions[finger] = tpCenter;
                    _lastPositions[finger] = tpCenter;
                    _touchTimers[finger].Restart(); // Start the timer
                }
            }
            else // Finger not present in the current frame
            {
                if (_touchTimers[finger].IsRunning) // Was active => Lifted up
                {
                    Point downPosition = _downPositions[finger];
                    Point lastPosition = _lastPositions[finger];
                    GestInfo<TouchSurface>($"{finger.ToString()} Up: {_touchTimers[finger].ElapsedMilliseconds}" +
                        $" | dX = {Abs(lastPosition.X - downPosition.X):F2}" +
                        $" | dY = {Abs(lastPosition.Y - downPosition.Y):F2}");
                    if (_touchTimers[finger].ElapsedMilliseconds < Config.TAP_TIME_MS
                        && Abs(lastPosition.X - downPosition.X) < Config.TAP_X_MOVE_LIMIT
                        && Abs(lastPosition.Y - downPosition.Y) < Config.TAP_Y_MOVE_LIMIT)
                    {
                        GestInfo<TouchSurface>($"{finger} Tapped!");
                        _gestureReceiver.IndexTap();
                    }

                    _gestureReceiver.IndexUp();
                    _touchTimers[finger].Stop();
                }
                
            }

        }

        /// <summary>
        /// Tracking the middle finger
        /// </summary>
        private void TapTrackMiddle()
        {
            // Get the last frame
            TouchFrame currentFrame = _frames.Last;
            //TouchPoint middleTouchPoint = lastFrame.GetPointer(Finger.Middle);
            Finger finger = Finger.Middle;

            if (currentFrame.HasTouchPoint(finger)) // Finger present
            {
                Point center = currentFrame.GetPointer(finger).GetCenter();
                if (_touchTimers[finger].IsRunning) // Already active => update position (move)
                {
                    _lastPositions[finger] = center;

                }
                else // First touch
                {
                    FILOG.Debug($"{finger.ToString()} Down.");
                    _downPositions[finger] = center;
                    _lastPositions[finger] = center;
                    _touchTimers[finger].Restart(); // Start the timer
                }
            }
            else // Finger NOT present in the current frame
            {
                if (_lastPositions.ContainsKey(finger))
                {
                    Point downPosition = _downPositions[finger];
                    Point lastPosition = _lastPositions[finger];
                    if (_touchTimers[finger].IsRunning) // Was active => Lifted up
                    {
                        FILOG.Information($"{finger.ToString()} Up: {_touchTimers[finger].ElapsedMilliseconds}" +
                            $" | dX = {Abs(lastPosition.X - downPosition.X):F3}" +
                            $" | dY = {Abs(lastPosition.Y - downPosition.Y):F3}");
                        if (_touchTimers[finger].ElapsedMilliseconds < Config.TAP_TIME_MS
                            && Abs(lastPosition.X - downPosition.X) < Config.TAP_X_MOVE_LIMIT
                            && Abs(lastPosition.Y - downPosition.Y) < Config.TAP_Y_MOVE_LIMIT)
                        {
                            _gestureReceiver.MiddleTap();
                            GestInfo<TouchSurface>($"{finger} Tapped!");
                        }

                        _touchTimers[finger].Stop();
                    }
                }
                
            }

            //if (middleTouchPoint == null) // No middle finger present
            //{

            //    if (_middle.IsDown) // It was previously down
            //    {
            //        FILOG.Debug($"{ShowPointers(lastFrame),-18} Middle up: down time = {_middle.GetDownTime()} | " +
            //            $"travel dist = {_middle.GetTravelDistX():F2}, {_middle.GetTravelDistY():F2}");
            //        // If the middle was down and hasn't moved outside of Tap limit => Tap!
            //        if (_middle.GetDownTime() < Config.TAP_TIME_MS
            //            && _middle.GetTravelDistX() < Config.TAP_X_MOVE_LIMIT
            //            && _middle.GetTravelDistY() < Config.TAP_Y_MOVE_LIMIT)
            //        {
            //            FILOG.Debug("Middle tapped!");
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
            //        FILOG.Debug($"{ShowPointers(lastFrame),-18} Middle down");
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
        private void TapTrackRing()
        {

            // Get the last frame
            TouchFrame currentFrame = _frames.Last;
            //TouchPoint ringTouchPoint = lastFrame.GetPointer(Finger.Ring);

            Finger finger = Finger.Ring;

            if (currentFrame.HasTouchPoint(finger)) // Finger present
            {
                Point center = currentFrame.GetPointer(finger).GetCenter();
                if (_touchTimers[finger].IsRunning) // Already active => update position (move)
                {
                    _lastPositions[finger] = center;
                }
                else // First touch
                {
                    FILOG.Debug($"{finger.ToString()} Down.");
                    _downPositions[finger] = center;
                    _lastPositions[finger] = center;
                    _touchTimers[finger].Restart(); // Start the timer
                }
            }
            else // Finger NOT present in the current frame
            {
                if (_touchTimers[finger].IsRunning) // Was active => Lifted up
                {
                    Point downPosition = _downPositions[finger];
                    Point lastPosition = _lastPositions[finger];

                    GestInfo<TouchSurface>($"{finger.ToString()} Up: {_touchTimers[finger].ElapsedMilliseconds}" +
                        $" | dX = {Abs(lastPosition.X - downPosition.X):F3}" +
                        $" | dY = {Abs(lastPosition.Y - downPosition.Y):F3}");
                    if (_touchTimers[finger].ElapsedMilliseconds < Config.TAP_TIME_MS
                        && Abs(lastPosition.X - downPosition.X) < Config.TAP_X_MOVE_LIMIT
                        && Abs(lastPosition.Y - downPosition.Y) < Config.TAP_Y_MOVE_LIMIT)
                    {
                        GestInfo<TouchSurface>($"{finger} Tapped!");
                        _gestureReceiver.RingTap();
                        
                    }

                    _touchTimers[finger].Stop();
                }
                
            }

            //if (ringTouchPoint == null) // No ring finger present
            //{
            //    if (_ring.IsDown)
            //    {
            //        FILOG.Debug($"{ShowPointers(lastFrame),-18} Ring up: down time = {_ring.GetDownTime()} | " +
            //            $"travel dist = {_ring.GetTravelDistX():F2}, {_ring.GetTravelDistY():F2}");
            //        // If the finger was down and hasn't moved outside of Tap limit => Tap!
            //        if (_ring.GetDownTime() < Config.TAP_TIME_MS
            //            && _ring.GetTravelDistX() < Config.TAP_X_MOVE_LIMIT
            //            && _ring.GetTravelDistY() < Config.TAP_Y_MOVE_LIMIT)
            //        {
            //            FILOG.Debug("Ring tapped!");
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
            //        FILOG.Debug($"{ShowPointers(lastFrame),-18} Ring down");
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
        private void TapTrackLittle()
        {

            // Get the last frame
            TouchFrame currentFrame = _frames.Last;
            //TouchFrame beforeLastFrame = _frames.BeforeLast;
            //TouchPoint littleTouchPoint = lastFrame.GetPointer(Finger.Little);

            Finger finger = Finger.Little;

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
                    FILOG.Debug($"{finger.ToString()} Down.");
                    _downPositions[finger] = center;
                    _lastPositions[finger] = center;
                    _touchTimers[finger].Restart(); // Start the timer
                }
            }
            else // Finger NOT present in the current frame
            {
                if (_touchTimers[finger].IsRunning) // Was active => Lifted up
                {
                    Point downPosition = _downPositions[finger];
                    Point lastPosition = _lastPositions[finger];

                    // Check Tap time and movement conditions
                    GestInfo<TouchSurface>($"{finger.ToString()} Up: {_touchTimers[finger].ElapsedMilliseconds}" +
                        $" | dX = {Abs(lastPosition.X - downPosition.X):F2}" +
                        $" | dY = {Abs(lastPosition.Y - downPosition.Y):F2}");
                    if (_touchTimers[finger].ElapsedMilliseconds < Config.TAP_TIME_MS
                        && Abs(lastPosition.X - downPosition.X) < Config.TAP_X_MOVE_LIMIT
                        && Abs(lastPosition.Y - downPosition.Y) < Config.TAP_Y_MOVE_LIMIT)
                    {
                        // Find the Tap position (Top or Bottom)
                        if (lastPosition.Y < LITTLE_TOP_LOWEST_ROW) // Top
                        {
                            GestInfo<TouchSurface>($"{finger.ToString()} Tapped! Top.");
                            _gestureReceiver.LittleTap(Location.Top);
                        }
                        else // Bottom
                        {
                            GestInfo<TouchSurface>($"{finger.ToString()} Tapped! Bottom.");
                            _gestureReceiver.LittleTap(Location.Bottom);
                        }
                    }

                    _touchTimers[finger].Stop();
                }

            }

        }

        //========= Swipe tech tracking ==========================

        /// <summary>
        /// Tracking of the thumb finger
        /// </summary>
        private void SwipeTechTrackThumb()
        {
            TouchFrame currentFrame = _frames.Last; // Get the current frame
            Finger finger = Finger.Thumb;

            if (currentFrame.HasTouchPoint(finger)) // Finger present
            {
                Point center = currentFrame.GetPointer(finger).GetCenter();
                if (_touchTimers[finger].IsRunning) // Already active => update position (move)
                {
                    _lastPositions[finger] = center;

                    // Check if Swipe happened
                    float gestureDT = (currentFrame.Timestamp - _thumbGestureStart.Timestamp)
                        / (float)Stopwatch.Frequency;

                    // Duration is good => Check movement
                    if (gestureDT < Config.SWIPE_TIME_MAX)
                    {
                        TouchPoint firstTouchPoint = _thumbGestureStart.GetPointer(finger);
                        double dX = center.X - firstTouchPoint.GetX();
                        double dY = center.Y - firstTouchPoint.GetY();
                        FILOG.Debug($"dT = {gestureDT:F2} | dX = {dX:F2}, dY = {dY:F2}");
                        if (Abs(dX) > Config.SWIPE_MOVE_THRESHOLD) // Good amount of movement along X
                        {
                            if (Abs(dY) < Config.SWIPE_MOVE_THRESHOLD) // Swipe should be only long one direction
                            {
                                // Swipe along X
                                _gestureReceiver.ThumbSwipe(dX > 0 ? Direction.Right : Direction.Left);
                                //_thumbGestureFrames.Clear(); // Reset after gesture is dected
                                _thumbGestureStart = currentFrame; // Reset after gesture is detected
                                FILOG.Debug($"{finger.ToString()} Swiped!");
                            }
                        }
                        else if (Abs(dY) > Config.SWIPE_MOVE_THRESHOLD) // Good amount of movement along Y
                        {
                            if (Abs(dX) < Config.SWIPE_MOVE_THRESHOLD) // Swipe should be only long one direction
                            {
                                // Swipe along Y
                                _gestureReceiver.ThumbSwipe(dY > 0 ? Direction.Down : Direction.Up);
                                //_thumbGestureFrames.Clear(); // Reset after gesture is dected
                                _thumbGestureStart = currentFrame; // Reset after gesture is detected
                                FILOG.Debug($"{finger.ToString()} Swiped!");
                            }
                        }

                    }
                    else // (Probably) gesture time expired => start over
                    {
                        _thumbGestureStart = currentFrame;
                    }

                }
                else // First touch
                {
                    _downPositions[finger] = center;
                    _lastPositions[finger] = center;
                    _touchTimers[finger].Restart(); // Start the timer
                    _thumbGestureStart = currentFrame;
                }
            }
            else // Finger NOT present in the current frame
            {
                if (_touchTimers[finger].IsRunning) // Was active => Lifted up
                {
                    _touchTimers[finger].Stop();
                }
            }

        }

        /// <summary>
        /// Tracking the index finger
        /// </summary>
        private void SwipeTechTrackIndex()
        {
            TouchFrame currentFrame = _frames.Last; // Get the current frame

            Finger finger = Finger.Index;

            if (currentFrame.HasTouchPoint(finger)) // Finger present
            {
                TouchPoint touchPoint = currentFrame.GetPointer(finger);
                if (_touchTimers[finger].IsRunning) // Already active => update position (move)
                {
                    _lastPositions[finger] = touchPoint.GetCenter();

                    // Index moving
                    _gestureReceiver.IndexMove(currentFrame.GetPointer(finger));
                }
                else // First touch
                {
                    FILOG.Debug($"{finger.ToString()} Down.");
                    _downPositions[finger] = touchPoint.GetCenter();
                    _lastPositions[finger] = touchPoint.GetCenter();
                    _touchTimers[finger].Restart(); // Start the timer
                }
            }
            else // Finger not present in the current frame
            {
                if (_touchTimers[finger].IsRunning) // Was active => Lifted up
                {
                    _touchTimers[finger].Stop();
                }
            }
        }

        /// <summary>
        /// Tracking the middle finger
        /// </summary>
        private void SwipeTechTrackMiddle()
        {
            TouchFrame currentFrame = _frames.Last; // Get current frame
            Finger finger = Finger.Middle;

            if (currentFrame.HasTouchPoint(finger)) // Finger present
            {
                Point center = currentFrame.GetPointer(finger).GetCenter();
                if (_touchTimers[finger].IsRunning) // Already active => update position (move)
                {
                    _lastPositions[finger] = center;

                }
                else // First touch
                {
                    FILOG.Debug($"{finger.ToString()} Down.");
                    _downPositions[finger] = center;
                    _lastPositions[finger] = center;
                    _touchTimers[finger].Restart(); // Start the timer
                }
            }
            else // Finger NOT present in the current frame
            {
                if (_touchTimers[finger].IsRunning) // Was active => Lifted up
                {
                    _touchTimers[finger].Stop();
                }
            }

        }

        /// <summary>
        /// Tracking the ring finger
        /// </summary>
        private void SwipeTechTrackRing()
        {
            TouchFrame currentFrame = _frames.Last; // Get the current frame
            Finger finger = Finger.Ring;

            if (currentFrame.HasTouchPoint(finger)) // Finger present
            {
                Point center = currentFrame.GetPointer(finger).GetCenter();
                if (_touchTimers[finger].IsRunning) // Already active => update position (move)
                {
                    _lastPositions[finger] = center;
                }
                else // First touch
                {
                    FILOG.Debug($"{finger.ToString()} Down.");
                    _downPositions[finger] = center;
                    _lastPositions[finger] = center;
                    _touchTimers[finger].Restart(); // Start the timer
                }
            }
            else // Finger NOT present in the current frame
            {
                if (_touchTimers[finger].IsRunning) // Was active => Lifted up
                {
                    _touchTimers[finger].Stop();
                }
            }

        }

        /// <summary>
        /// Tracking the pinky finger
        /// </summary>
        private void SwipeTechTrackLittle()
        {
            TouchFrame currentFrame = _frames.Last; // Get the current frame
            Finger finger = Finger.Little;

            if (currentFrame.HasTouchPoint(finger)) // Finger present
            {
                Point center = currentFrame.GetPointer(finger).GetCenter();
                if (_touchTimers[finger].IsRunning) // Already active => update position (move)
                {
                    _lastPositions[finger] = center;
                }
                else // First touch
                {
                    FILOG.Debug($"{finger.ToString()} Down.");
                    _downPositions[finger] = center;
                    _lastPositions[finger] = center;
                    _touchTimers[finger].Restart(); // Start the timer
                }
            }
            else // Finger NOT present in the current frame
            {
                if (_touchTimers[finger].IsRunning) // Was active => Lifted up
                {
                    _touchTimers[finger].Stop();
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

        private string Dig2(double d)
        {
            return $"{d:F2}";
        }
    }
}
