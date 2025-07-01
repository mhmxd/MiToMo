using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using static Multi.Cursor.Output;

namespace Multi.Cursor
{
    public abstract class AuxWindow : Window
    {
        // Class to store all the info regarding each button (positions, etc.)
        protected class ButtonInfo
        {
            public ButtonInfo(SButton button) { 
                Button = button;
                Position = new Point(0, 0);
                Rect = new Rect();
                DistToStart = new Range(0, 0);
            }
            public SButton Button {  get; set; }
            public Point Position { get; set; }
            public Rect Rect { get; set; }
            public Range DistToStart { get; set; }
        }

        protected List<Grid> _gridColumns = new List<Grid>(); // List of grid columns
        protected Dictionary<int, List<SButton>> _widthButtons = new Dictionary<int, List<SButton>>(); // Dictionary to hold buttons by their width multiples
        //protected Dictionary<int, SButton> _allButtons = new Dictionary<int, SButton>(); // List of all buttons in the grid (id as key)
        //protected Dictionary<int, Point> _buttonPositions = new Dictionary<int, Point>(); // Dictionary to hold button positions by their ID (rel. to window)
        //protected Dictionary<int, Rect> _buttonRects = new Dictionary<int, Rect>(); // Dictionary to hold button rectangles by their ID
        protected Dictionary<int, ButtonInfo> _buttonInfos = new Dictionary<int, ButtonInfo>();
        protected SButton _targetButton; // Currently selected button (if any)

        // Boundary of the grid (encompassing all buttons)
        protected double _gridMinX = double.MaxValue;
        protected double _gridMinY = double.MaxValue;
        protected double _gridMaxX = double.MinValue;
        protected double _gridMaxY = double.MinValue;

        protected GridNavigator _gridNavigator = new GridNavigator(Config.FRAME_DUR_MS / 1000.0);
        protected int _lastHighlightedButtonId = -1; // ID of the currently highlighted button
        protected Point _topLeftButtonPosition = new Point(10000, 10000); // Initialize to a large value to find the top-left button

        protected Rect _startConstraintsRectAbsolute = new Rect();

        public abstract void GenerateGrid(Rect startConstraintsRectAbsolute, params Func<Grid>[] columnCreators);

        protected int FindMiddleButtonId()
        {
            // Calculate the center of the overall button grid
            double gridCenterX = (_gridMinX + _gridMaxX) / 2;
            double gridCenterY = (_gridMinY + _gridMaxY) / 2;
            Point gridCenterPoint = new Point(gridCenterX, gridCenterY);
            this.TrialInfo($"Central Point: {gridCenterPoint}");
            // Distance to the center point
            double centerDistance = double.MaxValue;
            int closestButtonId = -1;

            foreach (int buttonId in _buttonInfos.Keys)
            {
                Rect buttonRect = _buttonInfos[buttonId].Rect;

                // Check which button contains the grid center point
                if (buttonRect.Contains(gridCenterPoint))
                {
                    // If we find a button that contains the center point, return its ID
                    this.TrialInfo($"Middle button found at ID#{buttonId} with position {gridCenterPoint}");
                    return buttonId;
                }
                else // if button doesn't containt the center point, calculate the distance
                {
                    double dist = Utils.Dist(gridCenterPoint, new Point(buttonRect.X + buttonRect.Width / 2, buttonRect.Y + buttonRect.Height / 2));

                    if (dist < centerDistance)
                    {
                        centerDistance = dist;
                        closestButtonId = buttonId; // Update the last highlighted button to the closest one
                    }
                }
            }

            return closestButtonId;

        }

        public int SelectRandButtonByConstraints(int widthMult, int dist)
        {
            //this.TrialInfo($"Selecting button by multiple: {widthMult}");
            //this.TrialInfo($"All buttons: ");
            //foreach (int bid in _allButtons.Keys)
            //{
            //    this.TrialInfo($"Button#{bid} -> {_allButtons[bid].Id}");
            //}

            //this.TrialInfo($"Available buttons:");
            //foreach (int wm in _widthButtons.Keys)
            //{
            //    string ids = string.Join(", ", _widthButtons[wm].Select(b => b.Id.ToString()));
            //    this.TrialInfo($"WM {wm} -> {ids}");
            //}

            if (_widthButtons[widthMult].Count > 0)
            {

                // Find the buttons with dist laying inside their dist to start range
                List<int> possibleButtons = new List<int>();
                foreach (int buttonId in _buttonInfos.Keys)
                {
                    if (_buttonInfos[buttonId].DistToStart.ContainsExc(dist))
                    {
                        possibleButtons.Add(buttonId);
                    }
                }

                // If we have options, return a random from them
                if (possibleButtons.Count > 0)
                {
                    return possibleButtons.GetRandomElement();
                }

                
                //if (button != null)
                //{
                //    //this.TrialInfo($"Selected button id: {button.Id}");
                //    return button.Id;
                //}
                //else
                //{
                //    this.TrialInfo($"No buttons found for width multiple {widthMult}.");
                //    return -1; // Return an invalid point if no buttons are found
                //}
            } else
            {
                this.TrialInfo($"No buttons found for width multiple {widthMult}.");
                return -1; // Return an invalid point if no buttons are found
            }

            this.TrialInfo($"No buttons found for width multiple {widthMult}.");
            return -1; // Return an invalid point if no buttons are found

        }

