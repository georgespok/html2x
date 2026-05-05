using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Contracts.Published;
using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Geometry.Primitives;
using Html2x.LayoutEngine.Pagination;
using Html2x.RenderModel.Documents;
using Html2x.RenderModel.Fragments;
using Html2x.RenderModel.Geometry;
using Html2x.RenderModel.Measurements.Units;
using Html2x.RenderModel.Styles;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Diagnostics;

public sealed class GeometrySnapshotMapperTests
{
    [Fact]
    public void From_PublishedGeometryAndPagination_MapsSingleSnapshot()
    {
        var layoutTree = new PublishedLayoutTree(
            new PublishedPage(PaperSizes.A4, new Spacing()),
            [
                CreatePublishedBlock(
                    "div",
                    "div",
                    new RectPt(10f, 20f, 120f, 40f),
                    new Spacing(2f, 3f, 4f, 5f),
                    new Spacing(1f, 1f, 1f, 1f),
                    markerOffset: 8f)
            ]);
        var fragment = new BlockFragment
        {
            FragmentId = 7,
            PageNumber = 1,
            Rect = new RectPt(10f, 20f, 120f, 40f),
            DisplayRole = FragmentDisplayRole.Block,
            Style = new VisualStyle()
        };
        var layout = new HtmlLayout();
        layout.AddPage(new LayoutPage(PaperSizes.A4, new Spacing(), [fragment]));
        var pagination = new PaginationResult
        {
            Layout = layout,
            AuditPages =
            [
                new PaginationPageAudit
                {
                    PageNumber = 1,
                    PageSize = PaperSizes.A4,
                    Margin = new Spacing(),
                    ContentArea = new RectPt(0f, 0f, PaperSizes.A4.Width, PaperSizes.A4.Height),
                    Placements =
                    [
                        new PaginationPlacementAudit
                        {
                            FragmentId = fragment.FragmentId,
                            PageNumber = 1,
                            PlacedRect = fragment.Rect,
                            DecisionKind = PaginationDecisionKind.Placed,
                            IsOversized = false,
                            OrderIndex = 0,
                            FragmentKind = "Block",
                            DisplayRole = fragment.DisplayRole
                        }
                    ]
                }
            ]
        };

        var snapshot = GeometrySnapshotMapper.From(layoutTree, pagination);

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
        boxSnapshot.SourceNodeId.ShouldBeNull();
        boxSnapshot.SourceContentId.ShouldBeNull();
        boxSnapshot.SourcePath.ShouldBeNull();
        boxSnapshot.SourceOrder.ShouldBeNull();
        boxSnapshot.SourceElementIdentity.ShouldBeNull();
        boxSnapshot.GeneratedSourceKind.ShouldBeNull();

        var placement = snapshot.Pagination.ShouldHaveSingleItem().Placements.ShouldHaveSingleItem();
        placement.FragmentId.ShouldBe(7);
        placement.Kind.ShouldBe("Block");
        placement.DecisionKind.ShouldBe(PaginationDecisionKind.Placed);
        placement.Size.ShouldBe(new SizePt(120f, 40f));
        placement.MetadataConsumer.ShouldBe("Pagination");
    }

