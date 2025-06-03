using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Multi.Cursor
{
    internal class ButtonGrid : Grid  // Inherits from Grid to use WPF's Grid layout capabilities
    {
        public static int GUTTER = Utils.MM2PX(Config.GRID_GUTTER_MM); // Space in-between the grid elements

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
            public static int ROW_HEIGHT = Utils.MM2PX(Config.GRID_ROW_HEIGHT_MM); // Height of the row in pixels
            public Point Position = new Point(0, 0); // Top-left position of the row
            public List<Button> Buttons = new List<Button>(); // List of elements in this column
        }

        private class Button
        {
            public Point Position
            {
                get { return new Point(_rect.Left, _rect.Top); }
                set { _rect.Location = value;  }
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

        public ButtonGrid(Point pos)
        {
            _position = pos; // Set the position

            // Create different types of buttons
            Button bigButton = new Button(15, 19); // 60x76px
            Button smallButton = new Button(6, 6); // 24x24px
            Button dropdownButton = new Button(3, 6); // 12x24px
            Button wideButton = new Button(12, 6); // 72x24px
            Button widerButton = new Button(30, 6); // 120x24px

        }

        //public void CreateDefaultColumn1(Button smallBtn, Button wideBtn, Button dropdownBtn, Button WiderBtn)
        //{
        //    // First row
        //    Row col1Row1 = new Row();
        //    col1Row1.Position = _column1.GetTopRowPosition();

        //    Button row1Btn1 = smallBtn.ShallowCopy();
        //    row1Btn1.Position = col1Row1.Position; // Set position to the top-left of the row

        //    Button row1Btn2 = wideBtn.ShallowCopy();
        //    row1Btn2.Position = row1Btn1.TopRight.OffsetPosition(GUTTER, 0); // Set position to the right of the first button + gutter

        //    Button row1Btn3 = ;
        //    row1Btn3.SetSizeUnits(3, 6); // 12x24px

        //    col1Row1.Buttons.AddRange(new[] { row1Btn1, row1Btn2, row1Btn3 });
        //    _column1.Rows.Add(col1Row1);

        //    // Second row


        //    Row col1Row2 = new Row();
        //    col1Row2.Position = _column2.GetMiddleRowPosition();
        //    _column1.Rows.Add(col1Row2);


        //}

    }
}
