using Common.Helpers;

namespace Common.Constants
{
    public static class ExpEnums
    {
        public enum ExperimentType
        {
            Practice, Test
        }

        public enum Side
        {
            Left, Top, Right, Down, Middle
        }

        public enum Direction
        {
            Up, Down, Left, Right
        }

        public enum Technique
        {
            TOMO_SWIPE = 0,
            TOMO_TAP = 1,
            TOMO = 2,
            MOUSE = 3
        }

        public enum Complexity
        {
            Simple = 0,
            Moderate = 1,
            Complex = 2,
        }

        public static List<Complexity> GetRandomComplexityList()
        {
            return new List<Complexity>((Enum.GetValues(typeof(Complexity)) as Complexity[])).Shuffle();
        }

        public enum TaskType
        {
            ONE_OBJ_ONE_FUNC = 0, // One object, one function
            ONE_OBJ_MULTI_FUNC = 1, // One object, multiple functions
            ONE_OBJECT = 2,
            ONE_FUNCTION = 3,
            MULTI_OBJ_ONE_FUNC = 4, // Multiple objects, one function
            MULTI_OBJ_MULTI_FUNC = 5, // Multiple objects, multiple functions
            MULTI_OBJECT = 6,
            MULTI_FUNCTION = 7,
            // Subtasks
            FUNCTION_POINT_SELECT = 8,
            MULTI_FUNCTION_SELECT = 9,
            OBJECT_SELECT = 10,
            PANEL_SELECT = 11,
            PANEL_NAVIGATE = 12
        }

        public enum Result
        {
            MISS,
            HIT
        }

        public enum ButtonState
        {
            DEFAULT = 0, MARKED = 1, ENABLED = 2, SELECTED = 3
        }

        public enum Finger
        {
            Thumb = 1,
            Index = 2,
            Middle = 3,
            Ring = 4,
            Pinky = 5
        }

        public static Technique GetDevice(this Technique tech)
        {
            return tech == Technique.TOMO_SWIPE || tech == Technique.TOMO_TAP ? Technique.TOMO : Technique.MOUSE;
        }

        public static bool IsTomo(this Technique tech)
        {
            return tech == Technique.TOMO_SWIPE || tech == Technique.TOMO_TAP || tech == Technique.TOMO;
        }

        public static Side Opposite(this Side side)
        {
            return side switch
            {
                Side.Left => Side.Right,
                Side.Right => Side.Left,
                Side.Top => Side.Down,
                Side.Down => Side.Top,
                _ => throw new ArgumentOutOfRangeException(nameof(side), "Unknown Side value")
            };
        }

        public static Side ToSide(this Direction dir)
        {
            return dir switch
            {
                Direction.Up => Side.Top,
                Direction.Down => Side.Down,
                Direction.Left => Side.Left,
                Direction.Right => Side.Right,
                _ => throw new ArgumentOutOfRangeException(nameof(dir), "Unknown Direction value")
            };
        }

        public static Side GetRandomLR()
        {
            return new List<Side> { Side.Left, Side.Right }.Shuffle().First();
        }
    }
}
