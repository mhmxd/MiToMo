using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes; // For the internal Rectangle type

namespace Panel.Nav
{
    public class Element : UserControl // No 'partial' if code-only
    {
        // Change from Rectangle to Border
        private Border _visualBorder;

        // --- Dependency Properties ---
        // (Id, ElementWidth, ElementHeight remain the same, as they define the *total* size)

        public static readonly DependencyProperty IdProperty =
            DependencyProperty.Register("Id", typeof(string), typeof(Element), new PropertyMetadata(string.Empty));

        public string Id
        {
            get { return (string)GetValue(IdProperty); }
            set { SetValue(IdProperty, value); }
        }

        public static readonly DependencyProperty ElementWidthProperty =
            DependencyProperty.Register("ElementWidth", typeof(double), typeof(Element), new PropertyMetadata(100.0, OnElementSizeChanged));

        public double ElementWidth
        {
            get { return (double)GetValue(ElementWidthProperty); }
            set { SetValue(ElementWidthProperty, value); }
        }

        public static readonly DependencyProperty ElementHeightProperty =
            DependencyProperty.Register("ElementHeight", typeof(double), typeof(Element), new PropertyMetadata(100.0, OnElementSizeChanged));

        public double ElementHeight
        {
            get { return (double)GetValue(ElementHeightProperty); }
            set { SetValue(ElementHeightProperty, value); }
        }

        // ElementFill now maps to Border.Background
        public static readonly DependencyProperty ElementFillProperty =
            DependencyProperty.Register("ElementFill", typeof(Brush), typeof(Element), new PropertyMetadata(Config.BUTTON_DEFAULT_FILL_COLOR, OnElementAppearanceChanged));

        public Brush ElementFill
        {
            get { return (Brush)GetValue(ElementFillProperty); }
            set { SetValue(ElementFillProperty, value); }
        }

        // ElementStroke now maps to Border.BorderBrush
        public static readonly DependencyProperty ElementStrokeProperty =
            DependencyProperty.Register("ElementStroke", typeof(Brush), typeof(Element), new PropertyMetadata(Config.BUTTON_DEFAULT_BORDER_COLOR, OnElementAppearanceChanged));

        public Brush ElementStroke
        {
            get { return (Brush)GetValue(ElementStrokeProperty); }
            set { SetValue(ElementStrokeProperty, value); }
        }

        // ElementStrokeThickness now maps to Border.BorderThickness
        public static readonly DependencyProperty ElementStrokeThicknessProperty =
            DependencyProperty.Register("ElementStrokeThickness", typeof(int), typeof(Element), new PropertyMetadata(Config.ELEMENT_BORDER_THICKNESS, OnElementAppearanceChanged));

        public int ElementStrokeThickness
        {
            get { return (int)GetValue(ElementStrokeThicknessProperty); }
            set { SetValue(ElementStrokeThicknessProperty, value); }
        }

        // --- Callbacks for Dependency Property Changes ---
        private static void OnElementSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Element element = (Element)d;
            if (element._visualBorder != null)
            {
                // Set the Border's Width and Height directly to the Element's desired total size
                element._visualBorder.Width = element.ElementWidth;
                element._visualBorder.Height = element.ElementHeight;
            }
        }

