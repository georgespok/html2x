using Html2x.Abstractions.File;
using Html2x.Renderers.Pdf.Files;
using Moq;
using Shouldly;

namespace Html2x.Renderers.Pdf.Test.Files;

public sealed class SkiaFontFileDiscoveryTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ListFontFiles_WhenDirectoryBlank_ReturnsEmptyAndDoesNotTouchFileSystem(string? directory)
    {
        var fileDirectory = new Mock<IFileDirectory>(MockBehavior.Strict);

        var result = SkiaFontFileDiscovery.ListFontFiles(fileDirectory.Object, directory!);

        result.ShouldBeEmpty();
        fileDirectory.VerifyNoOtherCalls();
    }

    [Fact]
    public void ListFontFiles_WhenDirectoryMissing_ReturnsEmpty()
    {
        var fileDirectory = new Mock<IFileDirectory>(MockBehavior.Strict);
        fileDirectory.Setup(x => x.DirectoryExists("C:\\fonts")).Returns(false);

        var result = SkiaFontFileDiscovery.ListFontFiles(fileDirectory.Object, "C:\\fonts");

        result.ShouldBeEmpty();
        fileDirectory.VerifyAll();
    }

    [Fact]
    public void ListFontFiles_WhenDirectoryProvided_EnumeratesRecursively()
    {
        var fileDirectory = new Mock<IFileDirectory>(MockBehavior.Strict);
        fileDirectory.Setup(x => x.DirectoryExists("C:\\fonts")).Returns(true);
        fileDirectory.Setup(x => x.EnumerateFiles("C:\\fonts", "*.*", true)).Returns(Array.Empty<string>());

        var result = SkiaFontFileDiscovery.ListFontFiles(fileDirectory.Object, "C:\\fonts");

        result.ShouldBeEmpty();
        fileDirectory.VerifyAll();
    }

    [Fact]
    public void ListFontFiles_FiltersByFontExtensionsAndSortsOrdinalIgnoreCase()
    {
        var fileDirectory = new Mock<IFileDirectory>(MockBehavior.Strict);
        fileDirectory.Setup(x => x.DirectoryExists("C:\\fonts")).Returns(true);
        fileDirectory.Setup(x => x.EnumerateFiles("C:\\fonts", "*.*", true))
            .Returns(
            [
                "C:\\fonts\\Z.TTF",
                "C:\\fonts\\a.otf",
                "C:\\fonts\\m.ttc",
                "C:\\fonts\\note.txt",
                "C:\\fonts\\image.png"
            ]);

        fileDirectory.Setup(x => x.GetExtension(It.IsAny<string>()))
            .Returns((string path) => Path.GetExtension(path));

        var result = SkiaFontFileDiscovery.ListFontFiles(fileDirectory.Object, "C:\\fonts");

        result.ShouldBe(
        [
            "C:\\fonts\\a.otf",
            "C:\\fonts\\m.ttc",
            "C:\\fonts\\Z.TTF"
        ]);

        fileDirectory.Verify(x => x.GetExtension(It.IsAny<string>()), Times.Exactly(5));
        fileDirectory.VerifyAll();
    }
}
