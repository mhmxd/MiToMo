using Common.Helpers;
using Common.Settings;
using CommonUI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Common.Constants.ExpEnums;

namespace SubTask.FunctionPointSelect
{
    public abstract class AuxWindow : Window
    {

        public Side Side { get; set; } // Side of the window (left, right, top)

        protected Grid _buttonsGrid; // The grid containing all buttons
        protected List<Grid> _gridColumns = new List<Grid>(); // List of grid columns
        protected Dictionary<int, List<int>> _widthButtons = new(); // Dictionary to hold button Ids by their width multiples
        protected Dictionary<int, ButtonWrap> _buttonWraps = new(); // Id to ButtonWrap mapping
        protected SButton _targetButton; // Currently selected button (if any)

        // Boundary of the grid (encompassing all buttons)
        protected double _gridMinX = double.MaxValue;
        protected double _gridMinY = double.MaxValue;
        protected double _gridMaxX = double.MinValue;
        protected double _gridMaxY = double.MinValue;

        protected GridNavigator _gridNavigator = new GridNavigator(ExpEnvironment.FRAME_DUR_MS / 1000.0);
        protected int _lastMarkedButtonId = -1; // ID of the currently highlighted button
        protected Point _topLeftButtonPosition = new Point(10000, 10000); // Initialize to a large value to find the top-left button
        protected int _middleButtonId = -1; // ID of the middle button in the grid

        protected Rect _startConstraintRectAbsolute = new();

        private const double Tolerance = 5.0; // A small tolerance for alignment checks (e.g., for slightly misaligned buttons)

        private MouseEventHandler _currentFuncMouseEnterHandler;
        private MouseButtonEventHandler _currentFuncMouseDownHandler;
        private MouseButtonEventHandler _currentFuncMouseUpHandler;
        private MouseEventHandler _currentFuncMouseExitHandler;
        private MouseButtonEventHandler _currentNonFuncMouseDownHandler;

        //public abstract void GenerateGrid(Rect startConstraintsRectAbsolute, params Func<Grid>[] columnCreators);

        public abstract Task PlaceGrid(Func<Grid> gridCreator, double topPadding, double leftPadding);

        public void SetStartConstraintRect(Rect rect)
        {
            _startConstraintRectAbsolute = rect;
            this.PositionInfo($"Start constraint rect set to: {rect.ToString()}");
        }

        protected void RegisterAllButtons(DependencyObject parent)
        {
            //-- Recursively find all SButton instances in the entire _buttonsGrid
            // Get the number of children in the current parent object
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

            // Loop through each child
            for (int i = 0; i < childrenCount; i++)
            {
                // Get the current child object
                var child = VisualTreeHelper.GetChild(parent, i);

                // If the child is an SButton, register it
                if (child is SButton sButton)
                {
                    RegisterButton(sButton);
                }

                // Recursively call the method for the current child to search its children
                RegisterAllButtons(child);
            }
        }

