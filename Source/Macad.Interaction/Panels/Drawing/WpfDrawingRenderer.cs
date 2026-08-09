using Macad.Common;
using Macad.Core.Drawing;
using Macad.Occt;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Macad.Core;
using FontStyle = Macad.Core.Drawing.FontStyle;

namespace Macad.Interaction.Panels;

/// <summary>
/// Renderer that converts a Drawing into a WPF DrawingGroup.
/// </summary>
public sealed class WpfDrawingRenderer : IDrawingRenderer, IRendererCapabilities
{
    #region Public API

    /// <summary>
    /// The root drawing group that contains all rendered elements.
    /// </summary>
    public DrawingGroup RootDrawingGroup { get; }

    //--------------------------------------------------------------------------------------------------

    /// <summary>
    /// Creates a WPF DrawingGroup from the given drawing.
    /// </summary>
    /// <param name="drawing">The drawing to convert.</param>
    /// <returns>A WPF DrawingGroup representing the drawing.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the drawing is null.</exception>
    public static DrawingGroup CreateDrawingGroup(Core.Drawing.Drawing drawing)
    {
        if (drawing == null)
        {
            throw new ArgumentNullException(nameof(drawing));
        }

        var renderer = new WpfDrawingRenderer();
        var rootDrawing = renderer.RootDrawingGroup;

        drawing.Render(renderer);

        // Translate so the drawing origin is at the top-left.
        var boundingBox = drawing.GetBoundingBox();
        if (!boundingBox.IsVoid())
        {
            double xmin = 0, xmax = 0, ymin = 0, ymax = 0;
            boundingBox.Get(ref xmin, ref ymin, ref xmax, ref ymax);
            rootDrawing.Transform = new TransformGroup
            {
                Children =
                [
                    new TranslateTransform(-xmin, ymax)
                ]
            };
        }

        return rootDrawing;
    }

    //--------------------------------------------------------------------------------------------------

    #endregion

    #region Members and c'tor

    readonly Stack<DrawingGroup> _GroupStack = new();
    DrawingElement _CurrentElement;
    StrokeStyle _Stroke;
    FillStyle _Fill;
    FontStyle _Font;
    PathGeometry _PathGeometry;
    PathFigure _PathFigure;
    Point _PathPosition;
    DrawingGroup _CurrentGroup;

    //--------------------------------------------------------------------------------------------------

    WpfDrawingRenderer()
    {
        RootDrawingGroup = new()
        {
            ClipGeometry = null,
            Transform = null
        };
        _CurrentGroup = RootDrawingGroup;
    }

    //--------------------------------------------------------------------------------------------------

    #endregion

    #region IRendererCapabilities

    int IRendererCapabilities.BezierCurveMaxDegree => 3;
    bool IRendererCapabilities.CircularArcAsCurve => true;
    bool IRendererCapabilities.EllipticalArcAsCurve => true;

    //--------------------------------------------------------------------------------------------------

    #endregion

    #region IDrawingRenderer

    IRendererCapabilities IDrawingRenderer.Capabilities => this;

    //--------------------------------------------------------------------------------------------------

    void IDrawingRenderer.BeginGroup(string name)
    {
        var group = new DrawingGroup();
        WpfDrawingSource.SetSource(group, _CurrentElement);

        _CurrentGroup.Children.Add(group);
        _GroupStack.Push(_CurrentGroup);
        _CurrentGroup = group;
    }

    //--------------------------------------------------------------------------------------------------

    void IDrawingRenderer.EndGroup()
    {
        _FinalizePath();

        if (_GroupStack.Count > 0)
        {
            _CurrentGroup = _GroupStack.Pop();
        }
    }

    //--------------------------------------------------------------------------------------------------

    void IDrawingRenderer.SetStyle(StrokeStyle stroke, FillStyle fill, FontStyle font)
    {
        _FinalizePath();
        _Stroke = stroke;
        _Fill = fill;
        _Font = font;
    }

    //--------------------------------------------------------------------------------------------------

    void IDrawingRenderer.BeginPath()
    {
        _FinalizePath();
        _PathGeometry = new PathGeometry
        {
            FillRule = FillRule.EvenOdd
        };
    }

    //--------------------------------------------------------------------------------------------------

    void IDrawingRenderer.EndPath()
    {
        _FinalizePath();
    }

    //--------------------------------------------------------------------------------------------------

