//using ExCSS;
using Common.Constants;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using static Common.Helpers.ExpUtils;

namespace SubTask.Panel.Selection
{
    internal class ColumnFactory : Grid  // Inherits from Grid to use WPF's Grid layout capabilities
    {
        private static double WithinGroupGutter = MM2PX(Config.GUTTER_05MM); // Space in-between the grid elements
        private static double UNIT = MM2PX(Config.GRID_UNIT_MM); // Unit of measurement for the grid (1mm = 4px)
        private static double ROW_HEIGHT = MM2PX(Config.GRID_ROW_HEIGHT_MM); // Height of each row in pixels

        public static double MAX_GROUP_WITH = MM2PX(2 * ExpSizes.BUTTON_MULTIPLES[ExpStrs.x15] + Config.GUTTER_05MM); // Maximum width of the group in pixels
        public static double COLUMN_HEIGHT = MM2PX(3 * Config.GRID_ROW_HEIGHT_MM + 2 * Config.GUTTER_05MM);

        private static Rectangle CreateInRowGutter()
        {
            return new Rectangle
            {
                Width = WithinGroupGutter, // Use WithinGroupGutter for width, not a derived UNIT value unless intentional
                Height = ROW_HEIGHT,
                //Fill = Brushes.Orange, // <-- Make it highly visible for debugging
                //Stroke = Brushes.Black, // Add a stroke
                //StrokeThickness = 0.5,
                HorizontalAlignment = HorizontalAlignment.Left // Consistent alignment
            };
        }

        private static Rectangle CreateInColumnGutter()
        {
            return new Rectangle
            {
                // Width will be set to HorizontalAlignment.Stretch in CreateCol1
                Height = WithinGroupGutter,
                //Fill = Brushes.Red, // <-- Make it highly visible for debugging
                //Stroke = Brushes.Green,
                //StrokeThickness = 0.5,
                HorizontalAlignment = HorizontalAlignment.Stretch // This should be set in CreateCol1 too
            };
        }

        private static Rectangle CreateGutter(double gutterMM)
        {
            return new Rectangle
            {
                Height = MM2PX(gutterMM), // Use WithinGroupGutter for width, not a derived UNIT value unless intentional
                //Fill = Brushes.Orange, // <-- Make it highly visible for debugging
                //Stroke = Brushes.Black, // Add a stroke
                //StrokeThickness = 0.5,
                //HorizontalAlignment = HorizontalAlignment.Stretch // This will stretch the gutter to fill the row height
            };
        }

        private static SButton CreateBigButton()
        {
            int wMultiple = ExpSizes.BUTTON_MULTIPLES[ExpStrs.x15];
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the width of the button in the grid 
                Width = MM2PX(wMultiple * Config.GRID_UNIT_MM), // BUTTON_WIDTHS_MULTIPLES[5] is defined in Experiment
                Height = MM2PX(19 * Config.GRID_UNIT_MM) // 19 * UNIT is the height in pixels
            };
            return sButton;
        }

