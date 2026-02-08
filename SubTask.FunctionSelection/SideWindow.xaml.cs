using Common.Helpers;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Common.Constants.ExpEnums;

namespace SubTask.FunctionSelection
{
    /// <summary>
    /// Interaction logic for SideWindow.xaml
    /// </summary>
    public partial class SideWindow : AuxWindow
    {
        public string WindowTitle { get; set; }

        public SideWindow(Side side, Point relPos)
        {
            InitializeComponent();
            //WindowTitle = title;
            WindowSide = side;
            this.DataContext = this; // Set DataContext for data binding

            this.Loaded += SideWindow_Loaded; // Add this line
        }

        private void SideWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //Console.WriteLine("MouseDown event triggered.");
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            var cursorPoint = e.GetPosition(this);
        }

        public override Task PlaceGrid(Func<Grid> gridCreator, double topPadding, double leftPadding)
        {
            // 1. IMMEDIATELY kill the old dictionary references
            // This prevents any "Fill" calls from finding old buttons during the transition
            _buttonWraps.Clear();
            _widthButtons.Clear();

            // A TaskCompletionSource allows us to create a Task
            // that we can complete manually later.
            var tcs = new TaskCompletionSource<bool>();

            // Clear any existing columns from the canvas and the list before generating new ones
            canvas.Children.Clear();

            _buttonsGrid = gridCreator(); // Create the new column Grid

            // Set top position on the Canvas (from padding)
            Canvas.SetTop(_buttonsGrid, topPadding);

            // Add to the Canvas
            canvas.Children.Add(_buttonsGrid);

            // Subscribe to the Loaded event to get the correct width.
            _buttonsGrid.Loaded += (sender, e) =>
            {
                try
                {
                    // Now ActualWidth has a valid value.
                    this.TrialInfo($"Grid loaded with ActualWidth: {_buttonsGrid.ActualWidth}, ActualHeight: {_buttonsGrid.ActualHeight}");
                    double leftPosition = (this.Width - _buttonsGrid.ActualWidth) / 2;
                    Canvas.SetLeft(_buttonsGrid, leftPosition);

                    RegisterAllButtons(_buttonsGrid);
                    LinkButtonNeighbors();

                    // Indicate that the task is successfully completed.
                    tcs.SetResult(true);
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
