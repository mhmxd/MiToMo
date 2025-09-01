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
using System.Windows.Shapes;
using static Multi.Cursor.Output;

namespace Multi.Cursor
{
    public abstract class AuxWindow : Window
    {
        // Class to store all the info regarding each button (positions, etc.)
        protected class ButtonInfo
        {
            public SButton Button { get; set; }
            public Point Position { get; set; }
            public Rect Rect { get; set; }
            public Range DistToStartRange { get; set; } // In pixels
            public Brush ButtonFill { get; set; } // Default background color for the button

            public ButtonInfo(SButton button) { 
                Button = button;
                Position = new Point(0, 0);
                Rect = new Rect();
                DistToStartRange = new Range(0, 0);
                ButtonFill = Config.BUTTON_DEFAULT_FILL_COLOR;
            }

            public void ChangeBackFill()
            {
                Button.Background = ButtonFill; // Reset the button background to the default color
            }

            public void ResetButtonFill()
            {
                ButtonFill = Config.BUTTON_DEFAULT_FILL_COLOR; // Reset the button fill color to the default
                Button.Background = ButtonFill; // Change the button background to the default color
            }

            public void ResetButonBorder()
            {
                Button.BorderBrush = Config.BUTTON_DEFAULT_BORDER_COLOR; // Reset the button border to the default color
            }

        }

        protected List<Grid> _gridColumns = new List<Grid>(); // List of grid columns
        protected Dictionary<int, List<SButton>> _widthButtons = new Dictionary<int, List<SButton>>(); // Dictionary to hold buttons by their width multiples
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

        private const double Tolerance = 5.0; // A small tolerance for alignment checks (e.g., for slightly misaligned buttons)

        public abstract void GenerateGrid(Rect startConstraintsRectAbsolute, params Func<Grid>[] columnCreators);

        public abstract void PlaceGrid(Func<Grid> gridCreator);