    [Fact]
    public void ToDiagnosticObject_PublishedGeometryAndPagination_PreservesSnapshotFieldShape()
    {
        var sourceIdentity = new GeometrySourceIdentity(
            new StyleNodeId(4),
            new StyleContentId(9),
            "body[0]/section[1]",
            "section.card",
            3,
            GeometryGeneratedSourceKind.AnonymousBlock);
        var layoutTree = new PublishedLayoutTree(
            new PublishedPage(PaperSizes.A4, new Spacing(10, 11, 12, 13)),
            [
                CreatePublishedBlock(
                    "layout/section",
                    "section.card",
                    new RectPt(10f, 20f, 120f, 40f),
                    new Spacing(2f, 3f, 4f, 5f),
                    new Spacing(1f, 1f, 1f, 1f),
                    markerOffset: 8f,
                    sourceIdentity: sourceIdentity)
            ]);
        var fragment = new BlockFragment
        {
            FragmentId = 42,
            PageNumber = 1,
            Rect = new RectPt(15f, 25f, 100f, 30f),
            DisplayRole = FragmentDisplayRole.Block,
            FormattingContext = FormattingContextKind.Block,
            Style = new VisualStyle()
        };
        var layout = new HtmlLayout();
        layout.AddPage(new LayoutPage(PaperSizes.A4, new Spacing(10, 11, 12, 13), [fragment]));
        var pagination = new PaginationResult
        {
            Layout = layout,
            AuditPages =
            [
                new PaginationPageAudit
                {
                    PageNumber = 1,
                    PageSize = PaperSizes.A4,
                    Margin = new Spacing(10, 11, 12, 13),
                    ContentArea = new RectPt(11f, 10f, 574f, 820f),
                    Placements =
                    [
                        new PaginationPlacementAudit
                        {
                            FragmentId = fragment.FragmentId,
                            PageNumber = 1,
                            PlacedRect = fragment.Rect,
                            DecisionKind = PaginationDecisionKind.Placed,
                            IsOversized = true,
                            OrderIndex = 2,
                            FragmentKind = "Block",
                            DisplayRole = FragmentDisplayRole.TableCell,
                            FormattingContext = FormattingContextKind.InlineBlock,
                            MarkerOffset = 5f,
                            DerivedColumnCount = 4,
                            RowIndex = 2,
                            ColumnIndex = 1,
                            IsHeader = true
                        }
                    ]
                }
            ]
        };

        var snapshot = GeometrySnapshotMapper.ToDiagnosticObject(layoutTree, pagination);

        KeysShouldBe(snapshot, "fragments", "boxes", "pagination");
        ObjectField(snapshot, "fragments")["pageCount"].ShouldBe(new DiagnosticNumberValue(1));

        var box = ObjectItem(ArrayField(snapshot, "boxes"), 0);
        KeysShouldBe(
            box,
            "sequenceId",
            "path",
            "kind",
            "tagName",
            "sourceNodeId",
            "sourceContentId",
            "sourcePath",
            "sourceOrder",
            "sourceElementIdentity",
            "generatedSourceKind",
            "x",
            "y",
            "size",
            "contentX",
            "contentY",
            "contentSize",
            "baseline",
            "markerOffset",
            "allowsOverflow",
            "isAnonymous",
            "isInlineBlockContext",
            "derivedColumnCount",
            "rowIndex",
            "columnIndex",
            "isHeader",
            "metadataOwner",
            "metadataConsumer",
            "children");
        box["sequenceId"].ShouldBe(new DiagnosticNumberValue(1));
        box["path"].ShouldBe(new DiagnosticStringValue("layout/section"));
        box["kind"].ShouldBe(new DiagnosticStringValue("block"));
        box["tagName"].ShouldBe(new DiagnosticStringValue("section"));
        box["sourceNodeId"].ShouldBe(new DiagnosticNumberValue(4));
        box["sourceContentId"].ShouldBe(new DiagnosticNumberValue(9));
        box["sourcePath"].ShouldBe(new DiagnosticStringValue("body[0]/section[1]"));
        box["sourceOrder"].ShouldBe(new DiagnosticNumberValue(3));
        box["sourceElementIdentity"].ShouldBe(new DiagnosticStringValue("section.card"));
        box["generatedSourceKind"].ShouldBe(new DiagnosticStringValue("anonymous-block"));
        NumberField(ObjectField(box, "size"), "width").ShouldBe(120);
        NumberField(ObjectField(box, "contentSize"), "width").ShouldBe(110);
        NumberField(box, "contentX").ShouldBe(16);
        NumberField(box, "contentY").ShouldBe(23);
        box["baseline"].ShouldBeNull();
        box["markerOffset"].ShouldBe(new DiagnosticNumberValue(8));
        box["allowsOverflow"].ShouldBe(new DiagnosticBooleanValue(false));
        box["isAnonymous"].ShouldBe(new DiagnosticBooleanValue(false));
        box["isInlineBlockContext"].ShouldBe(new DiagnosticBooleanValue(false));
        box["derivedColumnCount"].ShouldBeNull();
        box["metadataOwner"].ShouldBe(new DiagnosticStringValue("BlockLayoutEngine"));
        box["metadataConsumer"].ShouldBe(new DiagnosticStringValue("GeometrySnapshotMapper"));
        ArrayField(box, "children").Count.ShouldBe(0);

        var page = ObjectItem(ArrayField(snapshot, "pagination"), 0);
        KeysShouldBe(page, "pageNumber", "pageSize", "margin", "contentTop", "contentBottom", "placements");
        page["pageNumber"].ShouldBe(new DiagnosticNumberValue(1));
        NumberField(ObjectField(page, "pageSize"), "width").ShouldBe(PaperSizes.A4.Width);
        NumberField(ObjectField(page, "margin"), "left").ShouldBe(13);
        page["contentTop"].ShouldBe(new DiagnosticNumberValue(10));
        page["contentBottom"].ShouldBe(new DiagnosticNumberValue(830));

        var placement = ObjectItem(ArrayField(page, "placements"), 0);
        KeysShouldBe(
            placement,
            "fragmentId",
            "kind",
            "pageNumber",
            "orderIndex",
            "isOversized",
            "decisionKind",
            "x",
            "y",
            "size",
            "displayRole",
            "formattingContext",
            "markerOffset",
            "derivedColumnCount",
            "rowIndex",
            "columnIndex",
            "isHeader",
            "metadataOwner",
            "metadataConsumer");
        placement["fragmentId"].ShouldBe(new DiagnosticNumberValue(42));
        placement["kind"].ShouldBe(new DiagnosticStringValue("Block"));
        placement["orderIndex"].ShouldBe(new DiagnosticNumberValue(2));
        placement["isOversized"].ShouldBe(new DiagnosticBooleanValue(true));
        placement["decisionKind"].ShouldBe(new DiagnosticStringValue(nameof(PaginationDecisionKind.Placed)));
        NumberField(ObjectField(placement, "size"), "height").ShouldBe(30);
        placement["displayRole"].ShouldBe(new DiagnosticStringValue(nameof(FragmentDisplayRole.TableCell)));
        placement["formattingContext"].ShouldBe(new DiagnosticStringValue(nameof(FormattingContextKind.InlineBlock)));
        placement["markerOffset"].ShouldBe(new DiagnosticNumberValue(5));
        placement["derivedColumnCount"].ShouldBe(new DiagnosticNumberValue(4));
        placement["rowIndex"].ShouldBe(new DiagnosticNumberValue(2));
        placement["columnIndex"].ShouldBe(new DiagnosticNumberValue(1));
        placement["isHeader"].ShouldBe(new DiagnosticBooleanValue(true));
        placement["metadataOwner"].ShouldBe(new DiagnosticStringValue("FragmentBuilder"));
        placement["metadataConsumer"].ShouldBe(new DiagnosticStringValue("Pagination"));
    }

