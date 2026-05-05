using Html2x.LayoutEngine.Contracts.Geometry.Images;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Measurements.Units;
using Shouldly;
using CoreFragment = Html2x.RenderModel.Fragments.Fragment;

namespace Html2x.LayoutEngine.Test;

public partial class LayoutIntegrationTests
{
    [Fact]
    public async Task Build_InlineImageWithBorderAndPadding_IncludesOuterSize()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <p>Before <img src='image.png' width='100' height='80' style='padding: 10px; border: 2px solid black;' /> After</p>
              </body>
            </html>";

        var layoutOptions = new LayoutBuildSettings
        {
            PageSize = PaperSizes.Letter
        };

        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        layout.Pages.Count.ShouldBe(1);
        var page = layout.Pages[0];
        var image = page.Children
            .OfType<BlockFragment>()
            .Select(FindFirstImageFragment)
            .FirstOrDefault(fragment => fragment is not null);

        image.ShouldNotBeNull();
        image.Rect.Width.ShouldBe(93f, 0.5f);
        image.Rect.Height.ShouldBe(78f, 0.5f);
        image.ContentRect.Width.ShouldBe(75f, 0.5f);
        image.ContentRect.Height.ShouldBe(60f, 0.5f);
    }

    [Fact]
    public async Task Build_ImageWithOversizedBorderAndPadding_ClampsContentRectToZero()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div>
                    <img src='image.png' width='0' height='0' style='padding: 20px; border: 10px solid black;' />
                </div>
              </body>
            </html>";

        var layoutOptions = new LayoutBuildSettings
        {
            PageSize = PaperSizes.Letter
        };

        var layout = await CreateLayoutBuilder().BuildAsync(html, layoutOptions);

        layout.Pages.Count.ShouldBe(1);
        var page = layout.Pages[0];
        var image = page.Children
            .OfType<BlockFragment>()
            .Select(FindFirstImageFragment)
            .FirstOrDefault(fragment => fragment is not null);

        image.ShouldNotBeNull();
        image.ContentRect.Width.ShouldBe(0f);
        image.ContentRect.Height.ShouldBe(0f);
    }

    [Fact]
    public async Task Build_ImageSizing_AppliesCssRatioAndWidthCap()
    {
        const string html = @"
            <html>
              <body style='margin: 0;'>
                <div style='width: 120pt; margin: 0;'>
                  <img src='css.png' style='display: block; width: 40px; height: 20px;' />
                  <img src='intrinsic.png' style='display: block;' />
                </div>
              </body>
            </html>";

        var layout = await CreateLayoutBuilder(new FixedImageMetadataResolver(src =>
            string.Equals(src, "intrinsic.png", StringComparison.OrdinalIgnoreCase)
                ? new SizePx(400d, 200d)
                : new SizePx(80d, 40d)))
            .BuildAsync(html, new LayoutBuildSettings { PageSize = PaperSizes.Letter });

        var container = layout.Pages[0].Children.ShouldHaveSingleItem().ShouldBeOfType<BlockFragment>();
        var images = EnumerateLayoutFragments(container)
            .OfType<ImageFragment>()
            .ToList();

        images.Count.ShouldBe(2);
        var cssImage = images[0];
        var intrinsicImage = images[1];

        cssImage.ContentRect.Width.ShouldBe(30f, 0.01f);
        cssImage.ContentRect.Height.ShouldBe(15f, 0.01f);
        intrinsicImage.ContentRect.Width.ShouldBe(120f, 0.01f);
        intrinsicImage.ContentRect.Height.ShouldBe(60f, 0.01f);
    }

    private static ImageFragment? FindFirstImageFragment(CoreFragment fragment)
    {
        if (fragment is ImageFragment image)
        {
            return image;
        }

        if (fragment is BlockFragment block)
        {
            foreach (var child in block.Children)
            {
                var match = FindFirstImageFragment(child);
                if (match is not null)
                {
                    return match;
                }
            }
        }

        return null;
    }

    private static IEnumerable<CoreFragment> EnumerateLayoutFragments(CoreFragment fragment)
    {
        yield return fragment;

        if (fragment is not BlockFragment block)
        {
            yield break;
        }

        foreach (var child in block.Children)
        {
            foreach (var nested in EnumerateLayoutFragments(child))
            {
                yield return nested;
            }
        }
    }

    private sealed class FixedImageMetadataResolver(Func<string, SizePx> resolveSize) : IImageMetadataResolver
    {
        private readonly Func<string, SizePx> _resolveSize = resolveSize;

        public ImageMetadataResult Resolve(string src, string baseDirectory, long maxBytes)
        {
            return new ImageMetadataResult
            {
                Src = src,
                Status = ImageLoadStatus.Ok,
                IntrinsicSizePx = _resolveSize(src)
            };
        }
    }
}
