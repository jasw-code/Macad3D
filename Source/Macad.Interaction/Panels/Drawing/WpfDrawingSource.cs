using Macad.Core.Drawing;
using System.ComponentModel;
using System.Windows;

namespace Macad.Interaction.Panels;

public static class WpfDrawingSource
{
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.RegisterAttached("Source", typeof(DrawingElement), typeof(WpfDrawingSource), new PropertyMetadata(null));

    public static void SetSource(DependencyObject element, DrawingElement value)
    {
        element.SetValue(SourceProperty, value);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public static DrawingElement GetSource(DependencyObject element)
    {
        return (DrawingElement)element.GetValue(SourceProperty);
    }
}