    void IDrawingRenderer.EndPathSegment()
    {
        if (_PathFigure?.Segments.Count > 0)
        {
            if (!double.IsNaN(_PathPosition.X) && _AreEqual(_PathPosition, _PathFigure.StartPoint))
            {
                _PathFigure.IsClosed = true;
            }
        }
        _PathFigure = null;
    }


    //--------------------------------------------------------------------------------------------------

    void IDrawingRenderer.Line(Pnt2d start, Pnt2d end)
    {
        if (_PathGeometry != null)
        {
            _AddPathSegment(_ToPoint(start), _ToPoint(end), new LineSegment(_ToPoint(end), true));
        }
        else
        {
            _AddGeometryDrawing(new LineGeometry(_ToPoint(start), _ToPoint(end)));
        }
    }

    //--------------------------------------------------------------------------------------------------

    void IDrawingRenderer.Circle(Pnt2d center, double radius, double startAngle, double endAngle)
    {
        if (startAngle.Distance(endAngle).IsEqual(Maths.DoublePI, 0.00001))
        {
            var geometry = new EllipseGeometry(new Rect(center.X - radius, -center.Y - radius, radius * 2, radius * 2));
            _AddGeometryDrawing(geometry);
        }
        else
        {
            var circle = new Geom2d_Circle(new Ax2d(center, Dir2d.DX), radius);
            var start = circle.Value(startAngle);
            var end = circle.Value(endAngle);
            var isLargeArc = startAngle.Distance(endAngle) >= Maths.PI;
            var sweep = SweepDirection.Counterclockwise;
            if (endAngle < startAngle)
            {
                sweep = SweepDirection.Clockwise;
            }

            var segment = new ArcSegment(_ToPoint(end), new Size(radius, radius), 0.0, isLargeArc, sweep, true);
            _AddPathSegment(_ToPoint(start), _ToPoint(end), segment);
        }
    }

    //--------------------------------------------------------------------------------------------------

    void IDrawingRenderer.Ellipse(Pnt2d center, double majorRadius, double minorRadius, double rotation, double startAngle, double endAngle)
    {
        if (startAngle.Distance(endAngle).IsEqual(Maths.DoublePI, 0.00001))
        {
            // Full ellipse
            var transform = new TransformGroup
            {
                Children = new TransformCollection
                {
                    new TranslateTransform(center.X, -center.Y),
                    new RotateTransform(-rotation.ToDeg(), center.X, -center.Y)
                }
            };
            var geometry = new EllipseGeometry(new Point(0, 0), majorRadius, minorRadius)
            {
                Transform = transform
            };
            _AddGeometryDrawing(geometry);
        }
    }

    //--------------------------------------------------------------------------------------------------

    void IDrawingRenderer.BezierCurve(Pnt2d[] knots)
    {
        if (knots == null || knots.Length < 2)
            return;

        switch (knots.Length)
        {
            case 2:
                _AddPathSegment(_ToPoint(knots[0]), _ToPoint(knots[1]), new LineSegment(_ToPoint(knots[1]), true));
                break;

            case 3:
                var knot1 = knots[0].Coord + (knots[1].Coord - knots[0].Coord).Multiplied(2.0 / 3.0);
                var knot2 = knots[2].Coord + (knots[1].Coord - knots[2].Coord).Multiplied(2.0 / 3.0);
                _AddPathSegment(_ToPoint(knots[0]), _ToPoint(knots[2]),
                                new BezierSegment(_ToPoint(knot1), _ToPoint(knot2), _ToPoint(knots[2]), true));
                break;

            case 4:
                _AddPathSegment(_ToPoint(knots[0]), _ToPoint(knots[3]),
                                new BezierSegment(_ToPoint(knots[1]), _ToPoint(knots[2]), _ToPoint(knots[3]), true));
                break;
        }
    }

    //--------------------------------------------------------------------------------------------------

    void IDrawingRenderer.Text(string text, Pnt2d position, double rotation)
    {
        _FinalizePath();

        var brush = _CreateBrush(_Stroke?.Color ?? Common.Color.Black);
        var formattedText = _CreateFormattedText(text, _Font, brush);
        var geometry = formattedText.BuildGeometry(_ToPoint(position) - new Vector(0, formattedText.Baseline));

        if (!rotation.IsEqual(0.0, 1e-7))
        {
            var transform = new TransformGroup();
            transform.Children.Add(new RotateTransform(rotation.ToDeg(), position.X, -position.Y));
            geometry.Transform = transform;
        }

        _AddGeometryDrawing(geometry);
    }

    //--------------------------------------------------------------------------------------------------

