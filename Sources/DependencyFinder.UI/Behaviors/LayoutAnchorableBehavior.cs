using AvalonDock;
using AvalonDock.Layout;
using Microsoft.Xaml.Behaviors;
using System.Windows;

namespace DependencyFinder.UI.Behaviors
{
    public class LayoutAnchorableBehavior : Behavior<DockingManager>
    {
        private static LayoutAnchorable _panel;

        public bool IsPanelActive
        {
            get { return (bool)GetValue(IsPanelActiveProperty); }
            set { SetValue(IsPanelActiveProperty, value); }
        }

        public static readonly DependencyProperty IsPanelActiveProperty =
            DependencyProperty.Register("IsPanelActive", typeof(bool), typeof(LayoutAnchorableBehavior), new PropertyMetadata(false, PropertyChanged2));

        public LayoutAnchorable Panel
        {
            get { return (LayoutAnchorable)GetValue(PanelProperty); }
            set { SetValue(PanelProperty, value); }
        }

        public static readonly DependencyProperty PanelProperty =
            DependencyProperty.Register("Panel", typeof(LayoutAnchorable), typeof(LayoutAnchorableBehavior), new PropertyMetadata(null, PropertyChanged));

        private static void PropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (_panel == null)
            {
                _panel = e.NewValue as LayoutAnchorable;
            }
        }

        private static void PropertyChanged2(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            bool value = (bool)e.NewValue;
            if (value)
            {
                _panel.IsActive = true;
            }
        }
    }
}