using System.Windows;
using static Common.Constants.ExpEnums;

namespace CommonUI
{
    public class TFunction
    {
        public int Id { get; set; }
        public int WidthInUnits { get; set; }
        public Point Center { get; set; }
        public Point Position { get; set; } // Top-left corner of the button
        public int DistanceToObjArea; // in pixels
        public ButtonState State { get; set; }

        public TFunction(int id, int widthInUnits, Point center, Point position)
        {
            Id = id;
            Center = center;
            Position = position;
            WidthInUnits = widthInUnits;
            State = ButtonState.DEFAULT;
        }

        public override string ToString()
        {
            return $"TFunction(Id: {Id}, WidthInUnits: {WidthInUnits}, Center: {Center}, Position: {Position}, State: {State})";
        }

        public string GetPositionStr()
        {
            return $"({Position.X:F2},{Position.Y:F2})";
        }

    }
}
