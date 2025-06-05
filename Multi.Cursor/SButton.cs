using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Multi.Cursor
{
    internal class SButton : Button
    {
        public SButton()
        {
            this.BorderBrush = Config.ELEMENT_DEFAULT_BORDER_COLOR; // Set the border brush for the button
            this.BorderThickness = new System.Windows.Thickness(2); // Set the border thickness
            this.Padding = new System.Windows.Thickness(0); 
            this.Background = Config.ELEMENT_DEFAULT_FILL_COLOR; // Set the background color
        }

    }
}
