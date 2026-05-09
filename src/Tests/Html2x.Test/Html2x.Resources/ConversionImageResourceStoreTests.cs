using Html2x.RenderModel.Fragments;
using Html2x.Resources;

namespace Html2x.Test.Html2x.Resources;

public sealed class ConversionImageResourceStoreTests
{
    private const string TwoByOnePngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAIAAAABCAYAAAD0In+KAAAADklEQVR4nGP4z8DwHwQBEPgD/U6VwW8AAAAASUVORK5CYII=";

    [Fact]
    public async Task LoadMetadata_ThenLoad_ReturnsRetainedBytesFromSameResourceState()
    {
        var tempDirectory = Directory.CreateTempSubdirectory();
        try
        {
            var imagePath = Path.Combine(tempDirectory.FullName, "image.png");
            var originalBytes = Convert.FromBase64String(TwoByOnePngBase64);
            await File.WriteAllBytesAsync(imagePath, originalBytes);

            var store = new ConversionImageResourceStore(tempDirectory.FullName, 1024 * 1024);

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
}
