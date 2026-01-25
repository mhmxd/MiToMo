using System;
using System.Drawing;

namespace SubTask.PanelNavigation
{
    internal class FullFinger
    {
        // FullFinger enum
        public enum FingerName
        {
            Thumb,
            Index,
            Middle,
            Ring,
            Pinky
        }

        // Basic properties
        public int Id { get; private set; }
        public Point Position { get; private set; }
        public Point? PreviousPosition { get; private set; }
        public double Pressure { get; private set; }
        public int Area { get; private set; }

        // Motion tracking
        public double VelocityX { get; private set; }
        public double VelocityY { get; private set; }
        public DateTime LastMoved { get; private set; }
        public DateTime LastUpdated { get; private set; }

        // Gesture state
        public bool IsResting => DateTime.Now.Subtract(LastMoved).TotalMilliseconds > 200 && IsActive;
        public bool IsActive { get; private set; } = true;
        public FingerState State { get; private set; } = FingerState.Unknown;

        // For gesture detection
        public int ConsecutiveTapCount { get; private set; } = 0;
        public DateTime LastTapTime { get; private set; }

        public FullFinger(int id, Point position, double pressure, int area)
        {
            Id = id;
            Position = position;
            Pressure = pressure;
            Area = area;
            LastUpdated = DateTime.Now;
            LastMoved = DateTime.Now;
        }

        public void Update(Point newPosition, double pressure, int area)
        {
            // Store previous position
            PreviousPosition = Position;

            // Update motion tracking
            double deltaX = newPosition.X - Position.X;
            double deltaY = newPosition.Y - Position.Y;
            double distanceMoved = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            // Update current position and properties
            Position = newPosition;
            Pressure = pressure;
            Area = area;
            LastUpdated = DateTime.Now;

            // Only update velocity and movement Time if it moved significantly
            if (distanceMoved > 0.5)
            {
                VelocityX = deltaX;
                VelocityY = deltaY;
                LastMoved = DateTime.Now;
            }

            IsActive = true;

            // Update state based on movement
            UpdateState(distanceMoved);
        }

        private void UpdateState(double distanceMoved)
        {
            // Simple state machine for finger states
            switch (State)
            {
                case FingerState.Unknown:
                    State = FingerState.Down;
                    break;

                case FingerState.Down:
                    if (IsResting)
                        State = FingerState.Resting;
                    else if (distanceMoved > 3)
                        State = FingerState.Moving;
                    break;

                case FingerState.Moving:
                    if (IsResting)
                        State = FingerState.Resting;
                    break;

                case FingerState.Resting:
                    if (distanceMoved > 3)
                        State = FingerState.Moving;
                    break;

                case FingerState.Up:
                    State = FingerState.Down;
                    break;
            }
        }

        public void MarkInactive()
        {
            IsActive = false;
            State = FingerState.Up;

            // Check for tap gesture
            double timeSinceLastMoved = DateTime.Now.Subtract(LastMoved).TotalMilliseconds;
            if (timeSinceLastMoved < 200)
            {
                double timeSinceLastTap = DateTime.Now.Subtract(LastTapTime).TotalMilliseconds;
                if (timeSinceLastTap < 300)
                    ConsecutiveTapCount++;
                else
                    ConsecutiveTapCount = 1;

                LastTapTime = DateTime.Now;
            }
        }

        public override string ToString()
        {
            return $"Finger #{Id}: {State} at ({Position.X}, {Position.Y}), Pressure: {Pressure:F1}, " +
                   $"Velocity: ({VelocityX:F1}, {VelocityY:F1}), Taps: {ConsecutiveTapCount}";
        }
    }

    public enum FingerState
    {
        Unknown,
        Down,
        Moving,
        Resting,
        Up
    }
}
