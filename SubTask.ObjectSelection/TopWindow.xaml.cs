using Common.Settings;
using CommonUI;
using static Common.Constants.ExpEnums;

namespace SubTask.ObjectSelection
{
    /// <summary>
    /// Interaction logic for TopWindow.xaml
    /// </summary>
    public partial class TopWindow : AuxWindow
    {

        public TopWindow()
        {
            InitializeComponent();
            Side = Side.Top;
            //this.DataContext = this; // Set DataContext for data binding

            //EnableMouseInPointer(true);
            //SetForegroundWindow(new WindowInteropHelper(this).Handle); // Bring this window to the foreground

            _gridNavigator = new GridNavigator(ExpEnvironment.FRAME_DUR_MS / 1000.0);

        }


    }
}
