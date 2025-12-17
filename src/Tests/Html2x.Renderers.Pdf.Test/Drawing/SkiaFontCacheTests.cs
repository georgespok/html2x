using Html2x.Abstractions.File;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Renderers.Pdf.Drawing;
using Moq;
using Shouldly;
using System.Reflection;

namespace Html2x.Renderers.Pdf.Test.Drawing;

public sealed class SkiaFontCacheTests
{
    [Fact]
    public void GetTypeface_WhenFontPathIsExistingFile_DoesNotQueryDirectoryAndCachesResult()
    {
        // Use an existing file path that is guaranteed to exist without coupling tests to repo layout.
        // Skia may fail to parse it as a font; SkiaFontCache should then fall back to SKTypeface.Default.
        var fontPath = Assembly.GetExecutingAssembly().Location;

        var fileDirectory = new Mock<IFileDirectory>(MockBehavior.Strict);
        fileDirectory.Setup(x => x.FileExists(fontPath)).Returns(true);

        using var cache = new SkiaFontCache(fontPath, fileDirectory.Object);

        var key = new FontKey("Inter", FontWeight.W700, FontStyle.Normal);
        var first = cache.GetTypeface(key);
        var second = cache.GetTypeface(key);

        first.ShouldNotBeNull();
        second.ShouldBeSameAs(first);

        fileDirectory.VerifyAll();
    }

    [Fact]
    public void GetTypeface_WhenFontPathIsDirectory_EnumeratesRecursively()
    {
        const string directory = "C:\\fonts";

        var fileDirectory = new Mock<IFileDirectory>(MockBehavior.Strict);
        fileDirectory.Setup(x => x.FileExists(directory)).Returns(false);
        fileDirectory.Setup(x => x.DirectoryExists(directory)).Returns(true);
        fileDirectory.Setup(x => x.EnumerateFiles(directory, "*.*", true)).Returns(Array.Empty<string>());

        using var cache = new SkiaFontCache(directory, fileDirectory.Object);
        var key = new FontKey("Arial", FontWeight.W400, FontStyle.Normal);

        var result = cache.GetTypeface(key);

        result.ShouldNotBeNull();
        fileDirectory.Verify(x => x.EnumerateFiles(directory, "*.*", true), Times.Once);
        fileDirectory.VerifyAll();
    }
}
