using Common.Helpers;
using System.Windows;

namespace CommonUI
{
    public class ButtonWrap
    {
        public SButton Button { get; set; }
        public Point Position { get; set; }
        public Rect Rect { get; set; }
        public MRange DistToStartRange { get; set; } // In pixels

        public ButtonWrap(SButton button)
        {
            Button = button;
            Position = new Point(0, 0);
            Rect = new Rect();
            DistToStartRange = new MRange(0, 0);
            //ButtonFill = UIColors.COLOR_BUTTON_DEFAULT_FILL;
        }

        public void ChangeBackFill()
        {
            //Button.Background = ButtonFill; // Reset the button background to the default color
            Button.Background = UIColors.COLOR_BUTTON_DEFAULT_FILL;
        }

        public void ResetButtonFill()
        {
            //ButtonFill = UIColors.COLOR_BUTTON_DEFAULT_FILL; // Need this!!
            Button.Background = UIColors.COLOR_BUTTON_DEFAULT_FILL; // Change the button background to the default color
        }

        public void ResetButonBorder()
        {
            Button.BorderBrush = UIColors.COLOR_BUTTON_DEFAULT_BORDER; // Reset the button border to the default color
        }

    }
}
