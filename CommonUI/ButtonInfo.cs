using Common.Helpers;
using System.Windows;
using System.Windows.Media;

namespace CommonUI
{
    // Class to store all the info regarding each button (positions, etc.)
    public class ButtonInfo
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
}
