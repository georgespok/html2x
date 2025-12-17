using Html2x.Abstractions.File;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Renderers.Pdf.Drawing;
using Moq;
using Shouldly;
using SkiaSharp;

namespace Html2x.Renderers.Pdf.Test.Drawing;

public sealed class SkiaFontCacheTests
{
    public static IEnumerable<object[]> BestMatchCases()
    {
        yield return
        [
            "Inter",
            700,
            true,
            new[]
            {
                new SkiaFontCache.TypefaceCandidate("regular.ttf", 0, SKTypeface.Default, "Inter", 700, IsItalic: false),
                new SkiaFontCache.TypefaceCandidate("italic.ttf", 0, SKTypeface.Default, "Inter", 400, IsItalic: true)
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
                new SkiaFontCache.TypefaceCandidate("w400.ttf", 0, SKTypeface.Default, "Inter", 400, IsItalic: false),
                new SkiaFontCache.TypefaceCandidate("w700.ttf", 0, SKTypeface.Default, "Inter", 700, IsItalic: false)
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
                new SkiaFontCache.TypefaceCandidate("b.ttf", 0, SKTypeface.Default, "Inter", 600, IsItalic: false),
                new SkiaFontCache.TypefaceCandidate("a.ttf", 0, SKTypeface.Default, "Inter", 700, IsItalic: false)
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
                new SkiaFontCache.TypefaceCandidate("a.ttc", 1, SKTypeface.Default, "Inter", 600, IsItalic: false),
                new SkiaFontCache.TypefaceCandidate("a.ttc", 0, SKTypeface.Default, "Inter", 600, IsItalic: false)
            },
            "a.ttc",
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
        var best = SkiaFontCache.FindBestMatchCandidate((SkiaFontCache.TypefaceCandidate[])candidates, family, requestedWeight, wantsItalic);

        best.ShouldNotBeNull();
        best.Path.ShouldBe(expectedPath);
        best.FaceIndex.ShouldBe(expectedFaceIndex);
    }

    [Theory]
    [InlineData("MissingFamily")]
    [InlineData("Other")]
    public void FindBestMatchCandidate_WhenNoFamilyMatches_ReturnsNull(string requestedFamily)
    {
        var candidates = new[]
        {
            new SkiaFontCache.TypefaceCandidate("a.ttf", 0, SKTypeface.Default, "Inter", 400, IsItalic: false),
            new SkiaFontCache.TypefaceCandidate("b.ttf", 0, SKTypeface.Default, "Inter", 700, IsItalic: true)
        };

        var best = SkiaFontCache.FindBestMatchCandidate(candidates, requestedFamily, requestedWeight: 400, wantsItalic: false);

        best.ShouldBeNull();
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
                typefaceFactory.Verify(x => x.FromFile(file), Times.Once);
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
}
