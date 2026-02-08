using Common.Helpers;
using Common.Settings;
using CommonUI;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using static Common.Constants.ExpEnums;

namespace Multi.Cursor
{
    /// <summary>
    /// Interaction logic for SideWindow.xaml
    /// </summary>
    public partial class SideWindow : AuxWindow
    {
        public string WindowTitle { get; set; }

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("User32.dll")]
        private static extern void EnableMouseInPointer(bool fEnable);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private bool _isCursorVisible;

        private Rectangle _target = new Rectangle();
        private Point _relPos;
        public Point Rel_Pos
        {
            get { return _relPos; }
            set { _relPos = value; }
        }

        private GridNavigator _gridNavigator;
        private (int colInd, int rowInd) _selectedElement = (0, 0);

        private TranslateTransform _cursorTransform;

        private Dictionary<string, Element> _gridElements = new Dictionary<string, Element>(); // Key: "C{col}-R{row}", Value: Rectangle element

        private List<Grid> _gridGroups = new List<Grid>(); // List of grid rows

        public SideWindow(Side side, Point relPos)
        {
            InitializeComponent();
            //WindowTitle = title;
            Side = side;
            this.DataContext = this; // Set DataContext for data binding

            EnableMouseInPointer(true);
            SetForegroundWindow(new WindowInteropHelper(this).Handle);

            _relPos = relPos;

            _gridNavigator = new GridNavigator(ExpEnvironment.FRAME_DUR_MS / 1000.0);

            this.Loaded += SideWindow_Loaded; // Add this line
        }

        private void SideWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }


        public void ActivateGridNavigator()
        {
            _gridNavigator.Activate();
        }

        public void ActivateCursor()
        {
            //inactiveCursor.Visibility = Visibility.Hidden;
            //activeCursor.Visibility = Visibility.Visible;
            //_auxursor.Activate();
        }

        public void DeactivateCursor()
        {
            //activeCursor.Visibility = Visibility.Hidden;
            //inactiveCursor.Visibility = Visibility.Visible;
            //_auxursor.Deactivate();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //Console.WriteLine("MouseDown event triggered.");
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            var cursorPoint = e.GetPosition(this);

            _cursorTransform.X = cursorPoint.X;
            _cursorTransform.Y = cursorPoint.Y;
        }

        private void Window_MouseUp(object sender, MouseEventArgs e)
        {

        }

        public void SelectElement()
        {
            // De-select all elements first
            foreach (var element in _gridElements.Values)
            {
                element.ElementStroke = UIColors.COLOR_BUTTON_DEFAULT_BORDER;
                element.ElementStrokeThickness = ExpLayouts.ELEMENT_BORDER_THICKNESS;
            }

            string elementKey = $"C{_selectedElement.colInd}-R{_selectedElement.rowInd}";
            if (_gridElements.ContainsKey(elementKey))
            {
                Element element = _gridElements[elementKey];
                element.ElementStroke = UIColors.COLOR_ELEMENT_HIGHLIGHT;
                element.ElementStrokeThickness = ExpLayouts.ELEMENT_BORDER_THICKNESS;
            }
            else
            {
                Console.WriteLine($"Element {elementKey} not found.");
            }
        }

        public void SelectElement(int rowId, int colId)
        {
            _selectedElement.rowInd = rowId;
            _selectedElement.colInd = colId;
            SelectElement();
        }

        public void MoveSelection(int dCol, int dRow)
        {
            _selectedElement.rowInd += dRow;
            _selectedElement.colInd += dCol;
            if (_selectedElement.rowInd < 0) _selectedElement.rowInd = 0;
            if (_selectedElement.colInd < 0) _selectedElement.colInd = 0;
            SelectElement();
        }

        public void ResetElements()
        {
            foreach (Element element in _gridElements.Values)
            {
                element.ElementFill = UIColors.COLOR_BUTTON_DEFAULT_FILL; // Reset to default color
            }
        }

        public Point GetElementCenter(string key)
        {
            Element element = _gridElements[key];

            return new Point
            {
                X = Canvas.GetLeft(element) + element.ElementWidth / 2,
                Y = Canvas.GetTop(element) + element.ElementWidth / 2
            };
        }

        public override void ShowPoint(Point p)
        {
            // Create a small circle to represent the point
            Ellipse pointCircle = new Ellipse
            {
                Width = 5, // Diameter of the circle
                Height = 5,
                Fill = Brushes.Red // Color of the circle
            };
            // Position the circle on the canvas
            Canvas.SetLeft(pointCircle, p.X - pointCircle.Width / 2);
            Canvas.SetTop(pointCircle, p.Y - pointCircle.Height / 2);
            // Add the circle to the canvas
            if (this.canvas != null)
            {
                this.canvas.Children.Add(pointCircle);
            }
            else
            {
                this.PositionInfo("Canvas is not initialized, cannot show point.");
            }

        }

        public override Task PlaceGrid(Func<Grid> gridCreator, double topPadding, double leftPadding)
        {
            // A TaskCompletionSource allows us to create a Task
            // that we can complete manually later.
            var tcs = new TaskCompletionSource<bool>();

            // Clear any existing columns from the canvas and the list before generating new ones
            canvas.Children.Clear();

            _buttonsGrid = gridCreator(); // Create the new column Grid

            // Set left position on the Canvas (horizontally centered)
            //Output.TrialInfo(this, $"Placing single grid with size {grid.Width} in {this.Width}...");
            //double leftPosition = (this.Width - grid.Width) / 2;
            //Canvas.SetLeft(grid, leftPosition);

            // Set top position on the Canvas (from padding)
            Canvas.SetTop(_buttonsGrid, topPadding);

            // Add to the Canvas
            canvas.Children.Add(_buttonsGrid);

            //double leftPosition = (this.Width - _buttonsGrid.ActualWidth) / 2;
            //Canvas.SetLeft(_buttonsGrid, leftPosition);



            // Subscribe to the Loaded event to get the correct width.
            _buttonsGrid.Loaded += (sender, e) =>
            {
                try
                {
                    // Now ActualWidth has a valid value.
                    double leftPosition = (this.Width - _buttonsGrid.ActualWidth) / 2;
                    Canvas.SetLeft(_buttonsGrid, leftPosition);

                    RegisterAllButtons(_buttonsGrid);
                    LinkButtonNeighbors();

                    FindMiddleButton();

                    // Indicate that the task is successfully completed.
                    tcs.SetResult(true);

                    // Register buttons after the grid is loaded and positioned.
                    //Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
                    //{
                    //    RegisterAllButtons();
                    //    LinkButtonNeighbors();
                    //}));
                }
                catch (Exception ex)
                {
                    // If any error occurs, set the exception on the TaskCompletionSource
                    tcs.SetException(ex);
                }
            };

            return tcs.Task; // Return the Task to be awaited

        }

    }
}
