using Html2x.Resources;
using Html2x.RenderModel.Resources;

namespace Html2x.Test.Html2x.Resources;

public sealed class ImageResourceStoreTests
{
    private const string TwoByOnePngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAIAAAABCAYAAAD0In+KAAAADklEQVR4nGP4z8DwHwQBEPgD/U6VwW8AAAAASUVORK5CYII=";

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("missing.png")]
    public void Load_UnavailableSource_ReturnsMissing(string src)
    {
        var tempDirectory = Directory.CreateTempSubdirectory();
        try
        {
            var store = new ImageResourceStore(tempDirectory.FullName, 1024);

            var resource = store.Load(src);

            Assert.Equal(ImageLoadStatus.Missing, resource.Status);
            Assert.Null(resource.Bytes);
        }
        finally
        {
            tempDirectory.Delete(true);
        }
    }

    [Fact]
    public async Task LoadMetadata_ThenLoad_ReturnsRetainedBytesFromSameResourceState()
    {
        var tempDirectory = Directory.CreateTempSubdirectory();
        try
        {
            var imagePath = Path.Combine(tempDirectory.FullName, "image.png");
            var originalBytes = Convert.FromBase64String(TwoByOnePngBase64);
            await File.WriteAllBytesAsync(imagePath, originalBytes);

            var store = new ImageResourceStore(tempDirectory.FullName, 1024 * 1024);

            var metadata = store.LoadMetadata("image.png");
            await File.WriteAllBytesAsync(imagePath, [1, 2, 3]);
            var resource = store.Load("image.png");

            Assert.Equal(ImageLoadStatus.Ok, metadata.Status);
            Assert.Equal(ImageLoadStatus.Ok, resource.Status);
            Assert.Equal(2d, metadata.IntrinsicSizePx.Width);
            Assert.Equal(1d, metadata.IntrinsicSizePx.Height);
            Assert.Equal(originalBytes, resource.Bytes);
        }
        finally
        {
            tempDirectory.Delete(true);
        }
    }

    [Fact]
    public async Task Load_PathOutsideBaseDirectory_ReturnsOutOfScope()
    {
        var rootDirectory = Directory.CreateTempSubdirectory();
        var baseDirectory = Directory.CreateDirectory(Path.Combine(rootDirectory.FullName, "base"));
        try
        {
            await File.WriteAllBytesAsync(Path.Combine(rootDirectory.FullName, "outside.png"), [1]);
            var store = new ImageResourceStore(baseDirectory.FullName, 1024);

            var resource = store.Load("../outside.png");

            Assert.Equal(ImageLoadStatus.OutOfScope, resource.Status);
            Assert.Null(resource.Bytes);
        }
        finally
        {
            rootDirectory.Delete(true);
        }
    }

    [Fact]
    public async Task Load_FileLargerThanLimit_ReturnsOversizedBeforeDecode()
    {
        var tempDirectory = Directory.CreateTempSubdirectory();
        try
        {
            await File.WriteAllBytesAsync(Path.Combine(tempDirectory.FullName, "large.bin"), [1, 2]);
            var store = new ImageResourceStore(tempDirectory.FullName, 1);

            var resource = store.Load("large.bin");

            Assert.Equal(ImageLoadStatus.Oversized, resource.Status);
            Assert.Null(resource.Bytes);
        }
        finally
        {
            tempDirectory.Delete(true);
        }
    }

    [Fact]
    public void Load_Base64DataUriLargerThanLimit_ReturnsOversized()
    {
        var payload = Convert.ToBase64String([1, 2]);
        var store = new ImageResourceStore(null, 1);

        var resource = store.Load($"data:image/png;base64,{payload}");

        Assert.Equal(ImageLoadStatus.Oversized, resource.Status);
        Assert.Null(resource.Bytes);
    }

    [Fact]
    public void Load_TextDataUriLargerThanLimit_ReturnsOversized()
    {
        var store = new ImageResourceStore(null, 1);

        var resource = store.Load("data:text/plain,%61%62");

        Assert.Equal(ImageLoadStatus.Oversized, resource.Status);
        Assert.Null(resource.Bytes);
    }

    [Fact]
    public void Load_DataUriWithUndecodableBytes_ReturnsDecodeFailed()
    {
        var store = new ImageResourceStore(null, 1024);

        var resource = store.Load("data:image/png;base64,eA==");

        Assert.Equal(ImageLoadStatus.DecodeFailed, resource.Status);
        Assert.Null(resource.Bytes);
    }

    [Fact]
    public void Load_InvalidDataUriWithinLimit_ReturnsInvalidDataUri()
    {
        var store = new ImageResourceStore(null, 1024);

        var resource = store.Load("data:image/png;base64,not-base64");

        Assert.Equal(ImageLoadStatus.InvalidDataUri, resource.Status);
        Assert.Null(resource.Bytes);
    }

    [Theory]
    [InlineData("data:text/plain,%")]
    [InlineData("data:text/plain,%zz")]
    public void Load_MalformedTextDataUriPercentEncoding_ReturnsInvalidDataUri(string src)
    {
        var store = new ImageResourceStore(null, 1024);

        var resource = store.Load(src);

        Assert.Equal(ImageLoadStatus.InvalidDataUri, resource.Status);
        Assert.Null(resource.Bytes);
    }

    [Fact]
    public void Load_MalformedTextDataUriSurrogate_ReturnsInvalidDataUri()
    {
        var store = new ImageResourceStore(null, 1024);
        var src = "data:text/plain," + '\uD800';

        var resource = store.Load(src);

        Assert.Equal(ImageLoadStatus.InvalidDataUri, resource.Status);
        Assert.Null(resource.Bytes);
    }
}