        public virtual void FillGridButton(int buttonId, Brush color)
        {
            // Find the button with the specified ID
            if (_buttonInfos.ContainsKey(buttonId))
            {
                _buttonInfos[buttonId].Button.Background = color; // Change the background color of the button
                _buttonInfos[buttonId].Button.DisableBackgroundHover = true; // Disable hover fill for this button
                //this.TrialInfo($"Button with ID {buttonId} filled with color {color}.");
            }
            else
            {
                this.TrialInfo($"Button with ID {buttonId} not found.");
            }
        }

        public virtual void SetGridButtonHandlers(
            int buttonId,
            MouseButtonEventHandler targetMouseDownHandler, MouseButtonEventHandler targetMouseUpHandler)
        {
            // Find the button with the specified ID
            if (_buttonInfos.ContainsKey(buttonId))
            {
                _buttonInfos[buttonId].Button.AddHandler(UIElement.MouseDownEvent, targetMouseDownHandler, handledEventsToo: true);
                _buttonInfos[buttonId].Button.AddHandler(UIElement.MouseUpEvent, targetMouseUpHandler, handledEventsToo: true);

                //this.TrialInfo($"Button with ID {buttonId} added handlers.");
            }
            else
            {
                this.TrialInfo($"Button with ID {buttonId} not found.");
            }
        }

        public virtual Point GetGridButtonCenter(int buttonId)
        {
            //this.TrialInfo($"Button positions: {_buttonPositions.Stringify<int, Point>()}");
            // Find the button with the specified ID
            if (_buttonInfos.ContainsKey(buttonId))
            {
                //this.TrialInfo($"Button#{buttonId} position in window: {position}");
                double buttonHalfWidth = _buttonInfos[buttonId].Button.ActualWidth / 2;
                double buttonHalfHeight = _buttonInfos[buttonId].Button.ActualHeight / 2;
                return _buttonInfos[buttonId].Position.OffsetPosition(buttonHalfWidth, buttonHalfHeight);
            }
            else
            {
                this.TrialInfo($"Button with ID {buttonId} not found.");
                return new Point(0, 0); // Return an invalid point if no button is found
            }
        }

        public virtual void ResetGridSelection()
        {
            foreach (int buttonId in _buttonInfos.Keys)
            {
                _buttonInfos[buttonId].Button.Background = Config.BUTTON_DEFAULT_FILL_COLOR; // Reset the background color of all buttons
            }
        }

        //public virtual void MakeTargetAvailable()
        //{
        //    // Implemented in the derived classes
        //}

        //public virtual void MakeTargetUnavailable()
        //{
        //    // Implemented in the derived classes
        //}

        public void ActivateGridNavigator()
        {
            this.TrialInfo($"Last highlight = {_lastHighlightedButtonId}");
            ActivateGridNavigator(_lastHighlightedButtonId); // Activate the grid navigator with the last highlighted button ID
        }

        public void ActivateGridNavigator(int buttonId)
        {
            // Find the button with the specified ID
            if (_buttonInfos.ContainsKey(buttonId))
            {
                _gridNavigator.Activate(); // Activate the grid navigator
                HighlightButton(buttonId); // Highlight the button
            }
            else
            {
                this.TrialInfo($"Button with ID {buttonId} not found.");
            }
        }

        public void DeactivateGridNavigator()
        {
            _gridNavigator.Deactivate(); // Deactivate the grid navigator
            if (_lastHighlightedButtonId != -1 && _buttonInfos.ContainsKey(_lastHighlightedButtonId))
            {
                _buttonInfos[_lastHighlightedButtonId].Button.Background = Config.BUTTON_DEFAULT_FILL_COLOR;
                //button.BorderBrush = Config.BUTTON_DEFAULT_BORDER_COLOR; // Reset the border color of the last highlighted button
            }
        }

