﻿using System;
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

        // -------------- Kalman Filter
        public static int FRAME_DUR_MS = 20; // Duration of each frame to pass to Kalman
        public static double KF_PROC_NOISE_STD = 1.0;
        public static double KF_MEASURE_NOISE_STD = 50.0;
        public static double BASE_GAIN = 1.5;       // Minimum movement amplification (adjust for small target selection)
        public static double SCALE_FACTOR = 4.0;    // Maximum gain at high speed (adjust for fast movement)
        public static double SENSITIVITY = 0.02;    // Controls how quickly gain increases (adjust for balance)
        public static double JUMP_THRESHOLD = 0.5;
        // --------------------------------------

        // -------------- Cursors
        public static double MAPPING_GAIN = 50;
        public static double TAP_TIME_MS = 200; // Time to be down for Tap
        // --------------------------------------

        // --------------- Sizes and Margins
        public static Size SCREEN_SIZE_MM = new Size(545, 302);
        public static int SIDE_WINDOW_SIZE_MM = 50;
        public static double WINDOW_MARGIN_MM = 2;
        // --------------------------------------

        // --------------- Screen
        public static Screen ACTIVE_SCREEN; // Set by MainWindow
        // --------------------------------------

        // --------------- Radiusor -------------
        public static double SENSITIVIY_FACTOR = 0.1;
        public static double RAD_GAIN = 30;
        // --------------------------------------

        // --------------- Times ----------------
        public static double SWIPE_TIME_MIN = 0.3; // sec
        public static double SWIPE_TIME_MAX = 0.6; // sec
        // --------------------------------------

        // --------------- Movement Thresholds --
        public static double MOVE_LIMIT = 0.5; // pts
        public static double MOVE_THRESHOLD = 0.7; // pts
        public static double HIGHT_MOVE_THRESHOLD = 2.5; // pts
        // --------------------------------------

        // --------------- Colors ---------------
        public static readonly Brush GRAY_E6E6E6 =
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E6E6E6"));
        public static readonly Brush GRAY_F3F3F3 =
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F3F3"));
        // --------------------------------------

    }
}
