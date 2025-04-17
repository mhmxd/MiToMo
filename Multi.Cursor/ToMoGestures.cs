using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multi.Cursor
{
    internal interface ToMoGestures
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

        void ThumbSwipe(Location loc);
        void ThumbTap(Location loc); // Not exactly direction, rather position (up/down)
        void ThumbMove(TouchPoint thumbPoint);
        void ThumbUp(TouchPoint indPoint);

        void MiddleTap();
        void RingTap();
        void LittleTap(Location loc);
    }
}
