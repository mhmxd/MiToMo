//using ExCSS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.SessionState;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Tensorflow.Operations.Activation;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Multi.Cursor
{
    internal class SGrid : Grid  // Inherits from Grid to use WPF's Grid layout capabilities
    {
        public static double GUTTER = Utils.MmToDips(Config.GRID_GUTTER_MM); // Space in-between the grid elements
        public static double UNIT = Utils.MmToDips(Config.GRID_UNIT_MM); // Unit of measurement for the grid (1mm = 4px)
        public static double ROW_HEIGHT = 6 * UNIT; // Height of each row in pixels

        

        private class Column
        {
            public Point Position = new Point(0, 0); // Top-left position of the column
            public List<Row> Rows = new List<Row>(); // List of rows in this column

            public Point GetTopRowPosition()
            {
                return Position;
            }

            public Point GetMiddleRowPosition()
            {
                return Position.OffsetPosition(0, Row.ROW_HEIGHT + GUTTER);
            }

        }

        private class Row
        {
            public static int ROW_HEIGHT = ROW_HEIGHT; // Height of the row in pixels
            public Point Position = new Point(0, 0); // Top-left position of the row
            public List<Button> Buttons = new List<Button>(); // List of elements in this column
        }

        private class Button
        {
            public Point Position
            {
                get { return new Point(_rect.Left, _rect.Top); }
                set { _rect.Location = value; }
            }

            public int Right
            {
                get { return (int)_rect.Right; }
            }

            public Point TopRight
            {
                get { return new Point(_rect.Right, _rect.Top); }
            }

            public int Bottom
            {
                get { return (int)_rect.Bottom; }
            }

            private Rect _rect = new Rect();

            public Button(int widthUnits, int heightUnits)
            {
                Position = new Point(0, 0); // Default position at (0, 0)
                _rect.Width = Utils.MM2PX(Config.GRID_UNIT_MM * widthUnits);
                _rect.Height = Utils.MM2PX(Config.GRID_UNIT_MM * heightUnits);
            }

            public Button(Point position, int w, int h)
            {
                Position = position;
                _rect.Width = w;
                _rect.Height = h;
            }

            public Button(Point position)
            {
                Position = position;
            }

            public void SetSizeUnits(int widthUnits, int heightUnits)
            {
                _rect.Width = Utils.MM2PX(Config.GRID_UNIT_MM * widthUnits);
                _rect.Height = Utils.MM2PX(Config.GRID_UNIT_MM * heightUnits);
            }

            public Button ShallowCopy()
            {
                // MemberwiseClone() returns a shallow copy of the current instance.
                // It's a protected method of System.Object, so it must be called from within the class.
                // The result needs to be cast back to your Button type.
                return (Button)this.MemberwiseClone();
            }

        }

        private Point _position; // Top-left position of the grid
        private Column _column1 = new Column();
        private Column _column2 = new Column();
        private Column _column3 = new Column();

        public SGrid(Point pos)
        {
            _position = pos; // Set the position

            // Create different types of buttons
            Button bigButton = new Button(15, 19); // 60x76px
            Button smallButton = new Button(6, 6); // 24x24px
            Button dropdownButton = new Button(3, 6); // 12x24px
            Button wideButton = new Button(12, 6); // 72x24px
            Button widerButton = new Button(30, 6); // 120x24px

        }

        private static Rectangle CreateInRowGutter()
        {
            return new Rectangle
            {
                Width = GUTTER, // Use GUTTER for width, not a derived UNIT value unless intentional
                Height = ROW_HEIGHT,
                //Fill = Brushes.Orange, // <-- Make it highly visible for debugging
                //Stroke = Brushes.Black, // Add a stroke
                StrokeThickness = 0.5,
                HorizontalAlignment = HorizontalAlignment.Left // Consistent alignment
            };
        }

        private static Rectangle CreateInColumnGutter()
        {
            return new Rectangle
            {
                // Width will be set to HorizontalAlignment.Stretch in CreateCol1
                Height = GUTTER,
                //Fill = Brushes.Red, // <-- Make it highly visible for debugging
                //Stroke = Brushes.Green,
                StrokeThickness = 0.5,
                HorizontalAlignment = HorizontalAlignment.Stretch // This should be set in CreateCol1 too
            };
        }

        private static SButton CreateBigButton()
        {
            int wMultiple = Experiment.BUTTON_WIDTHS_MULTIPLES[5];
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the width of the button in the grid 
                Width = wMultiple * UNIT, // BUTTON_WIDTHS_MULTIPLES[5] is defined in Experiment
                Height = 19 * UNIT // 19 * UNIT is the height in pixels
            };
            return sButton;
        }

        private static SButton CreateSmallButton()
        {
            int wMultiple = Experiment.BUTTON_WIDTHS_MULTIPLES[1];
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the width of the button in the grid 
                Width = wMultiple * UNIT, // BUTTON_WIDTHS_MULTIPLES[1] is defined in Experiment
                Height = ROW_HEIGHT // Height in pixels
            };
            return sButton;
        }

        private static SButton CreateDropdownButton()
        {
            int wMultiple = Experiment.BUTTON_WIDTHS_MULTIPLES[0]; // Assuming 0 is the index for dropdown button width
            SButton sButton = new SButton
            {
                WidthMultiple = wMultiple, // Width ID for the button, used to identify the width of the button in the grid 
                Width = wMultiple * UNIT, // BUTTON_WIDTHS_MULTIPLES[0] is defined in Experiment
                Height = ROW_HEIGHT // Height in pixels
            };
            return sButton;
        }

        private static SButton CreateWideButton()
        {
            int wMultiple = Experiment.BUTTON_WIDTHS_MULTIPLES[3]; // Assuming 3 is the index for wide button width
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
            int wMultiple = Experiment.BUTTON_WIDTHS_MULTIPLES[4]; // Assuming 4 is the index for wider button width
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
            StackPanel stackPanel = new StackPanel { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Stretch 
            };

            stackPanel.Children.Add(CreateSmallButton());
            stackPanel.Children.Add(CreateInRowGutter());
            stackPanel.Children.Add(CreateWideButton());
            stackPanel.Children.Add(CreateDropdownButton());

            return stackPanel;

        }

        public static StackPanel CreateRowType2()
        {
            StackPanel stackPanel = new StackPanel { 
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            stackPanel.Children.Add(CreateSmallButton());
            stackPanel.Children.Add(CreateInRowGutter());
            stackPanel.Children.Add(CreateSmallButton());
            stackPanel.Children.Add(CreateInRowGutter());
            stackPanel.Children.Add(CreateSmallButton());
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
            return stackPanel;
        }

        // Helper to represent a "Gutter" in the sequence
        private static Func<UIElement> CreateGutterFunc() => () => CreateInColumnGutter();

        // Helper to wrap row creation functions in a Func<UIElement>
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
        /// <param name="elementsToAdd">A sequence of functions, each returning a UIElement (StackPanel row or Rectangle gutter).</param>
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
                    column.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ROW_HEIGHT) });
                }
                else if (element is Rectangle && ((Rectangle)element).Height == GUTTER)
                {
                    column.RowDefinitions.Add(new RowDefinition { Height = new GridLength(GUTTER) });
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

        public static Grid CreateColType1(int combination) // Renamed from CreateCol1, assuming 'combination' is the parameter
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
                        WrapRowFunc(() => CreateRowType3()),
                        CreateGutterFunc(),
                        WrapRowFunc(() => CreateRowType2()),
                        CreateGutterFunc(),
                        WrapRowFunc(() => CreateRowType1())
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

        public static Grid CreateColType2(int combination)
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

        public static Grid CreateColType3()
        {
            Grid column = new Grid { UseLayoutRounding = true };
            AddRowsAndGuttersToColumn(column,
                WrapRowFunc(() => CreateRowType2()),
                CreateGutterFunc(),
                WrapRowFunc(() => CreateRowType3()),
                CreateGutterFunc(),
                WrapRowFunc(() => CreateRowType1())
            );
            if (column.ColumnDefinitions.Count == 0) column.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            return column;
        }


        //public static Grid CreateColType1(int combination)
        //{
        //    Grid column = new Grid { UseLayoutRounding = true };

        //    // Define one column definition that stretches
        //    column.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Or GridLength.Star for stretching

        //    // Define row definitions for your content rows and gutters
        //    column.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ROW_HEIGHT) }); // Row 1 Fixed Height
        //    column.RowDefinitions.Add(new RowDefinition { Height = new GridLength(GUTTER) });    // Gutter 1
        //    column.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ROW_HEIGHT) }); // Row 2 Fixed Height
        //    column.RowDefinitions.Add(new RowDefinition { Height = new GridLength(GUTTER) });    // Gutter 2
        //    column.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ROW_HEIGHT) }); // Row 3 Fixed Height

        //    // Add rows based on the combination number
        //    StackPanel rowType1;
        //    StackPanel rowType2;
        //    StackPanel rowType3;
        //    Rectangle inColumnGutter;

        //    switch (combination)
        //    {
        //        case 1:

        //            int rowNum = 0; // Initialize row number

        //            rowType1 = CreateRowType1();
        //            Grid.SetRow(rowType1, rowNum);
        //            column.Children.Add(rowType1);

        //            rowNum++;
        //            inColumnGutter = CreateInColumnGutter();
        //            Grid.SetRow(inColumnGutter, rowNum);
        //            column.Children.Add(inColumnGutter);

        //            rowNum++;
        //            rowType2 = CreateRowType2();
        //            Grid.SetRow(rowType2, rowNum);
        //            column.Children.Add(rowType2);

        //            rowNum++;
        //            inColumnGutter = CreateInColumnGutter();
        //            Grid.SetRow(inColumnGutter, rowNum);
        //            column.Children.Add(inColumnGutter);

        //            rowNum++;
        //            rowType3 = CreateRowType3();
        //            Grid.SetRow(rowType3, rowNum);
        //            column.Children.Add(rowType3);

        //            break;

        //        case 2:

        //            rowNum = 0; // Reset row number
        //            rowType1 = CreateRowType1();
        //            Grid.SetRow(rowType1, rowNum);
        //            column.Children.Add(rowType1);

        //            rowNum++;
        //            inColumnGutter = CreateInColumnGutter();
        //            Grid.SetRow(inColumnGutter, rowNum);
        //            column.Children.Add(inColumnGutter);

        //            rowNum++;
        //            rowType3 = CreateRowType3();
        //            Grid.SetRow(rowType3, rowNum);
        //            column.Children.Add(rowType3);

        //            rowNum++;
        //            inColumnGutter = CreateInColumnGutter();
        //            Grid.SetRow(inColumnGutter, rowNum);
        //            column.Children.Add(inColumnGutter);

        //            rowNum++;
        //            rowType2 = CreateRowType2();
        //            Grid.SetRow(rowType2, rowNum);
        //            column.Children.Add(rowType2);

        //            break;

        //        case 3:

        //            rowNum = 0;
        //            rowType2 = CreateRowType2();
        //            Grid.SetRow(rowType2, rowNum);
        //            column.Children.Add(rowType2);

        //            rowNum++;
        //            inColumnGutter = CreateInColumnGutter();
        //            Grid.SetRow(inColumnGutter, rowNum);
        //            column.Children.Add(inColumnGutter);

        //            rowNum++;
        //            rowType1 = CreateRowType1();
        //            Grid.SetRow(rowType1, rowNum);
        //            column.Children.Add(rowType1);

        //            rowNum++;
        //            inColumnGutter = CreateInColumnGutter();
        //            Grid.SetRow(inColumnGutter, rowNum);
        //            column.Children.Add(inColumnGutter);

        //            rowNum++;
        //            rowType3 = CreateRowType3();
        //            Grid.SetRow(rowType3, rowNum);
        //            column.Children.Add(rowType3);

        //            break;

        //        case 4:

        //            rowNum = 0;
        //            rowType3 = CreateRowType3();
        //            Grid.SetRow(rowType3, rowNum);
        //            column.Children.Add(rowType3);

        //            rowNum++;
        //            inColumnGutter = CreateInColumnGutter();
        //            Grid.SetRow(inColumnGutter, rowNum);
        //            column.Children.Add(inColumnGutter);

        //            rowNum++;
        //            rowType2 = CreateRowType2();    
        //            Grid.SetRow(rowType2, rowNum);
        //            column.Children.Add(rowType2);

        //            rowNum++;
        //            inColumnGutter = CreateInColumnGutter();
        //            Grid.SetRow(inColumnGutter, rowNum);
        //            column.Children.Add(inColumnGutter);

        //            rowNum++;
        //            rowType1 = CreateRowType1();
        //            Grid.SetRow(rowType1, rowNum);
        //            column.Children.Add(rowType1);

        //            break;

        //        default:
        //            throw new ArgumentException("Invalid combination number");
        //    }



        //    return column;
        //}

        public static Grid CreateCol2(int combination)
        {
            Grid column = new Grid { UseLayoutRounding = true };

            // Define one column definition that stretches
            column.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Or GridLength.Star for stretching

            // Define row definitions for your content rows and gutters
            column.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ROW_HEIGHT) }); // Row 1 Fixed Height
            column.RowDefinitions.Add(new RowDefinition { Height = new GridLength(GUTTER) });    // Gutter 1
            column.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ROW_HEIGHT) }); // Row 2 Fixed Height
            column.RowDefinitions.Add(new RowDefinition { Height = new GridLength(GUTTER) });    // Gutter 2
            column.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ROW_HEIGHT) }); // Row 3 Fixed Height

            // Add rows
            StackPanel row1 = CreateRowType3();
            Grid.SetRow(row1, 0);
            row1.HorizontalAlignment = HorizontalAlignment.Stretch;
            column.Children.Add(row1);

            Rectangle inColumnGutter1 = CreateInColumnGutter(); // Use the new method
            Grid.SetRow(inColumnGutter1, 1);
            inColumnGutter1.HorizontalAlignment = HorizontalAlignment.Stretch; // Explicitly stretch
            column.Children.Add(inColumnGutter1);

            StackPanel row2 = CreateRowType4();
            Grid.SetRow(row2, 2);
            row2.HorizontalAlignment = HorizontalAlignment.Stretch;
            column.Children.Add(row2);

            Rectangle inColumnGutter2 = CreateInColumnGutter(); // Use the new method
            Grid.SetRow(inColumnGutter2, 3);
            inColumnGutter2.HorizontalAlignment = HorizontalAlignment.Stretch; // Explicitly stretch
            column.Children.Add(inColumnGutter2);

            StackPanel row3 = CreateRowType4();
            Grid.SetRow(row3, 4);
            row3.HorizontalAlignment = HorizontalAlignment.Stretch;
            column.Children.Add(row3);

            return column;
        }

        public static Grid CreateCol3()
        {
            Grid column = new Grid { UseLayoutRounding = true };

            // Define one column definition that stretches
            column.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Or GridLength.Star for stretching

            // Define row definitions for your content rows and gutters
            column.RowDefinitions.Add(new RowDefinition { Height = new GridLength(19 * UNIT) }); // Row 1 Fixed Height

            // Add rows
            StackPanel row1 = CreateRowType5();
            Grid.SetRow(row1, 0);
            row1.HorizontalAlignment = HorizontalAlignment.Stretch;
            column.Children.Add(row1);

            return column;
        }
    }
}
