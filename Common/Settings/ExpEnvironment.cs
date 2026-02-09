namespace Common.Settings
{
    public class ExpEnvironment
    {
        public static readonly int PTC_NUM = 70;

        public static readonly double PPI = 93.54; // BenQ = 93.54; Apple = 109

        //-- Mouse settings --------------------------------------------
        public static int TIME_CURSOR_MOVE_RESET = 2; // Cursor timer is reset after 2 seconds 
        public static double MIN_CURSOR_MOVE_MM = 5; // Minimum cursor movement to be considered as mouse moved 

        //-- Touch settings --------------------------------------------
        public static readonly int MIN_PRESSURE = 50; // Minimum value to consider touching the surface // Was 30 (seemed too sensitive)
        public static readonly int MIN_TOTAL_PRESSURE = 500; // Min value to consider a finger // Was 2 * MIN_PRESSURE
        public static byte LOCAL_MINIMA_DROP_THRESHOLD = 70;
        public static double SWIPE_TIME_MAX = 500; // ms (was 600)
        public static double TAP_TIME_MAX = 200; // Time to be down for Tap (200ms was too short)
        public static double TAP_TIME_MIN = 70; // Minimum Time to be down for Tap

        public static readonly (float DX, float DY) TAP_GENERAL_THRESHOLD = (0.2f, 0.2f);
        public static readonly (float DX, float DY) TAP_THUMB_THRESHOLD = (0.5f, 0.5f);
        public static readonly (float DX, float DY) TAP_INDEX_THRESHOLD = (0.5f, 0.5f); // Was 0.2f
        public static readonly (float DX, float DY) TAP_MIDDLE_THRESHOLD = (0.7f, 0.7f); // For better recognition of Taps
        public static readonly (float DX, float DY) TAP_RING_THRESHOLD = (0.5f, 0.5f);
        public static readonly (float DX, float DY) TAP_PINKY_THRESHOLD = (0.5f, 0.5f);

        public static readonly float SWIPE_MOVE_THRESHOLD = 0.8f; // pts

        //-- Grid navigation -------------------------------------------
        public static int CELL_WIDTH_THRESHOLD = 50; // px
        public static int CELL_HEIGHT_THRESHOLD = 50; // px

        //-- Vel. Kalman Filter ----------------------------------------
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

        public static double MAPPING_GAIN = 1; // Was 50 // Let's not use it (KvF will take care of it)

    }


}
