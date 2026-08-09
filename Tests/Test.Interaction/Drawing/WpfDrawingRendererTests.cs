using Macad.Core;
using Macad.Core.Drawing;
using Macad.Core.Shapes;
using Macad.Core.Toolkits;
using Macad.Core.Topology;
using Macad.Interaction.Panels;
using Macad.Occt;
using Macad.Occt.Helper;
using Macad.Test.Utils;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Macad.Test.Interaction.Drawing;

[TestFixture]
public class WpfDrawingRendererTests
{
    const string _BasePath = @"Interaction\Drawing\WpfDrawingRenderer";

    readonly Ax3 _Projection = new Ax3(Pnt.Origin, new Vec(1, 1, 1).ToDir(), new Vec(-2, 0, -1).ToDir());
    readonly Ax3 _TopProjection = new Ax3(Pnt.Origin, Dir.DZ, Dir.DX);

    //--------------------------------------------------------------------------------------------------

    [SetUp]
    public void SetUp()
    {
        Context.InitEmpty();
        TestSetup.SetupWpf();
    }

    //--------------------------------------------------------------------------------------------------

    [TearDown]
    public void TearDown()
    {
        Context.Current.Deinit();
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    public void Simple()
    {
        // Create simple geometry
        var imprint = TestGeomGenerator.CreateImprint();
        Assert.IsTrue(imprint.Make(Shape.MakeFlags.None));

        // Create WPF preview
        var xaml = RenderHlrToXaml(false, _Projection, null, imprint.Body);

        // Compare
        AssertHelper.IsSameTextFile(Path.Combine(_BasePath, "Simple.xaml"), xaml, AssertHelper.TextCompareFlags.IgnoreFloatPrecision);
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    public void PolySimple()
    {
        // Create simple geometry
        var imprint = TestGeomGenerator.CreateImprint();
        Assert.IsTrue(imprint.Make(Shape.MakeFlags.None));

        // Create WPF preview
        var xaml = RenderHlrToXaml(true, _Projection, null, imprint.Body);

        // Compare
        AssertHelper.IsSameTextFile(Path.Combine(_BasePath, "PolySimple.xaml"), xaml, AssertHelper.TextCompareFlags.IgnoreFloatPrecision);
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    public void Complex()
    {
        // Load geometry
        var body = TestData.GetBodyFromBRep(@"SourceData\Brep\Motor-c.brep");

        // Create WPF preview
        var xaml = RenderHlrToXaml(false, _Projection, null, body);

        // Compare
        AssertHelper.IsSameTextFile(Path.Combine(_BasePath, "Complex.xaml"), xaml, AssertHelper.TextCompareFlags.IgnoreFloatPrecision);
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    public void RudderBlade()
    {
        // Load geometry
        var body = TestData.GetBodyFromBRep(@"SourceData\Brep\Rudder.brep");

        // Create WPF preview
        var xaml = RenderHlrToXaml(false, _Projection, null, body);

        // Compare
        AssertHelper.IsSameTextFile(Path.Combine(_BasePath, "RudderBlade.xaml"), xaml, AssertHelper.TextCompareFlags.IgnoreFloatPrecision);
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    public void Circle()
    {
        // Create simple geometry
        var imprint = TestGeomGenerator.CreateImprint();
        Assert.IsTrue(imprint.Make(Shape.MakeFlags.None));

        // Create WPF preview
        var xaml = RenderHlrToXaml(false, _TopProjection, null, imprint.Body);

        // Compare
        AssertHelper.IsSameTextFile(Path.Combine(_BasePath, "Circle.xaml"), xaml, AssertHelper.TextCompareFlags.IgnoreFloatPrecision);
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    public void CircleArc()
    {
        // Create simple geometry
        var imprint = TestGeomGenerator.CreateImprint();
        Assert.IsTrue(imprint.Make(Shape.MakeFlags.None));

        // Cut circle
        var box = TestGeomGenerator.CreateBox();
        Assert.IsTrue(box.Make(Shape.MakeFlags.None));

        // Create WPF preview
        var xaml = RenderHlrToXaml(false, _TopProjection, null, imprint.Body, box.Body);

        // Compare
        AssertHelper.IsSameTextFile(Path.Combine(_BasePath, "CircleArc.xaml"), xaml, AssertHelper.TextCompareFlags.IgnoreFloatPrecision);
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    public void Ellipse()
    {
        // Create simple geometry
        var imprint = TestGeomGenerator.CreateImprint(TestSketchGenerator.SketchType.Ellipse);
        Assert.IsTrue(imprint.Make(Shape.MakeFlags.None));

        // Create WPF preview
        var xaml = RenderHlrToXaml(false, _TopProjection, null, imprint.Body);

        // Compare
        AssertHelper.IsSameTextFile(Path.Combine(_BasePath, "Ellipse.xaml"), xaml, AssertHelper.TextCompareFlags.IgnoreFloatPrecision);
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    [Description("Correct transformation of ellipse coordinates if they have a translation")]
    public void EllipseTranslated()
    {
        // Create simple geometry
        var imprint = TestGeomGenerator.CreateImprint(TestSketchGenerator.SketchType.Ellipse);
        imprint.Body.Position = new Pnt(20, 30, 40);
        Assert.IsTrue(imprint.Make(Shape.MakeFlags.None));

        // Create Hlr Exporter
        var svg = RenderHlrToXaml(false, _TopProjection, HlrEdgeTypes.VisibleSharp, imprint.Body);

        // Write to file and compare
        AssertHelper.IsSameTextFile(Path.Combine(_BasePath, "EllipseTranslated.xaml"), svg, AssertHelper.TextCompareFlags.IgnoreFloatPrecision);
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    public void EllipseArc()
    {
        // Create simple geometry
        var imprint = TestGeomGenerator.CreateImprint(TestSketchGenerator.SketchType.Ellipse);
        Assert.IsTrue(imprint.Make(Shape.MakeFlags.None));

        // Cut circle
        var box = TestGeomGenerator.CreateBox();
        Assert.IsTrue(box.Make(Shape.MakeFlags.None));

        // Create WPF preview
        var xaml = RenderHlrToXaml(false, _TopProjection, null, imprint.Body, box.Body);

        // Compare
        AssertHelper.IsSameTextFile(Path.Combine(_BasePath, "EllipseArc.xaml"), xaml, AssertHelper.TextCompareFlags.IgnoreFloatPrecision);
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    [Description("OCCT 28242")]
    public void PolyComplex()
    {
        // Load geometry
        var body = TestData.GetBodyFromBRep(@"SourceData\Brep\Motor-c.brep");

        // Create WPF preview
        var xaml = RenderHlrToXaml(true, _Projection, null, body);

        // Compare
        AssertHelper.IsSameTextFile(Path.Combine(_BasePath, "PolyComplex.xaml"), xaml, AssertHelper.TextCompareFlags.IgnoreFloatPrecision);
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    public void SimpleContour()
    {
        var source = TestData.GetBodyFromBRep(@"SourceData\Brep\SheetWithOneLayer.brep");
        Assert.That(source?.GetBRep() != null);

        var template = new SliceContourComponent
        {
            Owner = source,
            LayerCount = 1,
        };
        Assert.IsTrue(template.Make());

        var xaml = SerializeToXaml(WpfDrawingRenderer.CreateDrawingGroup(template.CreateDrawing()));
        AssertHelper.IsSameTextFile(Path.Combine(_BasePath, "SimpleContour.xaml"), xaml, AssertHelper.TextCompareFlags.IgnoreFloatPrecision);
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    public void TwoLayerCutout()
    {
        var source = TestData.GetBodyFromBRep(@"SourceData\Brep\SheetWithTwoLayers.brep");
        Assert.That(source?.GetBRep() != null);

        var template = new SliceContourComponent
        {
            Owner = source,
            LayerCount = 2,
        };
        Assert.IsTrue(template.Make());

        var xaml = SerializeToXaml(WpfDrawingRenderer.CreateDrawingGroup(template.CreateDrawing()));
        AssertHelper.IsSameTextFile(Path.Combine(_BasePath, "TwoLayerCutout.xaml"), xaml, AssertHelper.TextCompareFlags.IgnoreFloatPrecision);
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    public void HolesInPaths()
    {
        var source = TestData.GetBodyFromBRep(Path.Combine(@"Exchange\Svg\ExportDrawing", "HolesInPaths_Source.brep"));
        Assert.That(source?.GetBRep() != null);

        var component = new EtchingMaskComponent()
        {
            Owner = source,
            LayerCount = 1
        };
        Assert.IsTrue(component.Make());

        var xaml = SerializeToXaml(WpfDrawingRenderer.CreateDrawingGroup(component.CreateDrawing()));
        AssertHelper.IsSameTextFile(Path.Combine(_BasePath, "HolesInPaths.xaml"), xaml, AssertHelper.TextCompareFlags.IgnoreFloatPrecision);
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    public void TwoLayerEtchMask()
    {
        var source = TestData.GetBodyFromBRep(@"SourceData\Brep\SheetWithTwoLayers.brep");
        Assert.That(source?.GetBRep() != null);

        var component = new EtchingMaskComponent()
        {
            Owner = source,
            LayerCount = 2
        };
        Assert.IsTrue(component.Make());

        var xaml = SerializeToXaml(WpfDrawingRenderer.CreateDrawingGroup(component.CreateDrawing()));
        AssertHelper.IsSameTextFile(Path.Combine(_BasePath, "TwoLayerEtchMask.xaml"), xaml, AssertHelper.TextCompareFlags.IgnoreFloatPrecision);
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    public void MultipleHoles()
    {
        var source = TestData.GetBodyFromBRep(@"SourceData\Brep\ContourMultipleHoles.brep");
        Assert.That(source?.GetBRep() != null);

        var template = new SliceContourComponent()
        {
            Owner = source,
            LayerCount = 2,
        };
        Assert.IsTrue(template.Make());

        var xaml = SerializeToXaml(WpfDrawingRenderer.CreateDrawingGroup(template.CreateDrawing()));
        AssertHelper.IsSameTextFile(Path.Combine(_BasePath, "MultipleHoles.xaml"), xaml, AssertHelper.TextCompareFlags.IgnoreFloatPrecision);
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    public void BoundaryIsClosed()
    {
        var source = TestData.GetBodyFromBRep(Path.Combine(@"Exchange\Svg\ExportDrawing", "BoundaryIsClosed_Source.brep"));
        Assert.That(source?.GetBRep() != null);

        var template = new SliceContourComponent()
        {
            Owner = source,
            LayerCount = 1,
        };
        Assert.IsTrue(template.Make());

        var xaml = SerializeToXaml(WpfDrawingRenderer.CreateDrawingGroup(template.CreateDrawing()));
        AssertHelper.IsSameTextFile(Path.Combine(_BasePath, "BoundaryIsClosed.xaml"), xaml, AssertHelper.TextCompareFlags.IgnoreFloatPrecision);
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    public void LocatedWire()
    {
        var source = TestData.GetBodyFromBRep(@"SourceData\Brep\ContourLocatedWire.brep");
        Assert.That(source?.GetBRep() != null);

        var template = new SliceContourComponent()
        {
            Owner = source,
            LayerCount = 1,
            ReferenceFace = source.Shape.GetSubshapeReference(SubshapeType.Face, 2)
        };

        Assert.IsTrue(template.Make());

        var xaml = SerializeToXaml(WpfDrawingRenderer.CreateDrawingGroup(template.CreateDrawing()));
        AssertHelper.IsSameTextFile(Path.Combine(_BasePath, "LocatedWire.xaml"), xaml, AssertHelper.TextCompareFlags.IgnoreFloatPrecision);
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    public void LengthDimension()
    {
        // Create simple geometry
        var dim = new LengthDimension()
        {
            FirstPoint = new Pnt2d(-10, 5),
            SecondPoint = new Pnt2d(10, 10),
        };

        var drawing = new Macad.Core.Drawing.Drawing();
        drawing.Add(dim);

        var xaml = SerializeToXaml(WpfDrawingRenderer.CreateDrawingGroup(drawing));
        Assert.IsNotNull(xaml);

        // Compare
        AssertHelper.IsSameTextFile(Path.Combine(_BasePath, "LengthDimension.xaml"), xaml, AssertHelper.TextCompareFlags.IgnoreFloatPrecision);
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    public void PipeDrawing()
    {
        var pipe = TestGeomGenerator.CreatePipe();
        var pipeDrawing = Core.Drawing.PipeDrawing.Create(pipe.Body);
        Assert.IsNotNull(pipeDrawing.Extents);

        var drawing = new Macad.Core.Drawing.Drawing();
        drawing.Add(pipeDrawing);

        var xaml = SerializeToXaml(WpfDrawingRenderer.CreateDrawingGroup(drawing));
        Assert.IsNotNull(xaml);

        // Compare
        AssertHelper.IsSameTextFile(Path.Combine(_BasePath, "PipeDrawing.xaml"), xaml, AssertHelper.TextCompareFlags.IgnoreFloatPrecision);
    }

    //--------------------------------------------------------------------------------------------------

    [Test]
    [Description("Tests rendering of an elliptical curve with first parameter <0. This led to a construction exception.")]
    public void EllipseWrappedAsCurve()
    {
        var box = TestGeomGenerator.CreateBox();
        var offset = Offset.Create(box.Body);
        Assert.IsTrue(box.Make(Shape.MakeFlags.None));

        // This test is a bit flaky, so we run it multiple times to catch the crashes
        Ax3 projection = new Ax3(new(-10.55, -0.19, 152.55), new(0.4, 0.8, 0.46));
        MemoryStream xaml = null;
        Assert.That(() => xaml = RenderHlrToXaml(false, projection, HlrEdgeTypes.HiddenSmooth, offset.Body), Throws.Nothing);
        Assert.IsNotNull(xaml);
    }

    //--------------------------------------------------------------------------------------------------
    
    [Test]
    public void EllipseWrapped()
    {
        var body = TestGeomGenerator.CreateHollowCylinder()?.Body;
        Assert.IsNotNull(body);

        var section = CrossSection.Create(body, Pln.XOY.Translated(new Vec(0, 0, 5)));
        Assert.IsNotNull(section);
        AssertHelper.IsMade(section);

        MemoryStream xaml = RenderHlrToXaml(false, _Projection, null, section.Body);
        Assert.IsNotNull(xaml);

        AssertHelper.IsSameTextFile(Path.Combine(_BasePath, "EllipseWrapped.xaml"), xaml, AssertHelper.TextCompareFlags.IgnoreFloatPrecision);
    }

    //--------------------------------------------------------------------------------------------------
    //--------------------------------------------------------------------------------------------------

    #region Helper Methods

    MemoryStream RenderHlrToXaml(bool useTriangulation, Ax3 projection, HlrEdgeTypes? hlrEdgeTypes, params Body[] bodies)
    {
        hlrEdgeTypes ??= HlrEdgeTypes.VisibleSharp | HlrEdgeTypes.VisibleOutline | HlrEdgeTypes.VisibleSmooth
                         | HlrEdgeTypes.HiddenSharp | HlrEdgeTypes.HiddenOutline;
        IBrepSource[] sources = bodies.Select(body => (IBrepSource)new BodyBrepSource(body)).ToArray();
        var hlrBrepDrawing = HlrDrawing.Create(projection, hlrEdgeTypes.Value, sources);
        hlrBrepDrawing.UseTriangulation = useTriangulation;

        var drawing = new Macad.Core.Drawing.Drawing();
        drawing.Add(hlrBrepDrawing);

        return SerializeToXaml(WpfDrawingRenderer.CreateDrawingGroup(drawing));
    }

    //--------------------------------------------------------------------------------------------------

    static MemoryStream SerializeToXaml(System.Windows.Media.Drawing drawing)
    {
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);
        using var xmlWriter = new XmlTextWriter(writer);
        xmlWriter.Formatting = Formatting.Indented;
        xmlWriter.Indentation = 2;
        System.Windows.Markup.XamlWriter.Save(drawing, xmlWriter);
        return new(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    //--------------------------------------------------------------------------------------------------

    #endregion
}
