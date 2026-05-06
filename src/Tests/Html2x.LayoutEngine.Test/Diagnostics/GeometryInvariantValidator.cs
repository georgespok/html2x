using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Test.TestHelpers;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Geometry;
using Shouldly;
using LayoutFragment = Html2x.RenderModel.Fragments.Fragment;

namespace Html2x.LayoutEngine.Test.Diagnostics;

internal static class GeometryInvariantValidator
{
    public static void AssertInvariants(GeometryPipelineResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var snapshotText = GeometryTestHarness.RenderSnapshot(result.Snapshot);
        var boxes = result.Snapshot.Boxes.SelectMany(FlattenBoxes).ToList();

        foreach (var box in boxes)
        {
            box.Size.Width.ShouldBeGreaterThanOrEqualTo(0f, snapshotText);
            box.Size.Height.ShouldBeGreaterThanOrEqualTo(0f, snapshotText);
            box.ContentSize?.Width.ShouldBeGreaterThanOrEqualTo(0f, snapshotText);
            box.ContentSize?.Height.ShouldBeGreaterThanOrEqualTo(0f, snapshotText);
            box.MarkerOffset.ShouldBeGreaterThanOrEqualTo(0f, snapshotText);
        }

        AssertPublishedFragmentsMatch(result.PublishedLayout, result.Fragments, snapshotText);

        foreach (var parent in boxes)
        {
            if (parent.AllowsOverflow || parent.ContentX is null || parent.ContentY is null ||
                parent.ContentSize is null)
            {
                continue;
            }

            var parentContent = new RectPt(
                parent.ContentX.Value,
                parent.ContentY.Value,
                parent.ContentSize.Value.Width,
                parent.ContentSize.Value.Height);

            foreach (var child in parent.Children)
            {
                if (child.AllowsOverflow)
                {
                    continue;
                }

                var childRect = new RectPt(child.X, child.Y, child.Size.Width, child.Size.Height);
                RectContainedBy(childRect, parentContent)
                    .ShouldBeTrue(snapshotText);
            }
        }

        var sourceFragments = EnumerateFragments(result.Fragments.Blocks)
            .ToDictionary(static fragment => fragment.FragmentId);
        var placedFragments = result.Layout.Pages
            .SelectMany(static page => page.Children)
            .SelectMany(EnumerateFragments)
            .ToDictionary(static fragment => fragment.FragmentId);

        foreach (var snapshot in result.Snapshot.Fragments.Pages.SelectMany(static page =>
                     FlattenSnapshots(page.Fragments)))
        {
            if (snapshot.Kind is not ("block" or "table" or "table-row" or "table-cell"))
            {
                continue;
            }

            snapshot.MetadataOwner.ShouldBe("FragmentBuilder", snapshotText);
            snapshot.MetadataConsumer.ShouldBe("LayoutSnapshotMapper", snapshotText);
        }

        foreach (var page in result.Pagination.AuditPages)
        {
            foreach (var placement in page.Placements)
            {
                placement.PageNumber.ShouldBe(page.PageNumber, snapshotText);
                placedFragments.TryGetValue(placement.FragmentId, out var placedFragment).ShouldBeTrue(snapshotText);
                placedFragment.PageNumber.ShouldBe(page.PageNumber, snapshotText);
                Math.Abs(placement.Width - placedFragment.Rect.Width).ShouldBeLessThanOrEqualTo(0.01f, snapshotText);
                Math.Abs(placement.Height - placedFragment.Rect.Height).ShouldBeLessThanOrEqualTo(0.01f, snapshotText);
                Math.Abs(placement.PageX - placedFragment.Rect.X).ShouldBeLessThanOrEqualTo(0.01f, snapshotText);
                Math.Abs(placement.PageY - placedFragment.Rect.Y).ShouldBeLessThanOrEqualTo(0.01f, snapshotText);

                sourceFragments.TryGetValue(placement.FragmentId, out var sourceFragment).ShouldBeTrue(snapshotText);
                AssertTranslatedFragment(sourceFragment, placedFragment, page.PageNumber, null, null, snapshotText);
            }
        }
    }

    private static IEnumerable<BoxGeometrySnapshot> FlattenBoxes(BoxGeometrySnapshot box)
    {
        yield return box;

        foreach (var child in box.Children.SelectMany(FlattenBoxes))
        {
            yield return child;
        }
    }

    private static void AssertPublishedFragmentsMatch(
        PublishedLayoutTree publishedLayout,
        FragmentTree fragments,
        string snapshotText)
    {
        publishedLayout.Blocks.Count.ShouldBe(fragments.Blocks.Count, snapshotText);

        for (var i = 0; i < publishedLayout.Blocks.Count; i++)
        {
            AssertPublishedBlockMatchesFragment(publishedLayout.Blocks[i], fragments.Blocks[i], snapshotText);
        }
    }