        protected void RegisterButton(SButton button)
        {
            var window = Window.GetWindow(this); // Get current window instance
            if (window == null) return;

            _buttonWraps[button.Id] = new ButtonWrap(button);

            // Transform specifically to the window we are currently in
            GeneralTransform transformToWindow = button.TransformToVisual(window);
            Point positionInWindow = transformToWindow.Transform(new Point(0, 0));

            this.PositionInfo($"Registering button {button}");
            _widthButtons.TryAdd(button.WidthMultiple, new List<int>());
            _widthButtons[button.WidthMultiple].Add(button.Id); // Add the button to the dictionary with its width as the key
            _buttonWraps[button.Id] = new ButtonWrap(button);

            // Get the transform from the button to the Window (or the root visual)
            //GeneralTransform transformToWindow = button.TransformToVisual(Window.GetWindow(button));
            // Get the point representing the top-left corner of the button relative to the Window
            //Point positionInWindow = transformToWindow.Transform(new Point(0, 0));
            _buttonWraps[button.Id].Position = positionInWindow;

            Rect buttonRect = new Rect(positionInWindow.X, positionInWindow.Y, button.ActualWidth, button.ActualHeight);
            _buttonWraps[button.Id].Rect = buttonRect;
            this.PositionInfo($"ButtonRect: {buttonRect}");

            // Set the distance to start range for the button based on the object constraint rect
            Point buttonCenterAbsolute =
                positionInWindow
                .OffsetPosition(button.ActualWidth / 2, button.ActualHeight / 2)
                .OffsetPosition(this.Left, this.Top);

            _buttonWraps[button.Id].DistToStartRange = GetMinMaxDistances(buttonCenterAbsolute, _startConstraintRectAbsolute);

            // Update min/max x and y for grid bounds
            _gridMinX = Math.Min(_gridMinX, buttonRect.Left);
            _gridMinY = Math.Min(_gridMinY, buttonRect.Top);
            _gridMaxX = Math.Max(_gridMaxX, buttonRect.Right);
            _gridMaxY = Math.Max(_gridMaxY, buttonRect.Bottom);


            if (positionInWindow.X <= _topLeftButtonPosition.X && positionInWindow.Y <= _topLeftButtonPosition.Y)
            {
                //this.TrialInfo($"Top-left button position updated: {positionInWindow} for button ID#{button.Id}");
                _topLeftButtonPosition = positionInWindow; // Update the top-left button position
            }

            // Register globally instead of locally
            ButtonRegistry.Register(button.Id, button, this);
        }

        protected void FindMiddleButton()
        {
            int middleId = FindMiddleButtonId();
            if (middleId != -1)
            {
                this.TrialInfo($"Middle Id = {middleId}");
                _lastMarkedButtonId = middleId; // Set the last highlighted button to the middle button
                _middleButtonId = middleId;
            }
            else
            {
                this.TrialInfo("No middle button found in the grid.");
            }
        }

        protected int FindMiddleButtonId()
        {
            // Calculate the center of the overall button grid
            double gridCenterX = (_gridMinX + _gridMaxX) / 2;
            double gridCenterY = (_gridMinY + _gridMaxY) / 2;
            Point gridCenterPoint = new Point(gridCenterX, gridCenterY);
            //this.TrialInfo($"Central Point: {gridCenterPoint}");
            // DistanceMM to the center point
            double centerDistance = double.MaxValue;
            int closestButtonId = -1;

            foreach (int buttonId in _buttonWraps.Keys)
            {
                Rect buttonRect = _buttonWraps[buttonId].Rect;
                this.PositionInfo($"Button#{buttonId}; Rect: {buttonRect.ToString()}; Btn: {_buttonWraps[buttonId].Button}");
                // Check which button contains the grid center point
                if (buttonRect.Contains(gridCenterPoint))
                {
                    // If we find a button that contains the center point, return its ID
                    //this.TrialInfo($"Middle button found at ID#{buttonId} with position {gridCenterPoint}");
                    return buttonId;
                }
                else // if button doesn't containt the center point, calculate the distance
                {
                    double dist = UITools.Dist(gridCenterPoint, new Point(buttonRect.X + buttonRect.Width / 2, buttonRect.Y + buttonRect.Height / 2));
                    //this.TrialInfo($"Dist = {dist:F2}");
                    if (dist < centerDistance)
                    {
                        centerDistance = dist;
                        closestButtonId = buttonId; // Update the last highlighted button to the closest one
                    }
                }
            }

            return closestButtonId;

        }

        public int GetMiddleButtonId()
        {
            return _middleButtonId;
        }

        public int SelectRandButton(int widthMulti)
        {
            if (!_widthButtons.ContainsKey(widthMulti) || _widthButtons[widthMulti] == null)
            {
                this.TrialInfo($"No buttons available for width multiple {widthMulti}!");
                return -1;
            }

            return _widthButtons[widthMulti].GetRandomElement();
        }

