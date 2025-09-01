using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multi.Cursor
{
    internal class Str
    {
        public static string START = "start";

        public static string TRIAL_SHOW = "trial_show";
        public static string FIRST_MOVE = "first_move";

        //public static string START_PRESS = "start_press";
        //public static string START_RELEASE = "start_release";

        public static string OBJ = "obj";
        public static string FUNCTION = "function";

        public static string PRESS = "press";
        public static string RELEASE = "release";

        public static string OBJ_PRESS = "obj_press";
        public static string OBJ_RELEASE = "obj_release";

        public static string FUNCTION_PRESS = "function_press";
        public static string FUNCTION_RELEASE = "function_release";

        public static string START_ENTER = "start_enter";
        //public static string START1_LAST_ENTRY = "start1_last_entry";
        //public static string START2_LAST_ENTRY = "start2_last_entry";
        //public static string START1_LAST_EXIT = "start1_last_entry";
        //public static string START2_LAST_EXIT = "start2_last_entry";
        public static string START_LEAVE = "start_leave";
        public static string START_PRESS = "start_press";
        public static string START_RELEASE = "start_release";

        //public static string START_PRESS_ONE = "start1_press";
        //public static string START_RELEASE_ONE = "start1_release";

        //public static string START_PRESS_TWO = "start2_press";
        //public static string START_RELEASE_TWO = "start2_release";

        public static string TARGET_ENTER = "target_enter";
        //public static string TARGET_LAST_ENTRY = "target_last_entry";
        public static string TARGET_PRESS = "target_press";
        public static string TARGET_RELEASE = "target_release";
        public static string TARGET_LEAVE = "target_leave";

        public static string MAIN_WIN_PRESS = "main_win_press";
        public static string MAIN_WIN_RELEASE = "main_win_release";

        public static string TRIAL_END = "trial_end"; // Second release on Start

        public static string TOUCH_MOUSE_TAP = "Touch-Mouse-Tap";
        public static string TOUCH_MOUSE_SWIPE = "Touch-Mouse-Swipe";
        public static string MOUSE = "Mouse";

        public static string PRACTICE = "Practice";
        public static string TEST = "Test";

        public static string MAJOR_LINE = "===============================================================================";
        public static string MINOR_LINE = "-------------------------------------------------------------------------------";

        public static string x3 = "3mm";
        public static string x6 = "6mm";
        public static string x12 = "12mm";
        public static string x15 = "15mm";
        public static string x18 = "18mm";
        public static string x20 = "20mm";
        public static string x30 = "30mm";
        public static string x36 = "36mm";
        public static string _52MM = "52mm";

        public static readonly string SHORT_DIST = "Short-dist";
        public static readonly string MID_DIST = "Middle-dist";
        public static readonly string LONG_DIST = "Long-dist";

        public static readonly string ToMo = "tomo";
        public static readonly string Mouse = "mouse";

        public static readonly string TRIAL_TIME = "Trial Time";

        public static string Join(params string[] parts)
        {
            return string.Join("_", parts);
        }

    }
}
