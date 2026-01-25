using Common.Constants;
using Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Common.Constants.ExpEnums;
using SysWin = System.Windows;

namespace SubTask.PanelNavigation
{
    public abstract class AuxWindow : Window
    {

        protected Border _startButton;

        // Class to store all the info regarding each button (positions, etc.)
        protected class ButtonInfo
        {
            public SButton Button { get; set; }
            public Point Position { get; set; }
            public Rect Rect { get; set; }
            public Range DistToStartRange { get; set; } // In pixels
            public Brush ButtonFill { get; set; } // Default background color for the button

            public ButtonInfo(SButton button)
            {
                Button = button;
                Position = new Point(0, 0);
                Rect = new Rect();
                DistToStartRange = new Range(0, 0);
                ButtonFill = UIColors.COLOR_BUTTON_DEFAULT_FILL;
            }

            public void ChangeBackFill()
            {
                Button.Background = ButtonFill; // Reset the button background to the default color
            }

            public void ResetButtonFill()
            {
                ButtonFill = UIColors.COLOR_BUTTON_DEFAULT_FILL; // Reset the button fill color to the default
                Button.Background = ButtonFill; // Change the button background to the default color
            }

            public void ResetButonBorder()
            {
                Button.BorderBrush = UIColors.COLOR_BUTTON_DEFAULT_BORDER; // Reset the button border to the default color
            }

        }

        public Side Side { get; set; } // Side of the window (left, right, top)
                                       // 
        protected Grid _buttonsGrid; // The grid containing all buttons
        protected List<Grid> _gridColumns = new List<Grid>(); // List of grid columns
        protected Dictionary<int, List<SButton>> _widthButtons = new Dictionary<int, List<SButton>>(); // Dictionary to hold buttons by their width multiples
        protected Dictionary<int, ButtonInfo> _buttonInfos = new Dictionary<int, ButtonInfo>();
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

        protected Rect _objectConstraintRectAbsolute = new Rect();

        private const double Tolerance = 5.0; // A small tolerance for alignment checks (e.g., for slightly misaligned buttons)

        private MouseEventHandler _currentFuncMouseEnterHandler;
        private MouseButtonEventHandler _currentFuncMouseDownHandler;
        private MouseButtonEventHandler _currentFuncMouseUpHandler;
        private MouseEventHandler _currentFuncMouseExitHandler;
        private MouseButtonEventHandler _currentNonFuncMouseDownHandler;

        //public abstract void GenerateGrid(Rect startConstraintsRectAbsolute, params Func<Grid>[] columnCreators);

        public abstract Task PlaceGrid(Func<Grid> gridCreator, double topPadding, double leftPadding);

