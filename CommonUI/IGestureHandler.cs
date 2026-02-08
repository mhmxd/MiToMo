using System.Windows;
using static Common.Constants.ExpEnums;

namespace CommonUI
{
    public abstract class IGestureHandler
    {

        public virtual void LeftPress() { }
        public virtual void RightPress() { }
        public virtual void TopPress() { }

        public virtual void LeftMove(double dX, double dY) { }

        public virtual void IndexDown(TouchPoint indPoint) { }
        public virtual void IndexTap() { }
        public virtual void IndexMove(double dX, double dY) { }
        public virtual void IndexMove(TouchPoint indPoint) { }
        public virtual void IndexUp() { }

        public virtual void ThumbSwipe(Direction dir) { }
        public virtual void ThumbTap(long downInstant, long upInstant) { }
        public virtual void ThumbMove(TouchPoint thumbPoint) { }
        public virtual void ThumbUp() { }

        public virtual void MiddleTap() { }
        public virtual void RingTap() { }

        public virtual void PinkyTap(Side side) { }

        public virtual void RecordToMoAction(Finger finger, string action, Point point) { }
    }
}
