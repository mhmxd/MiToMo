using static Common.Constants.ExpEnums;

namespace Common.Constants
{
    public class ExpStrs
    {
        public static readonly string START = "start";
        public static readonly string START_CAP = "START";
        public static readonly string END = "end";
        public static readonly string END_CAP = "END";

        public static readonly string TRIAL_SHOW = "trial_show";
        public static readonly string FIRST_MOVE = "first_move";

        public static readonly string OBJ = "obj";
        public static readonly string FUN = "fun";

        public static readonly string PRESS = "press";
        public static readonly string RELEASE = "release";

        public static readonly string OBJ_ENTER = "obj_enter";
        public static readonly string OBJ_PRESS = "obj_press";
        public static readonly string OBJ_RELEASE = "obj_release";
        public static readonly string OBJ_EXIT = "obj_exit";

        public static readonly string FUN_ENTER = "fun_enter";
        public static readonly string FUN_PRESS = "fun_press";
        public static readonly string FUN_RELEASE = "fun_release";
        public static readonly string FUN_EXIT = "fun_exit";
        public static readonly string FUN_DEMARKED = "fun_demarked";

        public static readonly string STR_ENTER = "start_enter";
        public static readonly string STR_EXIT = "str_exit";
        public static readonly string STR_PRESS = "str_press";
        public static readonly string STR_RELEASE = "str_release";

        public static readonly string END_ENTER = "end_enter";
        public static readonly string END_EXIT = "end_exit";
        public static readonly string END_PRESS = "end_press";
        public static readonly string END_RELEASE = "end_release";

        public static readonly string ARA_ENTER = "ara_enter";
        public static readonly string ARA_PRESS = "ara_press";
        public static readonly string ARA_RELEASE = "ara_release";
        public static readonly string ARA_EXIT = "ara_exit";

        public static readonly string PNL_ENTER = "pnl_enter";
        public static readonly string PNL_EXIT = "pnl_exit";
        public static readonly string PNL_PRESS = "pnl_press";
        public static readonly string PNL_RELEASE = "pnl_release";

        public static readonly string PNL_SELECT = "pnl_select"; // For ToMo (when gesture is registered)
        public static readonly string FUN_MARKED = "fun_marked";
        public static readonly string BTN_MARKED = "btn_marked";

        public static readonly string MAIN_WIN_PRESS = "main_win_press";
        public static readonly string MAIN_WIN_RELEASE = "main_win_release";

        public static readonly string TRIAL_END = "trial_end"; // Second release on Start

        public static readonly string TAP_C = "Tap";
        public static readonly string SWIPE_C = "Swipe";
        public static readonly string MOUSE_C = "Mouse";

        public static readonly string PRACTICE = "Practice";
        public static readonly string TEST = "Test";

        public static readonly string MAJOR_LINE = "===============================================================================";
        public static readonly string MINOR_LINE = "-------------------------------------------------------------------------------";

        public static readonly string x3 = "3mm";
        public static readonly string x6 = "6mm";
        public static readonly string x12 = "12mm";
        public static readonly string x15 = "15mm";
        public static readonly string x18 = "18mm";
        public static readonly string x20 = "20mm";
        public static readonly string x30 = "30mm";
        public static readonly string x36 = "36mm";
        public static readonly string _52MM = "52mm";

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
        public static readonly string TAP_XLEN = "tap_xlen";
        public static readonly string TAP_YLEN = "tap_ylen";
        public static readonly string SWIPE_XLEN = "swipe_xlen";
        public static readonly string SWIPE_YLEN = "swipe_ylen";

        // Task types
        public static readonly string SOSF = "sosf";
        public static readonly string SOMF = "somf";
        public static readonly string MOSF = "mosf";
        public static readonly string MOMF = "momf";
        public static readonly string FPS = "fps";
        public static readonly string MFS = "mfs";
        public static readonly string OBS = "obs";
        public static readonly string PNS = "pns";
        public static readonly string PNV = "pnv";

        // Logs
        public static readonly string TRIALS_DETAIL_S = "trials-detail";
        public static readonly string TRIALS_DETAIL_C = "Trials-Detail";
        public static readonly string TRIALS_TOTAL_S = "trials-total";
        public static readonly string TRIALS_TOTAL_C = "Trials-Total";
        public static readonly string BLOCKS_S = "blocks";
        public static readonly string BLOCKS_C = "Blocks";
        public static readonly string Logs = "Logs";
        public static readonly string CURSOR_S = "cursor";
        public static readonly string CURSOR_C = "Cursor";
        public static readonly string DATE_TIME_FORMAT = "yyyyMMdd-HHmm";

        // Task type abbreviations
        public static Dictionary<TaskType, string> TASKTYPE_ABBR = new Dictionary<TaskType, string>
        {
            {TaskType.ONE_OBJ_ONE_FUNC, SOSF },
            {TaskType.ONE_OBJ_MULTI_FUNC, SOMF },
            {TaskType.MULTI_OBJ_ONE_FUNC, MOSF },
            {TaskType.MULTI_OBJ_MULTI_FUNC, MOMF },

            {TaskType.FUNCTION_POINT_SELECT, FPS },
            {TaskType.MULTI_FUNCTION_SELECT, MFS },
            {TaskType.OBJECT_SELECT, OBS },
            {TaskType.PANEL_SELECT, PNS },
            {TaskType.PANEL_NAVIGATE, PNV },
        };

        public static string JoinUs(params string[] parts)
        {
            return string.Join("_", parts);
        }

        public static string JoinDot(params string[] parts)
        {
            return string.Join(".", parts);
        }
    }
}
