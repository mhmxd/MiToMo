using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using static Multi.Cursor.Output;

namespace Multi.Cursor
{
    /// <summary>
    /// Interaction logic for TopWindow.xaml
    /// </summary>
    public partial class TopWindow : AuxWindow
    {
        private double HORIZONTAL_PADDING = Utils.MM2PX(Config.WINDOW_PADDING_MM);
        private double InterGroupGutter = Utils.MM2PX(Config.GUTTER_05MM);

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("User32.dll")]
        private static extern void EnableMouseInPointer(bool fEnable);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private Random _random = new Random(); 

        //private GridNavigator _gridNavigator;
        //private List<Grid> _gridColumns = new List<Grid>(); // List of grid columns
        //private static Dictionary<int, List<SButton>> _widthButtons = new Dictionary<int, List<SButton>>(); // Dictionary to hold buttons by their width multiples
        //private SButton _targetButton; // Currently selected button (if any)

        // Boundary of the grid (encompassing all buttons)
        //double _gridMinX = double.MaxValue;
        //double _gridMinY = double.MaxValue;
        //double _gridMaxX = double.MinValue;
        //double _gridMaxY = double.MinValue;

        public TopWindow()
        {
            InitializeComponent();
            //this.DataContext = this; // Set DataContext for data binding

            EnableMouseInPointer(true);
            SetForegroundWindow(new WindowInteropHelper(this).Handle); // Bring this window to the foreground

            _gridNavigator = new GridNavigator(Config.FRAME_DUR_MS / 1000.0);

            foreach (int wm in Experiment.BUTTON_MULTIPLES.Values)
            {
                _widthButtons.TryAdd(wm, new List<SButton>());
            }

        }

