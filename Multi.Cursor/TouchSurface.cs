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
            new TouchFinger(11, 12), // Ring
            new TouchFinger(13, 14) // Little
        };

        private Dictionary<Finger, Stopwatch> _touchTimers = new Dictionary<Finger, Stopwatch>();
        private Dictionary<Finger, Point> _downPositions = new Dictionary<Finger, Point>();
        private Dictionary<Finger, Point> _lastPositions = new Dictionary<Finger, Point>();
        private FixedBuffer<TouchFrame> _thumbGestureFrames = new FixedBuffer<TouchFrame>(100);
        private TouchFrame _thumbGestureStart;

        private Stopwatch debugWatch = new Stopwatch();

        public enum Finger
        {
            Thumb = 1,
            Index = 2,
            Middle = 3,
            Ring = 4,
            Pinky = 5
        }

        FingerTracker tracker = new FingerTracker();

        private Technique _activeTechnique = Technique.TOMO_TAP; // Set in the constructor

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

            public void Clear()
            {
                Pointers.Clear();
            }
        }

        private FixedBuffer<TouchFrame> _frames = new FixedBuffer<TouchFrame>(50);

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

        private IGestureHandler _gestureReceiver;

        private long _lastThumbGestureTimestamp = 0;
        private TouchPoint _lastIndexTP = null;
        private TouchPoint _lastThumbTP = null;

        public TouchSurface(Technique activeTechnique)
        {
            //_gestureReceiver = gestureReceiver;

            _upPointerIds = new List<int>() { -1 };
            _downPointersIds = new List<int>() { -1 };

            _touchTimers.Add(Finger.Thumb, new Stopwatch());
            _touchTimers.Add(Finger.Index, new Stopwatch());
            _touchTimers.Add(Finger.Middle, new Stopwatch());
            _touchTimers.Add(Finger.Ring, new Stopwatch());
            _touchTimers.Add(Finger.Pinky, new Stopwatch());

            _downPositions.Add(Finger.Thumb, new Point(-1, -1));
            _downPositions.Add(Finger.Index, new Point(-1, -1));
            _downPositions.Add(Finger.Middle, new Point(-1, -1));
            _downPositions.Add(Finger.Ring, new Point(-1, -1));
            _downPositions.Add(Finger.Pinky, new Point(-1, -1));

            _activeTechnique = activeTechnique;
        }

        public void SetGestureReceiver(IGestureHandler gestureReceiver)
        {
            this.TrialInfo("GestureReceiver set!");
            _gestureReceiver = gestureReceiver;
        }

        private TouchFrame FillActiveBlobs(Span2D<Byte> shotSpan)
        {
            //GestInfo<TouchSurface>(shotSpan.ToStr());
            TouchFrame activeFrame = new TouchFrame();
            List<List<(int, int)>> blobs = new List<List<(int, int)>>();
            bool[,] visited = new bool[shotSpan.Height, shotSpan.Width];

            // 1. Identify Potential Touch Blobs
            for (int y = 0; y < shotSpan.Height; y++)
            {
                for (int x = 0; x < shotSpan.Width; x++)
                {
                    if (shotSpan[y, x] > Config.MIN_PRESSURE && !visited[y, x])
                    {
                        List<(int, int)> blob = new List<(int, int)>();
                        Queue<(int, int)> toVisit = new Queue<(int, int)>();
                        toVisit.Enqueue((x, y));
                        visited[y, x] = true;
                        blob.Add((x, y));

                        while (toVisit.Count > 0)
                        {
                            (int currentX, int currentY) = toVisit.Dequeue();
                            int[] dx = { 0, 0, 1, -1 };
                            int[] dy = { 1, -1, 0, 0 };
                            for (int i = 0; i < 4; i++)
                            {
                                int nx = currentX + dx[i];
                                int ny = currentY + dy[i];
                                if (nx >= 0 && nx < shotSpan.Width && ny >= 0 && ny < shotSpan.Height &&
                                    shotSpan[ny, nx] > Config.MIN_PRESSURE && !visited[ny, nx])
                                {
                                    visited[ny, nx] = true;
                                    toVisit.Enqueue((nx, ny));
                                    blob.Add((nx, ny));
                                }
                            }
                        }
                        blobs.Add(blob);
                    }
                }
            }

            Dictionary<int, TouchPoint> potentialFingers = new Dictionary<int, TouchPoint>();

            foreach (var blob in blobs)
            {
                int minBlobX = shotSpan.Width;
                int maxBlobX = -1;
                int minBlobY = shotSpan.Height;
                int maxBlobY = -1;
                foreach (var pixel in blob)
                {
                    minBlobX = Math.Min(minBlobX, pixel.Item1);
                    maxBlobX = Math.Max(maxBlobX, pixel.Item1);
                    minBlobY = Math.Min(minBlobY, pixel.Item2);
                    maxBlobY = Math.Max(maxBlobY, pixel.Item2);
                }

                Dictionary<int, List<(int, int, byte)>> fingerSegments = new Dictionary<int, List<(int, int, byte)>>();
                bool separated = false;

                // 2 & 3. Analyze for Local Minima
                for (int y = minBlobY; y <= maxBlobY; y++)
                {
                    for (int f = 0; f < _fingers.Length - 1; f++)
                    {
                        int boundary = _fingers[f].MaxCol;
                        if (boundary >= minBlobX && boundary < maxBlobX)
                        {
                            int minimaX = -1;
                            byte minPressure = 255; // Initialize with max possible pressure
                            bool foundMinima = false;

                            for (int x = Math.Max(minBlobX + 1, boundary); x <= Math.Min(maxBlobX - 1, boundary + 1); x++)
                            {
                                if (blob.Contains((x, y)))
                                {
                                    byte currentPressure = shotSpan[y, x];
                                    byte leftPressure = (blob.Contains((x - 1, y))) ? shotSpan[y, x - 1] : (byte)0;
                                    byte rightPressure = (blob.Contains((x + 1, y))) ? shotSpan[y, x + 1] : (byte)0;

                                    if (currentPressure < leftPressure - Config.LOCAL_MINIMA_DROP_THRESHOLD &&
                                        currentPressure < rightPressure - Config.LOCAL_MINIMA_DROP_THRESHOLD)
                                    {
                                        if (currentPressure < minPressure)
                                        {
                                            minPressure = currentPressure;
                                            minimaX = x;
                                            foundMinima = true;
                                        }
                                    }
                                }
                            }

                            if (foundMinima && minimaX > boundary && minimaX < _fingers[f + 1].MinCol)
                            {
                                separated = true;
                                foreach (var pixel in blob)
                                {
                                    int pixelFingerId = -1;
                                    if (pixel.Item1 >= _fingers[f].MinCol && pixel.Item1 <= boundary)
                                        pixelFingerId = f + 1;
                                    else if (pixel.Item1 >= _fingers[f + 1].MinCol && pixel.Item1 <= _fingers[f + 1].MaxCol)
                                        pixelFingerId = f + 2;

                                    if (pixelFingerId != -1)
                                    {
                                        if (!fingerSegments.ContainsKey(pixelFingerId))
                                            fingerSegments[pixelFingerId] = new List<(int, int, byte)>();
                                        fingerSegments[pixelFingerId].Add((pixel.Item1, pixel.Item2, shotSpan[pixel.Item2, pixel.Item1]));
                                    }
                                }
                                goto MinimaDetected; // Break out after segmenting
                            }
                        }
                    }
                }

            MinimaDetected:
                if (separated)
                {
                    // 6. Calculate Center of Mass and Total Pressure for Each Finger Segment
                    foreach (var kvp in fingerSegments)
                    {
                        int fingerId = kvp.Key;
                        List<(int, int, byte)> segmentPixels = kvp.Value;
                        if (!potentialFingers.ContainsKey(fingerId))
                            potentialFingers[fingerId] = new TouchPoint { Id = fingerId };

                        double weightedSumX = 0;
                        double weightedSumY = 0;
                        int totalPressure = 0;

                        foreach (var pixelData in segmentPixels)
                        {
                            weightedSumX += pixelData.Item1 * pixelData.Item3;
                            weightedSumY += pixelData.Item2 * pixelData.Item3;
                            totalPressure += pixelData.Item3;
                            potentialFingers[fingerId].AddTouchData(pixelData.Item1, pixelData.Item2, pixelData.Item3);
                        }
                    }
                }
                else
                {
                    // 7. Fallback to Column Thresholds (process the entire blob)
                    foreach (var finger in _fingers)
                    {
                        int fingerId = Array.IndexOf(_fingers, finger) + 1;
                        if (!potentialFingers.ContainsKey(fingerId))
                            potentialFingers[fingerId] = new TouchPoint { Id = fingerId };

                        foreach (var pixel in blob)
                        {
                            if (pixel.Item1 >= finger.MinCol && pixel.Item1 <= finger.MaxCol)
                            {
                                potentialFingers[fingerId].AddTouchData(pixel.Item1, pixel.Item2, shotSpan[pixel.Item2, pixel.Item1]);
                            }
                        }
                    }
                }
            }

            // 8. Final Activation Check
            foreach (var kvp in potentialFingers)
            {
                if (kvp.Value.GetTotalPressure() > Config.MIN_TOTAL_PRESSURE) // Your min pressure
                {
                    activeFrame.AddPointer(kvp.Key, kvp.Value);
                }
            }

            return activeFrame;
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
            //FILOG.Debug(Output.ToStr(shotSpan));

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

                // If total pressure was above 2 X MIN_PRESSURE => finger active
                GestInfo<TouchSurface>($"Finger {f + 1} Pressure = {touchPoint.GetTotalPressure()}");
                if (touchPoint.GetTotalPressure() > Config.MIN_TOTAL_PRESSURE)
                {
                    //FILOG.Debug($"Pressure #{f + 1} = {touchPoint.GetTotalPressure()}");
                    touchPoint.Id = f + 1;
                    activeFrame.AddPointer(touchPoint.Id, touchPoint);
                }
            }

            //FILOG.Debug(Output.GetKeys(activeFrame.Pointers));
            //GestInfo<TouchSurface>(Output.GetKeys(activeFrame.Pointers));

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
                //GestInfo<TouchSurface>($"Track time = {debugWatch.ElapsedMilliseconds} ms");
            }
            else
            {
                debugWatch.Restart();
            }
            //TrackFingers(shotSpan);
            // First, get the current frame
            //TouchFrame activeFrame = FillActiveTouches(shotSpan);
            TouchFrame activeFrame = FillActiveBlobs(shotSpan);

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

                if (_activeTechnique == Technique.TOMO_TAP)
                {
                    TapTrackThumb();
                    TapTrackIndex();
                    TapTrackMiddle();
                    TapTrackRing();
                    TapTrackPinky();
                }

                if (_activeTechnique == Technique.TOMO_SWIPE)
                {
                    SwipeTechTrackThumb();
                    SwipeTechTrackIndex();
                    //SwipeTechTrackMiddle();
                    //SwipeTechTrackRing();
                    //SwipeTechTrackLittle();
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
                Point tpCenter = currentFrame.GetPointer(finger).GetCenter();
                //GestInfo<TouchSurface>($"Thumb Pos: {tpCenter.ToStr()}");
                if (_touchTimers[finger].IsRunning) // Already active => update position
                {
                    _lastPositions[finger] = tpCenter; // Update position
                }
                else // First touch
                {
                    //GestInfo<TouchSurface>($"{finger.ToString()} Down: {currentFrame.Timestamp}");
                    LogDown(finger.ToString(), currentFrame.Timestamp); // LOG
                    _downPositions[finger] = tpCenter;
                    _lastPositions[finger] = tpCenter;
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
                    //GestInfo<TouchSurface>($"{finger.ToString()} Up: {_touchTimers[finger].ElapsedMilliseconds}" +
                    //    $" | dX = {Abs(lastPosition.X - downPosition.X):F2}" +
                    //    $" | dY = {Abs(lastPosition.Y - downPosition.Y):F2}");
                    LogUp(finger.ToString(), _touchTimers[finger].ElapsedMilliseconds,
                        Abs(lastPosition.X - downPosition.X), Abs(lastPosition.Y - downPosition.Y)); // LOG

                    if (PassTapConditions(_touchTimers[finger].ElapsedMilliseconds,
                        Abs(lastPosition.X - downPosition.X),
                        Abs(lastPosition.Y - downPosition.Y)))
                    {
                        // Find the Tap position (Top or Down)
                        if (lastPosition.Y < THUMB_TOP_LOWEST_ROW) // Top
                        {
                            //GestInfo<TouchSurface>($"{finger.ToString()} Tapped! Top.");
                            LogTap(finger.ToString(), Side.Top, currentFrame.Timestamp); // LOG
                            _gestureReceiver?.ThumbTap(Side.Top);
                            
                        }
                        else // Down
                        {
                            //GestInfo<TouchSurface>($"{finger.ToString()} Tapped! Down.");
                            LogTap(finger.ToString(), Side.Down, currentFrame.Timestamp); // LOG
                            _gestureReceiver?.ThumbTap(Side.Down);
                            
                        }
                    }

                    _gestureReceiver?.ThumbUp();
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
                //GestInfo<TouchSurface>($"Index Pos: {tpCenter.ToStr()}");
                if (_touchTimers[finger].IsRunning) // Already active => update position (move)
                {
                    _lastPositions[finger] = tpCenter;

                    // Index moving
                    _gestureReceiver?.IndexMove(touchPoint);
                }
                else // First touch
                {
                    //GestInfo<TouchSurface>($"{finger.ToString()} Down: {currentFrame.Timestamp}");
                    LogDown(finger.ToString(), currentFrame.Timestamp); // LOG
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
                    //GestInfo<TouchSurface>($"{finger.ToString()} Up: {_touchTimers[finger].ElapsedMilliseconds}" +
                    //    $" | dX = {Abs(lastPosition.X - downPosition.X):F3}" +
                    //    $" | dY = {Abs(lastPosition.Y - downPosition.Y):F3}");
                    LogUp(finger.ToString(), _touchTimers[finger].ElapsedMilliseconds,
                        Abs(lastPosition.X - downPosition.X), Abs(lastPosition.Y - downPosition.Y)); // LOG
                    if (PassTapConditions(_touchTimers[finger].ElapsedMilliseconds,
                        Abs(lastPosition.X - downPosition.X),
                        Abs(lastPosition.Y - downPosition.Y)))
                    {
                        //GestInfo<TouchSurface>($"{finger} Tapped!");
                        LogTap(finger.ToString(), Side.Left, currentFrame.Timestamp); // LOG
                        _gestureReceiver?.IndexTap();
                    }

                    _gestureReceiver?.IndexUp();
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
                Point tpCenter = currentFrame.GetPointer(finger).GetCenter();
                //GestInfo<TouchSurface>($"Middle Pos: {tpCenter.ToStr()}");
                if (_touchTimers[finger].IsRunning) // Already active => update position (move)
                {
                    _lastPositions[finger] = tpCenter;

                }
                else // First touch
                {
                    //GestInfo<TouchSurface>($"{finger.ToString()} Down.");
                    LogDown(finger.ToString(), currentFrame.Timestamp); // LOG
                    _downPositions[finger] = tpCenter;
                    _lastPositions[finger] = tpCenter;
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
                        //GestInfo<TouchSurface>($"{finger.ToString()} Up: {_touchTimers[finger].ElapsedMilliseconds}" +
                        //    $" | dX = {Abs(lastPosition.X - downPosition.X):F3}" +
                        //    $" | dY = {Abs(lastPosition.Y - downPosition.Y):F3}");
                        LogUp(finger.ToString(), _touchTimers[finger].ElapsedMilliseconds,
                            Abs(lastPosition.X - downPosition.X), Abs(lastPosition.Y - downPosition.Y)); // LOG
                        if (PassTapConditions(_touchTimers[finger].ElapsedMilliseconds,
                            Abs(lastPosition.X - downPosition.X),
                            Abs(lastPosition.Y - downPosition.Y)))
                        {
                            LogTap(finger.ToString(), Side.Middle, currentFrame.Timestamp); // LOG
                            _gestureReceiver?.MiddleTap();
                        }

                        _touchTimers[finger].Stop();
                    }
                }
                
            }

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
                Point tpCenter = currentFrame.GetPointer(finger).GetCenter();
                //GestInfo<TouchSurface>($"Ring Pos: {tpCenter.ToStr()}");
                if (_touchTimers[finger].IsRunning) // Already active => update position (move)
                {
                    _lastPositions[finger] = tpCenter;
                }
                else // First touch
                {
                    //GestInfo<TouchSurface>($"{finger.ToString()} Down.");
                    LogDown(finger.ToString(), currentFrame.Timestamp); // LOG
                    _downPositions[finger] = tpCenter;
                    _lastPositions[finger] = tpCenter;
                    _touchTimers[finger].Restart(); // Start the timer
                }
            }
            else // Finger NOT present in the current frame
            {
                if (_touchTimers[finger].IsRunning) // Was active => Lifted up
                {
                    Point downPosition = _downPositions[finger];
                    Point lastPosition = _lastPositions[finger];

                    //GestInfo<TouchSurface>($"{finger.ToString()} Up: {_touchTimers[finger].ElapsedMilliseconds}" +
                    //    $" | dX = {Abs(lastPosition.X - downPosition.X):F3}" +
                    //    $" | dY = {Abs(lastPosition.Y - downPosition.Y):F3}");
                    LogUp(finger.ToString(), _touchTimers[finger].ElapsedMilliseconds,
                        Abs(lastPosition.X - downPosition.X), Abs(lastPosition.Y - downPosition.Y)); // LOG
                    if (PassTapConditions(_touchTimers[finger].ElapsedMilliseconds,
                        Abs(lastPosition.X - downPosition.X),
                        Abs(lastPosition.Y - downPosition.Y)))
                    {
                        //GestInfo<TouchSurface>($"{finger} Tapped!");
                        LogTap(finger.ToString(), Side.Right, currentFrame.Timestamp); // LOG
                        _gestureReceiver?.RingTap();
                        
                    }

                    _touchTimers[finger].Stop();
                }
                
            }

        }

        /// <summary>
        /// Tracking the pinky finger
        /// </summary>
        private void TapTrackPinky()
        {

            // Get the last frame
            TouchFrame currentFrame = _frames.Last;
            //TouchFrame beforeLastFrame = _frames.BeforeLast;
            //TouchPoint littleTouchPoint = lastFrame.GetPointer(Finger.Little);

            Finger finger = Finger.Pinky;

            if (currentFrame.HasTouchPoint(finger)) // Finger present
            {
                Point tpCenter = currentFrame.GetPointer(finger).GetCenter();
                //GestInfo<TouchSurface>($"Pinky Pos: {tpCenter.ToStr()}");
                if (_touchTimers[finger].IsRunning) // Already active => update position (move)
                {
                    _lastPositions[finger] = tpCenter;
                    // Mange the Swipe (later)
                }
                else // First touch
                {
                    //GestInfo<TouchSurface>($"{finger.ToString()} Down.");
                    LogDown(finger.ToString(), currentFrame.Timestamp); // LOG
                    _downPositions[finger] = tpCenter;
                    _lastPositions[finger] = tpCenter;
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
                    //GestInfo<TouchSurface>($"{finger.ToString()} Up: {_touchTimers[finger].ElapsedMilliseconds}" +
                    //    $" | dX = {Abs(lastPosition.X - downPosition.X):F2}" +
                    //    $" | dY = {Abs(lastPosition.Y - downPosition.Y):F2}");
                    LogUp(finger.ToString(), _touchTimers[finger].ElapsedMilliseconds,
                        Abs(lastPosition.X - downPosition.X), Abs(lastPosition.Y - downPosition.Y)); // LOG
                    if (PassTapConditions(_touchTimers[finger].ElapsedMilliseconds, 
                        Abs(lastPosition.X - downPosition.X), 
                        Abs(lastPosition.Y - downPosition.Y)))
                    {
                        // Find the Tap position (Top or Down)
                        if (lastPosition.Y < LITTLE_TOP_LOWEST_ROW) // Top
                        {
                            //GestInfo<TouchSurface>($"{finger.ToString()} Tapped! Top.");
                            LogTap(finger.ToString(), Side.Top, currentFrame.Timestamp); // LOG
                            _gestureReceiver?.PinkyTap(Side.Top);
                        }
                        else // Down
                        {
                            //GestInfo<TouchSurface>($"{finger.ToString()} Tapped! Down.");
                            LogTap(finger.ToString(), Side.Down, currentFrame.Timestamp); // LOG
                            _gestureReceiver?.PinkyTap(Side.Down);
                        }
                    }

                    _touchTimers[finger].Stop();
                }

            }

        }

        private bool PassTapConditions(long dT, double dX, double dY)
        {
            return Utils.In(dT, Config.TAP_TIME_MIN, Config.TAP_TIME_MAX)
                        && dX < Config.TAP_GENERAL_THRESHOLD.DX
                        && dY < Config.TAP_GENERAL_THRESHOLD.DY;
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
                    long deltaTicks = currentFrame.Timestamp - _thumbGestureStart.Timestamp;
                    int gestureDT = (int)((deltaTicks / (float)Stopwatch.Frequency) * 1000); //in ms

                    // Duration is good => Check movement
                    if (gestureDT < Config.SWIPE_TIME_MAX)
                    {
                        TouchPoint firstTouchPoint = _thumbGestureStart.GetPointer(finger);
                        double dX = center.X - firstTouchPoint.GetX();
                        double dY = center.Y - firstTouchPoint.GetY();
                        //GestInfo<TouchSurface>($"dT = {gestureDT:F2} | dX = {dX:F2}, dY = {dY:F2}");
                        //LogMove(finger.ToString(), gestureDT, dX, dY); // LOG
                        //-- Check for swipe left-right
                        if (Abs(dX) > Config.SWIPE_MOVE_THRESHOLD) // Good amount of movement along X
                        {
                            if (Abs(dY) < Config.SWIPE_MOVE_THRESHOLD) // Swipe should be only long one direction
                            {
                                // Swipe along X
                                _gestureReceiver?.ThumbSwipe(dX > 0 ? Direction.Right : Direction.Left);
                                //_thumbGestureFrames.Clear(); // Reset after gesture is dected
                                _thumbGestureStart = currentFrame; // Reset after gesture is detected
                                //GestInfo<TouchSurface>($"{finger.ToString()} Swiped!");
                                LogSwipe(finger.ToString(), gestureDT, dX, dY); // LOG
                            }
                        }
                        // -- Check for swipe up-down (either this or left-right)
                        else if (Abs(dY) > Config.SWIPE_MOVE_THRESHOLD) // Good amount of movement along Y
                        {
                            if (Abs(dX) < Config.SWIPE_MOVE_THRESHOLD) // Swipe should be only long one direction
                            {
                                // Swipe along Y
                                _gestureReceiver?.ThumbSwipe(dY > 0 ? Direction.Down : Direction.Up);
                                //_thumbGestureFrames.Clear(); // Reset after gesture is dected
                                _thumbGestureStart = currentFrame; // Reset after gesture is detected
                                //GestInfo<TouchSurface>($"{finger.ToString()} Swiped!");
                                LogSwipe(finger.ToString(), gestureDT, dX, dY); // LOG
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
                Point tpCenter = touchPoint.GetCenter();
                if (_touchTimers[finger].IsRunning) // Already active => update position (move)
                {
                    _lastPositions[finger] = tpCenter;
                    LogMove(finger.ToString(), _touchTimers[finger].ElapsedMilliseconds, tpCenter.X, tpCenter.Y); // LOG

                    _gestureReceiver?.IndexMove(touchPoint); // Index moving
                }
                else // First touch
                {
                    //GestInfo<TouchSurface>($"{finger.ToString()} Down: {currentFrame.Timestamp}");
                    LogDown(finger.ToString(), currentFrame.Timestamp); // LOG
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
                    //GestInfo<TouchSurface>($"{finger.ToString()} Up: {_touchTimers[finger].ElapsedMilliseconds}" +
                    //    $" | dX = {Abs(lastPosition.X - downPosition.X):F3}" +
                    //    $" | dY = {Abs(lastPosition.Y - downPosition.Y):F3}");
                    LogUp(finger.ToString(), _touchTimers[finger].ElapsedMilliseconds,
                        Abs(lastPosition.X - downPosition.X), Abs(lastPosition.Y - downPosition.Y)); // LOG

                    _gestureReceiver?.IndexUp();
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
                    GestInfo<TouchSurface>($"{finger.ToString()} Down.");
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
                    GestInfo<TouchSurface>($"{finger.ToString()} Down.");
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
            Finger finger = Finger.Pinky;

            if (currentFrame.HasTouchPoint(finger)) // Finger present
            {
                Point center = currentFrame.GetPointer(finger).GetCenter();
                if (_touchTimers[finger].IsRunning) // Already active => update position (move)
                {
                    _lastPositions[finger] = center;
                }
                else // First touch
                {
                    GestInfo<TouchSurface>($"{finger.ToString()} Down.");
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

        public bool IsFingerInactive(Finger finger)
        {
            return !_touchTimers[finger].IsRunning;
        }

        public bool IsFingerActive(Finger finger)
        {
            return _touchTimers[finger].IsRunning;
        }

        public bool AreAllFingersInactiveExcept(Finger finger)
        {
            foreach (Finger f in Enum.GetValues(typeof(Finger)))
            {
                if (f != finger && _touchTimers[f].IsRunning)
                {
                    return false;
                }
            }

            return true;

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


        //=========================== Logging =========================
        private void LogDown(string fingerName, long timestamp)
        {
            ExperiLogger.LogGestureEvent($"{fingerName} Down!");
        }

        private void LogUp(string fingerName, long duration, double dX, double dY)
        {
            ExperiLogger.LogGestureEvent($"{fingerName} Up! Duration: {duration} | dX: {dX:F3}, dY: {dY:F3}");
        }

        private void LogTap(string fingerName, Side loc, long timestamp)
        {
            ExperiLogger.LogGestureEvent($"{fingerName} Tapped! Location: {loc}");
        }

        private void LogMove(string fingerName, long duration, double dX, double dY)
        {
            ExperiLogger.LogGestureEvent($"{fingerName} Moved! Duration: {duration} | dX: {dX:F3}, dY: {dY:F3}");
        }

        private void LogSwipe(string fingerName, int duration, double dX, double dY)
        {
            ExperiLogger.LogGestureEvent($"{fingerName} Swiped! Duration: {duration} | dX: {dX:F3}, dY: {dY:F3}");
        }

    }
}
