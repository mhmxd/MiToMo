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
        protected GridNavigator _gridNavigator;
        protected List<Grid> _gridColumns = new List<Grid>(); // List of grid columns
        protected Dictionary<int, List<SButton>> _widthButtons = new Dictionary<int, List<SButton>>(); // Dictionary to hold buttons by their width multiples
        protected Dictionary<int, SButton> _allButtons = new Dictionary<int, SButton>(); // List of all buttons in the grid (id as key)
        protected Dictionary<int, Point> _buttonPositions = new Dictionary<int, Point>(); // Dictionary to hold button positions by their ID
        protected SButton _targetButton; // Currently selected button (if any)

        public abstract void GenerateGrid(params Func<Grid>[] columnCreators);

        public int SelectRandButtonByWidth(int widthMult)
        {
            this.TrialInfo($"Selecting button by multiple: {widthMult}");
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
        }

        public virtual void FillGridButton(int buttonId, Brush color)
        {
            // Find the button with the specified ID
            if (_allButtons.TryGetValue(buttonId, out SButton button))
            {
                button.Background = color; // Change the background color of the button
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
                button.Background = Config.ELEMENT_DEFAULT_FILL_COLOR; // Reset the background color of all buttons
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

        public virtual void ActivateGridNavigator()
        {
            // Implemented in the derived classes
        }
    }
}