        public int SelectRandButtonByConstraints(int widthMult, Rect objConstraintRect, int dist)
        {

            //this.TrialInfo($"Available buttons:");
            //foreach (int wm in _widthButtons.Keys)
            //{
            //    string ids = string.Join(", ", _widthButtons[wm].Select(b => b.Id.ToString()));
            //    //this.TrialInfo($"WM {wm} -> {ids}");
            //}

            this.TrialInfo($"Width: {widthMult}; Rect: {objConstraintRect}; Dist: {dist}");

            if (_widthButtons[widthMult] == null) return -1;

            if (_widthButtons[widthMult].Count > 0)
            {
                // Find the buttons with dist laying inside their dist to start range
                List<int> possibleButtons = new();
                foreach (int buttonId in _widthButtons[widthMult])
                {
                    Point buttonCenter = new(
                        _buttonWraps[buttonId].Rect.X + _buttonWraps[buttonId].Rect.Width / 2,
                        _buttonWraps[buttonId].Rect.Y + _buttonWraps[buttonId].Rect.Height / 2);
                    Point buttonCenterAbsolute = buttonCenter.OffsetPosition(this.Left, this.Top); // Offset to the top-left position
                    this.TrialInfo($"ButtonCenter: {buttonCenterAbsolute}; Rect: {objConstraintRect}; " +
                        $"Dist: {dist}; MaxDist: {objConstraintRect.MaxDistanceFromPoint(buttonCenterAbsolute)}");
                    if (objConstraintRect.MaxDistanceFromPoint(buttonCenterAbsolute) > dist)
                    {
                        possibleButtons.Add(buttonId);
                    }
                }

                // If we have options, return a random from them
                if (possibleButtons.Count > 0)
                {
                    return possibleButtons.GetRandomElement();
                }
                else
                {
                    this.TrialInfo($"No buttons with width multiple {widthMult} matched the distance!");
                    return -1; // Return an invalid point if no buttons are found
                }

            }
            else
            {
                this.TrialInfo($"No buttons available for width multiple {widthMult}!");
                return -1; // Return an invalid point if no buttons are found
            }

        }

        public int SelectRandButtonByConstraints(int widthMult, MRange distRange)
        {
            this.PositionInfo($"Width: {widthMult}; Dist: {distRange}");

            if (_widthButtons[widthMult].Count > 0)
            {

                // Find the buttons with dist laying inside their dist to start range
                List<int> possibleButtons = new();
                foreach (int btnId in _widthButtons[widthMult])
                {
                    this.PositionInfo($"DistRange: {distRange} | DistToStart: {_buttonWraps[btnId].DistToStartRange}");
                    if (_buttonWraps[btnId].DistToStartRange.ContainsInc(distRange))
                    {
                        possibleButtons.Add(btnId);
                    }
                }

                // If we have options, return a random from them
                if (possibleButtons.Count > 0)
                {
                    return possibleButtons.GetRandomElement();
                }
                else
                {
                    this.PositionInfo($"No buttons with width multiple {widthMult} matched the distance!");
                    return -1; // Return an invalid point if no buttons are found 0

                }
            }
            else
            {
                this.TrialInfo($"No buttons available for width multiple {widthMult}!");
                return -1; // Return an invalid point if no buttons are found
            }

        }

