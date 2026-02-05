using System.Windows;
using static Common.Constants.ExpEnums;

namespace SubTask.ObjectSelection
{
    /// <summary>
    /// Interaction logic for SideWindow.xaml
    /// </summary>
    public partial class SideWindow : AuxWindow
    {
        public string WindowTitle { get; set; }

        private Point _relPos;
        public Point Rel_Pos
        {
            get { return _relPos; }
            set { _relPos = value; }
        }

        public SideWindow(Side side, Point relPos)
        {
            InitializeComponent();
            //WindowTitle = title;
            Side = side;

            _relPos = relPos;
        }

    }
}
