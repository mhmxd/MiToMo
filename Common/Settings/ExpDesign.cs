namespace Common.Settings
{
    public class ExpDesign
    {
        // Large Tasks
        public static readonly int LT_N_MULTI_OBJ = 3;
        public static readonly int LT_N_MULTI_FUN = 3;
        public static readonly int LT_N_BLOCKS = 8;

        // Subtask: Function Point-and-Select
        public static readonly int FPS_N_BLOCKS = 8;

        // Subtask: Multi Function Selection
        public static readonly int MultiFuncSelectNumFunc = 3;
        public static readonly int MultiFuncSelectNumBlocks = 8;
        public static readonly int MutliFuncSelectBreakAfterBlocks = 2;

        // Subtask: Object Selection
        public static readonly int[] ObjSelectNumObjects = { 3, 5 };
        public static readonly int ObjectSelectNumBlocks = 8;
        public static readonly int ObjectSelectNumRep = 1; // No repetitions. Just 8 of everything

        // Subtask: Panel Selection
        public static readonly int PaneSelectNumBlocks = 4;
        public static readonly int PaneSelBreakAfterBlocks = 2;

        // Subtask: Panel Navigation
        public static readonly int PaneNavNumBlocks = 2;
        public static readonly int PaneNavBreakAfterBlocks = 1;
    }
}