        public void StopGridNavigator()
        {
            _gridNavigator.Stop();
        }

        private void ResetHighlights()
        {
            // Reset the border color of all buttons
            foreach (int buttonId in _buttonInfos.Keys)
            {
                _buttonInfos[buttonId].Button.BorderBrush = Config.BUTTON_DEFAULT_BORDER_COLOR;
                if (_buttonInfos[buttonId].Button.Background != Config.TARGET_AVAILABLE_COLOR 
                    && _buttonInfos[buttonId].Button.Background != Config.TARGET_UNAVAILABLE_COLOR)
                {
                    _buttonInfos[buttonId].Button.Background = Config.BUTTON_DEFAULT_FILL_COLOR; // Reset the background color of all buttons
                }
            }
        }

        public void HighlightButton(int buttonId)
        {
            // Reset the border aof all buttons
            //foreach (var btn in _allButtons.Values)
            //{
            //    btn.BorderBrush = Config.BUTTON_DEFAULT_BORDER_COLOR; // Reset the border color of all buttons
            //}

            // Find the button with the specified ID
            if (_buttonInfos.ContainsKey(buttonId))
            {
                _buttonInfos[buttonId].Button.BorderBrush = Config.ELEMENT_HIGHLIGHT_COLOR; // Change the border color to highlight
                if (_buttonInfos[buttonId].Button.Background != Config.TARGET_AVAILABLE_COLOR && 
                    _buttonInfos[buttonId].Button.Background != Config.TARGET_UNAVAILABLE_COLOR)
                {
                    _buttonInfos[buttonId].Button.Background = Config.BUTTON_HOVER_FILL_COLOR;
                }
                
                _lastHighlightedButtonId = buttonId; // Store the ID of the highlighted button
            }
            else
            {
                this.TrialInfo($"Button with ID {buttonId} not found.");
            }
        }

        public void MoveGridNavigator(TouchPoint tp)
        {
            // Update the grid navigator with the current touch point
            var (dGridX, dGridY) = _gridNavigator.Update(tp);

            if ((dGridX == 0 && dGridY == 0) || _lastHighlightedButtonId == -1)
            {
                return; // No movement needed
            }

            SButton highlightedButton = _buttonInfos[_lastHighlightedButtonId].Button;

            // --- Process Horizontal Movement ---
            if (dGridX > 0) // Move Right
            {
                for (int i = 0; i < dGridX; i++)
                {
                    if (highlightedButton.RightId == -1) break; // Hit the edge
                    highlightedButton = _buttonInfos[highlightedButton.RightId].Button;
                }
            }
            else // Move Left
            {
                for (int i = 0; i < -dGridX; i++)
                {
                    if (highlightedButton.LeftId == -1) break; // Hit the edge
                    highlightedButton = _buttonInfos[highlightedButton.LeftId].Button;
                }
            }

            // --- Process Vertical Movement ---
            if (dGridY > 0) // Move Down
            {
                for (int i = 0; i < dGridY; i++)
                {
                    if (highlightedButton.BottomId == -1) break; // Hit the edge
                    highlightedButton = _buttonInfos[highlightedButton.BottomId].Button;
                }
            }
            else // Move Up
            {
                for (int i = 0; i < -dGridY; i++)
                {
                    if (highlightedButton.TopId == -1) break; // Hit the edge
                    highlightedButton = _buttonInfos[highlightedButton.TopId].Button;
                }
            }

            // If the target button has changed, update the focus
            if (highlightedButton.Id != _lastHighlightedButtonId)
            {
                _lastHighlightedButtonId = highlightedButton.Id; // Update the last highlighted button ID
                ResetHighlights(); // Reset highlights for all buttons
                HighlightButton(highlightedButton.Id); // Highlight the new button
            }
        }

