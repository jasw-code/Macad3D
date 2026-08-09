using Macad.Core.Drawing;
using Macad.Interaction.Panels;
using System.Windows;

namespace Macad.Interaction.Dialogs;

public abstract class DrawingExportPanelBase : PanelBase
{
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header), typeof(string), typeof(DrawingExportPanelBase));

    public string Header
    {
        get { return (string)GetValue(HeaderProperty); }
        set { SetValue(HeaderProperty, value); }
    }

    //--------------------------------------------------------------------------------------------------

    public Drawing Drawing
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged();
        }
    }

    //--------------------------------------------------------------------------------------------------

    public abstract void CommitSettings();

    //--------------------------------------------------------------------------------------------------
}
