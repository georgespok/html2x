using Html2x.Renderers.Pdf.Test;
using Html2x.Text;
using Html2x.RenderModel;
using Html2x.Renderers.Pdf.Drawing;
using Shouldly;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Test.Drawing;

public sealed class SkiaFontCacheTests
{
    [Fact]
    public void Build_SkipsNonFontFilesAndDisposesNonDefaultTypefaces()
    {
        var fileDirectory = new TestFileDirectory()
            .AddDirectory("fonts")
            .AddEnumeration(
                "fonts",
                "*.*",
                recursive: true,
                [
                    "fonts\\a.ttf",
                    "fonts\\b.otf",
                    "fonts\\c.ttc",
                    "fonts\\readme.txt"
                ]);

        using var aTypeface = SKTypeface.FromFamilyName("Inter");
        using var bTypeface = SKTypeface.FromFamilyName("Inter");
        using var cTypeface = SKTypeface.FromFamilyName("Inter");

        var typefaceFactory = new TestSkiaTypefaceFactory()
            .AddFileTypeface("fonts\\a.ttf", aTypeface)
            .AddFileTypeface("fonts\\b.otf", bTypeface)
            .AddFileTypeface("fonts\\c.ttc", 0, cTypeface)
            .AddFileTypeface("fonts\\c.ttc", 1, null);

        var faces = FontDirectoryIndex.Build(fileDirectory, typefaceFactory, "fonts");

        faces.Count.ShouldBe(3);
        faces.Select(face => face.Path).ShouldBe(new[] { "fonts\\a.ttf", "fonts\\b.otf", "fonts\\c.ttc" }, ignoreOrder: true);
        fileDirectory.DirectoryExistsCalls.ShouldBe(["fonts", "fonts"]);
        fileDirectory.EnumerateFilesCalls.ShouldBe([new TestFileDirectory.FileEnumerationKey("fonts", "*.*", true)]);
        typefaceFactory.FromFileCalls.ShouldBe(["fonts\\a.ttf", "fonts\\b.otf"]);
        typefaceFactory.FromFileWithFaceIndexCalls.ShouldBe(
            [
                new TestSkiaTypefaceFactory.FileFaceKey("fonts\\c.ttc", 0),
                new TestSkiaTypefaceFactory.FileFaceKey("fonts\\c.ttc", 1)
            ]);
    }

    public static IEnumerable<object[]> BestMatchCases()
    {
        yield return
        [
            "Inter",
            700,
            true,
            new[]
            {
                new FontFaceEntry("regular.ttf", 0, "Inter", 700, IsItalic: false),
                new FontFaceEntry("italic.ttf", 0, "Inter", 400, IsItalic: true)
            },
            "italic.ttf",
            0
        ];

        yield return
        [
            "Inter",
            600,
            false,
            new[]
            {
                new FontFaceEntry("w400.ttf", 0, "Inter", 400, IsItalic: false),
                new FontFaceEntry("w700.ttf", 0, "Inter", 700, IsItalic: false)
            },
            "w700.ttf",
            0
        ];

        yield return
        [
            "Inter",
            650,
            false,
            new[]
            {
                new FontFaceEntry("b.ttf", 0, "Inter", 600, IsItalic: false),
                new FontFaceEntry("a.ttf", 0, "Inter", 700, IsItalic: false)
            },
            "a.ttf",
            0
        ];

        yield return
        [
            "Inter",
            600,
            false,
            new[]
            {
                new FontFaceEntry("a.ttc", 1, "Inter", 600, IsItalic: false),
                new FontFaceEntry("a.ttc", 0, "Inter", 600, IsItalic: false)
            },
            "a.ttc",
            0
        ];

        yield return
        [
            "MissingFamily",
            600,
            false,
            new[]
            {
                new FontFaceEntry("b.ttf", 0, "Beta", 500, IsItalic: false),
                new FontFaceEntry("a.ttf", 0, "Alpha", 700, IsItalic: false)
            },
            "a.ttf",
            0
        ];

        yield return
        [
            "MissingFamily",
            400,
            true,
            new[]
            {
                new FontFaceEntry("upright.ttf", 0, "Alpha", 400, IsItalic: false),
                new FontFaceEntry("italic.ttf", 0, "Beta", 900, IsItalic: true)
            },
            "italic.ttf",
            0
        ];
    }