    [Fact]
    public void From_NestedAndSiblingPublishedBlocks_AssignsDepthFirstSequenceIds()
    {
        var child = CreatePublishedBlock("div/p[1]", "p", new RectPt(0f, 0f, 80f, 20f));
        var root = CreatePublishedBlock(
            "div",
            "div",
            new RectPt(0f, 0f, 100f, 50f),
            children: [child]);
        var sibling = CreatePublishedBlock("section", "section", new RectPt(0f, 60f, 100f, 30f));
        var layoutTree = new PublishedLayoutTree(
            new PublishedPage(PaperSizes.A4, new Spacing()),
            [root, sibling]);

        var snapshot = GeometrySnapshotMapper.From(
            layoutTree,
            new PaginationResult { Layout = new HtmlLayout(), AuditPages = [] });

        snapshot.Boxes[0].SequenceId.ShouldBe(1);
        snapshot.Boxes[0].Children.ShouldHaveSingleItem().SequenceId.ShouldBe(2);
        snapshot.Boxes[1].SequenceId.ShouldBe(3);
    }

    [Fact]
    public void From_PublishedBlockWithSourceIdentity_MapsSourceIdentityFields()
    {
        var sourceIdentity = new GeometrySourceIdentity(
            new StyleNodeId(7),
            null,
            "body[0]/section[1]",
            "section#summary",
            12,
            GeometryGeneratedSourceKind.None);
        var layoutTree = new PublishedLayoutTree(
            new PublishedPage(PaperSizes.A4, new Spacing()),
            [
                CreatePublishedBlock(
                    "layout/section",
                    "section#summary",
                    new RectPt(0f, 0f, 100f, 20f),
                    sourceIdentity: sourceIdentity)
            ]);

        var snapshot = GeometrySnapshotMapper.From(
            layoutTree,
            new PaginationResult { Layout = new HtmlLayout(), AuditPages = [] });

        var box = snapshot.Boxes.ShouldHaveSingleItem();
        box.Path.ShouldBe("layout/section");
        box.TagName.ShouldBe("section");
        box.SourceNodeId.ShouldBe(7);
        box.SourceContentId.ShouldBeNull();
        box.SourcePath.ShouldBe("body[0]/section[1]");
        box.SourceOrder.ShouldBe(12);
        box.SourceElementIdentity.ShouldBe("section#summary");
        box.GeneratedSourceKind.ShouldBeNull();
    }

