using System.Windows;
using static Common.Constants.ExpEnums;

namespace CommonUI
{
    public class TObject
    {
        public int Id { get; set; }
        public Point Position { get; set; }
        public Point Center { get; set; }
        public ButtonState State { get; set; }

        public TObject(int id, Point position, Point center)
        {
            Id = id;
            Position = position;
            Center = center;
            State = ButtonState.DEFAULT;
        }

    }
}
