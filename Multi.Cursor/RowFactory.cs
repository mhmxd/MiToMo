﻿using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace Multi.Cursor
{
    internal class RowFactory : Grid
    {
        public static double WithinGroupGutter = Utils.MmToDips(Config.GRID_WITHINGROUP_GUTTER_MM); // Space in-between the grid elements
        public static double UNIT = Utils.MmToDips(Config.GRID_UNIT_MM); // Unit of measurement for the grid (1mm = 4px)
        public static double ROW_HEIGHT = 6 * UNIT; // Height of each row in pixels

        // Helper to represent a "Gutter" in the sequence
        private static Func<UIElement> CreateBetweenRowsGutterFunc() => () => CreateGutterBetweenRows();

        // Helper to wrap row creation functions in a Func<UIElement>
        private static Func<UIElement> WrapRowFunc(Func<StackPanel> rowCreator) => () =>
        {
            var row = rowCreator();
            // Ensure the row itself stretches horizontally if placed in a Grid column
            row.HorizontalAlignment = HorizontalAlignment.Stretch;
            return row;
        };

        private static Rectangle CreateGutterWithinRow()
        {
            return new Rectangle
            {
                Width = WithinGroupGutter, // Use WithinGroupGutter for width, not a derived UNIT value unless intentional
                Height = ROW_HEIGHT,
                //Fill = Brushes.Orange, // <-- Make it highly visible for debugging
                //Stroke = Brushes.Black, // Add a stroke
                //StrokeThickness = 0.5,
                HorizontalAlignment = HorizontalAlignment.Stretch // This will stretch the gutter to fill the row height
            };
        }

        private static Rectangle CreateGutterBetweenRows()
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

        private static SButton CreateDropdownButton()
        {
            int wMultiple = Experiment.BUTTON_MULTIPLES[Str.x3]; // 3 x Unit
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the width of the button in the grid 
                Width = wMultiple * UNIT, // BUTTON_WIDTHS_MULTIPLES[0] is defined in Experiment
                Height = ROW_HEIGHT // Height in pixels
            };
            return sButton;
        }

        private static SButton CreateSmallButton()
        {
            int wMultiple = Experiment.BUTTON_MULTIPLES[Str.x6];
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the width of the button in the grid 
                Width = wMultiple * UNIT, // BUTTON_WIDTHS_MULTIPLES[1] is defined in Experiment
                Height = ROW_HEIGHT // Height in pixels
            };
            return sButton;
        }

        private static SButton CreateWideButton()
        {
            int wMultiple = Experiment.BUTTON_MULTIPLES[Str.x12];
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the width of the button in the grid 
                Width = wMultiple * UNIT, // BUTTON_WIDTHS_MULTIPLES[3] is defined in Experiment
                Height = ROW_HEIGHT // Height in pixels
            };
            return sButton;
        }

        private static SButton CreateWiderButton()
        {
            int wMultiple = Experiment.BUTTON_MULTIPLES[Str.x18];
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the width of the button in the grid 
                Width = wMultiple * UNIT, // BUTTON_WIDTHS_MULTIPLES[4] is defined in Experiment
                Height = ROW_HEIGHT // Height in pixels
            };
            return sButton;
        }

        private static SButton CreateWidestButton()
        {
            int wMultiple = Experiment.BUTTON_MULTIPLES[Str.x30];
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the width of the button in the grid 
                Width = wMultiple * UNIT, // BUTTON_WIDTHS_MULTIPLES[4] is defined in Experiment
                Height = ROW_HEIGHT // Height in pixels
            };
            return sButton;
        }

        public static StackPanel CreateRowType1()
        {
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            stackPanel.Children.Add(CreateSmallButton());
            stackPanel.Children.Add(CreateGutterWithinRow());
            stackPanel.Children.Add(CreateSmallButton());
            stackPanel.Children.Add(CreateGutterWithinRow());
            stackPanel.Children.Add(CreateWideButton());
            stackPanel.Children.Add(CreateDropdownButton());
            stackPanel.Children.Add(CreateGutterWithinRow());
            stackPanel.Children.Add(CreateSmallButton());
            stackPanel.Children.Add(CreateGutterWithinRow());
            stackPanel.Children.Add(CreateSmallButton());

            return stackPanel;

        }

        public static StackPanel CreateRowType2()
        {
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            stackPanel.Children.Add(CreateWiderButton());
            stackPanel.Children.Add(CreateGutterWithinRow());
            stackPanel.Children.Add(CreateWideButton());
            stackPanel.Children.Add(CreateGutterWithinRow());
            stackPanel.Children.Add(CreateSmallButton());

            return stackPanel;
        }

        public static StackPanel CreateRowType3()
        {
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            stackPanel.Children.Add(CreateSmallButton());
            stackPanel.Children.Add(CreateDropdownButton());
            stackPanel.Children.Add(CreateGutterWithinRow());
            stackPanel.Children.Add(CreateWiderButton());
            stackPanel.Children.Add(CreateGutterWithinRow());
            stackPanel.Children.Add(CreateSmallButton());
            stackPanel.Children.Add(CreateGutterWithinRow());
            stackPanel.Children.Add(CreateSmallButton());

            return stackPanel;
        }


        public static StackPanel CreateRowType4()
        {
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            stackPanel.Children.Add(CreateWideButton());
            stackPanel.Children.Add(CreateGutterWithinRow());
            stackPanel.Children.Add(CreateGutterWithinRow());
            stackPanel.Children.Add(CreateSmallButton());
            stackPanel.Children.Add(CreateGutterWithinRow());
            stackPanel.Children.Add(CreateSmallButton());
            stackPanel.Children.Add(CreateGutterWithinRow());
            stackPanel.Children.Add(CreateSmallButton());
            stackPanel.Children.Add(CreateGutterWithinRow());
            stackPanel.Children.Add(CreateSmallButton());

            return stackPanel;
        }

        public static StackPanel CreateRowType5()
        {
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            stackPanel.Children.Add(CreateWidestButton());

            return stackPanel;
        }

        public static StackPanel CreateRowType6()
        {
            StackPanel stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            stackPanel.Children.Add(CreateSmallButton());
            stackPanel.Children.Add(CreateGutterWithinRow());
            stackPanel.Children.Add(CreateSmallButton());
            stackPanel.Children.Add(CreateGutterWithinRow());
            stackPanel.Children.Add(CreateWiderButton());

            return stackPanel;
        }

        public static Grid CreateGroupType1(int combination) // Renamed from CreateCol1, assuming 'combination' is the parameter
        {
            Grid group = new Grid { UseLayoutRounding = true }; // Ensure UseLayoutRounding is on the Grid

            switch (combination)
            {
                case 1:
                    AddRowsAndGuttersToGroup(group,
                        WrapRowFunc(() => CreateRowType1()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType2()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType3())
                    );
                    break;
                case 2:
                    AddRowsAndGuttersToGroup(group,
                        WrapRowFunc(() => CreateRowType1()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType3()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType2())
                    );
                    break;
                case 3:
                    AddRowsAndGuttersToGroup(group,
                        WrapRowFunc(() => CreateRowType2()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType1()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType3())
                    );
                    break;
                case 4:
                    AddRowsAndGuttersToGroup(group,
                        WrapRowFunc(() => CreateRowType2()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType3()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType1())
                    );
                    break;
                case 5:
                    AddRowsAndGuttersToGroup(group,
                        WrapRowFunc(() => CreateRowType3()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType2()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType1())
                    );
                    break;
                case 6:
                    AddRowsAndGuttersToGroup(group,
                        WrapRowFunc(() => CreateRowType3()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType1()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType2())
                    );
                    break;
                default:
                    throw new ArgumentException($"Invalid combination number: {combination}");
            }

            // The group will automatically get a single ColumnDefinition due to how Grid works
            // or you can explicitly add one if needed:
            if (group.ColumnDefinitions.Count == 0)
            {
                group.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            }


            return group;


        }

        public static Grid CreateGroupType2(int combination) // Renamed from CreateCol1, assuming 'combination' is the parameter
        {
            Grid group = new Grid { UseLayoutRounding = true }; // Ensure UseLayoutRounding is on the Grid

            switch (combination)
            {
                case 1:
                    AddRowsAndGuttersToGroup(group,
                        WrapRowFunc(() => CreateRowType4()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType5()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType6())
                    );
                    break;
                case 2:
                    AddRowsAndGuttersToGroup(group,
                        WrapRowFunc(() => CreateRowType4()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType6()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType5())
                    );
                    break;
                case 3:
                    AddRowsAndGuttersToGroup(group,
                        WrapRowFunc(() => CreateRowType5()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType4()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType6())
                    );
                    break;
                case 4:
                    AddRowsAndGuttersToGroup(group,
                        WrapRowFunc(() => CreateRowType5()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType6()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType4())
                    );
                    break;
                case 5:
                    AddRowsAndGuttersToGroup(group,
                        WrapRowFunc(() => CreateRowType6()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType5()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType4())
                    );
                    break;
                case 6:
                    AddRowsAndGuttersToGroup(group,
                        WrapRowFunc(() => CreateRowType6()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType4()),
                        CreateBetweenRowsGutterFunc(),
                        WrapRowFunc(() => CreateRowType5())
                    );
                    break;
                default:
                    throw new ArgumentException($"Invalid combination number: {combination}");
            }

            // The group will automatically get a single ColumnDefinition due to how Grid works
            // or you can explicitly add one if needed:
            if (group.ColumnDefinitions.Count == 0)
            {
                group.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            }


            return group;


        }

        private static void AddRowsAndGuttersToGroup(Grid group, params Func<UIElement>[] elementsToAdd)
        {
            group.Children.Clear(); // Clear existing if reusing the column Grid
            group.RowDefinitions.Clear(); // Clear existing row definitions

            int currentRowIndex = 0;
            foreach (var createElementFunc in elementsToAdd)
            {
                UIElement element = createElementFunc();

                // Add RowDefinition based on the element's type
                if (element is StackPanel) // Row
                {
                    group.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ROW_HEIGHT) });
                }
                else if (element is Rectangle && ((Rectangle)element).Height == WithinGroupGutter) // WithinRow gutter
                {
                    group.RowDefinitions.Add(new RowDefinition { Height = new GridLength(WithinGroupGutter) });
                    // Ensure gutter also stretches horizontally if it's not already
                    ((Rectangle)element).HorizontalAlignment = HorizontalAlignment.Stretch;
                }

                Grid.SetRow(element, currentRowIndex);
                group.Children.Add(element);

                currentRowIndex++;
            }
        }

        

    }

}
