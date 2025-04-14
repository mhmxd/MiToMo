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

        void IndexPointDown(TouchPoint indPoint);
        void IndexPointMove(double dX, double dY);
        void IndexPointMove(TouchPoint indPoint);
        void IndexPointUp();

        void ThumbSwipe(Direction dir);
        void ThumbTap(Direction dir); // Not exactly direction, rather position (up/down)
        void ThumbMove(TouchPoint thumbPoint);
        void ThumbUp(TouchPoint indPoint);

        void MiddleTap(Direction dir);
        void RingTap(Direction dir);
        void LittleTap(Direction dir);
    }
}
