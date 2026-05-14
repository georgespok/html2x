using Html2x.LayoutEngine.Diagnostics;
using Html2x.LayoutEngine.Fragments;
using Html2x.LayoutEngine.Pagination;
using Html2x.RenderModel.Fragments;
using Html2x.Renderers.Pdf.Paint;
using Shouldly;
using static Html2x.Architecture.Test.Support.TestSupport;

namespace Html2x.Architecture.Test.Fragments;

public sealed class FragmentKindUpdatePathTests
{
    private static readonly string[] RenderFragmentTypes =
    [
        nameof(BlockFragment),
        nameof(LineBoxFragment),
        nameof(ImageFragment),
        nameof(RuleFragment),
        nameof(TableFragment),
        nameof(TableRowFragment),
        nameof(TableCellFragment)
    ];

    [Fact]
    public void RenderFragmentSet_HasExplicitUpdatePath()
    {
        var fragmentFiles = SourceSetForNamespaceOf<BlockFragment>()
            .Files
            .Select(static file => Path.GetFileNameWithoutExtension(file.Path))
            .Where(static name => name.EndsWith(nameof(Fragment), StringComparison.Ordinal))
            .Where(static name => !string.Equals(name, nameof(Fragment), StringComparison.Ordinal))
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        fragmentFiles.ShouldBe(RenderFragmentTypes.OrderBy(static name => name, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void FragmentProjection_HandlesCurrentFragmentKinds()
    {
        var factory = SourceFileFor<PublishedFragmentFactory>();
        var builder = SourceFileFor<FragmentTreeBuilder>();

        factory.ShouldUseIdentifier(nameof(BlockFragment));
        factory.ShouldUseIdentifier(nameof(ImageFragment));
        factory.ShouldUseIdentifier(nameof(RuleFragment));
        factory.ShouldUseIdentifier(nameof(TableFragment));
        factory.ShouldUseIdentifier(nameof(TableRowFragment));
        factory.ShouldUseIdentifier(nameof(TableCellFragment));
        builder.ShouldUseIdentifier(nameof(LineBoxFragment));
        builder.ShouldUseIdentifier(nameof(PublishedFragmentFactory.CreateSpecialFragment));
    }

    [Fact]
    public void PaginationClonePaintAndSnapshots_HandleCurrentFragmentKinds()
    {
        var cloner = SourceFileFor<FragmentPlacementCloner>();
        var paginator = SourceFileFor<LayoutPaginator>();
        var paintPlanner = SourceFileFor<PaintCommandPlanner>();
        var snapshotMapper = SourceFileFor(typeof(LayoutSnapshotMapper));

        foreach (var fragmentType in RenderFragmentTypes)
        {
            cloner.ShouldUseIdentifier(fragmentType);
            paintPlanner.ShouldUseIdentifier(fragmentType);
            snapshotMapper.ShouldUseIdentifier(fragmentType);
        }

        paginator.ShouldUseIdentifier(nameof(PaginationPlacementAudit.FragmentKind));
        paginator.ShouldUseIdentifier(nameof(PaginationPlacementAudit.DisplayRole));
        cloner.ShouldUseIdentifier(nameof(NotSupportedException));
        paintPlanner.ShouldUseIdentifier(nameof(NotSupportedException));
    }

    [Fact]
    public void FragmentMetadataOwnerName_RemainsCompatibilityValue()
    {
        SourceFileFor<PublishedFragmentFactory>()
            .ShouldContainStringLiteral(PublishedFragmentFactory.MetadataOwnerName);
    }
}
