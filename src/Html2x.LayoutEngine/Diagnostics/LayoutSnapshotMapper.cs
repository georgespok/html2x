using Html2x.Abstractions.Diagnostics;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;

namespace Html2x.LayoutEngine.Diagnostics;

public static class LayoutSnapshotMapper
{
    public static LayoutSnapshot From(HtmlLayout layout)
    {
        if (layout is null)
        {
            throw new ArgumentNullException(nameof(layout));
        }

        var pages = new List<LayoutPageSnapshot>(layout.Pages.Count);

        foreach (var page in layout.Pages)
        {
            var fragments = page.Children
                .Select(MapFragment)
                .ToArray();

            pages.Add(new LayoutPageSnapshot
            {
                PageNumber = page.PageNumber,
                PageSize = page.PageSize,
                Margin = page.Margins,
                Fragments = fragments
            });
        }

        return new LayoutSnapshot
        {
            PageCount = layout.Pages.Count,
            Pages = pages
        };
    }

    private static FragmentSnapshot MapFragment(Html2x.Abstractions.Layout.Fragments.Fragment fragment)
    {
        return fragment switch
        {
            LineBoxFragment line => MapLineBox(line),
            BlockFragment block => MapBlock(block),
            ImageFragment image => MapImage(image),
            RuleFragment rule => MapRule(rule),
            _ => MapUnknown(fragment)
        };
    }

    private static FragmentSnapshot MapBlock(BlockFragment block)
    {
        var children = block.Children.Select(MapFragment).ToArray();

        return new FragmentSnapshot
        {
            Kind = "block",
            X = block.Rect.X,
            Y = block.Rect.Y,
            Size = block.Size,
            Children = children
        };
    }

    private static FragmentSnapshot MapLineBox(LineBoxFragment line)
    {
        var text = line.Runs is null
            ? null
            : string.Concat(line.Runs.Select(r => r.Text));

        return new FragmentSnapshot
        {
            Kind = "line",
            X = line.Rect.X,
            Y = line.Rect.Y,
            Size = line.Size,
            Text = text,
            Children = []
        };
    }

    private static FragmentSnapshot MapImage(ImageFragment image)
    {
        return new FragmentSnapshot
        {
            Kind = "image",
            X = image.Rect.X,
            Y = image.Rect.Y,
            Size = image.Size,
            ContentX = image.ContentRect.X,
            ContentY = image.ContentRect.Y,
            ContentSize = image.ContentSize,
            Borders = image.Style?.Borders,
            Children = []
        };
    }

    private static FragmentSnapshot MapRule(RuleFragment rule)
    {
        return new FragmentSnapshot
        {
            Kind = "rule",
            X = rule.Rect.X,
            Y = rule.Rect.Y,
            Size = rule.Size,
            Children = []
        };
    }

    private static FragmentSnapshot MapUnknown(Html2x.Abstractions.Layout.Fragments.Fragment fragment)
    {
        return new FragmentSnapshot
        {
            Kind = fragment.GetType().Name.ToLowerInvariant(),
            X = fragment.Rect.X,
            Y = fragment.Rect.Y,
            Size = fragment.Size,
            Children = []
        };
    }
}
