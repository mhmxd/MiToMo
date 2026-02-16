using Common.Helpers;
using System.Windows;
using static Common.Constants.ExpEnums;

namespace CommonUI
{
    public class ButtonWrap
    {
        public SButton Button { get; set; }
        public Point Position { get; set; }
        public Rect Rect { get; set; }
        public MRange DistToStartRange { get; set; } // In pixels
        public ButtonState State { get; set; }

        public ButtonWrap(SButton button)
        {
            Button = button;
            Position = new Point(0, 0);
            Rect = new Rect();
            DistToStartRange = new MRange(0, 0);
            State = ButtonState.NON_FUNC;
            //ButtonFill = UIColors.COLOR_BUTTON_DEFAULT_FILL;
        }

        public void ChangeBackFill()
        {
            //Button.Background = ButtonFill; // Reset the button background to the default color
            Button.Background = UIColors.COLOR_BUTTON_DEFAULT_FILL;
        }

        public void ResetButtonFill()
        {
            Button.Background = UIColors.COLOR_BUTTON_DEFAULT_FILL; // Change the button background to the default color
        }

        public void ResetButonBorder()
        {
            Button.BorderBrush = UIColors.COLOR_BUTTON_DEFAULT_BORDER; // Reset the button border to the default color
        }

        public override string ToString()
        {
            return $"ButtonWrap[Button#{Button.Id}]";
        }

    }
}
