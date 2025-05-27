using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes; // For the internal Rectangle type

namespace Multi.Cursor
{
    public class Element : UserControl // No 'partial' if code-only
    {
        private Rectangle _visualRectangle; // Internal Rectangle instance

        // --- Dependency Properties (remain mostly the same) ---
        // (Ensuring IdProperty's default is string.Empty as discussed)
        public static readonly DependencyProperty IdProperty =
            DependencyProperty.Register("Id", typeof(string), typeof(Element), new PropertyMetadata(string.Empty));

        public string Id
        {
            get { return (string)GetValue(IdProperty); }
            set { SetValue(IdProperty, value); }
        }

        public static readonly DependencyProperty ElementWidthProperty =
            DependencyProperty.Register("ElementWidth", typeof(double), typeof(Element), new PropertyMetadata(100.0, OnElementSizeChanged)); // Default value 100.0

        public double ElementWidth
        {
            get { return (double)GetValue(ElementWidthProperty); }
            set { SetValue(ElementWidthProperty, value); }
        }

        public static readonly DependencyProperty ElementHeightProperty =
            DependencyProperty.Register("ElementHeight", typeof(double), typeof(Element), new PropertyMetadata(100.0, OnElementSizeChanged)); // Default value 100.0

        public double ElementHeight
        {
            get { return (double)GetValue(ElementHeightProperty); }
            set { SetValue(ElementHeightProperty, value); }
        }

        public static readonly DependencyProperty ElementFillProperty =
            DependencyProperty.Register("ElementFill", typeof(Brush), typeof(Element), new PropertyMetadata(Config.ELEMENT_DEFAULT_COLOR, OnElementAppearanceChanged));

        public Brush ElementFill
        {
            get { return (Brush)GetValue(ElementFillProperty); }
            set { SetValue(ElementFillProperty, value); }
        }

        public static readonly DependencyProperty ElementStrokeProperty =
            DependencyProperty.Register("ElementStroke", typeof(Brush), typeof(Element), new PropertyMetadata(Brushes.Black, OnElementAppearanceChanged));

        public Brush ElementStroke
        {
            get { return (Brush)GetValue(ElementStrokeProperty); }
            set { SetValue(ElementStrokeProperty, value); }
        }

        public static readonly DependencyProperty ElementStrokeThicknessProperty =
            DependencyProperty.Register("ElementStrokeThickness", typeof(double), typeof(Element), new PropertyMetadata(0.0, OnElementAppearanceChanged)); // Default value 0.0

        public double ElementStrokeThickness
        {
            get { return (double)GetValue(ElementStrokeThicknessProperty); }
            set { SetValue(ElementStrokeThicknessProperty, value); }
        }

        // --- Callbacks for Dependency Property Changes ---
        // These are crucial and should always update the internal Rectangle
        private static void OnElementSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Element element = (Element)d;
            if (element._visualRectangle != null)
            {
                element._visualRectangle.Width = element.ElementWidth;
                element._visualRectangle.Height = element.ElementHeight;
            }
        }

        private static void OnElementAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Element element = (Element)d;
            if (element._visualRectangle != null)
            {
                element._visualRectangle.Fill = element.ElementFill;
                element._visualRectangle.Stroke = element.ElementStroke;
                element._visualRectangle.StrokeThickness = element.ElementStrokeThickness;
            }
        }

        // --- Constructor ---
        public Element()
        {
            _visualRectangle = new Rectangle();
            this.Content = _visualRectangle;
            this.Background = Brushes.Transparent; // For reliable mouse events over transparent areas

            _visualRectangle.Width = ElementWidth; // Will be 100.0 (default)
            _visualRectangle.Height = ElementHeight; // Will be 100.0 (default)
            _visualRectangle.Fill = ElementFill;     // Will be Brushes.Gray (default)
            _visualRectangle.Stroke = ElementStroke; // Will be Brushes.Black (default)
            _visualRectangle.StrokeThickness = ElementStrokeThickness; // Will be 0.0 (default)

            // Attach internal mouse event handlers to the _visualRectangle
            _visualRectangle.MouseEnter += VisualRectangle_MouseEnter;
            _visualRectangle.MouseLeave += VisualRectangle_MouseLeave;
            _visualRectangle.MouseLeftButtonDown += VisualRectangle_MouseLeftButtonDown;
            _visualRectangle.MouseLeftButtonUp += VisualRectangle_MouseLeftButtonUp;
        }

        // --- Internal Mouse Event Handlers for Visual Feedback ---
        // (These remain the same, ensure they reference _visualRectangle)
        private Brush _originalFillBrush;

        private void VisualRectangle_MouseEnter(object sender, MouseEventArgs e)
        {
            _originalFillBrush = _visualRectangle.Fill;
            //_visualRectangle.Fill = Brushes.Gray;
            Console.WriteLine($"Element {Id} (X: {Canvas.GetLeft(this)}, Y: {Canvas.GetTop(this)}) - Mouse Enter");
            OnElementMouseEnter();
        }

        private void VisualRectangle_MouseLeave(object sender, MouseEventArgs e)
        {
            //_visualRectangle.Fill = _originalFillBrush;
            Console.WriteLine($"Element {Id} (X: {Canvas.GetLeft(this)}, Y: {Canvas.GetTop(this)}) - Mouse Leave");
            OnElementMouseLeave();
        }

        private void VisualRectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //_visualRectangle.Fill = Brushes.DarkGreen;
            Console.WriteLine($"Element {Id} (X: {Canvas.GetLeft(this)}, Y: {Canvas.GetTop(this)}) - Mouse Down");
            OnElementMouseDown();
        }

        private void VisualRectangle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //_visualRectangle.Fill = _originalFillBrush;
            _visualRectangle.Fill = Brushes.Red;
            Console.WriteLine($"Element {Id} (X: {Canvas.GetLeft(this)}, Y: {Canvas.GetTop(this)}) - Mouse Up");
            OnElementMouseUp();
        }

        // --- Exposing Custom Routed Events for External Consumption ---
        // (These remain the same)
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
