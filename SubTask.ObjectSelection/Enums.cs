using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubTask.ObjectSelection
{
    public enum Side
    {
        Left, Top, Right, Down, Middle
    }

    public enum Direction
    {
        Up, Down, Left, Right
    }

    [Flags]
    public enum Technique
    {
        TOMO_SWIPE = 0,
        TOMO_TAP = 1,
        TOMO = 2,
        MOUSE = 3
    }

    [Flags]
    public enum Complexity
    {
        Simple = 0,
        Moderate = 1,
        Complex = 2,
    }

    [Flags]
    public enum TaskType
    {
        ONE_OBJ_ONE_FUNC = 0, // One object, one function
        ONE_OBJ_MULTI_FUNC = 1, // One object, multiple functions
        ONE_OBJECT = 2,
        ONE_FUNCTION = 3,
        MULTI_OBJ_ONE_FUNC = 4, // Multiple objects, one function
        MULTI_OBJ_MULTI_FUNC = 5, // Multiple objects, multiple functions
        MULTI_OBJECT = 6,
        MULTI_FUNCTION = 7
    }

    public enum Result
    {
        MISS,
        HIT
    }

    public enum Trial_Action
    {
        CONTINUE = 0,
        MISS = 1
    }

    public enum Finger
    {
        Thumb = 1,
        Index = 2,
        Middle = 3,
        Ring = 4,
        Pinky = 5
    }

    public enum ButtonState
    {
        DEFAULT = 0, MARKED = 1, APPLIED = 2
    }
}
