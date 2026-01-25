using Common.Constants;
using Common.Settings;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using static Common.Constants.ExpEnums;
using static Common.Helpers.Tools;

namespace SubTask.PanelNavigation
{
    public class Experiment
    {

        //--- Setting
        public Technique Active_Technique = Technique.TOMO; // Set in the intro dialog
        public Complexity Active_Complexity = Complexity.Simple; // Set in the intro dialog
        public ExperimentType Active_Type = ExperimentType.Practice; // Set from the intro dialog

        //--- Variables
        private static List<double> TARGET_WIDTHS_MM = new List<double>() { 4, 12, 20 }; // BenQ
        //private static List<double> TARGET_WIDTHS_MM = new List<double>() { 4, 9, 18 }; // Apple Display
        private static List<double> GRID_TARGET_WIDTHS_MM = new List<double>() { 3, 12, 30 }; // BenQ
        

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


        public static double START_WIDTH_MM = ExpSizes.EXCEL_CELL_W;

        //-- Colors
        public static readonly Brush START_INIT_COLOR = new SolidColorBrush(
            (Color)ColorConverter.ConvertFromString(ExpColors.PURPLE));


        //-- Information
        //public int Participant_Number { get; set; } // Set in the info dialog

        private readonly List<Block> _blocks = new();
        public List<Block> Blocks { get { return _blocks; } }

        public Experiment()
        {
            //Participant_Number = DEFAULT_PTC; // Default
        }

        public void Init(Complexity complexity, ExperimentType expType)
        {
            this.TrialInfo($"Participant: {ExpEnvironment.PTC_NUM}");
            //Participant_Number = ptc;
            //if (tech == ExpStrs.TOUCH_MOUSE_TAP)
            //{
            //    Active_Technique = Technique.TOMO_TAP;
            //    Config.SetMode(0);
            //}
            //else if (tech == ExpStrs.TOUCH_MOUSE_SWIPE)
            //{
            //    Active_Technique = Technique.TOMO_SWIPE;
            //    Config.SetMode(1);
            //}
            //else if (tech == ExpStrs.MOUSE)
            //{
            //    Active_Technique = Technique.MOUSE;
            //}

            Active_Complexity = complexity;
            Active_Type = expType;

            // Create and add blocks
            for (int i = 0; i < ExpDesign.PN_N_BLOCKS; i++)
            {
                int blockId = ExpEnvironment.PTC_NUM * 100 + i + 1;
                Block block = Block.CreateBlock(
                    Active_Technique, ExpEnvironment.PTC_NUM, 
                    blockId, complexity, expType, 
                    ExpDesign.PN_N_REP);
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
