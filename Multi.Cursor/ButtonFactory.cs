using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multi.Cursor
{
    internal class ButtonFactory
    {
        private static double UNIT = Utils.MM2PX(Config.GRID_UNIT_MM); // Unit of measurement for the grid (1mm = 4px)
        private static double BUTTON_HEIGHT = 6 * UNIT; // Height of each row in pixels

        public static SButton CreateDropdownButton() // The dropdown part!
        {
            int wMultiple = Experiment.BUTTON_MULTIPLES[Str.x3]; // 3 x Unit
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the width of the button in the grid 
                Width = wMultiple * UNIT, // BUTTON_WIDTHS_MULTIPLES[0] is defined in Experiment
                Height = BUTTON_HEIGHT // Height in pixels
            };
            return sButton;
        }

        public static SButton CreateSmallButton()
        {
            int wMultiple = Experiment.BUTTON_MULTIPLES[Str.x6];
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the width of the button in the grid 
                Width = wMultiple * UNIT, // BUTTON_WIDTHS_MULTIPLES[1] is defined in Experiment
                Height = BUTTON_HEIGHT // Height in pixels
            };
            return sButton;
        }

        public static SButton CreateWideButton()
        {
            int wMultiple = Experiment.BUTTON_MULTIPLES[Str.x18]; // 72 px
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the width of the button in the grid 
                Width = wMultiple * UNIT, // BUTTON_WIDTHS_MULTIPLES[3] is defined in Experiment
                Height = BUTTON_HEIGHT // Height in pixels
            };
            return sButton;
        }

        public static SButton CreateWiderButton()
        {
            int wMultiple = Experiment.BUTTON_MULTIPLES[Str.x30]; // 120 px
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the width of the button in the grid 
                Width = wMultiple * UNIT, // BUTTON_WIDTHS_MULTIPLES[4] is defined in Experiment
                Height = BUTTON_HEIGHT // Height in pixels
            };
            return sButton;
        }

        public static SButton CreateWidestButton()
        {
            int wMultiple = Experiment.BUTTON_MULTIPLES[Str.x36]; // 144 px
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the width of the button in the grid 
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
