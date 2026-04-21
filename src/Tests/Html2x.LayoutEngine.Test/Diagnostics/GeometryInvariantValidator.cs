using System.Drawing;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Test.TestHelpers;
using Shouldly;
using LayoutFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

namespace Html2x.LayoutEngine.Test.Diagnostics;

internal static class GeometryInvariantValidator
{
    public static void AssertInvariants(GeometryPipelineResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var snapshotText = GeometryTestHarness.RenderSnapshot(result.Snapshot);
        var laidOutBoxes = FlattenBoxes(result.BoxTree.Blocks)
            .ToHashSet(Html2x.LayoutEngine.Test.TestHelpers.ReferenceEqualityComparer.Instance);

        foreach (var box in laidOutBoxes)
        {
            box.UsedGeometry.ShouldNotBeNull(snapshotText);

            var geometry = box.UsedGeometry!.Value;
            RectEquals(geometry.BorderBoxRect, new RectangleF(box.X, box.Y, box.Width, box.Height))
                .ShouldBeTrue(snapshotText);
            Math.Abs(box.MarkerOffset - geometry.MarkerOffset).ShouldBeLessThanOrEqualTo(0.01f, snapshotText);
        }

        foreach (var binding in result.Observer.BlockBindings)
        {
            binding.Source.UsedGeometry.ShouldNotBeNull(snapshotText);
            RectEquals(binding.Source.UsedGeometry!.Value.BorderBoxRect, binding.Fragment.Rect)
                .ShouldBeTrue(snapshotText);
        }

        foreach (var binding in result.Observer.SpecialBindings)
        {
            if (binding.Source is not BlockBox sourceBlock || sourceBlock.UsedGeometry is null)
            {
                continue;
            }

            var geometry = sourceBlock.UsedGeometry.Value;
            RectEquals(geometry.BorderBoxRect, binding.Fragment.Rect)
                .ShouldBeTrue(snapshotText);

            if (binding.Fragment is ImageFragment imageFragment)
            {
                RectEquals(geometry.ContentBoxRect, imageFragment.ContentRect)
                    .ShouldBeTrue(snapshotText);
            }
        }

        foreach (var parent in laidOutBoxes)
        {
            if (parent.UsedGeometry is null)
            {
                continue;
            }

            var parentGeometry = parent.UsedGeometry.Value;
            if (parentGeometry.AllowsOverflow)
            {
                continue;
            }

            foreach (var child in DisplayNodeTraversal.EnumerateBlockChildren(parent))
            {
                if (child.UsedGeometry is null || child.UsedGeometry.Value.AllowsOverflow)
                {
                    continue;
                }

                RectContainedBy(child.UsedGeometry.Value.BorderBoxRect, parentGeometry.ContentBoxRect)
                    .ShouldBeTrue(snapshotText);
            }
        }

        var sourceFragments = EnumerateFragments(result.Fragments.Blocks)
            .ToDictionary(static fragment => fragment.FragmentId);

        foreach (var page in result.Pagination.Pages)
        {
            foreach (var placement in page.Placements)
            {
                placement.PageNumber.ShouldBe(page.PageNumber, snapshotText);
                placement.Fragment.PageNumber.ShouldBe(page.PageNumber, snapshotText);
                Math.Abs(placement.Width - placement.Fragment.Rect.Width).ShouldBeLessThanOrEqualTo(0.01f, snapshotText);
                Math.Abs(placement.Height - placement.Fragment.Rect.Height).ShouldBeLessThanOrEqualTo(0.01f, snapshotText);
                Math.Abs(placement.LocalX - placement.Fragment.Rect.X).ShouldBeLessThanOrEqualTo(0.01f, snapshotText);
                Math.Abs(placement.LocalY - placement.Fragment.Rect.Y).ShouldBeLessThanOrEqualTo(0.01f, snapshotText);

                sourceFragments.TryGetValue(placement.FragmentId, out var sourceFragment).ShouldBeTrue(snapshotText);
                AssertTranslatedFragment(sourceFragment!, placement.Fragment, page.PageNumber, null, null, snapshotText);
            }
        }
    }

    private static IEnumerable<BlockBox> FlattenBoxes(IEnumerable<BlockBox> blocks)
    {
        foreach (var block in blocks)
        {
            yield return block;

            foreach (var child in FlattenBoxes(DisplayNodeTraversal.EnumerateBlockChildren(block)))
            {
                yield return child;
            }
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
                new RectangleF(source.Rect.X, source.Rect.Y, source.Rect.Width, source.Rect.Height),
                new RectangleF(source.Rect.X, source.Rect.Y, placed.Rect.Width, placed.Rect.Height))
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

    private static bool RectEquals(RectangleF expected, RectangleF actual)
    {
        return Math.Abs(expected.X - actual.X) <= 0.01f &&
               Math.Abs(expected.Y - actual.Y) <= 0.01f &&
               Math.Abs(expected.Width - actual.Width) <= 0.01f &&
               Math.Abs(expected.Height - actual.Height) <= 0.01f;
    }

    private static bool RectContainedBy(RectangleF child, RectangleF parent)
    {
        const float epsilon = 0.01f;
        return child.Left >= parent.Left - epsilon &&
               child.Top >= parent.Top - epsilon &&
               child.Right <= parent.Right + epsilon &&
               child.Bottom <= parent.Bottom + epsilon;
    }
}
