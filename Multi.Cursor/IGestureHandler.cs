using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multi.Cursor
{
    internal interface IGestureHandler
    {

        void LeftPress();
        void RightPress();
        void TopPress();

        //void LeftTap();
        //void RightTap();
        //void TopTap();

        void LeftMove(double dX, double dY);

        void IndexDown(TouchPoint indPoint);
        void IndexTap();
        void IndexMove(double dX, double dY);
        void IndexMove(TouchPoint indPoint);
        void IndexUp();

        void ThumbSwipe(Direction dir);
        //void ThumbTap(Side side); // Not exactly direction, rather position (up/down)
        void ThumbTap(long downInstant, long upInstant);
        void ThumbMove(TouchPoint thumbPoint);
        void ThumbUp();

        void MiddleTap();
        void RingTap();

        void PinkyTap(Side side);

        void RecordToMoAction(Finger finger, string action);
    }
}
