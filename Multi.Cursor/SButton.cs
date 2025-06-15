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
        public SButton()
        {
            this.Id = Interlocked.Increment(ref _nextId);

            this.BorderBrush = Config.ELEMENT_DEFAULT_BORDER_COLOR; // Set the border brush for the button
            this.BorderThickness = new System.Windows.Thickness(2); // Set the border thickness
            this.Padding = new System.Windows.Thickness(0); 
            this.Background = Config.ELEMENT_DEFAULT_FILL_COLOR; // Set the background color
        }

    }
}
