using System.Windows.Media;

namespace CommonUI
{
    public class UIColors
    {
        public static readonly Brush GRAY_E6E6E6 =
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E6E6E6"));
        public static readonly Brush GRAY_F3F3F3 =
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F3F3"));

        public static readonly Brush DARK_ORANGE =
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EE6E36"));
        public static readonly Brush LIGHT_PURPLE =
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B19CD7"));

        public static readonly Brush START_AVAILABLE_COLOR = Brushes.Green;
        public static readonly Brush START_UNAVAILABLE_COLOR = DARK_ORANGE;

        public static readonly Brush OBJ_AREA_BG_COLOR = Brushes.White; // Background color of the object area

        public static readonly Brush OBJ_MARKED_COLOR = Brushes.LightGreen;
        public static readonly Brush OBJ_APPLIED_COLOR = Brushes.Green;
        public static readonly Brush OBJ_DEFAULT_COLOR = Brushes.Gray;

        public static readonly Brush FUNCTION_DEFAULT_COLOR = DARK_ORANGE;
        public static readonly Brush FUNCTION_ENABLED_COLOR = Brushes.LightGreen;
        public static readonly Brush FUNCTION_APPLIED_COLOR = Brushes.Green;

        public static readonly Brush ELEMENT_HIGHLIGHT_COLOR = Brushes.Black;
        public static readonly Brush GRID_TARGET_COLOR = Brushes.LightGreen;
        public static readonly Brush BUTTON_DEFAULT_FILL_COLOR = Brushes.White;
        public static readonly Brush BUTTON_DEFAULT_BORDER_COLOR = Brushes.LightGray;

        public static readonly Brush BUTTON_HOVER_FILL_COLOR = Brushes.LightGray; // Color when hovering over an element
        public static readonly Brush BUTTON_HOVER_BORDER_COLOR = Brushes.Black; // Color when hovering over an element border
    }
}
