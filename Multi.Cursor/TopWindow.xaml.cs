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
using static Multi.Cursor.Output;

namespace Multi.Cursor
{
    /// <summary>
    /// Interaction logic for TopWindow.xaml
    /// </summary>
    public partial class TopWindow : Window
    {
        private double HORIZONTAL_PADDING = Utils.MmToDips(Config.HORIZONTAL_PADDING_MM);
        private double GUTTER = Utils.MmToDips(Config.GRID_GUTTER_MM);

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("User32.dll")]
        private static extern void EnableMouseInPointer(bool fEnable);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private Random _random = new Random();

        private GridNavigator _gridNavigator;
        private List<Grid> _gridColumns = new List<Grid>(); // List of grid columns
        private static Dictionary<int, List<SButton>> _widthButtons = new Dictionary<int, List<SButton>>(); // Dictionary to hold buttons by their width multiples

        public TopWindow()
        {
            InitializeComponent();
            this.DataContext = this; // Set DataContext for data binding

            EnableMouseInPointer(true);
            SetForegroundWindow(new WindowInteropHelper(this).Handle);

            _gridNavigator = new GridNavigator(Config.FRAME_DUR_MS / 1000.0);

            foreach (int wm in Experiment.BUTTON_WIDTHS_MULTIPLES)
            {
                _widthButtons.TryAdd(wm, new List<SButton>());
            }

        }

        public void GenerateGrid(params Func<Grid>[] columnCreators)
        {
            // Clear any existing columns from the canvas and the list before generating new ones
            canvas.Children.Clear();
            _gridColumns.Clear();

            double currentLeftPosition = HORIZONTAL_PADDING; // Start with the initial padding

            foreach (var createColumnFunc in columnCreators)
            {
                Grid newColumnGrid = createColumnFunc(); // Create the new column Grid

                // Set its position on the Canvas
                Canvas.SetLeft(newColumnGrid, currentLeftPosition);
                Canvas.SetTop(newColumnGrid, HORIZONTAL_PADDING); // Assuming all columns start at the same top padding

                // Add to the Canvas
                canvas.Children.Add(newColumnGrid);

                // Add to our internal list for tracking/future reference
                _gridColumns.Add(newColumnGrid);

                // Register buttons in this column
                RegisterButtons(newColumnGrid);

                // Force a layout pass on the newly added column to get its ActualWidth
                // This is crucial because the next column's position depends on this one's actual size.
                newColumnGrid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                newColumnGrid.Arrange(new Rect(newColumnGrid.DesiredSize));

                // Update the currentLeftPosition for the next column, adding the current column's width and the gutter
                currentLeftPosition += newColumnGrid.ActualWidth + HORIZONTAL_PADDING;

                Debug.WriteLine($"Added column. Current left: {currentLeftPosition} DIPs. Column width: {newColumnGrid.ActualWidth}");
            }
        }

        private void RegisterButtons(Grid column)
        {
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
                        }
                    }
                }
            }
        }

        public void SelectRandButtonByWidth(int wMult)
        {
            SButton selectedButton = _widthButtons[wMult].GetRandomElement(); // Get a random button from the list for that width
            if (selectedButton != null)
            {
                // Highlight the selected button
                selectedButton.Background = Config.GRID_TARGET_COLOR;
                TrialInfo<SideWindow>($"Selected button with width multiple {wMult}: {selectedButton.Content}");
            }
            else
            {
                TrialInfo<SideWindow>($"No buttons found for width multiple {wMult}.");
            }
        }

        public void ActivateGridNavigator()
        {
            _gridNavigator.Activate();
        }

        public void DeactivateGridNavigator()
        {
            _gridNavigator.Deactivate();
        }
    }
}