        public virtual void FillGridButton(int buttonId, Brush color)
        {
            // Use Background priority to ensure the UI has finished "Resetting" from the previous trial
            //Owner.Dispatcher.BeginInvoke(new Action(() =>
            //{
            //    if (_buttonWraps.TryGetValue(buttonId, out var wrap))
            //    {
            //        var btn = wrap.Button;

            //        // 1. Force the window to the front and visible
            //        this.Visibility = Visibility.Visible;

            //        // 2. Apply the color
            //        btn.Background = color;

            //        // 3. Force a layout update for this specific element
            //        btn.UpdateLayout();

            //        this.TrialInfo($"[Post-Dispatch] Button {buttonId} IsVisible: {btn.IsVisible} | Parent: {VisualTreeHelper.GetParent(btn)}");

            //        if (!btn.IsVisible)
            //        {
            //            // Logic Check: If it's still not visible, check the opacity or parent visibility
            //            this.TrialInfo($"CRITICAL: Button {buttonId} is still hidden. Window Opacity: {this.Opacity}");
            //        }
            //    }
            //}), System.Windows.Threading.DispatcherPriority.Background);

            if (_buttonWraps.TryGetValue(buttonId, out var wrap))
            {
                var btn = wrap.Button;
                btn.Background = color;

                // DEBUG: Trace visibility up the tree
                //DependencyObject parent = btn;
                //this.TrialInfo($"--- Visibility Trace for Button {buttonId} ---");
                //while (parent != null)
                //{
                //    if (parent is UIElement ui)
                //    {
                //        this.TrialInfo($"{ui.GetType().Name} (ID: {(ui is FrameworkElement fe ? fe.Name : "N/A")}) - Visibility: {ui.Visibility}, IsVisible: {ui.IsVisible}");
                //    }
                //    parent = VisualTreeHelper.GetParent(parent);
                //}
            }

            // Find the button with the specified ID
            //if (_buttonWraps.ContainsKey(buttonId))
            //{
            //    this.TrialInfo($"Window Hash: {this.GetHashCode()}");
            //    this.TrialInfo($"Button {buttonId} IsVisible: {_buttonWraps[buttonId].Button.IsVisible}");
            //    this.TrialInfo($"Button {buttonId} Parent: {VisualTreeHelper.GetParent(_buttonWraps[buttonId].Button)}");
            //    _buttonWraps[buttonId].Button.Background = color; // Change the background color of the button
            //    this.TrialInfo($"Button {buttonId} filled with color {color}.");
            //}
            //else
            //{
            //    this.TrialInfo($"Button with ID {buttonId} not found.");
            //}
        }

