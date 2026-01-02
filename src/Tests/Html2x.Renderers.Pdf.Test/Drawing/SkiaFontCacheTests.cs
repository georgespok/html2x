using Html2x.Abstractions.File;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Renderers.Pdf.Drawing;
using Moq;
using Shouldly;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Test.Drawing;

public sealed class SkiaFontCacheTests
{
    [Fact]
    public void Build_SkipsNonFontFilesAndDisposesNonDefaultTypefaces()
    {
        var fileDirectory = new Mock<IFileDirectory>(MockBehavior.Strict);
        var typefaceFactory = new Mock<ISkiaTypefaceFactory>(MockBehavior.Strict);

        fileDirectory.Setup(x => x.DirectoryExists("fonts")).Returns(true);
        fileDirectory.Setup(x => x.EnumerateFiles("fonts", "*.*", true)).Returns(new[]
        {
            "fonts\\a.ttf",
            "fonts\\b.otf",
            "fonts\\c.ttc",
            "fonts\\readme.txt"
        });
        fileDirectory.Setup(x => x.GetExtension(It.IsAny<string>()))
            .Returns((string path) => Path.GetExtension(path));

        using var aTypeface = SKTypeface.FromFamilyName("Inter");
        using var bTypeface = SKTypeface.FromFamilyName("Inter");
        using var cTypeface = SKTypeface.FromFamilyName("Inter");

        typefaceFactory.Setup(x => x.FromFile("fonts\\a.ttf")).Returns(aTypeface);
        typefaceFactory.Setup(x => x.FromFile("fonts\\b.otf")).Returns(bTypeface);
        typefaceFactory.Setup(x => x.FromFile("fonts\\c.ttc", 0)).Returns(cTypeface);
        typefaceFactory.Setup(x => x.FromFile("fonts\\c.ttc", 1)).Returns((SKTypeface?)null);

        var faces = FontDirectoryIndex.Build(fileDirectory.Object, typefaceFactory.Object, "fonts");

        faces.Count.ShouldBe(3);
        faces.Select(face => face.Path).ShouldBe(new[] { "fonts\\a.ttf", "fonts\\b.otf", "fonts\\c.ttc" }, ignoreOrder: true);

        fileDirectory.VerifyAll();
        typefaceFactory.VerifyAll();
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

    public static IEnumerable<object[]> GetTypefaceBranchCases()
    {
        // Existing file path branch.
        yield return ["existing-file.ttf", true, false, Array.Empty<string>()];

        // Directory path branch.
        yield return ["fonts-dir", false, true, new[] { "fonts-dir\\a.ttf", "fonts-dir\\b.otf", "fonts-dir\\c.ttc" }];

        // No path match: system fallback branch.
        yield return ["missing", false, false, Array.Empty<string>()];
    }

    [Theory]
    [MemberData(nameof(GetTypefaceBranchCases))]
    public void GetTypeface_UsesExpectedPathBranchAndCachesResults(
        string fontPath,
        bool fileExists,
        bool directoryExists,
        string[] enumeratedFiles)
    {
        var fileDirectory = new Mock<IFileDirectory>(MockBehavior.Strict);
        fileDirectory.Setup(x => x.FileExists(fontPath)).Returns(fileExists);

        if (!fileExists)
        {
            fileDirectory.Setup(x => x.DirectoryExists(fontPath)).Returns(directoryExists);
        }

        if (!fileExists && directoryExists)
        {
            fileDirectory.Setup(x => x.EnumerateFiles(fontPath, "*.*", true)).Returns(enumeratedFiles);
            fileDirectory.Setup(x => x.GetExtension(It.IsAny<string>())).Returns((string p) => Path.GetExtension(p));
        }

        var typefaceFactory = new Mock<ISkiaTypefaceFactory>(MockBehavior.Strict);

        if (fileExists)
        {
            typefaceFactory.Setup(x => x.FromFile(fontPath)).Returns(SKTypeface.Default);
        }
        else if (directoryExists)
        {
            foreach (var file in enumeratedFiles.Where(f => !f.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase)))
            {
                typefaceFactory.Setup(x => x.FromFile(file)).Returns(SKTypeface.Default);
            }

            foreach (var ttc in enumeratedFiles.Where(f => f.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase)))
            {
                typefaceFactory.Setup(x => x.FromFile(ttc, 0)).Returns((SKTypeface?)null);
            }
        }
        else
        {
            typefaceFactory.Setup(x => x.FromFamilyName(It.IsAny<string>(), It.IsAny<SKFontStyle>())).Returns(SKTypeface.Default);
        }

        using var cache = new SkiaFontCache(fontPath, fileDirectory.Object, typefaceFactory.Object);

        var keyFamily = directoryExists ? SKTypeface.Default.FamilyName : "Inter";
        var key = new FontKey(keyFamily, FontWeight.W700, FontStyle.Normal);
        var first = cache.GetTypeface(key);
        var second = cache.GetTypeface(key);

        first.ShouldNotBeNull();
        second.ShouldBeSameAs(first);

        if (fileExists)
        {
            fileDirectory.Verify(x => x.DirectoryExists(It.IsAny<string>()), Times.Never);
            fileDirectory.Verify(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            typefaceFactory.Verify(x => x.FromFile(fontPath), Times.Once);
        }
        else if (directoryExists)
        {
            fileDirectory.Verify(x => x.EnumerateFiles(fontPath, "*.*", true), Times.Once);
            foreach (var file in enumeratedFiles.Where(f => !f.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase)))
            {
                typefaceFactory.Verify(x => x.FromFile(file), Times.AtLeastOnce);
            }

            foreach (var ttc in enumeratedFiles.Where(f => f.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase)))
            {
                typefaceFactory.Verify(x => x.FromFile(ttc, 0), Times.Once);
            }
        }
        else
        {
            typefaceFactory.Verify(x => x.FromFamilyName(It.IsAny<string>(), It.IsAny<SKFontStyle>()), Times.AtLeastOnce);
        }

        fileDirectory.VerifyAll();
        typefaceFactory.VerifyNoOtherCalls();
    }

    [Fact]
    public void GetTypeface_FallsBackToDefaultFamilyWhenCandidatesFail()
    {
        var defaultFamily = SKTypeface.Default.FamilyName;
        defaultFamily.ShouldNotBeNullOrWhiteSpace();

        var fileDirectory = new Mock<IFileDirectory>(MockBehavior.Strict);
        fileDirectory.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);
        fileDirectory.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(false);

        var typefaceFactory = new Mock<ISkiaTypefaceFactory>(MockBehavior.Strict);
        typefaceFactory.Setup(x => x.FromFamilyName(It.Is<string>(s => s != defaultFamily), It.IsAny<SKFontStyle>()))
            .Returns((SKTypeface?)null);
        typefaceFactory.SetupSequence(x => x.FromFamilyName(defaultFamily, It.IsAny<SKFontStyle>()))
            .Returns((SKTypeface?)null)
            .Returns(SKTypeface.Default);

        using var cache = new SkiaFontCache("missing", fileDirectory.Object, typefaceFactory.Object);
        var key = new FontKey("MissingFamily", FontWeight.W700, FontStyle.Italic);

        var typeface = cache.GetTypeface(key);

        typeface.ShouldBeSameAs(SKTypeface.Default);
        typefaceFactory.Verify(x => x.FromFamilyName(defaultFamily, It.IsAny<SKFontStyle>()), Times.Exactly(2));
    }
}
