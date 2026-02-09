namespace Common.Logs
{
    public class GestureLog
    {
        public long timestamp;           // timestamp of the gesture (ms since epoch)
        public string finger = "";       // finger performing the gesture
        public string action = "";       // action type (e.g., tap, swipe)
        public string x = "";            // x coordinate of the gesture (if applicable)
        public string y = "";            // y coordinate of the gesture (if applicable)
    }
}
