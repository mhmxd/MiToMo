using System;
using System.Security.Policy;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using SysDrawing = System.Drawing;

namespace Multi.Cursor
{
    internal class Config
    {
        public static int LAST_TOUCH_COL = 14; // Total number of touch columns

        public static int LAST_LEFT_TOUCH_COL = 2; // Width of the left touch area (in columns)
        public static int LAST_RIGHT_TOUCH_COL = 2; // Width of the right touch area (in columns)
        public static int LAST_TOP_TOUCH_ROW = 5; // Width of the top touch area (in rows)

        // Mouse movement
        public static int TIME_CURSOR_MOVE_RESET = 2; // Cursor timer is reset after 2 seconds 
        public static double MIN_CURSOR_MOVE_MM = 5; // Minimum cursor movement to be considered as mouse moved 

        // -------------- Touch -----------------
        public static readonly int MIN_PRESSURE = 50; // Minimum value to consider touching the surface // Was 30 (seemed too sensitive)
        // --------------------------------------

        // -------------- Vel. Kalman Filter ----
        public static int FRAME_DUR_MS = 20; // Duration of each frame to pass to Kalman

        public static double AUX_VKF_PROCESS_NOISE = 50;
        public static double AUX_VKF_MEASURE_NOISE = 10;
        public static double AUX_BASE_GAIN = 10;       // Minimum movement amplification (adjust for small target selection)
        public static double AUX_SCALE_FACTOR = 10;    // Maximum gain at high speed (adjust for fast movement)
        public static double AUX_SENSITIVITY = 5;    // Controls how quickly gain increases (adjust for balance)

        public static double RAD_BEAM_VKF_PROCESS_NOISE_STD = 1.2;
        public static double RAD_BEAM_VKF_MEASURE_NOISE_STD = 15.0;
        public static double RAD_PLUS_VKF_PROCESS_NOISE_STD = 0.4;
        public static double RAD_PLUS_VKF_MEASURE_NOISE_STD = 40.0;
        // --------------------------------------

        // -------------- Cursors
        public static double MAPPING_GAIN = 1; // Was 50 // Let's not use it (KvF will take care of it)
                                               // --------------------------------------

        // --------------- Sizes and Margins
        public const double PPI = 109; // BenQ = 89, Apple Display = 109
        public static int SIDE_WINDOW_SIZE_MM = 50;
        public static double WINDOW_PADDING_MM = 2;
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
        public static double TAP_TIME_MS = 300; // Time to be down for Tap (200ms was too short)
        // --------------------------------------

        // --------------- Movement Thresholds --
        //public static double TAP_MOVE_LIMIT = 0.2; // Amount of allowed movement for Tap
        //public static float TAP_X_MOVE_LIMIT = 0.5f; // Amount of allowed X movement for Tap
        //public static float TAP_Y_MOVE_LIMIT = 0.5f; // Amount of allowed Y movement for Tap

        public static readonly (float DX, float DY) TAP_THUMB_THRESHOLD = (0.5f, 0.5f);
        public static readonly (float DX, float DY) TAP_INDEX_THRESHOLD = (0.5f, 0.5f);
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
        // --------------------------------------

    }
}