        public void SetObjectConstraintRect(Rect rect)
        {
            _objectConstraintRectAbsolute = rect;
            this.TrialInfo($"Object constraint rect set to: {rect.ToString()}");
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
            //this.TrialInfo($"Registering button {button.ToString()}");
            _widthButtons.TryAdd(button.WidthMultiple, new List<SButton>());
            _widthButtons[button.WidthMultiple].Add(button); // Add the button to the dictionary with its width as the key
            _buttonInfos[button.Id] = new ButtonInfo(button);
            //_allButtons.Add(button.Id, button); // Add to the list of all buttons

            // Add button position to the dictionary

            // Get the transform from the button to the Window (or the root visual)
            GeneralTransform transformToWindow = button.TransformToVisual(Window.GetWindow(button));
            // Get the point representing the top-left corner of the button relative to the Window
            Point positionInWindow = transformToWindow.Transform(new Point(0, 0));
            _buttonInfos[button.Id].Position = positionInWindow;
            //_buttonPositions.Add(button.Id, positionInWindow); // Store the position of the button
            //this.TrialInfo($"Button Position: {positionInWindow}");

            Rect buttonRect = new Rect(positionInWindow.X, positionInWindow.Y, button.ActualWidth, button.ActualHeight);
            _buttonInfos[button.Id].Rect = buttonRect;
            this.TrialInfo($"ButtonRect: {buttonRect}");
            //_buttonRects.Add(button.Id, buttonRect); // Store the rect for later

            // Set possible distance range to the Start positions
            Point buttonCenterAbsolute =
                positionInWindow
                .OffsetPosition(button.ActualWidth / 2, button.ActualHeight / 2)
                .OffsetPosition(this.Left, this.Top);

            // Correct way of finding min and max dist
            _buttonInfos[button.Id].DistToStartRange = GetMinMaxDistances(buttonCenterAbsolute, _objectConstraintRectAbsolute);

            // Update min/max x and y for grid bounds
            _gridMinX = Math.Min(_gridMinX, buttonRect.Left);
            _gridMinY = Math.Min(_gridMinY, buttonRect.Top);
            _gridMaxX = Math.Max(_gridMaxX, buttonRect.Right);
            _gridMaxY = Math.Max(_gridMaxY, buttonRect.Bottom);


            if (positionInWindow.X <= _topLeftButtonPosition.X && positionInWindow.Y <= _topLeftButtonPosition.Y)
            {
                //this.TrialInfo($"Top-left button position updated: {positionInWindow} for button ID#{button.Id}");
                _topLeftButtonPosition = positionInWindow; // Update the top-left button position
                                                           //_lastMarkedButtonId = button.Id; // Set the last highlighted button to this one
            }
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
            // Distance to the center point
            double centerDistance = double.MaxValue;
            int closestButtonId = -1;

            foreach (int buttonId in _buttonInfos.Keys)
            {
                Rect buttonRect = _buttonInfos[buttonId].Rect;
                this.TrialInfo($"Button#{buttonId}; Rect: {buttonRect.ToString()}; Btn: {_buttonInfos[buttonId].Button.ToString()}");
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

        public int SelectRandButton(int widthMult)
        {

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
                    possibleButtons.Add(buttonId);
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
            //this.TrialInfo($"Available buttons: ");
            //foreach (int wm in _widthButtons.Keys)
            //{
            //    string ids = string.Join(", ", _widthButtons[wm].Select(b => b.Id.ToString()));
            //    this.TrialInfo($"WM {wm} -> {ids}");
            //}

            //this.TrialInfo($"Look for {widthMult}");
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

        public TFunction FillRandomGridBtn(Brush color)
        {
            // Select a random button
            int buttonInd = _buttonInfos.GetRandomEntry().Key;
            _buttonInfos[buttonInd].ButtonFill = color; // Store the default background color
            _buttonInfos[buttonInd].Button.Background = color; // Change the background color of the button

            TFunction resultFunction = new
                (
                    id: _buttonInfos[buttonInd].Button.Id,
                    widthInUnit: _buttonInfos[buttonInd].Button.WidthMultiple,
                    center: GetGridButtonCenter(_buttonInfos[buttonInd].Button.Id),
                    position: GetGridButtonPosition(_buttonInfos[buttonInd].Button.Id)
                );

            return resultFunction;
        }

        public virtual void FillGridButton(int buttonId, Brush color)
        {
            // Find the button with the specified ID
            if (_buttonInfos.ContainsKey(buttonId))
            {
                _buttonInfos[buttonId].ButtonFill = color; // Store the default background color
                _buttonInfos[buttonId].Button.Background = color; // Change the background color of the button
                _buttonInfos[buttonId].Button.DisableBackgroundHover = true; // Disable hover fill for this button
                //this.TrialInfo($"Button {buttonId} filled with color {color}.");
            }
            else
            {
                //this.TrialInfo($"Button with ID {buttonId} not found.");
            }
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
                foreach (int id in _buttonInfos.Keys)
                {
                    _buttonInfos[id].Button.RemoveHandler(UIElement.MouseEnterEvent, _currentFuncMouseEnterHandler);
                    _buttonInfos[id].Button.RemoveHandler(UIElement.MouseDownEvent, _currentFuncMouseDownHandler);
                    _buttonInfos[id].Button.RemoveHandler(UIElement.MouseUpEvent, _currentFuncMouseUpHandler);
                    _buttonInfos[id].Button.RemoveHandler(UIElement.MouseLeaveEvent, _currentFuncMouseExitHandler);
                    _buttonInfos[id].Button.RemoveHandler(UIElement.MouseDownEvent, _currentNonFuncMouseDownHandler);
                }
            }

            // Store NEW handlers
            _currentFuncMouseEnterHandler = funcMouseEnterHandler;
            _currentFuncMouseDownHandler = funcMouseDownHandler;
            _currentFuncMouseUpHandler = funcMouseUpHandler;
            _currentFuncMouseExitHandler = funcMouseExitHandler;
            _currentNonFuncMouseDownHandler = nonFuncMouseDownHandler;

            // Add NEW handlers
            foreach (int id in _buttonInfos.Keys)
            {
                if (funcIds.Contains(id))
                {
                    _buttonInfos[id].Button.AddHandler(UIElement.MouseEnterEvent, _currentFuncMouseEnterHandler, handledEventsToo: true);
                    _buttonInfos[id].Button.AddHandler(UIElement.MouseDownEvent, _currentFuncMouseDownHandler, handledEventsToo: true);
                    _buttonInfos[id].Button.AddHandler(UIElement.MouseUpEvent, _currentFuncMouseUpHandler, handledEventsToo: true);
                    _buttonInfos[id].Button.AddHandler(UIElement.MouseLeaveEvent, _currentFuncMouseExitHandler, handledEventsToo: true);
                }
                else
                {
                    _buttonInfos[id].Button.AddHandler(UIElement.MouseDownEvent, _currentNonFuncMouseDownHandler, handledEventsToo: true);
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

        public virtual void Reset()
        {
            foreach (int buttonId in _buttonInfos.Keys)
            {
                _buttonInfos[buttonId].ResetButtonFill();
            }

            // Remove start button
            RemoveStartBtn();

            // Hide marker
            DeactivateMarker();

        }

        //public virtual void MakeTargetAvailable()
        //{
        //    // Implemented in the derived classes
        //}

        //public virtual void MakeTargetUnavailable()
        //{
        //    // Implemented in the derived classes
        //}

        public virtual void ShowStartBtn(int btnW, int btnH, Brush btnColor, MouseEvents btnEvents)
        {
            // Create the start button
            _startButton = new Border
            {
                Width = btnW,
                Height = btnH,
                Background = btnColor,
                BorderBrush = Brushes.Black,
            };

            // Add label inside
            var label = new TextBlock
            {
                Text = ExpStrs.START_CAP,
                HorizontalAlignment = SysWin.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontSize = ExpLayouts.START_BUTTON_FONT_SIZE,
                Margin = new Thickness(10, 8, 10, 8) // Optional: to center the text nicely
            };
            _startButton.Child = label;

            // Add event handlers
            _startButton.MouseEnter += btnEvents.MouseEnter;
            _startButton.MouseLeave += btnEvents.MouseLeave;
            _startButton.MouseDown += btnEvents.MouseDown;
            _startButton.MouseUp += btnEvents.MouseUp;

            //--> Positioning is handled in the derived classes
        }

        public int CreateStartButton(int largerSide, Brush btnColor, int prevDis, MouseEvents btnEvents)
        {
            // Create the start button
            _startButton = new Border
            {
                // Will be changed in the overridden method
                Width = 0,
                Height = 0, 
                Background = btnColor,
                BorderBrush = Brushes.Black,
            };

            // Add label inside
            var label = new TextBlock
            {
                Text = ExpStrs.START_CAP,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                FontSize = ExpLayouts.START_BUTTON_FONT_SIZE,
                Margin = new Thickness(10, 8, 10, 8) // Optional: to center the text nicely
            };
            _startButton.Child = label;

            // Add event handlers
            _startButton.MouseEnter += btnEvents.MouseEnter;
            _startButton.MouseLeave += btnEvents.MouseLeave;
            _startButton.MouseDown += btnEvents.MouseDown;
            _startButton.MouseUp += btnEvents.MouseUp;

            // Show and return the distance
            return PositionStartButton(largerSide, prevDis);
        }

        public abstract int PositionStartButton(int largerSide, int prevDis);

        public virtual void RemoveStartBtn()
        {
            
        }

        public void ChangeStartBtnColor(Brush newColor)
        {
            if (_startButton != null)
            {
                _startButton.Background = newColor;
            }
        }

        public void ChangeStartBtnLabel(string newText)
        {
            if (_startButton != null && _startButton.Child is TextBlock label)
            {
                label.Text = newText;
            }
        }

        public void ShowMarker(Action<int, GridPos> OnFunctionMarked)
        {
            int buttonId = _lastMarkedButtonId;
            if (buttonId != -1 && _buttonInfos.ContainsKey(_lastMarkedButtonId))
            {
                //MarkButton(_lastMarkedButtonId, OnFunctionMarked); // Highlight the last highlighted button
                this.TrialInfo($"Last highlight = {_lastMarkedButtonId}");
                if (_buttonInfos.ContainsKey(buttonId))
                {
                    _buttonInfos[buttonId].Button.BorderBrush = UIColors.COLOR_ELEMENT_HIGHLIGHT; // Change the border color to highlight
                                                                                                // Change the old button background based on the previous state
                    if (_buttonInfos[buttonId].Button.Background.Equals(UIColors.COLOR_BUTTON_HOVER_FILL)) // Gray => White
                    {
                        //this.TrialInfo($"Set {_lastMarkedButtonId} to Default Fill");
                        _buttonInfos[buttonId].Button.Background = UIColors.COLOR_BUTTON_DEFAULT_FILL;
                    }
                    else if (_buttonInfos[buttonId].Button.Background.Equals(UIColors.COLOR_FUNCTION_ENABLED)) // Light green => Orange
                    {
                        _buttonInfos[buttonId].Button.Background = UIColors.COLOR_FUNCTION_DEFAULT;
                    }
                }
                else
                {
                    this.TrialInfo($"Button with ID {buttonId} not found.");
                }
            }
            else
            {
                this.TrialInfo("No button was highlighted yet.");
            }
        }

        public void ActivateMarkerRandomly(int funcId)
        {
            // Mark a random button different from funcId
            List<int> possibleButtons = _buttonInfos.Keys.Where(id => id != funcId).ToList();
            if (possibleButtons.Count > 0)
            {
                int randomButtonId = possibleButtons.GetRandomElement();
                GridPos pos = _buttonInfos[randomButtonId].Button.RowCol;
                _gridNavigator.Activate(); // Activate the grid navigator
                MarkButton(randomButtonId, (id, pos) => { }); // Highlight the button
            }
            else
            {
                this.TrialInfo("No alternative button found to highlight.");
            }
        }

        public void ActivateMarker(Action<int, GridPos> OnFunctionMarked)
        {
            this.TrialInfo($"Last highlight = {_lastMarkedButtonId}");

            // Find the button with the specified ID
            if (_buttonInfos.ContainsKey(_lastMarkedButtonId))
            {
                _gridNavigator.Activate(); // Activate the grid navigator
                MarkButton(_lastMarkedButtonId, OnFunctionMarked); // Highlight the button
            }
            else
            {
                this.TrialInfo($"Button with ID {_lastMarkedButtonId} not found.");
            }
        }

        public void DeactivateMarker()
        {
            _gridNavigator.Deactivate(); // Deactivate the grid navigator
            if (_lastMarkedButtonId != -1 && _buttonInfos.ContainsKey(_lastMarkedButtonId))
            {
                _buttonInfos[_lastMarkedButtonId].ChangeBackFill();
                _buttonInfos[_lastMarkedButtonId].Button.BorderBrush = UIColors.COLOR_BUTTON_DEFAULT_BORDER; // Reset the border color
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
                //if (_buttonInfos[buttonId].Button.Background != UIColors.COLOR_FUNCTION_ENABLED 
                //    && _buttonInfos[buttonId].Button.Background != UIColors.COLOR_FUNCTION_DEFAULT)
                //{
                //    _buttonInfos[buttonId].Button.Background = UIColors.COLOR_BUTTON_DEFAULT_FILL; // Reset the background color of all buttons
                //}
            }
        }

        public void MarkButton(int buttonId, Action<int, GridPos> OnFunctionMarked)
        {
            var buttonBgOrange =
                _buttonInfos[buttonId].Button.Background.Equals(UIColors.COLOR_FUNCTION_DEFAULT);
            var buttonBgLightGreen =
                _buttonInfos[buttonId].Button.Background.Equals(UIColors.COLOR_FUNCTION_ENABLED);
            var buttonBgDarkGreen =
                _buttonInfos[buttonId].Button.Background.Equals(UIColors.COLOR_FUNCTION_APPLIED);

            // Reset the border aof all buttons
            //foreach (var btn in _allButtons.Values)
            //{
            //    btn.BorderBrush = UIColors.COLOR_BUTTON_DEFAULT_BORDER; // Reset the border color of all buttons
            //}

            // Find the button with the specified ID
            if (_buttonInfos.ContainsKey(buttonId))
            {
                _buttonInfos[buttonId].Button.BorderBrush = UIColors.COLOR_ELEMENT_HIGHLIGHT; // Change the border color to highlight

                if (_buttonInfos[buttonId].Button.Background.Equals(UIColors.COLOR_BUTTON_DEFAULT_FILL)) // Normal button
                {
                    //this.TrialInfo($"Set {markedButton.Id} to Hover Fill");
                    _buttonInfos[buttonId].Button.Background = UIColors.COLOR_BUTTON_HOVER_FILL;
                }
                else if (_buttonInfos[buttonId].Button.Background.Equals(UIColors.COLOR_FUNCTION_DEFAULT)) // Function (default)
                {
                    this.TrialInfo($"Set {_buttonInfos[buttonId].Button.Id} to Enabled");
                    _buttonInfos[buttonId].Button.Background = UIColors.COLOR_FUNCTION_ENABLED;
                    // Call the event
                    OnFunctionMarked(_buttonInfos[buttonId].Button.Id, _buttonInfos[buttonId].Button.RowCol);
                }

                _lastMarkedButtonId = buttonId; // Store the ID of the highlighted button
            }
            else
            {
                this.TrialInfo($"Button with ID {buttonId} not found.");
            }
        }

        public void MoveMarker(
            TouchPoint tp, 
            Action<int, GridPos> OnFunctionMarked, Action<int, GridPos> OnFunctionDeMarked,
            Action<GridPos> OnButtonMarked)
        {
            // Update the grid navigator with the current touch point
            var (dGridX, dGridY) = _gridNavigator.Update(tp);

            if ((dGridX == 0 && dGridY == 0) || _lastMarkedButtonId == -1)
            {
                return; // No movement needed
            }

            SButton markedButton = _buttonInfos[_lastMarkedButtonId].Button;

            // --- Process Horizontal Movement ---
            if (dGridX > 0) // Move Right
            {
                for (int i = 0; i < dGridX; i++)
                {
                    if (markedButton.RightId == -1) break; // Hit the edge
                    markedButton = _buttonInfos[markedButton.RightId].Button;
                }

            }
            else // Move Left
            {
                for (int i = 0; i < -dGridX; i++)
                {
                    if (markedButton.LeftId == -1) break; // Hit the edge
                    markedButton = _buttonInfos[markedButton.LeftId].Button;
                }
            }

            // --- Process Vertical Movement ---
            if (dGridY > 0) // Move Down
            {
                for (int i = 0; i < dGridY; i++)
                {
                    if (markedButton.BottomId == -1) break; // Hit the edge
                    markedButton = _buttonInfos[markedButton.BottomId].Button;
                }
            }
            else // Move Up
            {
                for (int i = 0; i < -dGridY; i++)
                {
                    if (markedButton.TopId == -1) break; // Hit the edge
                    markedButton = _buttonInfos[markedButton.TopId].Button;
                }
            }

            if (markedButton.Id != _lastMarkedButtonId)
            {
                // STEP 1: Handle the old button's state change
                if (_lastMarkedButtonId != -1 && _buttonInfos.ContainsKey(_lastMarkedButtonId))
                {
                    var oldButton = _buttonInfos[_lastMarkedButtonId].Button;
                    oldButton.BorderBrush = UIColors.COLOR_BUTTON_DEFAULT_BORDER;
                    markedButton.BorderBrush = UIColors.COLOR_ELEMENT_HIGHLIGHT;

                    OnButtonMarked(markedButton.RowCol); // Call the event

                    // Change the old button background based on the previous state
                    if (oldButton.Background.Equals(UIColors.COLOR_BUTTON_HOVER_FILL)) // Gray => White
                    {
                        //this.TrialInfo($"Set {_lastMarkedButtonId} to Default Fill");
                        oldButton.Background = UIColors.COLOR_BUTTON_DEFAULT_FILL;
                        
                    }
                    else if (oldButton.Background.Equals(UIColors.COLOR_FUNCTION_ENABLED)) // Light green => Orange
                    {
                        oldButton.Background = UIColors.COLOR_FUNCTION_DEFAULT;
                        OnFunctionDeMarked(oldButton.Id, oldButton.RowCol); // Call the event
                    }

                    // Change the new button background based on its previous state
                    MarkButton(markedButton.Id, OnFunctionMarked);
                }

                // STEP 2: Update the last marked button ID
                _lastMarkedButtonId = markedButton.Id;
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
            return _lastMarkedButtonId == buttonId;
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

            // For WPF Rect, x is Left, y is Top.
            // rect.Right and rect.Bottom are calculated properties (x + Width, y + Height).
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
                double currentDist = UITools.Dist(outsidePoint, corner); // Or Point.Subtract(outsidePoint, corner).Length;
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
