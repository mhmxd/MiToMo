using Common.Constants;
using Common.Helpers;
using Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Common.Helpers.ExpUtils;

namespace SubTask.FunctionSelection
{
    internal class ButtonFactory
    {
        private static double UNIT = MM2PX(Config.GRID_UNIT_MM); // Unit of measurement for the grid (1mm = 4px)
        private static double BUTTON_HEIGHT = 6 * UNIT; // Height of each row in pixels

        public static SButton CreateButton(string widthX, int row, int col)
        {
            int wMultiple = ExpLayouts.BUTTON_MULTIPLES[widthX]; // 3 x Unit
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the widthX of the button in the grid 
                Width = wMultiple * UNIT, // BUTTON_WIDTHS_MULTIPLES[0] is defined in Experiment
                Height = BUTTON_HEIGHT, // Height in pixels
                RowCol = new ExpGridPos(row, col)
            };

            return sButton;
        }

        public static double GetButtonHeight()
        {
            return BUTTON_HEIGHT;
        }
    }
}
