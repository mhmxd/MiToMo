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
using static Function.Select.Output;

namespace Function.Select
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
            Side = Side.Top;
            //this.DataContext = this; // Set DataContext for data binding

            EnableMouseInPointer(true);
            SetForegroundWindow(new WindowInteropHelper(this).Handle); // Bring this window to the foreground

            _gridNavigator = new GridNavigator(Config.FRAME_DUR_MS / 1000.0);

            //foreach (int wm in Experiment.BUTTON_MULTIPLES.Values)
            //{
            //    _widthButtons.TryAdd(wm, new List<SButton>());
            //}

        }

        public override Task PlaceGrid(Func<Grid> gridCreator, double topPadding, double leftPadding)
        {
            // A TaskCompletionSource allows us to create a Task
            // that we can complete manually later.
            var tcs = new TaskCompletionSource<bool>();

            // Clear any existing columns from the canvas and the list before generating new ones
            canvas.Children.Clear();

            _buttonsGrid = gridCreator(); // Create the new column Grid

            // Set left position on the Canvas (from padding)
            Canvas.SetLeft(_buttonsGrid, leftPadding);

            // Add to the Canvas
            canvas.Children.Add(_buttonsGrid);
            //this.TrialInfo($"Grid added to canvas. Canvas size: {canvas.ActualWidth}x{canvas.ActualHeight}");

            //this.TrialInfo($"Grid loaded with ActualWidth: {_buttonsGrid.ActualWidth}, ActualHeight: {_buttonsGrid.ActualHeight}");
            // Now ActualWidth has a valid value.
            //double topPosition = (this.Height - _buttonsGrid.ActualHeight) / 2;
            //Canvas.SetTop(_buttonsGrid, topPosition);

            //RegisterAllButtons(_buttonsGrid);
            //LinkButtonNeighbors();

            // Subscribe to the Loaded event to get the correct width.
            _buttonsGrid.Loaded += (sender, e) =>
            {
                try
                {
                    this.TrialInfo($"Grid loaded with ActualWidth: {_buttonsGrid.ActualWidth}, ActualHeight: {_buttonsGrid.ActualHeight}");
                    double topPosition = (this.Height - _buttonsGrid.ActualHeight) / 2;
                    Canvas.SetTop(_buttonsGrid, topPosition);

                    RegisterAllButtons(_buttonsGrid);
                    LinkButtonNeighbors();

                    FindMiddleButton();

                    // Indicate that the task is successfully completed.
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    // If any error occurs, set the exception on the TaskCompletionSource
                    tcs.SetException(ex);
                }
            };

            return tcs.Task; // Return the task to be awaited

        }

        //public override void GenerateGrid(Rect startConstraintsRectAbsolute, params Func<Grid>[] columnCreators)
        //{
        //    _objectConstraintRectAbsolute = startConstraintsRectAbsolute;

        //    // Clear any existing columns from the canvas and the list before generating new ones
        //    canvas.Children.Clear();
        //    _gridColumns.Clear();

        //    double currentLeftPosition = HORIZONTAL_PADDING; // Start with the initial padding

        //    foreach (var createColumnFunc in columnCreators)
        //    {
        //        Grid newColumnGrid = createColumnFunc(); // Create the new column Grid
        //        //newColumnGrid.Background = Brushes.Transparent; // Set background to transparent for visibility

        //        // Set its position on the Canvas
        //        Canvas.SetLeft(newColumnGrid, currentLeftPosition);
        //        Canvas.SetTop(newColumnGrid, HORIZONTAL_PADDING); // Assuming all columns start at the same top padding

        //        // Add to the Canvas
        //        canvas.Children.Add(newColumnGrid);

        //        // Add to our internal list for tracking/future reference
        //        _gridColumns.Add(newColumnGrid);


        //        // Register buttons in this column
        //        //RegisterButtons(newColumnGrid);

        //        // Force a layout pass on the newly added column to get its ActualWidth
        //        // This is crucial because the next column's position depends on this one's actual size.
        //        newColumnGrid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        //        newColumnGrid.Arrange(new Rect(newColumnGrid.DesiredSize));

        //        // Update the currentLeftPosition for the next column, adding the current column's width and the gutter
        //        currentLeftPosition += newColumnGrid.ActualWidth + InterGroupGutter;
        //    }

        //    RegisterAllButtons(_buttonsGrid); // Register buttons in all columns after they are created
        //    LinkButtonNeighbors();


        //    //Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
        //    //{
        //    //    RegisterAllButtons(_buttonsGrid); // Register buttons in all columns after they are created
        //    //    LinkButtonNeighbors();
        //    //}));
        //}


        //private void RegisterAllButtons(DependencyObject parent)
        //{
        //    //foreach (Grid column in _gridColumns)
        //    //{
        //    //    RegisterButtons(column);
        //    //}

        //    //-- Recursively find all SButton instances in the entire _buttonsGrid
        //    // Get the number of children in the current parent object
        //    int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

        //    // Loop through each child
        //    for (int i = 0; i < childrenCount; i++)
        //    {
        //        // Get the current child object
        //        var child = VisualTreeHelper.GetChild(parent, i);

        //        // If the child is an SButton, register it
        //        if (child is SButton sButton)
        //        {
        //            RegisterButton(sButton);
        //        }

        //        // Recursively call the method for the current child to search its children
        //        RegisterAllButtons(child);
        //    }
        //}

        //private void RegisterButton(SButton button)
        //{
        //    _widthButtons[button.WidthMultiple].Add(button); // Add the button to the dictionary with its width as the key
        //    _buttonInfos[button.Id] = new ButtonInfo(button);
        //    //_allButtons.Add(button.Id, button); // Add to the list of all buttons

        //    // Add button position to the dictionary

        //    // Get the transform from the button to the Window (or the root visual)
        //    GeneralTransform transformToWindow = button.TransformToVisual(Window.GetWindow(button));
        //    // Get the point representing the top-left corner of the button relative to the Window
        //    Point positionInWindow = transformToWindow.Transform(new Point(0, 0));
        //    _buttonInfos[button.Id].Position = positionInWindow;
        //    //_buttonPositions.Add(button.Id, positionInWindow); // Store the position of the button
        //    //this.TrialInfo($"Button Position: {positionInWindow}");

        //    Rect buttonRect = new Rect(positionInWindow.x, positionInWindow.y, button.ActualWidth, button.ActualHeight);
        //    _buttonInfos[button.Id].Rect = buttonRect;
        //    //_buttonRects.Add(button.Id, buttonRect); // Store the rect for later

        //    // Set possible distance range to the Start positions
        //    Point buttonCenterAbsolute =
        //        positionInWindow
        //        .OffsetPosition(button.ActualWidth / 2, button.ActualHeight / 2)
        //        .OffsetPosition(this.Left, this.Top);

        //    // Correct way of finding min and max dist
        //    _buttonInfos[button.Id].DistToStartRange = GetMinMaxDistances(buttonCenterAbsolute, _objectConstraintRectAbsolute);

        //    // Update min/max x and y for grid bounds
        //    _gridMinX = Math.Min(_gridMinX, buttonRect.Left);
        //    _gridMinY = Math.Min(_gridMinY, buttonRect.Top);
        //    _gridMaxX = Math.Max(_gridMaxX, buttonRect.Right);
        //    _gridMaxY = Math.Max(_gridMaxY, buttonRect.Bottom);


        //    if (positionInWindow.x <= _topLeftButtonPosition.x && positionInWindow.y <= _topLeftButtonPosition.y)
        //    {
        //        //this.TrialInfo($"Top-left button position updated: {positionInWindow} for button ID#{button.Id}");
        //        _topLeftButtonPosition = positionInWindow; // Update the top-left button position
        //                                                   //_lastMarkedButtonId = button.Id; // Set the last highlighted button to this one
        //    }
        //}

        //private void RegisterButtons(Grid column)
        //{
        //    //this.TrialInfo($"Registering buttons in column with {column.Children.Count} children...");



        //    // Iterate through all direct children of the Grid column
        //    foreach (UIElement childOfColumn in column.Children)
        //    {
        //        // We know our rows are StackPanels
        //        if (childOfColumn is StackPanel rowStackPanel)
        //        {
        //            // Iterate through all children of the StackPanel (which should be buttons or in-row gutters)
        //            foreach (UIElement childOfRow in rowStackPanel.Children)
        //            {
        //                // Check if the child is an SButton
        //                if (childOfRow is SButton button)
        //                {
        //                    _widthButtons[button.WidthMultiple].Add(button); // Add the button to the dictionary with its width as the key
        //                    _buttonInfos[button.Id] = new ButtonInfo(button);
        //                    //_allButtons.Add(button.Id, button); // Add to the list of all buttons

        //                    // Add button position to the dictionary

        //                    // Get the transform from the button to the Window (or the root visual)
        //                    GeneralTransform transformToWindow = button.TransformToVisual(Window.GetWindow(button));
        //                    // Get the point representing the top-left corner of the button relative to the Window
        //                    Point positionInWindow = transformToWindow.Transform(new Point(0, 0));
        //                    _buttonInfos[button.Id].Position = positionInWindow;
        //                    //_buttonPositions.Add(button.Id, positionInWindow); // Store the position of the button
        //                    //this.TrialInfo($"Button Position: {positionInWindow}");

        //                    Rect buttonRect = new Rect(positionInWindow.x, positionInWindow.y, button.ActualWidth, button.ActualHeight);
        //                    _buttonInfos[button.Id].Rect = buttonRect;
        //                    //_buttonRects.Add(button.Id, buttonRect); // Store the rect for later

        //                    // Set possible distance range to the Start positions
        //                    Point buttonCenterAbsolute = 
        //                        positionInWindow
        //                        .OffsetPosition(button.ActualWidth/2, button.ActualHeight/2)
        //                        .OffsetPosition(this.Left, this.Top);

        //                    //double distToStartTL = Utils.Dist(buttonCenterAbsolute, _objectConstraintRectAbsolute.TopLeft);
        //                    //double distToStartTR = Utils.Dist(buttonCenterAbsolute, _objectConstraintRectAbsolute.TopRight);
        //                    //double distToStartLL = Utils.Dist(buttonCenterAbsolute, _objectConstraintRectAbsolute.BottomLeft);
        //                    //double distToStartLR = Utils.Dist(buttonCenterAbsolute, _objectConstraintRectAbsolute.BottomRight);

        //                    //double[] dists = { distToStartTL, distToStartTR, distToStartLL, distToStartLR };
        //                    //_buttonInfos[button.Id].DistToStartRange = new Range(dists.Min(), dists.Max());

        //                    // Correct way of finding min and max dist
        //                    _buttonInfos[button.Id].DistToStartRange = GetMinMaxDistances(buttonCenterAbsolute, _objectConstraintRectAbsolute);

        //                    // Update min/max x and y for grid bounds
        //                    _gridMinX = Math.Min(_gridMinX, buttonRect.Left);
        //                    _gridMinY = Math.Min(_gridMinY, buttonRect.Top);
        //                    _gridMaxX = Math.Max(_gridMaxX, buttonRect.Right);
        //                    _gridMaxY = Math.Max(_gridMaxY, buttonRect.Bottom);


        //                    if (positionInWindow.x <= _topLeftButtonPosition.x && positionInWindow.y <= _topLeftButtonPosition.y)
        //                    {
        //                        //this.TrialInfo($"Top-left button position updated: {positionInWindow} for button ID#{button.Id}");
        //                        _topLeftButtonPosition = positionInWindow; // Update the top-left button position
        //                        //_lastMarkedButtonId = button.Id; // Set the last highlighted button to this one
        //                    }
        //                }
        //            }
        //        }
        //    }



        //}



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
        //            double dist = Utils.Dist(gridCenterPoint, new Point(idRect.Value.x + idRect.Value.Width / 2, idRect.Value.y + idRect.Value.Height / 2));
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
