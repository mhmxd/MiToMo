using System.Windows.Input;

namespace SubTask.PanelNavigation
{
    public class MouseEvents
    {
        public MouseEventHandler MouseEnter { get; set; }
        public MouseButtonEventHandler MouseDown { get; set; }
        public MouseButtonEventHandler MouseUp { get; set; }
        public MouseEventHandler MouseLeave { get; set; }

        public MouseEvents(MouseButtonEventHandler mouseDown,
                           MouseButtonEventHandler mouseUp,
                           MouseEventHandler mouseEnter = null,
                           MouseEventHandler mouseLeave = null)
        {
            MouseDown = mouseDown;
            MouseUp = mouseUp;
            MouseEnter = mouseEnter;
            MouseLeave = mouseLeave;
        }

        public MouseEvents(MouseEventHandler mouseEnter,
                           MouseButtonEventHandler mouseDown,
                           MouseButtonEventHandler mouseUp,
                           MouseEventHandler mouseLeave)
        {
            MouseEnter = mouseEnter;
            MouseDown = mouseDown;
            MouseUp = mouseUp;
            MouseLeave = mouseLeave;
        }
    }
}