    bool IDrawingRenderer.RenderElement(DrawingElement element)
    {
        _CurrentElement = element;
        return false;
    }

    //--------------------------------------------------------------------------------------------------

    bool IDrawingRenderer.RenderBrepShape(TopoDS_Shape shape)
    {
        return false;
    }

    //--------------------------------------------------------------------------------------------------

    #endregion

    #region Path & Geometry

    void _AddGeometryDrawing(Geometry geometry)
    {
        var fillBrush = _CreateBrush(_Fill?.Color);
        var pen = _CreatePen(_Stroke);

        if (fillBrush != null || pen != null)
        {
            var drawing = new GeometryDrawing(fillBrush, pen, geometry);
            WpfDrawingSource.SetSource(drawing, _CurrentElement);
            _CurrentGroup.Children.Add(drawing);
        }
    }

    //--------------------------------------------------------------------------------------------------

    void _AddPathSegment(Point startPoint, Point endPoint, PathSegment segment)
    {
        _EnsurePathFigure(startPoint);
        _PathFigure.Segments.Add(segment);
        _PathPosition = endPoint;
    }

    //--------------------------------------------------------------------------------------------------

    void _EnsurePathFigure(Point startPoint)
    {
        if (_PathFigure == null)
        {
            _PathGeometry ??= new PathGeometry
            {
                FillRule = FillRule.EvenOdd
            };
            _PathFigure = new PathFigure
            {
                IsClosed = false
            };
            _PathPosition = new Point(double.NaN, double.NaN);
            _PathGeometry.Figures.Add(_PathFigure);
        }

        if (_PathFigure.Segments.Count == 0 && !double.IsNaN(startPoint.X))
        {
            _PathFigure.StartPoint = startPoint;
        }
    }

    //--------------------------------------------------------------------------------------------------

    void _FinalizePath()
    {
        if (_PathGeometry?.Figures.Count > 0)
        {
            _AddGeometryDrawing(_PathGeometry);
            _PathGeometry = null;
        }

        _PathGeometry = null;
        _PathFigure = null;
    }

    //--------------------------------------------------------------------------------------------------

    #endregion

    #region Helper

    bool _AreEqual(Point point1, Point point2, double tolerance = 0.0001)
    {
        return Math.Abs(point1.X - point2.X) < tolerance && Math.Abs(point1.Y - point2.Y) < tolerance;
    }

    //--------------------------------------------------------------------------------------------------

    Point _ToPoint(Pnt2d point)
    {
        return new Point(point.X, -point.Y);
    }

    //--------------------------------------------------------------------------------------------------

    Point _ToPoint(XY point)
    {
        return new Point(point.X, -point.Y);
    }

    //--------------------------------------------------------------------------------------------------

    #endregion

    #region Styles

    Pen _CreatePen(StrokeStyle stroke)
    {
        if (stroke?.Color == null)
            return null;

        var pen = new Pen
        {
            Brush = _CreateBrush(stroke.Color),
            Thickness = stroke.Width,
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Miter,
            MiterLimit = 1.0
        };

        if (stroke.LineStyle != LineStyle.Solid)
        {
            var pattern = stroke.LineStyle.Pattern();
            if (pattern.Length > 0)
            {
                var dashArray = new DoubleCollection();
                foreach (var value in pattern)
                {
                    dashArray.Add(value / Math.Max(stroke.Width, 0.1));
                }

                pen.DashStyle = new DashStyle(dashArray, 0);
            }
        }

        pen.Freeze();
        return pen;
    }

    //--------------------------------------------------------------------------------------------------

    Brush _CreateBrush(Common.Color? color)
    {
        if (color == null)
            return null;

        var wpfColor = System.Windows.Media.Color.FromRgb((byte)(color.Value.Red * 255.0f),
                                                          (byte)(color.Value.Green * 255.0f),
                                                          (byte)(color.Value.Blue * 255.0f));
        var brush = new SolidColorBrush(wpfColor);
        brush.Freeze();
        return brush;
    }

    //--------------------------------------------------------------------------------------------------

    FormattedText _CreateFormattedText(string text, FontStyle style, Brush brush)
    {
        style ??= DrawingRenderHelper.GetDefaultFontStyle();

        // No null check for 'text' - could throw if null
        return new FormattedText(text, 
                                 CultureInfo.InvariantCulture,
                                 FlowDirection.LeftToRight,
                                 new Typeface(style.Family),
                                 style.Size,
                                 brush,
                                 1.0);
    }

    //--------------------------------------------------------------------------------------------------

    #endregion

}
