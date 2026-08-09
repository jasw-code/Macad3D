using Macad.Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Macad.Interaction.Panels;

public partial class DrawingPreviewPanel : UserControl
{
    public static readonly DependencyProperty DrawingProperty = DependencyProperty.Register(
        nameof(Drawing), typeof(Core.Drawing.Drawing), typeof(DrawingPreviewPanel),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender,
            OnDrawingChanged));

    public Core.Drawing.Drawing Drawing
    {
        get { return (Core.Drawing.Drawing)GetValue(DrawingProperty); }
        set { SetValue(DrawingProperty, value); }
    }

    //--------------------------------------------------------------------------------------------------

    public DrawingPreviewPanel()
    {
        InitializeComponent();
    }

    //--------------------------------------------------------------------------------------------------

    static void OnDrawingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var panel = (DrawingPreviewPanel)d;
        panel._UpdatePreview();
    }

    //--------------------------------------------------------------------------------------------------

    void _UpdatePreview()
    {
        if (Drawing == null)
        {
            Image.Source = null;
            return;
        }

        try
        {
            using (new ProcessingScope(null, "Exporting Drawing"))
            {
                var drawingGroup = WpfDrawingRenderer.CreateDrawingGroup(Drawing);
                Image.Source = new DrawingImage(drawingGroup);
            }
        }
        catch (Exception e)
        {
            Messages.Exception("Exception while rendering drawing preview.", e);
            Image.Source = null;
        }
    }

    //--------------------------------------------------------------------------------------------------

}