        public void ClearAllEventHandlers(UIElement element)
        {
            var eventHandlersStoreField = typeof(UIElement).GetField("_eventHandlersStore",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (eventHandlersStoreField?.GetValue(element) != null)
            {
                eventHandlersStoreField.SetValue(element, null);
            }
        }

        public virtual void SetGridButtonHandlers(
        List<int> funcIds,
        MouseEventHandler funcMouseEnterHandler,
        MouseButtonEventHandler funcMouseDownHandler,
        MouseButtonEventHandler funcMouseUpHandler,
        MouseEventHandler funcMouseExitHandler,
        MouseButtonEventHandler nonFuncMouseDownHandler)
        {
            // Remove OLD handlers (using stored references)
            if (_currentFuncMouseDownHandler != null)
            {
                foreach (int id in _buttonWraps.Keys)
                {
                    _buttonWraps[id].Button.RemoveHandler(UIElement.MouseEnterEvent, _currentFuncMouseEnterHandler);
                    _buttonWraps[id].Button.RemoveHandler(UIElement.MouseDownEvent, _currentFuncMouseDownHandler);
                    _buttonWraps[id].Button.RemoveHandler(UIElement.MouseUpEvent, _currentFuncMouseUpHandler);
                    _buttonWraps[id].Button.RemoveHandler(UIElement.MouseLeaveEvent, _currentFuncMouseExitHandler);
                    _buttonWraps[id].Button.RemoveHandler(UIElement.MouseDownEvent, _currentNonFuncMouseDownHandler);
                }
            }

            // Store NEW handlers
            _currentFuncMouseEnterHandler = funcMouseEnterHandler;
            _currentFuncMouseDownHandler = funcMouseDownHandler;
            _currentFuncMouseUpHandler = funcMouseUpHandler;
            _currentFuncMouseExitHandler = funcMouseExitHandler;
            _currentNonFuncMouseDownHandler = nonFuncMouseDownHandler;

            // Add NEW handlers
            foreach (int id in _buttonWraps.Keys)
            {
                if (funcIds.Contains(id))
                {
                    _buttonWraps[id].Button.AddHandler(UIElement.MouseEnterEvent, _currentFuncMouseEnterHandler, handledEventsToo: true);
                    _buttonWraps[id].Button.AddHandler(UIElement.MouseDownEvent, _currentFuncMouseDownHandler, handledEventsToo: true);
                    _buttonWraps[id].Button.AddHandler(UIElement.MouseUpEvent, _currentFuncMouseUpHandler, handledEventsToo: true);
                    _buttonWraps[id].Button.AddHandler(UIElement.MouseLeaveEvent, _currentFuncMouseExitHandler, handledEventsToo: true);
                }
                else
                {
                    _buttonWraps[id].Button.AddHandler(UIElement.MouseDownEvent, _currentNonFuncMouseDownHandler, handledEventsToo: true);
                }
            }
        }

        public virtual void SetGridButtonHandlers(
            int targetId,
            MouseButtonEventHandler targetMouseDownHandler, MouseButtonEventHandler targetMouseUpHandler,
            MouseButtonEventHandler nonTargetMouseDownHandler)
        {
            // Clear existing handlers for all buttons
            foreach (int id in _buttonWraps.Keys)
            {
                _buttonWraps[id].Button.RemoveHandler(UIElement.MouseDownEvent, targetMouseDownHandler);
                _buttonWraps[id].Button.RemoveHandler(UIElement.MouseUpEvent, targetMouseUpHandler);
                _buttonWraps[id].Button.RemoveHandler(UIElement.MouseDownEvent, nonTargetMouseDownHandler);
            }

            // Set new handlers for buttons
            foreach (int id in _buttonWraps.Keys)
            {
                if (id == targetId) // Handling Target
                {
                    //this.TrialInfo($"Adding target handler for button #{id}");
                    _buttonWraps[id].Button.AddHandler(UIElement.MouseDownEvent, targetMouseDownHandler, handledEventsToo: true);
                    _buttonWraps[id].Button.AddHandler(UIElement.MouseUpEvent, targetMouseUpHandler, handledEventsToo: true);
                }
                else // Handling non-Targets
                {
                    //this.TrialInfo($"Adding non-target handler for button #{id}");
                    _buttonWraps[id].Button.AddHandler(UIElement.MouseDownEvent, nonTargetMouseDownHandler, handledEventsToo: true);
                }
            }

        }

        public virtual Point GetGridButtonCenter(int buttonId)
        {
            this.PositionInfo($"Buttons: {_buttonWraps.Str()}");
            // Find the button with the specified ID
            if (_buttonWraps.ContainsKey(buttonId))
            {
                //this.TrialInfo($"Button#{targetId} position in window: {position}");
                double buttonHalfWidth = _buttonWraps[buttonId].Button.ActualWidth / 2;
                double buttonHalfHeight = _buttonWraps[buttonId].Button.ActualHeight / 2;
                return _buttonWraps[buttonId].Position.OffsetPosition(buttonHalfWidth, buttonHalfHeight);
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
            if (_buttonWraps.ContainsKey(buttonId))
            {
                //this.TrialInfo($"Button#{targetId} position in window: {position}");
                return _buttonWraps[buttonId].Position; // Return the position of the button
            }
            else
            {
                this.TrialInfo($"Button with ID {buttonId} not found.");
                return new Point(0, 0); // Return an invalid point if no button is found
            }
        }

        public virtual void ResetButtonFills()
        {
            foreach (int buttonId in _buttonWraps.Keys)
            {
                _buttonWraps[buttonId].ResetButtonFill();
            }
        }

        public SButton GetNeighbor(SButton currentButton, Side direction)
        {
            if (currentButton == null || !_buttonWraps.ContainsKey(currentButton.Id))
            {
                return null; // Cannot navigate from an unregistered button.
            }

            // Use the Rect property for bounds
            Rect currentRect = _buttonWraps[currentButton.Id].Rect;

            List<SButton> potentialCandidates = new List<SButton>();

            foreach (int candidateId in _buttonWraps.Keys)
            {
                if (candidateId == currentButton.Id) continue; // Don't compare a button to itself.

                Rect candidateRect = _buttonWraps[candidateId].Rect;

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
                    potentialCandidates.Add(_buttonWraps[candidateId].Button);
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
                Rect candidateRect = _buttonWraps[candidate.Id].Rect;

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
            if (_buttonWraps.Count == 0) return;
            //if (_allButtons.Count == 0) return;

            // For each button in the grid...
            foreach (int buttonId in _buttonWraps.Keys)
            {
                // ...find its neighbor in each of the four directions.
                SButton topNeighbor = GetNeighbor(_buttonWraps[buttonId].Button, Side.Top);
                SButton bottomNeighbor = GetNeighbor(_buttonWraps[buttonId].Button, Side.Down);
                SButton leftNeighbor = GetNeighbor(_buttonWraps[buttonId].Button, Side.Left);
                SButton rightNeighbor = GetNeighbor(_buttonWraps[buttonId].Button, Side.Right);

                // Get the ID of each neighbor, or -1 if the neighbor is null.
                int topId = topNeighbor?.Id ?? -1;
                int bottomId = bottomNeighbor?.Id ?? -1;
                int leftId = leftNeighbor?.Id ?? -1;
                int rightId = rightNeighbor?.Id ?? -1;

                // Call the method on the button to store its neighbor IDs.
                _buttonWraps[buttonId].Button.SetNeighbors(topId, bottomId, leftId, rightId);
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
            return _lastMarkedButtonId == buttonId;
        }

        /// <summary>
        /// Calculates the minimum and maximum possible distances from an outside point
        /// to the points inside a WPF Rect.
        /// </summary>
        /// <param name="outsidePoint">The point outside (or potentially inside/on the edge of) the rectangle.</param>
        /// <param name="rect">The WPF Rect object.</param>
        /// <returns>A Tuple where Item1 is the minimum distance and Item2 is the maximum distance.</returns>
        public static MRange GetMinMaxDistances(Point outsidePoint, Rect rect)
        {
            double minDist;
            double maxDist;

            // --- Calculate Minimum DistanceMM ---
            // The closest point in the rectangle to outsidePoint could be:
            // 1. The outsidePoint itself, if it's inside or on the edge of the rectangle (minDist = 0).
            // 2. A point on one of the rectangle's edges (if outsidePoint projects onto the edge).
            // 3. One of the rectangle's corners (if outsidePoint projects outside the edge).

            // For WPF Rect, x is Left, y is Top.
            // rect.Right and rect.Bottom are calculated properties (x + Width, y + Height).
            double dx = Math.Max(0.0, Math.Max(rect.X - outsidePoint.X, outsidePoint.X - rect.Right));
            double dy = Math.Max(0.0, Math.Max(rect.Y - outsidePoint.Y, outsidePoint.Y - rect.Bottom));

            // If the point is inside the rectangle, dx and dy will both be 0, resulting in minDist = 0.
            // Otherwise, it's the distance to the closest point on the boundary.
            minDist = Math.Sqrt(dx * dx + dy * dy);


            // --- Calculate Maximum DistanceMM ---
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
                double currentDist = UITools.Dist(outsidePoint, corner); // Or Point.Subtract(outsidePoint, corner).Length;
                if (currentDist > maxDist)
                {
                    maxDist = currentDist;
                }
            }

            return new MRange(minDist, maxDist);
        }


    }
}