        /// <summary>
        /// Finds the nearest neighboring SButton in a given direction.
        /// </summary>
        /// <param name="currentButton">The starting button.</param>
        /// <param name="direction">The direction to navigate (Up, Down, Left, Right).</param>
        /// <returns>The closest SButton in the specified direction, or null if none is found.</returns>
        public SButton GetNeighbor(SButton currentButton, Side direction)
        {
            // 1. Validate input and get the position of the current button.
            if (currentButton == null || !_buttonInfos.ContainsKey(currentButton.Id))
            {
                // Cannot navigate from a button that isn't registered.
                return null;
            }

            //if (currentButton == null || !_buttonPositions.TryGetValue(currentButton.Id, out Point currentPosition))
            //{
            //    // Cannot navigate from a button that isn't registered.
            //    return null;
            //}

            SButton bestCandidate = null;
            double minDistanceSquared = double.PositiveInfinity;

            // 2. Iterate through all other buttons to find the best candidate.
            foreach (int buttonId in _buttonInfos.Keys)
            {
                // Don't compare a button to itself.
                if (buttonId == currentButton.Id) continue;

                // 3. Check if the candidate is in the correct direction.
                // We use a simple heuristic: to be considered "to the right", a button's horizontal distance
                // should be greater than its vertical distance. This prevents jumping to a different row.
                double deltaX = _buttonInfos[buttonId].Position.X - _buttonInfos[currentButton.Id].Position.X;
                double deltaY = _buttonInfos[buttonId].Position.Y - _buttonInfos[currentButton.Id].Position.Y;

                bool isPotentialCandidate = false;
                switch (direction)
                {
                    case Side.Right:
                        // Must be to the right and more horizontal than vertical.
                        if (deltaX > 0 && Math.Abs(deltaX) > Math.Abs(deltaY)) isPotentialCandidate = true;
                        break;

                    case Side.Left:
                        // Must be to the left and more horizontal than vertical.
                        if (deltaX < 0 && Math.Abs(deltaX) > Math.Abs(deltaY)) isPotentialCandidate = true;
                        break;

                    case Side.Down:
                        // Must be below and more vertical than horizontal.
                        if (deltaY > 0 && Math.Abs(deltaY) > Math.Abs(deltaX)) isPotentialCandidate = true;
                        break;

                    case Side.Top:
                        // Must be above and more vertical than horizontal.
                        if (deltaY < 0 && Math.Abs(deltaY) > Math.Abs(deltaX)) isPotentialCandidate = true;
                        break;
                }

                if (isPotentialCandidate)
                {
                    // 4. If it's a valid candidate, check if it's the closest one yet.
                    // We use the square of the distance to avoid costly square root operations.
                    double distanceSquared = (_buttonInfos[buttonId].Position - _buttonInfos[currentButton.Id].Position).LengthSquared;

                    if (distanceSquared < minDistanceSquared)
                    {
                        minDistanceSquared = distanceSquared;
                        bestCandidate = _buttonInfos[buttonId].Button;
                    }
                }
            }

            //foreach (var candidateButton in _allButtons.Values)
            //{
            //    // Don't compare a button to itself.
            //    if (candidateButton.Id == currentButton.Id)
            //    {
            //        continue;
            //    }

            //    if (!_buttonPositions.TryGetValue(candidateButton.Id, out Point candidatePosition))
            //    {
            //        // Skip any candidate button that doesn't have a registered position.
            //        continue;
            //    }

            //    // 3. Check if the candidate is in the correct direction.
            //    // We use a simple heuristic: to be considered "to the right", a button's horizontal distance
            //    // should be greater than its vertical distance. This prevents jumping to a different row.
            //    double deltaX = candidatePosition.X - currentPosition.X;
            //    double deltaY = candidatePosition.Y - currentPosition.Y;

            //    bool isPotentialCandidate = false;
            //    switch (direction)
            //    {
            //        case Side.Right:
            //            // Must be to the right and more horizontal than vertical.
            //            if (deltaX > 0 && Math.Abs(deltaX) > Math.Abs(deltaY)) isPotentialCandidate = true;
            //            break;

            //        case Side.Left:
            //            // Must be to the left and more horizontal than vertical.
            //            if (deltaX < 0 && Math.Abs(deltaX) > Math.Abs(deltaY)) isPotentialCandidate = true;
            //            break;

            //        case Side.Down:
            //            // Must be below and more vertical than horizontal.
            //            if (deltaY > 0 && Math.Abs(deltaY) > Math.Abs(deltaX)) isPotentialCandidate = true;
            //            break;

            //        case Side.Top:
            //            // Must be above and more vertical than horizontal.
            //            if (deltaY < 0 && Math.Abs(deltaY) > Math.Abs(deltaX)) isPotentialCandidate = true;
            //            break;
            //    }


            //    if (isPotentialCandidate)
            //    {
            //        // 4. If it's a valid candidate, check if it's the closest one yet.
            //        // We use the square of the distance to avoid costly square root operations.
            //        double distanceSquared = (candidatePosition - currentPosition).LengthSquared;

            //        if (distanceSquared < minDistanceSquared)
            //        {
            //            minDistanceSquared = distanceSquared;
            //            bestCandidate = candidateButton;
            //        }
            //    }
            //}

            return bestCandidate;
        }

        public bool IsNavigatorOnButton(int buttonId)
        {
            return _lastHighlightedButtonId == buttonId;
        }
    }
}
