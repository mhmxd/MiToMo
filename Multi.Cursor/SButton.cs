using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Multi.Cursor
{
    public class SButton : Button
    {
        // A static counter to generate unique IDs across all SButton instances
        private static int _nextId = 0;

        // Width ID for the button, used to identify the width of the button in the grid
        public int WidthMultiple = 0;

        // Property to store the unique ID
        public int Id { get; private set; }

        public int LeftId { get; private set; } = -1; // Default to -1 (no neighbor)
        public int RightId { get; private set; } = -1;
        public int TopId { get; private set; } = -1;
        public int BottomId { get; private set; } = -1;

        public SButton()
        {
            this.Id = Interlocked.Increment(ref _nextId);

            this.BorderBrush = Config.ELEMENT_DEFAULT_BORDER_COLOR; // Set the border brush for the button
            this.BorderThickness = new System.Windows.Thickness(2); // Set the border thickness
            this.Padding = new System.Windows.Thickness(0); 
            this.Background = Config.ELEMENT_DEFAULT_FILL_COLOR; // Set the background color
        }

        /// <summary>
        /// Sets the unique IDs of the four spatial neighbors for this button.
        /// </summary>
        /// <param name="topId">The ID of the button above.</param>
        /// <param name="bottomId">The ID of the button below.</param>
        /// <param name="leftId">The ID of the button to the left.</param>
        /// <param name="rightId">The ID of the button to the right.</param>
        public void SetNeighbors(int topId, int bottomId, int leftId, int rightId)
        {
            this.TopId = topId;
            this.BottomId = bottomId;
            this.LeftId = leftId;
            this.RightId = rightId;
        }

        
    }
}
