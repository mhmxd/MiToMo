namespace Common.Settings
{
    public class ExpDesign
    {
        // Large Tasks
        public static readonly int MainTaskNumObj = 3;
        public static readonly int MainTaskNumFunc = 3;
        public static readonly int MainTaskNumBlocks = 8;
        public static readonly int MainTaskShowBreakAfterBlocks = 2; // After each 2 blocks

        // Subtask: Function Point-and-Select
        public static readonly int FuncPointSelectNumBlocks = 8;
        public static readonly int FuncPointSelectBreakAfterBlocks = (FuncPointSelectNumBlocks * 3) / 2; // Halfway (3 for complexities)

        // Subtask: Multi Function Selection
        public static readonly int MultiFuncSelectNumFunc = 3;
        public static readonly int MultiFuncSelectNumBlocks = 8;
        public static readonly int MutliFuncSelectBreakAfterBlocks = (MultiFuncSelectNumBlocks * 3) / 2; // Halfway (3 for complexities)

        // Subtask: Object Selection
        public static readonly int[] ObjSelectNumObjects = { 3, 5 };
        public static readonly int ObjectSelectNumBlocks = 8;
        public static readonly int ObjectSelectNumRep = 1; // No repetitions. Just 8 of everything

        // Subtask: Panel Selection
        public static readonly int PaneSelectNumBlocks = 8;
        public static readonly int PaneSelBreakAfterBlocks = (PaneSelectNumBlocks * 3) / 2; // Halfway (3 for complexities)

        // Subtask: Panel Navigation
        public static readonly int PaneNavNumBlocks = 8;
        public static readonly int PaneNavBreakAfterBlocks = (PaneNavNumBlocks * 3) / 2; // Halfway (3 for complexities)
    }
}