        private static SButton CreateSmallButton()
        {
            int wMultiple = ExpSizes.BUTTON_MULTIPLES[ExpStrs.x6];
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the width of the button in the grid 
                Width = MM2PX(wMultiple * Config.GRID_UNIT_MM), // BUTTON_WIDTHS_MULTIPLES[1] is defined in Experiment
                Height = ROW_HEIGHT // Height in pixels
            };
            return sButton;
        }

        private static SButton CreateDropdownButton()
        {
            int wMultiple = ExpSizes.BUTTON_MULTIPLES[ExpStrs.x3]; // Assuming 0 is the index for dropdown button width
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the width of the button in the grid 
                Width = MM2PX(wMultiple * Config.GRID_UNIT_MM), // BUTTON_WIDTHS_MULTIPLES[0] is defined in Experiment
                Height = ROW_HEIGHT // Height in pixels
            };
            return sButton;
        }

        private static SButton CreateWideButton()
        {
            int wMultiple = ExpSizes.BUTTON_MULTIPLES[ExpStrs.x18]; // Assuming 3 is the index for wide button width
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the width of the button in the grid 
                Width = MM2PX(wMultiple * Config.GRID_UNIT_MM), // BUTTON_WIDTHS_MULTIPLES[3] is defined in Experiment
                Height = ROW_HEIGHT // Height in pixels
            };
            return sButton;
        }

        private static SButton CreateWiderButton()
        {
            int wMultiple = ExpSizes.BUTTON_MULTIPLES[ExpStrs.x30]; // Assuming 4 is the index for wider button width
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the width of the button in the grid 
                Width = MM2PX(wMultiple * Config.GRID_UNIT_MM),
                Height = ROW_HEIGHT // Height in pixels
            };
            return sButton;
        }

        private static SButton CreateWidestButton()
        {
            int wMultiple = ExpSizes.BUTTON_MULTIPLES[ExpStrs.x36]; // Assuming 4 is the index for wider button width
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the width of the button in the grid 
                Width = MM2PX(wMultiple * Config.GRID_UNIT_MM),
                Height = ROW_HEIGHT // Height in pixels
            };
            return sButton;
        }

        public static StackPanel CreateSimpleColumn()
        {
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            for (int i = 0; i < 9; i++)
            {
                stackPanel.Children.Add(CreateWidestButton());
                stackPanel.Children.Add(CreateGutter(Config.GUTTER_SIDE_SIMPLE_MM));
            }
            stackPanel.Children.Add(CreateWidestButton());

            return stackPanel;
        }

        public static StackPanel CreateRowType1()
        {
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            stackPanel.Children.Add(CreateSmallButton());
            stackPanel.Children.Add(CreateInRowGutter());
            stackPanel.Children.Add(CreateWideButton());
            stackPanel.Children.Add(CreateInRowGutter());
            stackPanel.Children.Add(CreateDropdownButton());

            return stackPanel;

        }

        public static StackPanel CreateRowType2()
        {
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            stackPanel.Children.Add(CreateSmallButton());
            stackPanel.Children.Add(CreateInRowGutter());
            stackPanel.Children.Add(CreateSmallButton());
            stackPanel.Children.Add(CreateInRowGutter());
            stackPanel.Children.Add(CreateSmallButton());
            stackPanel.Children.Add(CreateInRowGutter());
            stackPanel.Children.Add(CreateDropdownButton());
            stackPanel.Children.Add(CreateInRowGutter());
            stackPanel.Children.Add(CreateSmallButton());

            return stackPanel;
        }

        public static StackPanel CreateRowType3()
        {
            StackPanel stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            stackPanel.Children.Add(CreateWiderButton());
            return stackPanel;
        }

        private static StackPanel CreateRowType4()
        {
            StackPanel stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            stackPanel.Children.Add(CreateWideButton());
            return stackPanel;
        }

        private static StackPanel CreateRowType5()
        {
            StackPanel stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            stackPanel.Children.Add(CreateBigButton());
            stackPanel.Children.Add(CreateInRowGutter());
            stackPanel.Children.Add(CreateBigButton());
            return stackPanel;
        }

        public static Grid CreateSimpleGrid()
        {
            double columnWidth = MM2PX(ExpSizes.BUTTON_MULTIPLES[ExpStrs.x36]);

            Grid group = new Grid { UseLayoutRounding = true, Width = columnWidth }; // Ensure UseLayoutRounding is on the Grid

            group.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(columnWidth) });
            UIElement element = CreateSimpleColumn();
            Grid.SetColumn(element, 0);
            group.Children.Add(element);

            return group;
        }

        // Helper to represent a "Gutter" in the sequence
        private static Func<UIElement> CreateGutterFunc() => () => CreateInColumnGutter();

        // Helper to wrap row creation Functions in a Func<UIElement>
        private static Func<UIElement> WrapRowFunc(Func<StackPanel> rowCreator) => () =>
        {
            var row = rowCreator();
            // Ensure the row itself stretches horizontally if placed in a Grid column
            row.HorizontalAlignment = HorizontalAlignment.Stretch;
            return row;
        };

        /// <summary>
        /// Adds a sequence of UI elements (rows and gutters) to a Grid column,
        /// managing row definitions and Grid.SetRow automatically.
        /// </summary>
        /// <param name="column">The Grid to add elements to.</param>
        /// <param name="elementsToAdd">A sequence of Functions, each returning a UIElement (StackPanel row or Rectangle gutter).</param>
        private static void AddRowsAndGuttersToColumn(Grid column, params Func<UIElement>[] elementsToAdd)
        {
            column.Children.Clear(); // Clear existing if reusing the column Grid
            column.RowDefinitions.Clear(); // Clear existing row definitions

            int currentRowIndex = 0;
            foreach (var createElementFunc in elementsToAdd)
            {
                UIElement element = createElementFunc();

                // Add RowDefinition based on the element's type
                if (element is StackPanel)
                {
                    column.RowDefinitions.Add(new RowDefinition { });
                }
                else if (element is Rectangle && ((Rectangle)element).Height == WithinGroupGutter)
                {
                    column.RowDefinitions.Add(new RowDefinition { Height = new GridLength(WithinGroupGutter) });
                    // Ensure gutter also stretches horizontally if it's not already
                    ((Rectangle)element).HorizontalAlignment = HorizontalAlignment.Stretch;
                }
                else
                {
                    // Fallback for other elements, or handle error
                    column.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                }

                Grid.SetRow(element, currentRowIndex);
                column.Children.Add(element);

                currentRowIndex++;
            }
        }

        public static Grid CreateGroupType1(int combination)
        {
            Grid column = new Grid { UseLayoutRounding = true }; // Ensure UseLayoutRounding is on the Grid

            switch (combination)
            {
                case 1:
                    AddRowsAndGuttersToColumn(column,
                        WrapRowFunc(() => CreateRowType1()),
                        CreateGutterFunc(),
                        WrapRowFunc(() => CreateRowType2()),
                        CreateGutterFunc(),
                        WrapRowFunc(() => CreateRowType3())
                    );
                    break;
                case 2:
                    AddRowsAndGuttersToColumn(column,
                        WrapRowFunc(() => CreateRowType1()),
                        CreateGutterFunc(),
                        WrapRowFunc(() => CreateRowType3()),
                        CreateGutterFunc(),
                        WrapRowFunc(() => CreateRowType2())
                    );
                    break;
                case 3:
                    AddRowsAndGuttersToColumn(column,
                        WrapRowFunc(() => CreateRowType2()),
                        CreateGutterFunc(),
                        WrapRowFunc(() => CreateRowType1()),
                        CreateGutterFunc(),
                        WrapRowFunc(() => CreateRowType3())
                    );
                    break;
                case 4:
                    AddRowsAndGuttersToColumn(column,
                        WrapRowFunc(() => CreateRowType2()),
                        CreateGutterFunc(),
                        WrapRowFunc(() => CreateRowType3()),
                        CreateGutterFunc(),
                        WrapRowFunc(() => CreateRowType1())
                    );
                    break;
                case 5:
                    AddRowsAndGuttersToColumn(column,
                        WrapRowFunc(() => CreateRowType3()),
                        CreateGutterFunc(),
                        WrapRowFunc(() => CreateRowType2()),
                        CreateGutterFunc(),
                        WrapRowFunc(() => CreateRowType1())
                    );
                    break;
                case 6:
                    AddRowsAndGuttersToColumn(column,
                        WrapRowFunc(() => CreateRowType3()),
                        CreateGutterFunc(),
                        WrapRowFunc(() => CreateRowType1()),
                        CreateGutterFunc(),
                        WrapRowFunc(() => CreateRowType2())
                    );
                    break;
                default:
                    throw new ArgumentException($"Invalid combination number: {combination}");
            }

            // The column will automatically get a single ColumnDefinition due to how Grid works
            // or you can explicitly add one if needed:
            if (column.ColumnDefinitions.Count == 0)
            {
                column.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            }
            return column;
        }

        public static Grid CreateGroupType2(int combination)
        {
            Grid column = new Grid { UseLayoutRounding = true };

            switch (combination)
            {
                case 1:
                    AddRowsAndGuttersToColumn(column,
                        WrapRowFunc(() => CreateRowType3()),
                        CreateGutterFunc(),
                        WrapRowFunc(() => CreateRowType4()),
                        CreateGutterFunc(),
                        WrapRowFunc(() => CreateRowType4())
                    );
                    break;
                case 2:
                    AddRowsAndGuttersToColumn(column,
                        WrapRowFunc(() => CreateRowType4()),
                        CreateGutterFunc(),
                        WrapRowFunc(() => CreateRowType3()),
                        CreateGutterFunc(),
                        WrapRowFunc(() => CreateRowType4())
                    );
                    break;
                case 3:
                    AddRowsAndGuttersToColumn(column,
                        WrapRowFunc(() => CreateRowType4()),
                        CreateGutterFunc(),
                        WrapRowFunc(() => CreateRowType4()),
                        CreateGutterFunc(),
                        WrapRowFunc(() => CreateRowType3())
                    );
                    break;
                default:
                    throw new ArgumentException($"Invalid combination number: {combination}");
            }


            if (column.ColumnDefinitions.Count == 0) column.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            return column;
        }

        public static Grid CreateGroupType3()
        {
            Grid column = new Grid { UseLayoutRounding = true };
            AddRowsAndGuttersToColumn(column,
                WrapRowFunc(() => CreateRowType5())
            );
            if (column.ColumnDefinitions.Count == 0) column.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            return column;
        }

    }
}