    private static void AssertPublishedBlockMatchesFragment(
        PublishedBlock source,
        BlockFragment fragment,
        string snapshotText)
    {
        RectEquals(source.Geometry.BorderBoxRect, fragment.Rect).ShouldBeTrue(snapshotText);
        fragment.DisplayRole.ShouldBe(source.Display.Role, snapshotText);
        fragment.FormattingContext.ShouldBe(source.Display.FormattingContext, snapshotText);
        fragment.MarkerOffset.ShouldBe(source.Display.MarkerOffset, snapshotText);

        if (source.Image is not null)
        {
            var image = fragment.Children.OfType<ImageFragment>().FirstOrDefault();
            image.ShouldNotBeNull(snapshotText);
            RectEquals(source.Geometry.BorderBoxRect, image.Rect).ShouldBeTrue(snapshotText);
            RectEquals(source.Geometry.ContentBoxRect, image.ContentRect).ShouldBeTrue(snapshotText);
        }

        if (source.Rule is not null)
        {
            var rule = fragment.Children.OfType<RuleFragment>().FirstOrDefault();
            rule.ShouldNotBeNull(snapshotText);
            RectEquals(source.Geometry.BorderBoxRect, rule.Rect).ShouldBeTrue(snapshotText);
        }

        var publishedChildren = source.Flow
            .OrderBy(static item => item.Order)
            .OfType<PublishedChildBlockItem>()
            .Select(static item => item.Block)
            .ToList();
        var fragmentChildren = fragment.Children.OfType<BlockFragment>().ToList();

        for (var i = 0; i < publishedChildren.Count; i++)
        {
            i.ShouldBeLessThan(fragmentChildren.Count, snapshotText);
            AssertPublishedBlockMatchesFragment(publishedChildren[i], fragmentChildren[i], snapshotText);
        }
    }

    private static IEnumerable<LayoutFragment> EnumerateFragments(IEnumerable<BlockFragment> fragments)
    {
        foreach (var fragment in fragments)
        {
            foreach (var nested in EnumerateFragments(fragment))
            {
                yield return nested;
            }
        }
    }

    private static IEnumerable<LayoutFragment> EnumerateFragments(LayoutFragment fragment)
    {
        yield return fragment;

        if (fragment is not BlockFragment block)
        {
            yield break;
        }

        foreach (var child in block.Children)
        {
            foreach (var nested in EnumerateFragments(child))
            {
                yield return nested;
            }
        }
    }

