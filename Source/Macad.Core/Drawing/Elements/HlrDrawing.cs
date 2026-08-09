using Macad.Common;
using Macad.Common.Serialization;
using Macad.Occt;
using Macad.Occt.Helper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Macad.Core.Drawing;

public class HlrDrawing : DrawingElement
{
    static readonly Dictionary<HlrEdgeTypes, StrokeStyle> _StrokeStyles = new()
        {
            { HlrEdgeTypes.VisibleOutline, new(Color.Black, 0.2f, LineStyle.Solid) },
            { HlrEdgeTypes.VisibleSharp, new(Color.Black, 0.2f, LineStyle.Solid) },
            { HlrEdgeTypes.VisibleSmooth, new(Color.Black, 0.1f, LineStyle.Solid) },
            { HlrEdgeTypes.VisibleSewn, new(Color.Black, 0.1f, LineStyle.Solid) },
            { HlrEdgeTypes.HiddenOutline, new(Color.Black, 0.1f, LineStyle.Dash) },
            { HlrEdgeTypes.HiddenSharp, new(Color.Black, 0.1f, LineStyle.Dash) },
            { HlrEdgeTypes.HiddenSmooth, new(Color.Black, 0.05f, LineStyle.ShortDash) },
            { HlrEdgeTypes.HiddenSewn, new(Color.Black, 0.05f, LineStyle.ShortDash)}
        };

    //--------------------------------------------------------------------------------------------------

    [SerializeMember]
    public IBrepSource[] Sources
    {
        get { return _Sources; }
        set
        {
            if (_Sources != value)
            {
                SaveUndo();
                _Sources = value;
                _Invalidate();
                RaisePropertyChanged();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------

    [SerializeMember]
    public Ax3 Projection
    {
        get { return _Projection; }
        set
        {
            if (_Projection != value)
            {
                SaveUndo();
                _Projection = value;
                _Invalidate();
                RaisePropertyChanged();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------

    [SerializeMember]
    public HlrEdgeTypes IncludedEdgeTypes
    {
        get { return _IncludedEdgeTypes; }
        set
        {
            if (_IncludedEdgeTypes != value)
            {
                SaveUndo();
                _IncludedEdgeTypes = value;
                _UpdateExtents();
                RaisePropertyChanged();
            }
        }
    }

    //--------------------------------------------------------------------------------------------------

    [SerializeMember]
    public bool UseTriangulation
    {
        get { return _UseTriangulation; }
        set
        {
            if (_UseTriangulation != value)
            {
                SaveUndo();
                _UseTriangulation = value;
                _Invalidate();
                RaisePropertyChanged();
            }
        }
    }
        
    //--------------------------------------------------------------------------------------------------

    bool _UseTriangulation;
    HlrEdgeTypes _IncludedEdgeTypes;
    Ax3 _Projection;
    IBrepSource[] _Sources;
    readonly Dictionary<HlrEdgeTypes, TopoDS_Shape> _Shapes = new();

    //--------------------------------------------------------------------------------------------------

    public static HlrDrawing Create(Ax3 projection, HlrEdgeTypes includedEdges, params IBrepSource[] sources)
    {
        return new HlrDrawing()
        {
            _Projection = projection,
            _IncludedEdgeTypes = includedEdges,
            _Sources = sources,
        };
    }

    //--------------------------------------------------------------------------------------------------

    void _Invalidate()
    {
        _Shapes.Clear();
        Extents = null;
    }

    //--------------------------------------------------------------------------------------------------
        
    protected override void CalculateExtents()
    {
        _EnsureShapes();
    }

    //--------------------------------------------------------------------------------------------------

    bool _EnsureShapes()
    {
        if (_Shapes.Count > 0)
            return true; // everything is ready

        if (_Sources == null)
            return true; // nothing to do

        var breps = _Sources.SelectMany(s => s.GetBreps());

        // Create algo instance
        HlrBRepAlgoBase hlrAlgo = _UseTriangulation ? new HlrBRepAlgoPoly(breps) : new HlrBRepAlgo(breps);

        // Set Projection
        hlrAlgo.SetProjection(_Projection);

        // Do it
        hlrAlgo.Update();

        // Fetch layer
        foreach (var edgeType in Enum.GetValues<HlrEdgeTypes>())
        {
            TopoDS_Shape shape = hlrAlgo.GetResult(edgeType);
            if (shape != null)
            {
                _Shapes[edgeType] = shape;
            }
        }

        _UpdateExtents();

        return true;
    }

    //--------------------------------------------------------------------------------------------------

    void _UpdateExtents()
    {
        Bnd_Box2d aabb = new Bnd_Box2d();
        foreach (var edgeType in Enum.GetValues<HlrEdgeTypes>())
        {
            if (!IncludedEdgeTypes.HasFlag(edgeType))
                continue;
            if (!_Shapes.TryGetValue(edgeType, out var shape))
                continue;

            // Update bounding rect
            double xmin = 0;
            double xmax = 0;
            double ymin = 0;
            double ymax = 0;
            double zmin = 0;
            double zmax = 0;
            shape.BoundingBox()?.Get(ref xmin, ref ymin, ref zmin, ref xmax, ref ymax, ref zmax);
            aabb.Add(new Pnt2d(xmin, ymin));
            aabb.Add(new Pnt2d(xmax, ymax));
        }
        Extents = aabb;
    }

    //--------------------------------------------------------------------------------------------------
        
    public override bool Render(IDrawingRenderer renderer)
    {
        if (!_EnsureShapes())
        {
            return false;
        }

        if (renderer.RenderElement(this))
        {
            return true;
        }

        // Render from hidden to visible and from inline to outline, so that the more relevant edges
        // are drawn on top of the less important ones.
        bool res = true;
        res &= _RenderLayer(renderer, HlrEdgeTypes.HiddenSmooth);
        res &= _RenderLayer(renderer, HlrEdgeTypes.HiddenSewn);
        res &= _RenderLayer(renderer, HlrEdgeTypes.VisibleSmooth);
        res &= _RenderLayer(renderer, HlrEdgeTypes.VisibleSewn);
        res &= _RenderLayer(renderer, HlrEdgeTypes.HiddenOutline);
        res &= _RenderLayer(renderer, HlrEdgeTypes.HiddenSharp);
        res &= _RenderLayer(renderer, HlrEdgeTypes.VisibleOutline);
        res &= _RenderLayer(renderer, HlrEdgeTypes.VisibleSharp);

        return res;
    }

    //--------------------------------------------------------------------------------------------------

    bool _RenderLayer(IDrawingRenderer renderer, HlrEdgeTypes type)
    {
        if (!IncludedEdgeTypes.HasFlag(type))
        {
            return true;
        }

        if (!_Shapes.TryGetValue(type, out var shape))
        {
            return true; // Nothing to render
        }

        renderer.SetStyle(_StrokeStyles[type], null, null);
        renderer.BeginGroup(type.ToString());

        bool res = BrepRenderHelper.RenderShape(renderer, shape);

        renderer.EndGroup();
        return res;
    }

    //--------------------------------------------------------------------------------------------------

    internal TopoDS_Shape GetBrepShape(HlrEdgeTypes type)
    {
        if (!_EnsureShapes())
        {
            return null;
        }
        return _Shapes.GetValueOrDefault(type);
    }

    //--------------------------------------------------------------------------------------------------

}