        private static void OnElementAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Element element = (Element)d;
            if (element._visualBorder != null)
            {
                element._visualBorder.Background = element.ElementFill;    // Fill maps to Background
                element._visualBorder.BorderBrush = element.ElementStroke; // Stroke maps to BorderBrush
                // StrokeThickness maps to BorderThickness (uniform thickness)
                element._visualBorder.BorderThickness = new Thickness(element.ElementStrokeThickness);
            }
        }

        // --- Constructor ---
        public Element()
        {
            // 1. Create the internal Border
            _visualBorder = new Border();

            // 2. Set the Content of the UserControl to the Border
            this.Content = _visualBorder;

            // 3. Set the initial properties of the _visualBorder based on Dependency Property defaults
            // These will be picked up by the callbacks, but setting them here ensures immediate visual state
            _visualBorder.Width = ElementWidth;
            _visualBorder.Height = ElementHeight;
            _visualBorder.Background = ElementFill;
            _visualBorder.BorderBrush = ElementStroke;
            _visualBorder.BorderThickness = new Thickness(ElementStrokeThickness); // Must use Thickness struct

            // 4. Set the background of the UserControl itself for reliable mouse events
            this.Background = Brushes.Transparent;

            // 5. Attach internal mouse event handlers to the _visualBorder (or this UserControl)
            // It's often better to attach mouse events directly to the UserControl itself
            // if you want the *entire area* of your custom control to be clickable/hoverable,
            // including its transparent background.
            this.MouseEnter += Element_MouseEnter;
            this.MouseLeave += Element_MouseLeave;
            this.MouseLeftButtonDown += Element_MouseLeftButtonDown;
            this.MouseLeftButtonUp += Element_MouseLeftButtonUp;

            // If you attached to _visualRectangle before, change to _visualBorder, or preferably to 'this'
            // For mouse feedback (fill color changes), you'll still modify _visualBorder.Background
        }

        // --- Internal MOUSE TrialEvent Handlers for Visual Feedback ---
        // Change from VisualRectangle_... to Element_... if attaching to 'this'
        private Brush _originalFillBrush;

        private void Element_MouseEnter(object sender, MouseEventArgs e)
        {
            //_originalFillBrush = _visualBorder.Background; // Store original background
            //_visualBorder.Background = Brushes.LightGreen;
            Console.WriteLine($"Element {Id} (X: {Canvas.GetLeft(this)}, Y: {Canvas.GetTop(this)}) - Mouse Enter");
            OnElementMouseEnter();
        }

        private void Element_MouseLeave(object sender, MouseEventArgs e)
        {
            //_visualBorder.Background = _originalFillBrush; // Restore original background
            Console.WriteLine($"Element {Id} (X: {Canvas.GetLeft(this)}, Y: {Canvas.GetTop(this)}) - Mouse Leave");
            OnElementMouseLeave();
        }

        private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //_visualBorder.Background = Brushes.DarkGreen; // Change background on click
            Console.WriteLine($"Element {Id} (X: {Canvas.GetLeft(this)}, Y: {Canvas.GetTop(this)}) - Mouse Down");
            OnElementMouseDown();
        }

        private void Element_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //_visualBorder.Background = _originalFillBrush; // Restore original background
            Console.WriteLine($"Element {Id} (X: {Canvas.GetLeft(this)}, Y: {Canvas.GetTop(this)}) - Mouse Up");
            OnElementMouseUp();
        }

        // --- Exposing Custom Routed Events (unchanged) ---
        public static readonly RoutedEvent ElementMouseEnterEvent =
            EventManager.RegisterRoutedEvent("ElementMouseEnter", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Element));

        public event RoutedEventHandler ElementMouseEnter
        {
            add { AddHandler(ElementMouseEnterEvent, value); }
            remove { RemoveHandler(ElementMouseEnterEvent, value); }
        }

        protected virtual void OnElementMouseEnter()
        {
            RaiseEvent(new RoutedEventArgs(ElementMouseEnterEvent, this));
        }

        public static readonly RoutedEvent ElementMouseLeaveEvent =
            EventManager.RegisterRoutedEvent("ElementMouseLeave", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Element));

        public event RoutedEventHandler ElementMouseLeave
        {
            add { AddHandler(ElementMouseLeaveEvent, value); }
            remove { RemoveHandler(ElementMouseLeaveEvent, value); }
        }

        protected virtual void OnElementMouseLeave()
        {
            RaiseEvent(new RoutedEventArgs(ElementMouseLeaveEvent, this));
        }

        public static readonly RoutedEvent ElementMouseDownEvent =
            EventManager.RegisterRoutedEvent("ElementMouseDown", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Element));

        public event RoutedEventHandler ElementMouseDown
        {
            add { AddHandler(ElementMouseDownEvent, value); }
            remove { RemoveHandler(ElementMouseDownEvent, value); }
        }

        protected virtual void OnElementMouseDown()
        {
            RaiseEvent(new RoutedEventArgs(ElementMouseDownEvent, this));
        }

        public static readonly RoutedEvent ElementMouseUpEvent =
            EventManager.RegisterRoutedEvent("ElementMouseUp", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Element));

        public event RoutedEventHandler ElementMouseUp
        {
            add { AddHandler(ElementMouseUpEvent, value); }
            remove { RemoveHandler(ElementMouseUpEvent, value); }
        }

        protected virtual void OnElementMouseUp()
        {
            RaiseEvent(new RoutedEventArgs(ElementMouseUpEvent, this));
        }
    }
}
