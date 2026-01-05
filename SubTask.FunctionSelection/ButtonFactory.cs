using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Constants;
using static Common.Helpers.ExpUtils;

namespace SubTask.FunctionSelection
{
    internal class ButtonFactory
    {
        private static double UNIT = MM2PX(Config.GRID_UNIT_MM); // Unit of measurement for the grid (1mm = 4px)
        private static double BUTTON_HEIGHT = 6 * UNIT; // Height of each row in pixels

        public static SButton CreateButton(int widthX)
        {
            SButton sButton = new SButton
            {
                WidthMultiple = widthX, // Width ID for the button, used to identify the widthX of the button in the grid 
                Width = widthX * UNIT, // BUTTON_WIDTHS_MULTIPLES[0] is defined in Experiment
                Height = BUTTON_HEIGHT // Height in pixels
            };

            return sButton;
        }

        public static SButton CreateDropdownButton() // The dropdown part!
        {
            int wMultiple = ExpSizes.BUTTON_MULTIPLES[ExpStrs.x3]; // 3 x Unit
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the widthX of the button in the grid 
                Width = wMultiple * UNIT, // BUTTON_WIDTHS_MULTIPLES[0] is defined in Experiment
                Height = BUTTON_HEIGHT // Height in pixels
            };
            return sButton;
        }

        public static SButton CreateX6Button()
        {
            int wMultiple = ExpSizes.BUTTON_MULTIPLES[ExpStrs.x6];
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the widthX of the button in the grid 
                Width = wMultiple * UNIT, // BUTTON_WIDTHS_MULTIPLES[1] is defined in Experiment
                Height = BUTTON_HEIGHT // Height in pixels
            };
            return sButton;
        }

        public static SButton CreateX18Button()
        {
            int wMultiple = ExpSizes.BUTTON_MULTIPLES[ExpStrs.x18]; // 72 px
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the widthX of the button in the grid 
                Width = wMultiple * UNIT, // BUTTON_WIDTHS_MULTIPLES[3] is defined in Experiment
                Height = BUTTON_HEIGHT // Height in pixels
            };
            return sButton;
        }

        public static SButton CreateX30Button()
        {
            int wMultiple = ExpSizes.BUTTON_MULTIPLES[ExpStrs.x30]; // 120 px
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the widthX of the button in the grid 
                Width = wMultiple * UNIT, // BUTTON_WIDTHS_MULTIPLES[4] is defined in Experiment
                Height = BUTTON_HEIGHT // Height in pixels
            };
            return sButton;
        }

        public static SButton CreateX36Button()
        {
            int wMultiple = ExpSizes.BUTTON_MULTIPLES[ExpStrs.x36]; // 144 px
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the widthX of the button in the grid 
                Width = wMultiple * UNIT, // BUTTON_WIDTHS_MULTIPLES[4] is defined in Experiment
                Height = BUTTON_HEIGHT // Height in pixels
            };
            return sButton;
        }

        public static double GetButtonHeight()
        {
            return BUTTON_HEIGHT;
        }
    }
}
