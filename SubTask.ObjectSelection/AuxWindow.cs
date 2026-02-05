using Common.Helpers;
using CommonUI;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using static Common.Constants.ExpEnums;

namespace SubTask.ObjectSelection
{
    public abstract class AuxWindow : Window
    {
        public Side Side { get; set; } // Side of the window (left, right, top)
                                       // 

        protected SButton _targetButton; // Currently selected button (if any)

        protected Rect _objectConstraintRectAbsolute = new Rect();

        private MouseEventHandler _currentFuncMouseEnterHandler;
        private MouseButtonEventHandler _currentFuncMouseDownHandler;
        private MouseButtonEventHandler _currentFuncMouseUpHandler;
        private MouseEventHandler _currentFuncMouseExitHandler;
        private MouseButtonEventHandler _currentNonFuncMouseDownHandler;

        public void SetObjectConstraintRect(Rect rect)
        {
            _objectConstraintRectAbsolute = rect;
            this.TrialInfo($"Object constraint rect set to: {rect.ToString()}");
        }


        public void ClearAllEventHandlers(UIElement element)
        {
            var eventHandlersStoreField = typeof(UIElement).GetField("_eventHandlersStore",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (eventHandlersStoreField?.GetValue(element) != null)
            {
                eventHandlersStoreField.SetValue(element, null);
            }
        }

    }
}
