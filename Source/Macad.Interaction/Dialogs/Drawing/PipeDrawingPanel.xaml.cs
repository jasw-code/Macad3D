using Macad.Common;
using Macad.Common.Serialization;
using Macad.Core.Drawing;
using Macad.Core.Topology;
using System;
using System.ComponentModel;

namespace Macad.Interaction.Dialogs;

[SerializeType]
public class PipeDrawingSettings : BaseObject
{
    // To be defined later
}

//--------------------------------------------------------------------------------------------------

public partial class PipeDrawingPanel : DrawingExportPanelBase
{
    #region Properties

    public PipeDrawingSettings Settings
    {
        get;
        set
        {
            field = value;
            DataContext = value;
        }
    }

    //--------------------------------------------------------------------------------------------------

    #endregion

    #region Constructors

    readonly Body _Body;

    //--------------------------------------------------------------------------------------------------

    public PipeDrawingPanel(Body body)
    {
        _Body = body ?? throw new ArgumentNullException(nameof(body));

        Settings = InteractiveContext.Current.LoadLocalSettings<PipeDrawingSettings>(nameof(PipeDrawingPanel))
                   ?? new PipeDrawingSettings();
        Settings.PropertyChanged += _Settings_PropertyChanged;
        DataContext = this;
        _RecreateDrawing();

        InitializeComponent();
    }

    //--------------------------------------------------------------------------------------------------

    void _Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
    }

    //--------------------------------------------------------------------------------------------------

    public override void CommitSettings()
    {
        InteractiveContext.Current.SaveLocalSettings(nameof(PipeDrawingPanel), Settings);
    }

    //--------------------------------------------------------------------------------------------------

    void _RecreateDrawing()
    {
        Drawing drawing = new()
        {
            Name = "Pipe"
        };
        drawing.Add(PipeDrawing.Create(_Body));
        Drawing = drawing;
    }

    //--------------------------------------------------------------------------------------------------

    #endregion
}
