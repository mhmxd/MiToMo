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
        protected List<Grid> _gridColumns = new List<Grid>(); // List of grid columns
        protected Dictionary<int, List<SButton>> _widthButtons = new Dictionary<int, List<SButton>>(); // Dictionary to hold buttons by their width multiples
        protected Dictionary<int, SButton> _allButtons = new Dictionary<int, SButton>(); // List of all buttons in the grid (id as key)
        protected Dictionary<int, Point> _buttonPositions = new Dictionary<int, Point>(); // Dictionary to hold button positions by their ID
        protected SButton _targetButton; // Currently selected button (if any)

        protected GridNavigator _gridNavigator = new GridNavigator(Config.FRAME_DUR_MS / 1000.0);
        protected int _lastHighlightedButtonId = -1; // ID of the currently highlighted button
        protected Point _topLeftButtonPosition = new Point(10000, 10000); // Initialize to a large value to find the top-left button

        public abstract void GenerateGrid(params Func<Grid>[] columnCreators);

        public int SelectRandButtonByWidth(int widthMult)
        {
            this.TrialInfo($"Selecting button by multiple: {widthMult}");
            //this.TrialInfo($"All buttons: ");
            //foreach (int bid in _allButtons.Keys)
            //{
            //    this.TrialInfo($"Button#{bid} -> {_allButtons[bid].Id}");
            //}

            this.TrialInfo($"Available buttons:");
            foreach (int wm in _widthButtons.Keys)
            {
                string ids = string.Join(", ", _widthButtons[wm].Select(b => b.Id.ToString()));
                this.TrialInfo($"WM {wm} -> {ids}");
            }

            if (_widthButtons[widthMult].Count > 0)
            {
                SButton button = _widthButtons[widthMult].GetRandomElement(); // Get a random button from the list for that width
                if (button != null)
                {

                    this.TrialInfo($"Selected button id in window: {button.Id}");
                    return button.Id;

                }
                else
                {
                    this.TrialInfo($"No buttons found for width multiple {widthMult}.");
                    return -1; // Return an invalid point if no buttons are found
                }
            } else
            {
                this.TrialInfo($"No buttons found for width multiple {widthMult}.");
                return -1; // Return an invalid point if no buttons are found
            }
            
        }

        public virtual void FillGridButton(int buttonId, Brush color)
        {
            // Find the button with the specified ID
            if (_allButtons.TryGetValue(buttonId, out SButton button))
            {
                button.Background = color; // Change the background color of the button
                button.DisableBackgroundHover = true; // Disable hover fill for this button
                this.TrialInfo($"Button with ID {buttonId} filled with color {color}.");
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
            if (_allButtons.TryGetValue(buttonId, out SButton button))
            {
                button.AddHandler(UIElement.MouseDownEvent, targetMouseDownHandler, handledEventsToo: true);
                button.AddHandler(UIElement.MouseUpEvent, targetMouseUpHandler, handledEventsToo: true);

                this.TrialInfo($"Button with ID {buttonId} added handlers.");
            }
            else
            {
                this.TrialInfo($"Button with ID {buttonId} not found.");
            }
        }

        public virtual Point GetGridButtonPosition(int buttonId)
        {
            //this.TrialInfo($"Button positions: {_buttonPositions.Stringify<int, Point>()}");
            // Find the button with the specified ID
            if (_buttonPositions.TryGetValue(buttonId, out Point position))
            {
                this.TrialInfo($"Button#{buttonId} position in window: {position}");
                return position;
            }
            else
            {
                this.TrialInfo($"Button with ID {buttonId} not found.");
                return new Point(0, 0); // Return an invalid point if no button is found
            }
        }

        public virtual void ResetGridSelection()
        {
            foreach (var button in _allButtons.Values)
            {
                button.Background = Config.BUTTON_DEFAULT_FILL_COLOR; // Reset the background color of all buttons
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
            ActivateGridNavigator(_lastHighlightedButtonId); // Activate the grid navigator with the last highlighted button ID
        }

        public void ActivateGridNavigator(int buttonId)
        {
            // Find the button with the specified ID
            if (_allButtons.TryGetValue(buttonId, out SButton button))
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
            if (_lastHighlightedButtonId != -1 && _allButtons.TryGetValue(_lastHighlightedButtonId, out SButton button))
            {
                button.BorderBrush = Config.BUTTON_DEFAULT_BORDER_COLOR; // Reset the border color of the last highlighted button
            }
        }

        public void StopGridNavigator()
        {
            _gridNavigator.Stop();
        }

        public void HighlightButton(int buttonId)
        {
            // Reset the border of all buttons
            foreach (var btn in _allButtons.Values)
            {
                btn.BorderBrush = Config.BUTTON_DEFAULT_BORDER_COLOR; // Reset the border color of all buttons
            }

            // Find the button with the specified ID
            if (_allButtons.TryGetValue(buttonId, out SButton button))
            {
                button.BorderBrush = Config.ELEMENT_HIGHLIGHT_COLOR; // Change the border color to highlight
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

            SButton highlightedButton = _allButtons[_lastHighlightedButtonId];

            // --- Process Horizontal Movement ---
            if (dGridX > 0) // Move Right
            {
                for (int i = 0; i < dGridX; i++)
                {
                    if (highlightedButton.RightId == -1) break; // Hit the edge
                    highlightedButton = _allButtons[highlightedButton.RightId];
                }
            }
            else // Move Left
            {
                for (int i = 0; i < -dGridX; i++)
                {
                    if (highlightedButton.LeftId == -1) break; // Hit the edge
                    highlightedButton = _allButtons[highlightedButton.LeftId];
                }
            }

            // --- Process Vertical Movement ---
            if (dGridY > 0) // Move Down
            {
                for (int i = 0; i < dGridY; i++)
                {
                    if (highlightedButton.BottomId == -1) break; // Hit the edge
                    highlightedButton = _allButtons[highlightedButton.BottomId];
                }
            }
            else // Move Up
            {
                for (int i = 0; i < -dGridY; i++)
                {
                    if (highlightedButton.TopId == -1) break; // Hit the edge
                    highlightedButton = _allButtons[highlightedButton.TopId];
                }
            }

            // If the target button has changed, update the focus
            if (highlightedButton.Id != _lastHighlightedButtonId)
            {
                _lastHighlightedButtonId = highlightedButton.Id; // Update the last highlighted button ID
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
            if (currentButton == null || !_buttonPositions.TryGetValue(currentButton.Id, out Point currentPosition))
            {
                // Cannot navigate from a button that isn't registered.
                return null;
            }

            SButton bestCandidate = null;
            double minDistanceSquared = double.PositiveInfinity;

            // 2. Iterate through all other buttons to find the best candidate.
            foreach (var candidateButton in _allButtons.Values)
            {
                // Don't compare a button to itself.
                if (candidateButton.Id == currentButton.Id)
                {
                    continue;
                }

                if (!_buttonPositions.TryGetValue(candidateButton.Id, out Point candidatePosition))
                {
                    // Skip any candidate button that doesn't have a registered position.
                    continue;
                }

                // 3. Check if the candidate is in the correct direction.
                // We use a simple heuristic: to be considered "to the right", a button's horizontal distance
                // should be greater than its vertical distance. This prevents jumping to a different row.
                double deltaX = candidatePosition.X - currentPosition.X;
                double deltaY = candidatePosition.Y - currentPosition.Y;

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
                    double distanceSquared = (candidatePosition - currentPosition).LengthSquared;

                    if (distanceSquared < minDistanceSquared)
                    {
                        minDistanceSquared = distanceSquared;
                        bestCandidate = candidateButton;
                    }
                }
            }

            return bestCandidate;
        }

        public bool IsNavigatorOnButton(int buttonId)
        {
            return _lastHighlightedButtonId == buttonId;
        }
    }
}