        protected int FindMiddleButtonId()
        {
            // Calculate the center of the overall button grid
            double gridCenterX = (_gridMinX + _gridMaxX) / 2;
            double gridCenterY = (_gridMinY + _gridMaxY) / 2;
            Point gridCenterPoint = new Point(gridCenterX, gridCenterY);
            //this.TrialInfo($"Central Point: {gridCenterPoint}");
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
                    //this.TrialInfo($"Middle button found at ID#{buttonId} with position {gridCenterPoint}");
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

        public int SelectRandButtonByConstraints(int widthMult, Rect objConstraintRect, int dist)
        {
            //this.TrialInfo($"Selecting button by multiple: {widthMult}");
            //this.TrialInfo($"All buttons: ");
            //foreach (int bid in _allButtons.Keys)
            //{
            //    this.TrialInfo($"Button#{bid} -> {_allButtons[bid].Id}");
            //}

            //this.TrialInfo($"Available buttons:");
            foreach (int wm in _widthButtons.Keys)
            {
                string ids = string.Join(", ", _widthButtons[wm].Select(b => b.Id.ToString()));
                //this.TrialInfo($"WM {wm} -> {ids}");
            }

            if (_widthButtons[widthMult].Count > 0)
            {

                // Find the buttons with dist laying inside their dist to start range
                List<int> possibleButtons = new List<int>();
                foreach (int buttonId in _buttonInfos.Keys)
                {
                    Point buttonCenter = new Point(
                        _buttonInfos[buttonId].Rect.X + _buttonInfos[buttonId].Rect.Width / 2,
                        _buttonInfos[buttonId].Rect.Y + _buttonInfos[buttonId].Rect.Height / 2);
                    Point buttonCenterAbsolute = buttonCenter.OffsetPosition(this.Left, this.Top); // Offset to the top-left position
                    //this.TrialInfo($"ButtonCenter: {buttonCenterAbsolute}; Rect: {objConstraintRect.ToString()}; " +
                    //    $"Dist: {dist}; MaxDist: {objConstraintRect.MaxDistanceFromPoint(buttonCenterAbsolute)}");
                    if (objConstraintRect.MaxDistanceFromPoint(buttonCenterAbsolute) > dist)
                    {
                        possibleButtons.Add(buttonId);
                    }
                    //this.TrialInfo($"Dist = {dist} | DistToStart: {_buttonInfos[buttonId].DistToStartRange.ToString()}");
                    //if (_buttonInfos[buttonId].DistToStartRange.ContainsExc(dist))
                    //{
                    //    possibleButtons.Add(buttonId);
                    //}
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
            } 
            else
            {
                this.TrialInfo($"No buttons available for width multiple {widthMult}!");
                return -1; // Return an invalid point if no buttons are found
            }

            this.TrialInfo($"No buttons with width multiple {widthMult} matched the distance!");
            return -1; // Return an invalid point if no buttons are found

        }

        public int SelectRandButtonByConstraints(int widthMult, Range distRange)
        {
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
                foreach (SButton button in _widthButtons[widthMult])
                {
                    //this.TrialInfo($"Dist range = {distRange.ToString()} | DistToStart: {_buttonInfos[button.Id].DistToStartRange.ToString()}");
                    if (_buttonInfos[button.Id].DistToStartRange.ContainsExc(distRange))
                    {
                        possibleButtons.Add(button.Id);
                    }
                }

                // If we have options, return a random from them
                if (possibleButtons.Count > 0)
                {
                    return possibleButtons.GetRandomElement();
                }
            }
            else
            {
                this.TrialInfo($"No buttons available for width multiple {widthMult}!");
                return -1; // Return an invalid point if no buttons are found
            }

            this.TrialInfo($"No buttons with width multiple {widthMult} matched the distance!");
            return -1; // Return an invalid point if no buttons are found

        }

        public virtual void FillGridButton(int buttonId, Brush color)
        {
            // Find the button with the specified ID
            if (_buttonInfos.ContainsKey(buttonId))
            {
                _buttonInfos[buttonId].ButtonFill = color; // Store the default background color
                _buttonInfos[buttonId].Button.Background = color; // Change the background color of the button
                _buttonInfos[buttonId].Button.DisableBackgroundHover = true; // Disable hover fill for this button
                //this.TrialInfo($"Button with ID {targetId} filled with color {color}.");
            }
            else
            {
                this.TrialInfo($"Button with ID {buttonId} not found.");
            }
        }

        public virtual void SetGridButtonHandlers(
            List<int> funcIds,
            MouseButtonEventHandler funcMouseDownHandler, MouseButtonEventHandler funcMouseUpHandler,
            MouseButtonEventHandler nonFuncMouseDownHandler)
        {
            // Clear existing handlers for all buttons
            foreach (int id in _buttonInfos.Keys)
            {
                _buttonInfos[id].Button.RemoveHandler(UIElement.MouseDownEvent, funcMouseDownHandler);
                _buttonInfos[id].Button.RemoveHandler(UIElement.MouseUpEvent, funcMouseUpHandler);
                _buttonInfos[id].Button.RemoveHandler(UIElement.MouseDownEvent, nonFuncMouseDownHandler);
            }

            // Set new handlers for buttons
            foreach (int id in _buttonInfos.Keys)
            {
                if (funcIds.Contains(id)) // Handling Target
                {
                    //this.TrialInfo($"Adding target handler for button #{id}");
                    _buttonInfos[id].Button.AddHandler(UIElement.MouseDownEvent, funcMouseDownHandler, handledEventsToo: true);
                    _buttonInfos[id].Button.AddHandler(UIElement.MouseUpEvent, funcMouseUpHandler, handledEventsToo: true);
                }
                else // Handling non-Targets
                {
                    //this.TrialInfo($"Adding non-target handler for button #{id}");
                    _buttonInfos[id].Button.AddHandler(UIElement.MouseDownEvent, nonFuncMouseDownHandler, handledEventsToo: true);
                }
            }

        }

        public virtual void SetGridButtonHandlers(
            int targetId,
            MouseButtonEventHandler targetMouseDownHandler, MouseButtonEventHandler targetMouseUpHandler,
            MouseButtonEventHandler nonTargetMouseDownHandler)
        {
            // Clear existing handlers for all buttons
            foreach (int id in _buttonInfos.Keys)
            {
                _buttonInfos[id].Button.RemoveHandler(UIElement.MouseDownEvent, targetMouseDownHandler);
                _buttonInfos[id].Button.RemoveHandler(UIElement.MouseUpEvent, targetMouseUpHandler);
                _buttonInfos[id].Button.RemoveHandler(UIElement.MouseDownEvent, nonTargetMouseDownHandler);
            }

            // Set new handlers for buttons
            foreach (int id in _buttonInfos.Keys)
            {
                if (id == targetId) // Handling Target
                {
                    //this.TrialInfo($"Adding target handler for button #{id}");
                    _buttonInfos[id].Button.AddHandler(UIElement.MouseDownEvent, targetMouseDownHandler, handledEventsToo: true);
                    _buttonInfos[id].Button.AddHandler(UIElement.MouseUpEvent, targetMouseUpHandler, handledEventsToo: true);
                }
                else // Handling non-Targets
                {
                    //this.TrialInfo($"Adding non-target handler for button #{id}");
                    _buttonInfos[id].Button.AddHandler(UIElement.MouseDownEvent, nonTargetMouseDownHandler, handledEventsToo: true);
                }
            }

        }

        public virtual Point GetGridButtonCenter(int buttonId)
        {
            //this.TrialInfo($"Button positions: {_buttonPositions.Stringify<int, Point>()}");
            // Find the button with the specified ID
            if (_buttonInfos.ContainsKey(buttonId))
            {
                //this.TrialInfo($"Button#{targetId} position in window: {position}");
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

        public virtual Point GetGridButtonPosition(int buttonId)
        {
            // Find the button with the specified ID
            if (_buttonInfos.ContainsKey(buttonId))
            {
                //this.TrialInfo($"Button#{targetId} position in window: {position}");
                return _buttonInfos[buttonId].Position; // Return the position of the button
            }
            else
            {
                this.TrialInfo($"Button with ID {buttonId} not found.");
                return new Point(0, 0); // Return an invalid point if no button is found
            }
        }

        public virtual void ResetButtons()
        {
            foreach (int buttonId in _buttonInfos.Keys)
            {
                _buttonInfos[buttonId].ResetButtonFill();
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

        public void ShowMarker()
        {
            if (_lastHighlightedButtonId != -1 && _buttonInfos.ContainsKey(_lastHighlightedButtonId))
            {
                MarkButton(_lastHighlightedButtonId); // Highlight the last highlighted button
                this.TrialInfo($"Last highlight = {_lastHighlightedButtonId}");
            }
            else
            {
                this.TrialInfo("No button was highlighted yet.");
            }
        }

        public void ActivateMarker()
        {
            this.TrialInfo($"Last highlight = {_lastHighlightedButtonId}");
            ActivateMarker(_lastHighlightedButtonId); // Activate the grid navigator with the last highlighted button ID
        }

        public void ActivateMarker(int buttonId)
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
                _buttonInfos[_lastHighlightedButtonId].ChangeBackFill();
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
                _buttonInfos[buttonId].ResetButonBorder();
                _buttonInfos[buttonId].ChangeBackFill();
                //if (_buttonInfos[buttonId].Button.Background != Config.FUNCTION_ENABLED_COLOR 
                //    && _buttonInfos[buttonId].Button.Background != Config.FUNCTION_DEFAULT_COLOR)
                //{
                //    _buttonInfos[buttonId].Button.Background = Config.BUTTON_DEFAULT_FILL_COLOR; // Reset the background color of all buttons
                //}
            }
        }

        private void MarkButton(int buttonId)
        {
            // Find the button with the specified ID
            if (_buttonInfos.ContainsKey(buttonId))
            {
                _buttonInfos[buttonId].Button.BorderBrush = Config.ELEMENT_HIGHLIGHT_COLOR; // Change the border color to highlight
            }
            else
            {
                this.TrialInfo($"Button with ID {buttonId} not found.");
            }
        }

        public void HighlightButton(int buttonId)
        {
            var buttonBgOrangeOrLightGreen =
                _buttonInfos[buttonId].Button.Background.Equals(Config.FUNCTION_DEFAULT_COLOR) ||
                _buttonInfos[buttonId].Button.Background.Equals(Config.FUNCTION_ENABLED_COLOR);
            // Reset the border aof all buttons
            //foreach (var btn in _allButtons.Values)
            //{
            //    btn.BorderBrush = Config.BUTTON_DEFAULT_BORDER_COLOR; // Reset the border color of all buttons
            //}

            // Find the button with the specified ID
            if (_buttonInfos.ContainsKey(buttonId))
            {
                _buttonInfos[buttonId].Button.BorderBrush = Config.ELEMENT_HIGHLIGHT_COLOR; // Change the border color to highlight

                // Change the background to selected (green) if orange or light green
                if (buttonBgOrangeOrLightGreen)
                {
                    _buttonInfos[buttonId].Button.Background = Config.FUNCTION_APPLIED_COLOR;


                    // Tell the MainWindow to mark the mapped object and set function as applied
                    ((MainWindow)this.Owner).MarkMappedObject(buttonId);
                    ((MainWindow)this.Owner).SetFunctionAsApplied(buttonId);
                }
                else // Other buttons
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

        public void MoveMarker(TouchPoint tp)
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

            // Change the last highlighted button if it has changed
            if (highlightedButton.Id != _lastHighlightedButtonId)
            {
                _lastHighlightedButtonId = highlightedButton.Id; // Update the last highlighted button ID
                ResetHighlights(); // Reset highlights for all buttons
                HighlightButton(highlightedButton.Id); // Highlight the new button
            }
        }

        public SButton GetNeighbor(SButton currentButton, Side direction)
        {
            if (currentButton == null || !_buttonInfos.ContainsKey(currentButton.Id))
            {
                return null; // Cannot navigate from an unregistered button.
            }

            // Use the Rect property for bounds
            Rect currentRect = _buttonInfos[currentButton.Id].Rect;

            List<SButton> potentialCandidates = new List<SButton>();

            foreach (int candidateId in _buttonInfos.Keys)
            {
                if (candidateId == currentButton.Id) continue; // Don't compare a button to itself.

                Rect candidateRect = _buttonInfos[candidateId].Rect;

                bool isCandidate = false;

                switch (direction)
                {
                    case Side.Right:
                        // Candidate's left edge must be to the right of current's right edge
                        // And their vertical projections must overlap
                        if (candidateRect.Left >= currentRect.Right - Tolerance &&
                            candidateRect.Left > currentRect.Left && // Ensure it's truly "to the right"
                            (currentRect.IntersectsWith(candidateRect) || IsVerticallyAligned(currentRect, candidateRect)))
                        {
                            isCandidate = true;
                        }
                        break;

                    case Side.Left:
                        // Candidate's right edge must be to the left of current's left edge
                        // And their vertical projections must overlap
                        if (candidateRect.Right <= currentRect.Left + Tolerance &&
                            candidateRect.Right < currentRect.Right && // Ensure it's truly "to the left"
                            (currentRect.IntersectsWith(candidateRect) || IsVerticallyAligned(currentRect, candidateRect)))
                        {
                            isCandidate = true;
                        }
                        break;

                    case Side.Down:
                        // Candidate's top edge must be below current's bottom edge
                        // And their horizontal projections must overlap
                        if (candidateRect.Top >= currentRect.Bottom - Tolerance &&
                            candidateRect.Top > currentRect.Top && // Ensure it's truly "below"
                            (currentRect.IntersectsWith(candidateRect) || IsHorizontallyAligned(currentRect, candidateRect)))
                        {
                            isCandidate = true;
                        }
                        break;

                    case Side.Top:
                        // Candidate's bottom edge must be above current's top edge
                        // And their horizontal projections must overlap
                        if (candidateRect.Bottom <= currentRect.Top + Tolerance &&
                            candidateRect.Bottom < currentRect.Bottom && // Ensure it's truly "above"
                            (currentRect.IntersectsWith(candidateRect) || IsHorizontallyAligned(currentRect, candidateRect)))
                        {
                            isCandidate = true;
                        }
                        break;
                }

                if (isCandidate)
                {
                    potentialCandidates.Add(_buttonInfos[candidateId].Button);
                }
            }

            if (potentialCandidates.Count == 0)
            {
                return null; // No neighbor found in this direction
            }

            // Now, select the best candidate based on distance and leftmost preference
            SButton bestNeighbor = null;
            double minDistance = double.PositiveInfinity; // Euclidean distance between centers
            double minHorizontalPosition = double.PositiveInfinity; // For leftmost preference

            foreach (SButton candidate in potentialCandidates)
            {
                Rect candidateRect = _buttonInfos[candidate.Id].Rect;

                // Calculate Euclidean distance between centers
                double currentCenterX = currentRect.X + currentRect.Width / 2;
                double currentCenterY = currentRect.Y + currentRect.Height / 2;
                double candidateCenterX = candidateRect.X + candidateRect.Width / 2;
                double candidateCenterY = candidateRect.Y + candidateRect.Height / 2;

                double euclideanDistance = Math.Sqrt(
                    Math.Pow(candidateCenterX - currentCenterX, 2) +
                    Math.Pow(candidateCenterY - currentCenterY, 2)
                );

                // Preference: Closest overall, then leftmost if distances are very similar.
                if (euclideanDistance < minDistance - Tolerance) // Significantly closer
                {
                    minDistance = euclideanDistance;
                    minHorizontalPosition = candidateRect.Left;
                    bestNeighbor = candidate;
                }
                else if (Math.Abs(euclideanDistance - minDistance) <= Tolerance) // Similar distance, apply leftmost preference
                {
                    if (candidateRect.Left < minHorizontalPosition)
                    {
                        minDistance = euclideanDistance; // Update minDistance to reflect the new best
                        minHorizontalPosition = candidateRect.Left;
                        bestNeighbor = candidate;
                    }
                }
            }

            return bestNeighbor;
        }

        protected void LinkButtonNeighbors()
        {
            if (_buttonInfos.Count == 0) return;
            //if (_allButtons.Count == 0) return;

            // For each button in the grid...
            foreach (int buttonId in _buttonInfos.Keys)
            {
                // ...find its neighbor in each of the four directions.
                SButton topNeighbor = GetNeighbor(_buttonInfos[buttonId].Button, Side.Top);
                SButton bottomNeighbor = GetNeighbor(_buttonInfos[buttonId].Button, Side.Down);
                SButton leftNeighbor = GetNeighbor(_buttonInfos[buttonId].Button, Side.Left);
                SButton rightNeighbor = GetNeighbor(_buttonInfos[buttonId].Button, Side.Right);

                // Get the ID of each neighbor, or -1 if the neighbor is null.
                int topId = topNeighbor?.Id ?? -1;
                int bottomId = bottomNeighbor?.Id ?? -1;
                int leftId = leftNeighbor?.Id ?? -1;
                int rightId = rightNeighbor?.Id ?? -1;

                // Call the method on the button to store its neighbor IDs.
                _buttonInfos[buttonId].Button.SetNeighbors(topId, bottomId, leftId, rightId);
            }
        }

        // Helper to check if two rectangles have horizontal overlap within a tolerance
        private bool IsHorizontallyAligned(Rect r1, Rect r2)
        {
            // Checks if the horizontal projection of r1 and r2 overlap
            return (r1.Left < r2.Right - Tolerance && r1.Right > r2.Left + Tolerance);
        }

        // Helper to check if two rectangles have vertical overlap within a tolerance
        private bool IsVerticallyAligned(Rect r1, Rect r2)
        {
            // Checks if the vertical projection of r1 and r2 overlap
            return (r1.Top < r2.Bottom - Tolerance && r1.Bottom > r2.Top + Tolerance);
        }

        public bool IsNavigatorOnButton(int buttonId)
        {
            return _lastHighlightedButtonId == buttonId;
        }

        /// <summary>
        /// Calculates the minimum and maximum possible distances from an outside point
        /// to the points inside a WPF Rect.
        /// </summary>
        /// <param name="outsidePoint">The point outside (or potentially inside/on the edge of) the rectangle.</param>
        /// <param name="rect">The WPF Rect object.</param>
        /// <returns>A Tuple where Item1 is the minimum distance and Item2 is the maximum distance.</returns>
        public static Range GetMinMaxDistances(Point outsidePoint, Rect rect)
        {
            double minDist;
            double maxDist;

            // --- Calculate Minimum Distance ---
            // The closest point in the rectangle to outsidePoint could be:
            // 1. The outsidePoint itself, if it's inside or on the edge of the rectangle (minDist = 0).
            // 2. A point on one of the rectangle's edges (if outsidePoint projects onto the edge).
            // 3. One of the rectangle's corners (if outsidePoint projects outside the edge).

            // For WPF Rect, X is Left, Y is Top.
            // rect.Right and rect.Bottom are calculated properties (X + Width, Y + Height).
            double dx = Math.Max(0.0, Math.Max(rect.X - outsidePoint.X, outsidePoint.X - rect.Right));
            double dy = Math.Max(0.0, Math.Max(rect.Y - outsidePoint.Y, outsidePoint.Y - rect.Bottom));

            // If the point is inside the rectangle, dx and dy will both be 0, resulting in minDist = 0.
            // Otherwise, it's the distance to the closest point on the boundary.
            minDist = Math.Sqrt(dx * dx + dy * dy);


            // --- Calculate Maximum Distance ---
            // The furthest point from outsidePoint will always be one of the four corners of the rectangle.
            Point[] corners = new Point[]
            {
            new Point(rect.X, rect.Y),           // Top-Left
            new Point(rect.Right, rect.Y),       // Top-Right
            new Point(rect.X, rect.Bottom),      // Bottom-Left
            new Point(rect.Right, rect.Bottom)   // Bottom-Right
            };

            maxDist = 0.0;
            foreach (Point corner in corners)
            {
                // Use the standard Euclidean distance formula.
                // WPF Point already has a handy static method for this.
                double currentDist = Utils.Dist(outsidePoint, corner); // Or Point.Subtract(outsidePoint, corner).Length;
                if (currentDist > maxDist)
                {
                    maxDist = currentDist;
                }
            }

            return new Range(minDist, maxDist);
        }

        public abstract void ShowPoint(Point p);

        
    }
}
