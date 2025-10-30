using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Panel.Select
{
    public class SButton : Button
    {
        // A static counter to generate unique IDs across all SButton instances
        private static int _nextId = 0;

        // Width ID for the button, used to identify the width of the button in the grid
        public int WidthMultiple = 0;

        // Property to store the unique ID
        public int Id { get; private set; }

        public int LeftId { get; private set; } = -1; // Default to -1 (no neighbor)
        public int RightId { get; private set; } = -1;
        public int TopId { get; private set; } = -1;
        public int BottomId { get; private set; } = -1;

        public static readonly DependencyProperty DisableBackgroundHoverProperty =
         DependencyProperty.Register("DisableBackgroundHover", typeof(bool), typeof(SButton), new PropertyMetadata(false));

        public bool DisableBackgroundHover
        {
            get { return (bool)GetValue(DisableBackgroundHoverProperty); }
            set { SetValue(DisableBackgroundHoverProperty, value); }
        }

        public SButton()
        {
            this.Id = Interlocked.Increment(ref _nextId);
            this.Tag = Id;

            this.Background = Config.BUTTON_DEFAULT_FILL_COLOR; // Set the background color
            this.BorderBrush = Config.BUTTON_DEFAULT_BORDER_COLOR; // Set the border brush for the button
            this.BorderThickness = new Thickness(2); // Set the border thickness
            this.Padding = new Thickness(0); 
            this.Margin = new Thickness(0); // Set the margin to zero

            // Set the default style for the button
            StyleButton();
        }

        private void StyleButton()
        {
            var template = new ControlTemplate(typeof(Button));

            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "MainBorder";
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(BorderThicknessProperty));

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentFactory);

            template.VisualTree = borderFactory;

            // Trigger for background hover (only when not disabled)
            var backgroundHoverTrigger = new MultiTrigger();
            backgroundHoverTrigger.Conditions.Add(new Condition(IsMouseOverProperty, true));
            backgroundHoverTrigger.Conditions.Add(new Condition(DisableBackgroundHoverProperty, false));
            backgroundHoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, Config.BUTTON_HOVER_FILL_COLOR, "MainBorder"));
            template.Triggers.Add(backgroundHoverTrigger);

            // Trigger for border hover (always active)
            var borderHoverTrigger = new Trigger { Property = IsMouseOverProperty, Value = true };
            borderHoverTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, Config.BUTTON_HOVER_BORDER_COLOR, "MainBorder"));
            template.Triggers.Add(borderHoverTrigger);

            this.Template = template;

        }

        /// <summary>
        /// Sets the unique IDs of the four spatial neighbors for this button.
        /// </summary>
        /// <param name="topId">The ID of the button above.</param>
        /// <param name="bottomId">The ID of the button below.</param>
        /// <param name="leftId">The ID of the button to the left.</param>
        /// <param name="rightId">The ID of the button to the right.</param>
        public void SetNeighbors(int topId, int bottomId, int leftId, int rightId)
        {
            this.TopId = topId;
            this.BottomId = bottomId;
            this.LeftId = leftId;
            this.RightId = rightId;

            // Update the Tag dictionary with neighbor IDs
            var tagDict = this.Tag as Dictionary<string, int>;
            if (tagDict != null)
            {
                tagDict["TopId"] = topId;
                tagDict["BottomId"] = bottomId;
                tagDict["LeftId"] = leftId;
                tagDict["RightId"] = rightId;
            }
        }

        public string ToString()
        {
            return $"SButton(Id={Id}, Width={WidthMultiple}, LeftId={LeftId}, RightId={RightId}, TopId={TopId}, BottomId={BottomId})";
        }


    }
}
