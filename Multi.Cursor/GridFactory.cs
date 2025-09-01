using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace Multi.Cursor
{
    internal class GridFactory
    {
        private static double ROW_HEIGHT = ButtonFactory.GetButtonHeight();

        private static double SMALL_BUTTON_W = Experiment.BUTTON_MULTIPLES[Str.x6] * Utils.MM2PX(Config.GRID_UNIT_MM);
        private static double WIDE_BUTTON_W = Experiment.BUTTON_MULTIPLES[Str.x18] * Utils.MM2PX(Config.GRID_UNIT_MM);
        private static double DROPDOWN_BUTTON_W = Experiment.BUTTON_MULTIPLES[Str.x3] * Utils.MM2PX(Config.GRID_UNIT_MM);

        private static double GUTTER_4PX = 1 * Utils.MM2PX(Config.GRID_UNIT_MM);
        private static double GUTTER_8PX = 2 * Utils.MM2PX(Config.GRID_UNIT_MM);
        private static double GUTTER_12PX = 3 * Utils.MM2PX(Config.GRID_UNIT_MM);
        private static double GUTTER_20PX = 5 * Utils.MM2PX(Config.GRID_UNIT_MM);

        private static double HORIZONTAL_GUTTER = 3 * Utils.MM2PX(Config.GRID_UNIT_MM);
        private static double VERRTICAL_GUTTER = 3 * Utils.MM2PX(Config.GRID_UNIT_MM);

        private static Rectangle CreateHorizontalGutter(double gutterH)
        {
            return new Rectangle
            {
                Width = 2, // Doesn't matter
                Height = gutterH,
                //Fill = Brushes.Orange, // <-- Make it highly visible for debugging
                //Stroke = Brushes.Black, // Add a stroke
                //StrokeThickness = 0.5,
                HorizontalAlignment = HorizontalAlignment.Stretch // This will stretch the gutter to fill the row height
            };
        }

        private static Rectangle CreateVerticalGutter(double gutterW)
        {
            return new Rectangle
            {
                Width = gutterW, 
                Height = 2, // Doesn't matter
                //Fill = Brushes.Orange, // <-- Make it highly visible for debugging
                //Stroke = Brushes.Black, // Add a stroke
                //StrokeThickness = 0.5,
                HorizontalAlignment = HorizontalAlignment.Stretch // This will stretch the gutter to fill the row height
            };
        }

        public static Grid CreateTopModerateGrid()
        {
            Grid grid = new Grid { UseLayoutRounding = true, Height = ROW_HEIGHT * 2 + GUTTER_8PX }; // Ensure UseLayoutRounding is on the Grid

            // Row1
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ROW_HEIGHT) });
            UIElement element = CreateModerateRow1();
            Grid.SetRow(element, 0);
            grid.Children.Add(element);

            // Horizontal gutter
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(GUTTER_8PX) });
            element = CreateHorizontalGutter(GUTTER_8PX);
            Grid.SetRow(element, 1);
            grid.Children.Add(element);

            // Row2
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ROW_HEIGHT) });
            element = CreateModerateRow2();
            Grid.SetRow(element, 2);
            grid.Children.Add(element);

            return grid;
        }

        public static Grid CreateSideModerateGrid()
        {
            double column1Width = 6 * Utils.MM2PX(Config.GRID_UNIT_MM);
            double column2Width = 30 * Utils.MM2PX(Config.GRID_UNIT_MM);

            Grid grid = new Grid { UseLayoutRounding = true, Width = column1Width + GUTTER_12PX + column2Width }; // Ensure UseLayoutRounding is on the Grid

            // Column1
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(column1Width) });
            UIElement element = CreateModerateColumn1();
            Grid.SetColumn(element, 0);
            grid.Children.Add(element);

            // Vertical gutter
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(GUTTER_12PX) });
            element = CreateVerticalGutter(GUTTER_12PX);
            Grid.SetColumn(element, 1);
            grid.Children.Add(element);

            // Column2
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(column2Width) });
            element = CreateModerateColumn2();
            Grid.SetColumn(element, 2);
            grid.Children.Add(element);

            return grid;
        }

        public static Grid CreateSideComplexGrid()
        {
            double maxW = 5 * SMALL_BUTTON_W + WIDE_BUTTON_W + DROPDOWN_BUTTON_W + 5 * GUTTER_4PX;

            Grid grid = new Grid { UseLayoutRounding = true }; // Ensure UseLayoutRounding is on the Grid
            Rectangle gutter;
            Grid group;
            int rowInd = -1;

            //-- Add groups and gutters
            // Group1
            group = CreateSideComplexGroup1();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            Grid.SetRow(group, ++rowInd);
            grid.Children.Add(group);
            // Horizontal gutter
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(GUTTER_20PX) });
            gutter = CreateHorizontalGutter(GUTTER_20PX);
            Grid.SetRow(gutter, ++rowInd);
            grid.Children.Add(gutter);
            // Group2
            group = CreateSideComplexGroup2();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            Grid.SetRow(group, ++rowInd);
            grid.Children.Add(group);

            return grid;
        }

        private static StackPanel CreateModerateRow1()
        {
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            stackPanel.Children.Add(ButtonFactory.CreateWideButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(ButtonFactory.CreateDropdownButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateWideButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(ButtonFactory.CreateDropdownButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateWideButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(ButtonFactory.CreateDropdownButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateWideButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(ButtonFactory.CreateDropdownButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateWideButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(ButtonFactory.CreateDropdownButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(ButtonFactory.CreateDropdownButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateWideButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(ButtonFactory.CreateDropdownButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateWideButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());

            return stackPanel;
        }

        private static StackPanel CreateModerateRow2()
        {
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(ButtonFactory.CreateDropdownButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateWideButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(ButtonFactory.CreateDropdownButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateWideButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateWideButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(ButtonFactory.CreateDropdownButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateWideButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(ButtonFactory.CreateDropdownButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateWideButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateWideButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(ButtonFactory.CreateDropdownButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(ButtonFactory.CreateDropdownButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(ButtonFactory.CreateDropdownButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_8PX));
            stackPanel.Children.Add(ButtonFactory.CreateWideButton());

            return stackPanel;
        }

        private static StackPanel CreateModerateColumn1()
        {
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                //VerticalAlignment = VerticalAlignment.Stretch
            };

            for (int i = 0; i < 9; i++)
            {
                stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
                stackPanel.Children.Add(CreateHorizontalGutter(GUTTER_12PX));
            }
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());

            return stackPanel;
        }

        private static StackPanel CreateModerateColumn2()
        { 
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                //VerticalAlignment = VerticalAlignment.Stretch
            };

            for (int i = 0; i < 9; i++)
            {
                stackPanel.Children.Add(ButtonFactory.CreateWiderButton());
                stackPanel.Children.Add(CreateHorizontalGutter(GUTTER_12PX));
            }
            stackPanel.Children.Add(ButtonFactory.CreateWiderButton());

            return stackPanel;
        }

        private static Grid CreateSideComplexGroup1()
        {
            double maxW = 2 * SMALL_BUTTON_W + 2 * WIDE_BUTTON_W + DROPDOWN_BUTTON_W + 3 * GUTTER_4PX;

            Grid grid = new Grid { UseLayoutRounding = true }; // Ensure UseLayoutRounding is on the Grid
            StackPanel stackPanel;
            Rectangle gutter;

            int rowInd = 0;

            // Row1
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ROW_HEIGHT) });
            stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            stackPanel.Children.Add(ButtonFactory.CreateWiderButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_4PX));
            stackPanel.Children.Add(ButtonFactory.CreateWideButton());
            Grid.SetRow(stackPanel, rowInd);
            grid.Children.Add(stackPanel);

            rowInd++;

            // Horizontal gutter
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(GUTTER_4PX) });
            gutter = new Rectangle{ Height = GUTTER_4PX };
            Grid.SetRow(gutter, rowInd);
            grid.Children.Add(gutter);

            rowInd++;

            // Row2
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ROW_HEIGHT) });
            stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            stackPanel.Children.Add(ButtonFactory.CreateWideButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_4PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_4PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_4PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_4PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(ButtonFactory.CreateDropdownButton());
            Grid.SetRow(stackPanel, rowInd);
            grid.Children.Add(stackPanel);

            rowInd++;

            // Horizontal gutter
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(GUTTER_4PX) });
            gutter = new Rectangle { Height = GUTTER_4PX };
            Grid.SetRow(gutter, rowInd);
            grid.Children.Add(gutter);

            rowInd++;

            // Row3
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ROW_HEIGHT) });
            stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_4PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_4PX));
            stackPanel.Children.Add(ButtonFactory.CreateWideButton());
            stackPanel.Children.Add(ButtonFactory.CreateDropdownButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_4PX));
            stackPanel.Children.Add(ButtonFactory.CreateWideButton());
            Grid.SetRow(stackPanel, rowInd);
            grid.Children.Add(stackPanel);

            return grid;
        }

        private static Grid CreateSideComplexGroup2()
        {
            double maxW = 5 * SMALL_BUTTON_W + WIDE_BUTTON_W + 5 * GUTTER_4PX;

            Grid grid = new Grid { UseLayoutRounding = true }; // Ensure UseLayoutRounding is on the Grid
            StackPanel stackPanel;
            Rectangle gutter;

            int rowInd = 0;

            // Row1
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ROW_HEIGHT) });
            stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            for (int i = 0; i < 4; i++)
            {
                stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
                stackPanel.Children.Add(CreateVerticalGutter(GUTTER_4PX));
            }
            stackPanel.Children.Add(ButtonFactory.CreateWideButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_4PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            Grid.SetRow(stackPanel, rowInd);
            grid.Children.Add(stackPanel);

            rowInd++;

            // Horizontal gutter
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(GUTTER_4PX) });
            gutter = new Rectangle { Height = GUTTER_4PX };
            Grid.SetRow(gutter, rowInd);
            grid.Children.Add(gutter);

            rowInd++;

            // Row2
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ROW_HEIGHT) });
            stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_4PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(ButtonFactory.CreateDropdownButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_4PX));
            stackPanel.Children.Add(ButtonFactory.CreateSmallButton());
            stackPanel.Children.Add(CreateVerticalGutter(GUTTER_4PX));
            stackPanel.Children.Add(ButtonFactory.CreateWiderButton());
            Grid.SetRow(stackPanel, rowInd);
            grid.Children.Add(stackPanel);

            return grid;
        }
    }
}
