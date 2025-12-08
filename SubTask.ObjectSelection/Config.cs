using System;
using System.Linq;
using System.Security.Policy;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using SysDrawing = System.Drawing;

namespace SubTask.ObjectSelection
{
    internal class Config
    {

        //public const double PPI = 109; // Apple Display
        public const double PPI = 93.54; // BenQ = 89 (changed to 93.54), Apple Display = 109

        // Used as basis (in Experiment)
        public const double EXCEL_CELL_W = 15;
        public const double EXCEL_CELL_H = 5;

        public static int LAST_TOUCH_COL = 14; // Total number of touch columns

        public static int LAST_LEFT_TOUCH_COL = 2; // Width of the left touch area (in columns)
        public static int LAST_RIGHT_TOUCH_COL = 2; // Width of the right touch area (in columns)
        public static int LAST_TOP_TOUCH_ROW = 5; // Width of the top touch area (in rows)

        // MOUSE movement
        public static int TIME_CURSOR_MOVE_RESET = 2; // Cursor timer is reset after 2 seconds 
        public static double MIN_CURSOR_MOVE_MM = 5; // Minimum cursor movement to be considered as mouse moved 

        // -------------- Touch -----------------
        public static readonly int MIN_PRESSURE = 50; // Minimum value to consider touching the surface // Was 30 (seemed too sensitive)
        public static readonly int MIN_TOTAL_PRESSURE = 500; // Min value to consider a finger // Was 2 * MIN_PRESSURE
        public static byte LOCAL_MINIMA_DROP_THRESHOLD = 70;
        // --------------------------------------

        // -------------- Vel. Kalman Filter ----
        public static int FRAME_DUR_MS = 20; // Duration of each frame to pass to Kalman

        // Tap
        public static double NORMAL_VKF_PROCESS_NOISE = 100; // Was 50
        public static double NORMAL_VKF_MEASURE_NOISE = 10; // Was 10
        public static double NORMAL_BASE_GAIN = 20;       // Minimum movement amplification (adjust for small target selection) // Was 10
        public static double NORMAL_SCALE_FACTOR = 40;    // Maximum gain at high speed (adjust for fast movement) // was 12 
        public static double NORMAL_SENSITIVITY = 20;    // Controls how quickly gain increases (adjust for balance)

        // Swipe (needs to be faster, as cursor starts from the middle)
        public static double FAST_VKF_PROCESS_NOISE = 100;
        public static double FAST_VKF_MEASURE_NOISE = 2;
        public static double FAST_BASE_GAIN = 10;       // Minimum movement amplification (adjust for small target selection)
        public static double FAST_SCALE_FACTOR = 10;    // Maximum gain at high speed (adjust for fast movement)
        public static double FAST_SENSITIVITY = 5;    // Controls how quickly gain increases (adjust for balance)

        //--- Active
        public static double VKF_PROCESS_NOISE = 100;
        public static double VKF_MEASURE_NOISE = 5;
        public static double BASE_GAIN = 50;
        public static double SCALE_FACTOR = 60;
        public static double SENSITIVITY = 20;

        public static double MIN_MOVEMENT_THRESHOLD = 0.5; // Minimum movement to be considered a movement (in px)

        // Radiusor
        public static double RAD_BEAM_VKF_PROCESS_NOISE_STD = 1.2;
        public static double RAD_BEAM_VKF_MEASURE_NOISE_STD = 15.0;
        public static double RAD_PLUS_VKF_PROCESS_NOISE_STD = 0.4;
        public static double RAD_PLUS_VKF_MEASURE_NOISE_STD = 40.0;


        // --------------------------------------

        // -------------- Cursors
        public static double MAPPING_GAIN = 1; // Was 50 // Let's not use it (KvF will take care of it)

        // --------------------------------------

        // --------------- Screen
        public static Screen ACTIVE_SCREEN; // Set by MainWindow
        // --------------------------------------

        // --------------- Radiusor -------------
        public static double SENSITIVIY_FACTOR = 0.1;
        public static double ANGLE_GAIN = 20;
        // --------------------------------------

