using Common.Constants;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using static Common.Helpers.ExpUtils;

namespace SubTask.Panel.Selection
{
    public class Experiment
    {

        //--- Flags
        public bool MULTI_FUNC_SAME_W = false;
        public Trial_Action OUTSIDE_OBJECT_PRESS = Trial_Action.CONTINUE;
        public Trial_Action OUTSIDE_AREA_PRESS = Trial_Action.CONTINUE;
        public Trial_Action MARKER_NOT_ON_FUNCTION_OBJECT_PRESS = Trial_Action.CONTINUE;

        //--- Setting
        private readonly int N_BLOCKS = 3;
        private readonly int N_REP = 4; // Number of repetitions inside each block (total = 12 repetitions)
        public static int DEFAULT_PTC = 1000;
        public Technique Active_Technique = Technique.TOMO_TAP; // Set in the info dialog
        public Complexity Active_Complexity = Complexity.Simple; // Set in the info dialog

        //--- Variables
        private static List<double> TARGET_WIDTHS_MM = new List<double>() { 4, 12, 20 }; // BenQ
        //private static List<double> TARGET_WIDTHS_MM = new List<double>() { 4, 9, 18 }; // Apple Display
        private static List<double> GRID_TARGET_WIDTHS_MM = new List<double>() { 3, 12, 30 }; // BenQ
        public static Dictionary<string, int> BUTTON_MULTIPLES = new Dictionary<string, int>()
        {
            { Str.x3, 3 },
            { Str.x6, 6 },
            { Str.x12, 12 },
            { Str.x15, 15 },
            { Str.x18, 18 },
            { Str.x30, 30 },
            { Str.x36, 36 }
        };

        public static Dictionary<Complexity, Dictionary<Side, List<int>>> BUTTON_WIDTHS = new Dictionary<Complexity, Dictionary<Side, List<int>>>()
        {
            {
                Complexity.Simple, new Dictionary<Side, List<int>>()
                {
                    { Side.Top, new List<int>() { 6, 18 } },
                    { Side.Left, new List<int>() { 36 } },
                    { Side.Right, new List<int>() { 36 } }
                }
            },

            {
                Complexity.Moderate, new Dictionary<Side, List<int>>()
                {
                    { Side.Top, new List<int>() { 3, 6, 18 } },
                    { Side.Left, new List<int>() { 6, 30 } },
                    { Side.Right, new List<int>() { 6, 30 } }
                }
            },
            {
                Complexity.Complex, new Dictionary<Side, List<int>>()
                {
                    { Side.Top, new List<int>() { 3, 6, 18, 30 } },
                    { Side.Left, new List<int>() { 3, 6, 18, 30 } },
                    { Side.Right, new List<int>() { 3, 6, 18, 30 } }
                }
            }
        };

        public static double START_WIDTH_MM = Config.EXCEL_CELL_W;

        //-- Colors
        public static readonly Brush START_INIT_COLOR = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(ExpColors.PURPLE));

        //-- Information

        public int Participant_Number { get; set; } // Set in the info dialog

        private readonly List<Block> _blocks = new();
        public List<Block> Blocks { get { return _blocks; } }

        public Experiment()
        {
            Participant_Number = DEFAULT_PTC; // Default
        }

        public void Init(int ptc, string tech, Complexity complexity)
        {
            this.TrialInfo($"Participant: {ptc}, Technique: {tech}");
            Participant_Number = ptc;
            if (tech == Str.TOUCH_MOUSE_TAP)
            {
                Active_Technique = Technique.TOMO_TAP;
                Config.SetMode(0);
            }
            else if (tech == Str.TOUCH_MOUSE_SWIPE)
            {
                Active_Technique = Technique.TOMO_SWIPE;
                Config.SetMode(1);
            }
            else if (tech == Str.MOUSE)
            {
                Active_Technique = Technique.MOUSE;
            }

            Active_Complexity = complexity;

            // Create and add blocks
            for (int i = 0; i < N_BLOCKS; i++)
            {
                int blockId = Participant_Number * 100 + i + 1;
                Block block = Block.CreateBlock(Active_Technique, Participant_Number, blockId, complexity, N_REP);
                _blocks.Add(block);
            }
        }

        public int GetNumBlocks()
        {
            return _blocks.Count;
        }

        public Block GetBlock(int blockNum)
        {
            int index = blockNum - 1;
            if (index < _blocks.Count()) return _blocks[index];
            else return null;
        }

        public static double GetMinTargetWidthMM()
        {
            return TARGET_WIDTHS_MM.First();
        }

        public static int GetNumGridTargetWidths()
        {
            return GRID_TARGET_WIDTHS_MM.Count;
        }

        public static List<double> GetGridTargetWidthsMM()
        {
            return GRID_TARGET_WIDTHS_MM;
        }

        public static double GetGridMinTargetWidthMM()
        {
            return GRID_TARGET_WIDTHS_MM.Min();
        }

        public static int GetStartHalfWidth()
        {
            return MM2PX(START_WIDTH_MM / 2);
        }
    }
}
