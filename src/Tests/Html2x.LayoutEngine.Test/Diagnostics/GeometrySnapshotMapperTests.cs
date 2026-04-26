using System.Drawing;
using AngleSharp;
using Html2x.Abstractions.Layout.Documents;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Pagination;
using Shouldly;
using Html2x.LayoutEngine.Geometry;

namespace Html2x.LayoutEngine.Test.Diagnostics;

public sealed class GeometrySnapshotMapperTests
{
    [Fact]
    public void From_MapsBoxGeometryFragmentsAndPaginationPlacementsIntoSingleSnapshot()
    {
        var block = new BlockBox(DisplayRole.Block)
        {
            Element = CreateElement("div"),
            Style = new ComputedStyle(),
            UsedGeometry = BoxGeometryFactory.FromBorderBox(
                new RectangleF(10f, 20f, 120f, 40f),
                new Spacing(2f, 3f, 4f, 5f),
                new Spacing(1f, 1f, 1f, 1f),
                markerOffset: 8f)
        };

        var boxTree = new BoxTree();
        boxTree.Blocks.Add(block);

        var fragment = new BlockFragment
        {
            FragmentId = 7,
            PageNumber = 1,
            Rect = new RectangleF(10f, 20f, 120f, 40f),
            DisplayRole = FragmentDisplayRole.Block,
            Style = new VisualStyle()
        };

        var layout = new HtmlLayout();
        layout.Pages.Add(new LayoutPage(PaperSizes.A4, new Spacing(), [fragment]));

        var pagination = new PaginationResult
        {
            Pages =
            [
                new PageModel
                {
                    PageNumber = 1,
                    PageSize = PaperSizes.A4,
                    Margins = new Spacing(),
                    ContentTop = 0f,
                    ContentBottom = PaperSizes.A4.Height,
                    Placements =
                    [
                        new BlockFragmentPlacement
                        {
                            FragmentId = fragment.FragmentId,
                            PageNumber = 1,
                            IsOversized = false,
                            OrderIndex = 0,
                            Fragment = fragment
                        }
                    ]
                }
            ]
        };

        var snapshot = GeometrySnapshotMapper.From(boxTree, layout, pagination);

        snapshot.Fragments.PageCount.ShouldBe(1);

        var boxSnapshot = snapshot.Boxes.ShouldHaveSingleItem();
        boxSnapshot.Path.ShouldBe("div");
        boxSnapshot.Kind.ShouldBe("block");
        boxSnapshot.Size.ShouldBe(new SizePt(120f, 40f));
        boxSnapshot.ContentX.ShouldBe(16f);
        boxSnapshot.ContentY.ShouldBe(23f);
        boxSnapshot.ContentSize.ShouldBe(new SizePt(110f, 32f));
        boxSnapshot.MarkerOffset.ShouldBe(8f);
        boxSnapshot.MetadataOwner.ShouldBe("BlockLayoutEngine");
        boxSnapshot.MetadataConsumer.ShouldBe("GeometrySnapshotMapper");

        var placement = snapshot.Pagination.ShouldHaveSingleItem().Placements.ShouldHaveSingleItem();
        placement.FragmentId.ShouldBe(7);
        placement.Kind.ShouldBe("Block");
        placement.Size.ShouldBe(new SizePt(120f, 40f));
    }

    [Fact]
    public void From_NestedAndSiblingBoxes_AssignsDepthFirstSequenceIds()
    {
        var root = CreateLaidOutBlock("div", new RectangleF(0f, 0f, 100f, 50f));
        var child = CreateLaidOutBlock("p", new RectangleF(0f, 0f, 80f, 20f), root);
        var sibling = CreateLaidOutBlock("section", new RectangleF(0f, 60f, 100f, 30f));
        root.Children.Add(child);

        var boxTree = new BoxTree();
        boxTree.Blocks.Add(root);
        boxTree.Blocks.Add(sibling);

        var snapshot = GeometrySnapshotMapper.From(
            boxTree,
            new HtmlLayout(),
            new PaginationResult { Pages = [] });

        snapshot.Boxes[0].SequenceId.ShouldBe(1);
        snapshot.Boxes[0].Children.ShouldHaveSingleItem().SequenceId.ShouldBe(2);
        snapshot.Boxes[1].SequenceId.ShouldBe(3);
    }

    private static BlockBox CreateLaidOutBlock(string tagName, RectangleF borderBox, DisplayNode? parent = null)
    {
        return new BlockBox(DisplayRole.Block)
        {
            Element = CreateElement(tagName),
            Parent = parent,
            Style = new ComputedStyle(),
            UsedGeometry = BoxGeometryFactory.FromBorderBox(borderBox, new Spacing(), new Spacing())
        };
    }

    private static AngleSharp.Dom.IElement CreateElement(string tagName)
    {
        return AngleSharp.BrowsingContext.New(AngleSharp.Configuration.Default)
            .OpenNewAsync().Result.CreateElement(tagName);
    }
}
