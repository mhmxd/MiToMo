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

        public static readonly Brush COLOR_START_AVAILABLE = Brushes.Green;
        public static readonly Brush COLOR_START_UNAVAILABLE = DARK_ORANGE;

        public static readonly Brush COLOR_OBJ_AREA_BG = Brushes.White; // Background color of the object area

        public static readonly Brush COLOR_OBJ_MARKED = Brushes.LightGreen;
        public static readonly Brush COLOR_OBJ_APPLIED = Brushes.Green;
        public static readonly Brush COLOR_OBJ_DEFAULT = Brushes.Gray;

        public static readonly Brush COLOR_FUNCTION_DEFAULT = DARK_ORANGE;
        public static readonly Brush COLOR_FUNCTION_ENABLED = Brushes.LightGreen;
        public static readonly Brush COLOR_FUNCTION_APPLIED = Brushes.Green;

        public static readonly Brush COLOR_ELEMENT_HIGHLIGHT = Brushes.Black;
        public static readonly Brush COLOR_GRID_TARGET = Brushes.LightGreen;
        public static readonly Brush COLOR_BUTTON_DEFAULT_FILL = Brushes.White;
        public static readonly Brush COLOR_BUTTON_DEFAULT_BORDER = Brushes.LightGray;

        public static readonly Brush COLOR_BUTTON_HOVER_FILL = Brushes.LightGray; // Color when hovering over an element
        public static readonly Brush COLOR_BUTTON_HOVER_BORDER = Brushes.Black; // Color when hovering over an element border
    }
}
