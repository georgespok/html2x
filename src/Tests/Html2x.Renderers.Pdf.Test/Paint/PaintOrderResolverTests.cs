using Html2x.RenderModel;
using Html2x.Renderers.Pdf.Paint;
using Shouldly;

namespace Html2x.Renderers.Pdf.Test.Paint;

public sealed class PaintOrderResolverTests
{
    private static readonly BorderEdges Border = BorderEdges.Uniform(
        new BorderSide(1f, ColorRgba.Black, BorderLineStyle.Solid));

    [Fact]
    public void Resolve_BlockWithTextImageAndRule_PreservesCurrentTraversalOrder()
    {
        var line = CreateLine(2, "alpha", new RectPt(12f, 14f, 60f, 12f));
        var image = new ImageFragment
        {
            FragmentId = 3,
            Rect = new RectPt(20f, 30f, 40f, 20f),
            ContentRect = new RectPt(21f, 31f, 38f, 18f),
            Src = "image.png",
            Style = new VisualStyle(Borders: Border)
        };
        var rule = new RuleFragment
        {
            FragmentId = 4,
            Rect = new RectPt(10f, 60f, 90f, 2f),
            Style = new VisualStyle(Borders: Border)
        };
        var block = new BlockFragment([line, image, rule])
        {
            FragmentId = 1,
            Rect = new RectPt(10f, 10f, 100f, 70f),
            Style = new VisualStyle(
                BackgroundColor: new ColorRgba(240, 240, 240, 255),
                Borders: Border)
        };

        var commands = new PaintOrderResolver().Resolve(CreatePage(block));

        commands.Select(static command => command.Kind).ShouldBe([
            PaintCommandKind.PageBackground,
            PaintCommandKind.Background,
            PaintCommandKind.Border,
            PaintCommandKind.Text,
            PaintCommandKind.Image,
            PaintCommandKind.Border,
            PaintCommandKind.Rule
        ]);
        commands.Select(static command => command.SourceFragmentId).ShouldBe([
            null,
            1,
            1,
            2,
            3,
            3,
            4
        ]);
        commands.Select(static command => command.CommandIndex).ShouldBe(Enumerable.Range(0, commands.Count));
    }

    [Fact]
    public void Resolve_Table_PreservesCurrentLayeringOrder()
    {
        var line = CreateLine(4, "cell", new RectPt(14f, 16f, 40f, 12f));
        var cell = new TableCellFragment([line])
        {
            FragmentId = 3,
            Rect = new RectPt(12f, 12f, 80f, 30f),
            Style = new VisualStyle(
                BackgroundColor: new ColorRgba(220, 240, 220, 255),
                Borders: Border)
        };
        var row = new TableRowFragment([cell])
        {
            FragmentId = 2,
            Rect = new RectPt(10f, 10f, 90f, 34f),
            Style = new VisualStyle(
                BackgroundColor: new ColorRgba(220, 220, 240, 255),
                Borders: Border)
        };
        var table = new TableFragment([row])
        {
            FragmentId = 1,
            Rect = new RectPt(8f, 8f, 100f, 40f),
            Style = new VisualStyle(
                BackgroundColor: new ColorRgba(240, 240, 240, 255),
                Borders: Border)
        };

        var commands = new PaintOrderResolver().Resolve(CreatePage(table));

        commands.Select(static command => command.Kind).ShouldBe([
            PaintCommandKind.PageBackground,
            PaintCommandKind.Background,
            PaintCommandKind.Background,
            PaintCommandKind.Background,
            PaintCommandKind.Border,
            PaintCommandKind.Border,
            PaintCommandKind.Border,
            PaintCommandKind.Text
        ]);
        commands.Select(static command => command.SourceFragmentId).ShouldBe([
            null,
            1,
            2,
            3,
            1,
            2,
            3,
            4
        ]);
    }

    [Fact]
    public void Resolve_OutOfOrderZValues_PaintsLowerZOrderFirst()
    {
        var first = new BlockFragment
        {
            FragmentId = 10,
            ZOrder = 20,
            Rect = new RectPt(0f, 0f, 10f, 10f),
            Style = new VisualStyle(BackgroundColor: new ColorRgba(10, 10, 10, 255))
        };
        var second = new BlockFragment
        {
            FragmentId = 20,
            ZOrder = 1,
            Rect = new RectPt(20f, 0f, 10f, 10f),
            Style = new VisualStyle(BackgroundColor: new ColorRgba(20, 20, 20, 255))
        };

        var commands = new PaintOrderResolver().Resolve(CreatePage(first, second));

        commands.Skip(1).Select(static command => command.SourceFragmentId).ShouldBe([20, 10]);
        commands.Skip(1).Select(static command => command.ZOrder).ShouldBe([1, 20]);
    }

    [Fact]
    public void Resolve_Fragments_DoesNotMutateSourceFragments()
    {
        var line = CreateLine(2, "stable", new RectPt(12f, 14f, 60f, 12f));
        var block = new BlockFragment([line])
        {
            FragmentId = 1,
            Rect = new RectPt(10f, 10f, 100f, 30f),
            Style = new VisualStyle(BackgroundColor: new ColorRgba(240, 240, 240, 255))
        };
        var originalBlockRect = block.Rect;
        var originalLineRect = line.Rect;
        var originalRunOrigin = line.Runs.ShouldHaveSingleItem().Origin;

        _ = new PaintOrderResolver().Resolve(CreatePage(block));

        block.Rect.ShouldBe(originalBlockRect);
        line.Rect.ShouldBe(originalLineRect);
        line.Runs.ShouldHaveSingleItem().Origin.ShouldBe(originalRunOrigin);
    }

    [Fact]
    public void Resolve_UnknownFragmentType_ThrowsWithClosedFragmentSetGuidance()
    {
        var fragment = new CustomFragment
        {
            FragmentId = 99,
            Rect = new RectPt(0f, 0f, 10f, 10f)
        };

        var exception = Should.Throw<NotSupportedException>(() =>
            new PaintOrderResolver().Resolve(CreatePage(fragment)));

        exception.Message.ShouldContain(nameof(CustomFragment));
        exception.Message.ShouldContain("Unsupported fragment type");
    }

    private static LayoutPage CreatePage(params Fragment[] fragments)
    {
        return new LayoutPage(
            new SizePt(200f, 200f),
            new Spacing(),
            fragments,
            PageNumber: 1,
            PageBackground: new ColorRgba(255, 255, 255, 255));
    }

    private static LineBoxFragment CreateLine(int fragmentId, string text, RectPt rect)
    {
        return new LineBoxFragment
        {
            FragmentId = fragmentId,
            Rect = rect,
            Runs =
            [
                RendererFontTestData.CreateTextRun(
                    text,
                    RendererFontTestData.CreateFont(),
                    12f,
                    new PointPt(rect.X + 1f, rect.Y + 10f),
                    30f,
                    9f,
                    3f)
            ]
        };
    }

    private sealed class CustomFragment : Fragment
    {
    }
}