    private static IEnumerable<FragmentSnapshot> FlattenSnapshots(
        IEnumerable<FragmentSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            yield return snapshot;

            foreach (var child in FlattenSnapshots(snapshot.Children))
            {
                yield return child;
            }
        }
    }

    private static void AssertTranslatedFragment(
        LayoutFragment source,
        LayoutFragment placed,
        int expectedPageNumber,
        float? expectedDeltaX,
        float? expectedDeltaY,
        string snapshotText)
    {
        placed.GetType().ShouldBe(source.GetType(), snapshotText);
        placed.PageNumber.ShouldBe(expectedPageNumber, snapshotText);
        placed.FragmentId.ShouldBe(source.FragmentId, snapshotText);
        placed.ZOrder.ShouldBe(source.ZOrder, snapshotText);
        RectEquals(
                new(source.Rect.X, source.Rect.Y, source.Rect.Width, source.Rect.Height),
                new(source.Rect.X, source.Rect.Y, placed.Rect.Width, placed.Rect.Height))
            .ShouldBeTrue(snapshotText);

        var deltaX = placed.Rect.X - source.Rect.X;
        var deltaY = placed.Rect.Y - source.Rect.Y;

        if (expectedDeltaX.HasValue)
        {
            Math.Abs(deltaX - expectedDeltaX.Value).ShouldBeLessThanOrEqualTo(0.01f, snapshotText);
        }

        if (expectedDeltaY.HasValue)
        {
            Math.Abs(deltaY - expectedDeltaY.Value).ShouldBeLessThanOrEqualTo(0.01f, snapshotText);
        }

        source.Rect.Width.ShouldBe(placed.Rect.Width, 0.01f, snapshotText);
        source.Rect.Height.ShouldBe(placed.Rect.Height, 0.01f, snapshotText);

        switch (source, placed)
        {
            case (TableFragment sourceTable, TableFragment placedTable):
                sourceTable.DerivedColumnCount.ShouldBe(placedTable.DerivedColumnCount, snapshotText);
                break;
            case (TableRowFragment sourceRow, TableRowFragment placedRow):
                sourceRow.RowIndex.ShouldBe(placedRow.RowIndex, snapshotText);
                break;
            case (TableCellFragment sourceCell, TableCellFragment placedCell):
                sourceCell.ColumnIndex.ShouldBe(placedCell.ColumnIndex, snapshotText);
                sourceCell.IsHeader.ShouldBe(placedCell.IsHeader, snapshotText);
                break;
            case (LineBoxFragment sourceLine, LineBoxFragment placedLine):
                AssertTranslatedLine(sourceLine, placedLine, deltaX, deltaY, snapshotText);
                return;
            case (ImageFragment sourceImage, ImageFragment placedImage):
                AssertTranslatedImage(sourceImage, placedImage, deltaX, deltaY, snapshotText);
                return;
        }

        if (source is BlockFragment sourceBlock && placed is BlockFragment placedBlock)
        {
            sourceBlock.DisplayRole.ShouldBe(placedBlock.DisplayRole, snapshotText);
            sourceBlock.FormattingContext.ShouldBe(placedBlock.FormattingContext, snapshotText);
            sourceBlock.MarkerOffset.ShouldBe(placedBlock.MarkerOffset, snapshotText);
            sourceBlock.Children.Count.ShouldBe(placedBlock.Children.Count, snapshotText);

            for (var i = 0; i < sourceBlock.Children.Count; i++)
            {
                AssertTranslatedFragment(
                    sourceBlock.Children[i],
                    placedBlock.Children[i],
                    expectedPageNumber,
                    deltaX,
                    deltaY,
                    snapshotText);
            }
        }
    }

    private static void AssertTranslatedLine(
        LineBoxFragment source,
        LineBoxFragment placed,
        float deltaX,
        float deltaY,
        string snapshotText)
    {
        source.BaselineY.ShouldBe(placed.BaselineY - deltaY, 0.01f, snapshotText);
        source.LineHeight.ShouldBe(placed.LineHeight, 0.01f, snapshotText);
        source.TextAlign.ShouldBe(placed.TextAlign, snapshotText);
        source.Runs.Count.ShouldBe(placed.Runs.Count, snapshotText);

        for (var i = 0; i < source.Runs.Count; i++)
        {
            var sourceRun = source.Runs[i];
            var placedRun = placed.Runs[i];

            sourceRun.Text.ShouldBe(placedRun.Text, snapshotText);
            sourceRun.Font.ShouldBe(placedRun.Font, snapshotText);
            sourceRun.FontSizePt.ShouldBe(placedRun.FontSizePt, 0.01f, snapshotText);
            sourceRun.AdvanceWidth.ShouldBe(placedRun.AdvanceWidth, 0.01f, snapshotText);
            sourceRun.Ascent.ShouldBe(placedRun.Ascent, 0.01f, snapshotText);
            sourceRun.Descent.ShouldBe(placedRun.Descent, 0.01f, snapshotText);
            sourceRun.Decorations.ShouldBe(placedRun.Decorations, snapshotText);
            sourceRun.Color.ShouldBe(placedRun.Color, snapshotText);
            (placedRun.Origin.X - sourceRun.Origin.X).ShouldBe(deltaX, 0.01f, snapshotText);
            (placedRun.Origin.Y - sourceRun.Origin.Y).ShouldBe(deltaY, 0.01f, snapshotText);
        }
    }

    private static void AssertTranslatedImage(
        ImageFragment source,
        ImageFragment placed,
        float deltaX,
        float deltaY,
        string snapshotText)
    {
        source.Src.ShouldBe(placed.Src, snapshotText);
        source.AuthoredSizePx.ShouldBe(placed.AuthoredSizePx, snapshotText);
        source.IntrinsicSizePx.ShouldBe(placed.IntrinsicSizePx, snapshotText);
        source.IsMissing.ShouldBe(placed.IsMissing, snapshotText);
        source.IsOversize.ShouldBe(placed.IsOversize, snapshotText);
        source.ContentRect.Width.ShouldBe(placed.ContentRect.Width, 0.01f, snapshotText);
        source.ContentRect.Height.ShouldBe(placed.ContentRect.Height, 0.01f, snapshotText);
        (placed.ContentRect.X - source.ContentRect.X).ShouldBe(deltaX, 0.01f, snapshotText);
        (placed.ContentRect.Y - source.ContentRect.Y).ShouldBe(deltaY, 0.01f, snapshotText);
    }

    private static bool RectEquals(RectPt expected, RectPt actual) =>
        Math.Abs(expected.X - actual.X) <= 0.01f &&
        Math.Abs(expected.Y - actual.Y) <= 0.01f &&
        Math.Abs(expected.Width - actual.Width) <= 0.01f &&
        Math.Abs(expected.Height - actual.Height) <= 0.01f;

    private static bool RectContainedBy(RectPt child, RectPt parent)
    {
        const float epsilon = 0.01f;
        return child.Left >= parent.Left - epsilon &&
               child.Top >= parent.Top - epsilon &&
               child.Right <= parent.Right + epsilon &&
               child.Bottom <= parent.Bottom + epsilon;
    }
}