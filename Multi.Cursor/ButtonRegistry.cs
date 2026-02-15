using CommonUI;
using Multi.Cursor;
using System.Collections.Generic;
using System.Windows.Media;

namespace Multi.Cursor
{
    public static class ButtonRegistry
    {
        // Maps Button ID -> The actual Button object AND the Window it lives in
        private static readonly Dictionary<int, (SButton Button, AuxWindow Window)> _masterMap = new();

        public static void Register(int id, SButton button, AuxWindow window)
        {
            _masterMap[id] = (button, window);
        }

        public static void Clear() => _masterMap.Clear();

        public static void FillButton(int id, Brush color)
        {
            if (_masterMap.TryGetValue(id, out var entry))
            {
                entry.Button.Background = color;
            }
        }
    }
}