    [Theory]
    [MemberData(nameof(BestMatchCases))]
    public void FindBestMatchCandidate_SelectsExpectedFace(
        string family,
        int requestedWeight,
        bool wantsItalic,
        object candidates,
        string expectedPath,
        int expectedFaceIndex)
    {
        var key = new FontKey(family, (FontWeight)requestedWeight, wantsItalic ? FontStyle.Italic : FontStyle.Normal);
        var best = FontDirectoryIndex.FindBestMatch((FontFaceEntry[])candidates, key);

        best.ShouldNotBeNull();
        best.Path.ShouldBe(expectedPath);
        best.FaceIndex.ShouldBe(expectedFaceIndex);
    }

    [Fact]
    public void GetTypeface_ResolvedTextRun_UsesAuthoritativeOutcome()
    {
        const string resolvedPath = "fonts\\resolved.ttf";
        var requested = new FontKey("PolicyFamily", FontWeight.W700, FontStyle.Normal);
        var resolved = new ResolvedFont(
            "PolicyFamily",
            FontWeight.W700,
            FontStyle.Normal,
            resolvedPath,
            FilePath: resolvedPath,
            ConfiguredPath: "fonts");
        var run = new TextRun(
            "Trace",
            requested,
            12f,
            PointPt.Zero,
            24f,
            9f,
            3f,
            ResolvedFont: resolved);

        var fileDirectory = new TestFileDirectory()
            .AddFile(resolvedPath);

        var typefaceFactory = new TestSkiaTypefaceFactory()
            .AddFileTypeface(resolvedPath, SKTypeface.Default);

        using var cache = new SkiaFontCache(fileDirectory, typefaceFactory);

        var first = cache.GetTypeface(run);
        var second = cache.GetTypeface(run);

        first.ShouldBeSameAs(SKTypeface.Default);
        second.ShouldBeSameAs(first);
        fileDirectory.FileExistsCalls.ShouldBe([resolvedPath]);
        typefaceFactory.FromFileCalls.ShouldBe([resolvedPath]);
        typefaceFactory.FromFamilyNameCalls.ShouldBeEmpty();
    }

    [Fact]
    public void GetTypeface_TextRunWithoutResolvedFont_ThrowsClearException()
    {
        var requested = new FontKey("PolicyFamily", FontWeight.W700, FontStyle.Normal);
        var run = new TextRun(
            "Trace",
            requested,
            12f,
            PointPt.Zero,
            24f,
            9f,
            3f);
        var fileDirectory = new TestFileDirectory();
        var typefaceFactory = new TestSkiaTypefaceFactory();
        using var cache = new SkiaFontCache(fileDirectory, typefaceFactory);

        var exception = Should.Throw<FontResolutionException>(() => cache.GetTypeface(run));

        exception.Message.ShouldContain("TextRun.ResolvedFont is required before PDF rendering");
        exception.RequestedFont.ShouldBe(requested);
        exception.Text.ShouldBe("Trace");
        typefaceFactory.FromFileCalls.ShouldBeEmpty();
        typefaceFactory.FromFileWithFaceIndexCalls.ShouldBeEmpty();
        typefaceFactory.FromFamilyNameCalls.ShouldBeEmpty();
        fileDirectory.FileExistsCalls.ShouldBeEmpty();
        fileDirectory.DirectoryExistsCalls.ShouldBeEmpty();
        fileDirectory.EnumerateFilesCalls.ShouldBeEmpty();
    }
}