        // --------------- Times ----------------
        public static double SWIPE_TIME_MAX = 500; // ms (was 600)
        public static double TAP_TIME_MAX = 200; // Time to be down for Tap (200ms was too short)
        public static double TAP_TIME_MIN = 70; // Minimum Time to be down for Tap
        // --------------------------------------

        // --------------- Movement Thresholds --
        public static readonly (float DX, float DY) TAP_GENERAL_THRESHOLD = (0.2f, 0.2f);

        public static readonly (float DX, float DY) TAP_THUMB_THRESHOLD = (0.5f, 0.5f);
        public static readonly (float DX, float DY) TAP_INDEX_THRESHOLD = (0.5f, 0.5f); // Was 0.2f
        public static readonly (float DX, float DY) TAP_MIDDLE_THRESHOLD = (0.7f, 0.7f); // For better recognition of Taps
        public static readonly (float DX, float DY) TAP_RING_THRESHOLD = (0.5f, 0.5f);
        public static readonly (float DX, float DY) TAP_PINKY_THRESHOLD = (0.5f, 0.5f);

        public static readonly float SWIPE_MOVE_THRESHOLD = 0.8f; // pts
        // --------------------------------------

        // --------------- Colors ---------------
        public static readonly Brush GRAY_E6E6E6 =
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E6E6E6"));
        public static readonly Brush GRAY_F3F3F3 =
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F3F3"));
        //public static readonly Brush GRAY_A0A0A0 =
        //    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0A0A0"));
        public static readonly Brush DARK_ORANGE =
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EE6E36"));
        public static readonly Brush LIGHT_PURPLE =
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B19CD7"));

        public static readonly Brush START_AVAILABLE_COLOR = Brushes.LightGreen;
        public static readonly Brush START_UNAVAILABLE_COLOR = DARK_ORANGE;

        public static readonly Brush OBJ_AREA_BG_COLOR = Brushes.White; // Background color of the object area

        public static readonly Brush OBJ_ENABLED_COLOR = Brushes.LightGreen;
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
        // --------------------------------------

        // --------------- Grid --------------------
        
        public static double GUTTER_TOP_SIMPLE = 4; // mm
        public static double GUTTER_SIDE_SIMPLE_MM = 4; // mm
        public static double GRID_MAX_ELEMENT_WIDTH_MM = 45; // Width of the widest element in the grid
        public static double GRID_MIN_ELEMENT_WIDTH_MM = 3; // Width of the narrowest element in the grid
        public static int ELEMENT_BORDER_THICKNESS = 2; // Thickness of the border around the grid elements

        public static int CELL_WIDTH_THRESHOLD = 50; // px
        public static int CELL_HEIGHT_THRESHOLD = 50; // px
        // -----------------------------------------

        public static Dimensions TRIAL_START_BUTTON_DIM_MM = new Dimensions(20, 10);
        public static int TRIAL_START_BUTTON_FONT_SIZE = 16;

        public static void SetMode(int speed)
        {
            //if (speed == 0)
            //{
            //    VKF_PROCESS_NOISE = NORMAL_VKF_PROCESS_NOISE;
            //    VKF_MEASURE_NOISE = NORMAL_VKF_MEASURE_NOISE;
            //    BASE_GAIN = NORMAL_BASE_GAIN;
            //    SCALE_FACTOR = NORMAL_SCALE_FACTOR;
            //    SENSITIVITY = NORMAL_SENSITIVITY;
            //}

            //if (speed == 1)
            //{
            //    VKF_PROCESS_NOISE = FAST_VKF_PROCESS_NOISE;
            //    VKF_MEASURE_NOISE = FAST_VKF_MEASURE_NOISE;
            //    BASE_GAIN = FAST_BASE_GAIN;
            //    SCALE_FACTOR = FAST_SCALE_FACTOR;
            //    SENSITIVITY = FAST_SENSITIVITY;
            //}
        }
    }
}