        public override void PlaceGrid(Func<Grid> gridCreator, double topPadding, double leftPadding)
        {
            // Clear any existing columns from the canvas and the list before generating new ones
            canvas.Children.Clear();

            Grid grid = gridCreator(); // Create the new column Grid

            // Set left position on the Canvas (from padding)
            Canvas.SetLeft(grid, leftPadding);

            // Add to the Canvas
            canvas.Children.Add(grid);

            // Subscribe to the Loaded event to get the correct width.
            grid.Loaded += (sender, e) =>
            {
                // Now ActualWidth has a valid value.
                double topPosition = (this.Height - grid.ActualHeight) / 2;
                Canvas.SetTop(grid, topPosition);

                // Register buttons after the grid is loaded and positioned.
                Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
                {
                    RegisterAllButtons();
                    LinkButtonNeighbors();
                }));
            };

        }

        public override void GenerateGrid(Rect startConstraintsRectAbsolute, params Func<Grid>[] columnCreators)
        {
            _startConstraintsRectAbsolute = startConstraintsRectAbsolute;

            // Clear any existing columns from the canvas and the list before generating new ones
            canvas.Children.Clear();
            _gridColumns.Clear();

            double currentLeftPosition = HORIZONTAL_PADDING; // Start with the initial padding

            foreach (var createColumnFunc in columnCreators)
            {
                Grid newColumnGrid = createColumnFunc(); // Create the new column Grid
                //newColumnGrid.Background = Brushes.Transparent; // Set background to transparent for visibility

                // Set its position on the Canvas
                Canvas.SetLeft(newColumnGrid, currentLeftPosition);
                Canvas.SetTop(newColumnGrid, HORIZONTAL_PADDING); // Assuming all columns start at the same top padding

                // Add to the Canvas
                canvas.Children.Add(newColumnGrid);

                // Add to our internal list for tracking/future reference
                _gridColumns.Add(newColumnGrid);

                // Register buttons in this column
                //RegisterButtons(newColumnGrid);

                // Force a layout pass on the newly added column to get its ActualWidth
                // This is crucial because the next column's position depends on this one's actual size.
                newColumnGrid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                newColumnGrid.Arrange(new Rect(newColumnGrid.DesiredSize));

                // Update the currentLeftPosition for the next column, adding the current column's width and the gutter
                currentLeftPosition += newColumnGrid.ActualWidth + InterGroupGutter;
            }

            Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
            {
                RegisterAllButtons(); // Register buttons in all columns after they are created
                LinkButtonNeighbors();
            }));
        }

        private void RegisterAllButtons()
        {
            foreach (Grid column in _gridColumns)
            {
                RegisterButtons(column);
            }

            int middleId = FindMiddleButtonId();
            if (middleId != -1)
            {
                _lastHighlightedButtonId = middleId; // Set the last highlighted button to the middle button
            }
            else
            {
                this.TrialInfo("No middle button found in the grid.");
            }
        }

        private void RegisterButtons(Grid column)
        {
            //this.TrialInfo($"Registering buttons in column with {column.Children.Count} children...");

            // Iterate through all direct children of the Grid column
            foreach (UIElement childOfColumn in column.Children)
            {
                // We know our rows are StackPanels
                if (childOfColumn is StackPanel rowStackPanel)
                {
                    // Iterate through all children of the StackPanel (which should be buttons or in-row gutters)
                    foreach (UIElement childOfRow in rowStackPanel.Children)
                    {
                        // Check if the child is an SButton
                        if (childOfRow is SButton button)
                        {
                            _widthButtons[button.WidthMultiple].Add(button); // Add the button to the dictionary with its width as the key
                            _buttonInfos[button.Id] = new ButtonInfo(button);
                            //_allButtons.Add(button.Id, button); // Add to the list of all buttons
                            
                            // Add button position to the dictionary

                            // Get the transform from the button to the Window (or the root visual)
                            GeneralTransform transformToWindow = button.TransformToVisual(Window.GetWindow(button));
                            // Get the point representing the top-left corner of the button relative to the Window
                            Point positionInWindow = transformToWindow.Transform(new Point(0, 0));
                            _buttonInfos[button.Id].Position = positionInWindow;
                            //_buttonPositions.Add(button.Id, positionInWindow); // Store the position of the button
                            //this.TrialInfo($"Button Position: {positionInWindow}");

                            Rect buttonRect = new Rect(positionInWindow.X, positionInWindow.Y, button.ActualWidth, button.ActualHeight);
                            _buttonInfos[button.Id].Rect = buttonRect;
                            //_buttonRects.Add(button.Id, buttonRect); // Store the rect for later

                            // Set possible distance range to the Start positions
                            Point buttonCenterAbsolute = 
                                positionInWindow
                                .OffsetPosition(button.ActualWidth/2, button.ActualHeight/2)
                                .OffsetPosition(this.Left, this.Top);

                            //double distToStartTL = Utils.Dist(buttonCenterAbsolute, _startConstraintsRectAbsolute.TopLeft);
                            //double distToStartTR = Utils.Dist(buttonCenterAbsolute, _startConstraintsRectAbsolute.TopRight);
                            //double distToStartLL = Utils.Dist(buttonCenterAbsolute, _startConstraintsRectAbsolute.BottomLeft);
                            //double distToStartLR = Utils.Dist(buttonCenterAbsolute, _startConstraintsRectAbsolute.BottomRight);

                            //double[] dists = { distToStartTL, distToStartTR, distToStartLL, distToStartLR };
                            //_buttonInfos[button.Id].DistToStartRange = new Range(dists.Min(), dists.Max());

                            // Correct way of finding min and max dist
                            _buttonInfos[button.Id].DistToStartRange = GetMinMaxDistances(buttonCenterAbsolute, _startConstraintsRectAbsolute);

                            // Update min/max X and Y for grid bounds
                            _gridMinX = Math.Min(_gridMinX, buttonRect.Left);
                            _gridMinY = Math.Min(_gridMinY, buttonRect.Top);
                            _gridMaxX = Math.Max(_gridMaxX, buttonRect.Right);
                            _gridMaxY = Math.Max(_gridMaxY, buttonRect.Bottom);


                            if (positionInWindow.X <= _topLeftButtonPosition.X && positionInWindow.Y <= _topLeftButtonPosition.Y)
                            {
                                //this.TrialInfo($"Top-left button position updated: {positionInWindow} for button ID#{button.Id}");
                                _topLeftButtonPosition = positionInWindow; // Update the top-left button position
                                //_lastHighlightedButtonId = button.Id; // Set the last highlighted button to this one
                            }
                        }
                    }
                }
            }

            

        }

        //private int FindMiddleButtonId()
        //{
        //    // Calculate the center of the overall button grid
        //    double gridCenterX = (_gridMinX + _gridMaxX) / 2;
        //    double gridCenterY = (_gridMinY + _gridMaxY) / 2;
        //    Point gridCenterPoint = new Point(gridCenterX, gridCenterY);

        //    // Distance to the center point
        //    double centerDistance = double.MaxValue;
        //    int closestButtonId = -1;

        //    foreach (KeyValuePair<int, Rect> idRect in _buttonRects)
        //    {
        //        // Check which button contains the grid center point
        //        if (idRect.Value.Contains(gridCenterPoint))
        //        {
        //            // If we find a button that contains the center point, return its ID
        //            this.TrialInfo($"Middle button found at ID#{idRect.Key} with position {gridCenterPoint}");
        //            return idRect.Key;
        //        }
        //        else // if button doesn't containt the center point, calculate the distance
        //        {
        //            double dist = Utils.Dist(gridCenterPoint, new Point(idRect.Value.X + idRect.Value.Width / 2, idRect.Value.Y + idRect.Value.Height / 2));
        //            if (dist < centerDistance)
        //            {
        //                centerDistance = dist;
        //                closestButtonId = idRect.Key; // Update the last highlighted button to the closest one
        //            }
        //        }
        //    }

        //    return closestButtonId;

        //}

        /// <summary>
        /// Calculates and stores the spatial neighbor links for every button
        /// by setting the neighbor IDs directly on each SButton instance.
        /// </summary>
        //private void LinkButtonNeighbors()
        //{
        //    this.TrialInfo("Linking neighbor IDs for all buttons...");
        //    if (_buttonInfos.Count == 0) return;
        //    //if (_allButtons.Count == 0) return;

        //    // For each button in the grid...
        //    foreach (int buttonId in _buttonInfos.Keys)
        //    {
        //        // ...find its neighbor in each of the four directions.
        //        SButton topNeighbor = GetNeighbor(_buttonInfos[buttonId].Button, Side.Top);
        //        SButton bottomNeighbor = GetNeighbor(_buttonInfos[buttonId].Button, Side.Down);
        //        SButton leftNeighbor = GetNeighbor(_buttonInfos[buttonId].Button, Side.Left);
        //        SButton rightNeighbor = GetNeighbor(_buttonInfos[buttonId].Button, Side.Right);

        //        // Get the ID of each neighbor, or -1 if the neighbor is null.
        //        int topId = topNeighbor?.Id ?? -1;
        //        int bottomId = bottomNeighbor?.Id ?? -1;
        //        int leftId = leftNeighbor?.Id ?? -1;
        //        int rightId = rightNeighbor?.Id ?? -1;

        //        // Call the method on the button to store its neighbor IDs.
        //        _buttonInfos[buttonId].Button.SetNeighbors(topId, bottomId, leftId, rightId);
        //    }
        //}

        //public override void MakeTargetAvailable()
        //{
        //    if (_targetButton != null)
        //    {
        //        _targetButton.Background = Config.TARGET_AVAILABLE_COLOR; // Change the background color of the selected button
        //    }
        //}

        //public override void MakeTargetUnavailable()
        //{
        //    if (_targetButton != null)
        //    {
        //        _targetButton.Background = Config.TARGET_UNAVAILABLE_COLOR; // Change the background color of the selected button
        //        this.TrialInfo("Target button made unavailable.");
        //    }
        //    else
        //    {
        //        this.TrialInfo("No button selected to make unavailable.");
        //    }
        //}

        //public override void ActivateMarker()
        //{
        //    _gridNavigator.Activate();
        //}

        public void DeactivateGridNavigator()
        {
            _gridNavigator.Deactivate();
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
                this.TrialInfo("Canvas is not initialized, cannot show point.");
            }
        }
    }
}