    [Fact]
    public void From_PublishedBlockWithGeneratedIdentity_MapsGeneratedSourceKind()
    {
        var sourceIdentity = new GeometrySourceIdentity(
            new StyleNodeId(4),
            new StyleContentId(9),
            "body[0]/div[0]/text[0]::anonymous-block",
            "div.card",
            15,
            GeometryGeneratedSourceKind.AnonymousBlock);
        var layoutTree = new PublishedLayoutTree(
            new PublishedPage(PaperSizes.A4, new Spacing()),
            [
                CreatePublishedBlock(
                    "layout/div/anonymous[0]",
                    "div.card",
                    new RectPt(0f, 0f, 100f, 20f),
                    sourceIdentity: sourceIdentity)
            ]);

        var snapshot = GeometrySnapshotMapper.From(
            layoutTree,
            new PaginationResult { Layout = new HtmlLayout(), AuditPages = [] });

        var box = snapshot.Boxes.ShouldHaveSingleItem();
        box.Path.ShouldBe("layout/div/anonymous[0]");
        box.SourceNodeId.ShouldBe(4);
        box.SourceContentId.ShouldBe(9);
        box.SourcePath.ShouldBe("body[0]/div[0]/text[0]::anonymous-block");
        box.SourceOrder.ShouldBe(15);
        box.SourceElementIdentity.ShouldBe("div.card");
        box.GeneratedSourceKind.ShouldBe("anonymous-block");
    }

    [Fact]
    public void From_BlockWithoutSourceIdentity_LeavesSourceFieldsNull()
    {
        var layoutTree = new PublishedLayoutTree(
            new PublishedPage(PaperSizes.A4, new Spacing()),
            [
                CreatePublishedBlock(
                    "layout/div",
                    "div",
                    new RectPt(0f, 0f, 100f, 20f))
            ]);

        var snapshot = GeometrySnapshotMapper.From(
            layoutTree,
            new PaginationResult { Layout = new HtmlLayout(), AuditPages = [] });

        var box = snapshot.Boxes.ShouldHaveSingleItem();
        box.SourceNodeId.ShouldBeNull();
        box.SourceContentId.ShouldBeNull();
        box.SourcePath.ShouldBeNull();
        box.SourceOrder.ShouldBeNull();
        box.SourceElementIdentity.ShouldBeNull();
        box.GeneratedSourceKind.ShouldBeNull();
    }

    private static PublishedBlock CreatePublishedBlock(
        string nodePath,
        string elementIdentity,
        RectPt borderBox,
        Spacing? padding = null,
        Spacing? border = null,
        float markerOffset = 0f,
        IReadOnlyList<PublishedBlock>? children = null,
        GeometrySourceIdentity? sourceIdentity = null)
    {
        return new PublishedBlock(
            new PublishedBlockIdentity(nodePath, elementIdentity, sourceOrder: 0, sourceIdentity),
            new PublishedDisplayFacts(
                FragmentDisplayRole.Block,
                FormattingContextKind.Block,
                markerOffset > 0f ? markerOffset : null),
            new VisualStyle(),
            UsedGeometryCalculator.FromBorderBox(
                borderBox,
                padding ?? new Spacing(),
                border ?? new Spacing(),
                markerOffset: markerOffset),
            inlineLayout: null,
            image: null,
            rule: null,
            table: null,
            children ?? []);
    }

    private static DiagnosticArray ArrayField(DiagnosticObject value, string fieldName) =>
        value[fieldName].ShouldBeOfType<DiagnosticArray>();

    private static DiagnosticObject ObjectField(DiagnosticObject value, string fieldName) =>
        value[fieldName].ShouldBeOfType<DiagnosticObject>();

    private static DiagnosticObject ObjectItem(DiagnosticArray value, int index) =>
        value[index].ShouldBeOfType<DiagnosticObject>();

    private static double NumberField(DiagnosticObject value, string fieldName) =>
        value[fieldName].ShouldBeOfType<DiagnosticNumberValue>().Value;

    private static void KeysShouldBe(DiagnosticObject value, params string[] expectedKeys)
    {
        var actual = value.Keys.OrderBy(static key => key, StringComparer.Ordinal).ToArray();
        var expected = expectedKeys.OrderBy(static key => key, StringComparer.Ordinal).ToArray();

        actual.ShouldBe(expected);
    }
}
