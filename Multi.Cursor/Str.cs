using System.Collections.Generic;

namespace Multi.Cursor
{
    internal class Str
    {
        public static string START = "start";

        public static string TRIAL_SHOW = "trial_show";
        public static string FIRST_MOVE = "move";

        //public static string STR_PRESS = "start_press";
        //public static string STR_RELEASE = "start_release";

        public static string OBJ = "obj";
        public static string FUN = "fun";

        public static string PRESS = "press";
        public static string RELEASE = "release";

        public static string OBJ_ENTER = "obj_enter";
        public static string OBJ_PRESS = "obj_press";
        public static string OBJ_RELEASE = "obj_release";
        public static string OBJ_EXIT = "obj_exit";

        public static string FUN_ENTER = "fun_enter";
        public static string FUN_PRESS = "fun_press";
        public static string FUN_RELEASE = "fun_release";
        public static string FUN_EXIT = "fun_exit";

        public static string STR_ENTER = "start_enter";
        //public static string START1_LAST_ENTRY = "start1_last_entry";
        //public static string START2_LAST_ENTRY = "start2_last_entry";
        //public static string START1_LAST_EXIT = "start1_last_entry";
        //public static string START2_LAST_EXIT = "start2_last_entry";
        public static string STR_EXIT = "str_exit";
        public static string STR_PRESS = "str_press";
        public static string STR_RELEASE = "str_release";

        public static string ARA_ENTER = "ara_enter";
        public static string ARA_PRESS = "ara_press";
        public static string ARA_RELEASE = "ara_release";
        public static string ARA_EXIT = "ara_exit";

        public static string PNL_ENTER = "pnl_enter";
        public static string PNL_EXIT = "pnl_exit";

        //public static string START_PRESS_ONE = "start1_press";
        //public static string START_RELEASE_ONE = "start1_release";

        //public static string START_PRESS_TWO = "start2_press";
        //public static string START_RELEASE_TWO = "start2_release";

        //public static string FUN_ENTER = "fun_enter";
        //public static string TARGET_LAST_ENTRY = "target_last_entry";
        //public static string TARGET_PRESS = "target_press";
        //public static string TARGET_RELEASE = "target_release";
        //public static string TARGET_LEAVE = "target_leave";

        public static string FUN_MARKED = "fun_marked";

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

        // Fingers
        public static readonly string INDEX = "index";
        public static readonly string MIDDLE = "middle";
        public static readonly string RING = "ring";
        public static readonly string PINKY = "pinky";
        public static readonly string THUMB = "thumb";

        // Gestures
        public static readonly string TAP = "tap";
        public static readonly string TAP_DOWN = "tap_down";
        public static readonly string TAP_UP = "tap_up";
        public static readonly string DOWN = "down";
        public static readonly string UP = "up";
        public static readonly string SWIPE_START = "swipe_start";
        public static readonly string SWIPE_END = "swipe_end";
        public static readonly string FLICK = "flick";

        // Task type abbreviations
        public static Dictionary<TaskType, string> TASKTYPE_ABBR = new Dictionary<TaskType, string>
        {
            {TaskType.ONE_OBJ_ONE_FUNC, "sosf" },
            {TaskType.ONE_OBJ_MULTI_FUNC, "somf" },
            {TaskType.MULTI_OBJ_ONE_FUNC, "mosf" },
            {TaskType.MULTI_OBJ_MULTI_FUNC, "momf" },
        };


        public static string Join(params string[] parts)
        {
            return string.Join("_", parts);
        }

        public static bool IsGesture(string str)
        {
            return str == TAP_DOWN || str == TAP_UP || str == SWIPE_START || str == SWIPE_END;
        }

        public static string GetIndexedStr(string str, int num)
        {
            return str.Insert(3, num.ToString());
        }

        public static string GetCountedStr(string str, int count)
        {
            return str + count.ToString();
        }

    }
}
