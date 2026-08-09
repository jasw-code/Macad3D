using Macad.Common;
using Macad.Common.Serialization;
using Macad.Core;
using Macad.Core.Drawing;
using Macad.Core.Topology;
using Macad.Occt;
using Macad.Occt.Helper;
using System;
using System.ComponentModel;
using System.Linq;

namespace Macad.Interaction.Dialogs;

[SerializeType]
public class ViewportHlrDrawingSettings : BaseObject
{
    #region Properties

    [SerializeMember]
    public bool VisibleOutline
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged();
        }
    } = true;

    //--------------------------------------------------------------------------------------------------

    [SerializeMember]
    public bool VisibleSmooth
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged();
        }
    }

    //--------------------------------------------------------------------------------------------------

    [SerializeMember]
    public bool VisibleSewn
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged();
        }
    }

    //--------------------------------------------------------------------------------------------------

    [SerializeMember]
    public bool HiddenOutline
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged();
        }
    } = true;

    //--------------------------------------------------------------------------------------------------

    [SerializeMember]
    public bool HiddenSmooth
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged();
        }
    }

    //--------------------------------------------------------------------------------------------------

    [SerializeMember]
    public bool HiddenSewn
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged();
        }
    }

    //--------------------------------------------------------------------------------------------------

    [SerializeMember]
    public bool UseTriangulation
    {
        get;
        set
        {
            field = value;
            RaisePropertyChanged();
        }
    }

    //--------------------------------------------------------------------------------------------------

    #endregion
}

//--------------------------------------------------------------------------------------------------

public partial class ViewportHlrDrawingPanel : DrawingExportPanelBase
{
    #region Properties

    public ViewportHlrDrawingSettings Settings
    {
        get;
        set
        {
            field = value;
            DataContext = value;
        }
    }

    //--------------------------------------------------------------------------------------------------

    public bool SelectedElementsOptionAvailable { get; }

    public bool IncludeSelectedElementsOnly
    {
        get;
        set
        {
            field = value;
            _RecreateDrawing();
        }
    }

    //--------------------------------------------------------------------------------------------------

    #endregion

    #region Constructors

    readonly Viewport _Viewport;
    HlrDrawing HlrBrepDrawing;

    //--------------------------------------------------------------------------------------------------

    public ViewportHlrDrawingPanel(Viewport viewport)
    {
        _Viewport = viewport ?? throw new ArgumentNullException(nameof(viewport));

        Settings = InteractiveContext.Current.LoadLocalSettings<ViewportHlrDrawingSettings>(nameof(ViewportHlrDrawingPanel))
                   ?? new ViewportHlrDrawingSettings();
        Settings.PropertyChanged += _Settings_PropertyChanged;

        DataContext = this;

        SelectedElementsOptionAvailable = InteractiveContext.Current.WorkspaceController.Selection.SelectedEntities.Count > 0;
        IncludeSelectedElementsOnly = SelectedElementsOptionAvailable;

        _RecreateDrawing();

        InitializeComponent();
    }

    //--------------------------------------------------------------------------------------------------

    void _Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if(e.PropertyName == nameof(Settings.UseTriangulation))
        {
            _RecreateDrawing();
        }
        else
        {
            _UpdateEdgeTypes();
        }
    }

    //--------------------------------------------------------------------------------------------------

    public override void CommitSettings()
    {
        InteractiveContext.Current.SaveLocalSettings(nameof(ViewportHlrDrawingPanel), Settings);
    }

    //--------------------------------------------------------------------------------------------------

    void _RecreateDrawing()
    {
        var projection = new Ax3(_Viewport.EyePoint, _Viewport.GetViewDirection().Reversed(), _Viewport.GetRightDirection());
        var hlrEdgeTypes = _GetSelectedEdgeTypes();

        var breps = (SelectedElementsOptionAvailable && IncludeSelectedElementsOnly
                          ? InteractiveContext.Current.WorkspaceController.Selection.SelectedEntities
                          : InteractiveContext.Current.WorkspaceController.VisualObjects.GetVisibleEntities())
                    .OfType<Body>()
                    .Select(body => body.GetTransformedBRep(true))
                    .Where(shape => shape != null);
        var source = new TopoDSBrepSource(breps.ToArray());
        HlrBrepDrawing = HlrDrawing.Create(projection, hlrEdgeTypes, source);
        HlrBrepDrawing.UseTriangulation = Settings.UseTriangulation;

        var drawing = new Drawing();
        drawing.Add(HlrBrepDrawing);
        Drawing = drawing;
    }

    //--------------------------------------------------------------------------------------------------

    void _UpdateEdgeTypes()
    {
        if (HlrBrepDrawing == null)
        {
            _RecreateDrawing();
        }
        else
        {
            HlrBrepDrawing.IncludedEdgeTypes = _GetSelectedEdgeTypes();
            RaisePropertyChanged(nameof(Drawing));
        }
    }

    //--------------------------------------------------------------------------------------------------

    HlrEdgeTypes _GetSelectedEdgeTypes()
    {
        var hlrEdgeTypes = HlrEdgeTypes.None;

        if (Settings.VisibleOutline)
        {
            hlrEdgeTypes |= HlrEdgeTypes.VisibleSharp;
            hlrEdgeTypes |= HlrEdgeTypes.VisibleOutline;
        }
        if (Settings.VisibleSmooth)
            hlrEdgeTypes |= HlrEdgeTypes.VisibleSmooth;
        if (Settings.VisibleSewn)
            hlrEdgeTypes |= HlrEdgeTypes.VisibleSewn;
        if (Settings.HiddenOutline)
        {
            hlrEdgeTypes |= HlrEdgeTypes.HiddenSharp;
            hlrEdgeTypes |= HlrEdgeTypes.HiddenOutline;
        }
        if (Settings.HiddenSmooth)
            hlrEdgeTypes |= HlrEdgeTypes.HiddenSmooth;
        if (Settings.HiddenSewn)
            hlrEdgeTypes |= HlrEdgeTypes.HiddenSewn;

        return hlrEdgeTypes;
    }

    //--------------------------------------------------------------------------------------------------

    #endregion
}
