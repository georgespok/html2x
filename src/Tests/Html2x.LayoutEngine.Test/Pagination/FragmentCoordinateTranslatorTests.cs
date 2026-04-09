using System.Drawing;
using Html2x.Abstractions.Layout.Fragments;
using Html2x.Abstractions.Layout.Styles;
using Html2x.Abstractions.Measurements.Units;
using Html2x.LayoutEngine.Pagination;
using Shouldly;
using LayoutFragment = Html2x.Abstractions.Layout.Fragments.Fragment;

namespace Html2x.LayoutEngine.Test.Pagination;

public sealed class FragmentCoordinateTranslatorTests
{
    [Fact]
    public void BuiltInSupportedTypes_ShouldMatchKnownFragmentSet()
    {
        var actual = FragmentCoordinateTranslator.GetBuiltInSupportedTypes()
            .OrderBy(static type => type.Name)
            .ToList();

        var expected = new[]
        {
            typeof(BlockFragment),
            typeof(ImageFragment),
            typeof(LineBoxFragment),
            typeof(RuleFragment),
            typeof(TableCellFragment),
            typeof(TableFragment),
            typeof(TableRowFragment)
        }
        .OrderBy(static type => type.Name)
        .ToList();

        actual.ShouldBe(expected);
    }

    [Fact]
    public void Paginate_WithBuiltInTableFragments_ShouldCloneTableSubtreeWithoutRegistration()
    {
        var paginator = new BlockPaginator();
        var table = new TableFragment([
            new TableRowFragment([
                new TableCellFragment([
                    new LineBoxFragment
                    {
                        FragmentId = 1001,
                        Rect = new RectangleF(0f, 665f, 20f, 10f),
                        Runs = []
                    }
                ])
                {
                    FragmentId = 1000,
                    Rect = new RectangleF(0f, 660f, 50f, 20f),
                    ColumnIndex = 0
                }
            ])
            {
                FragmentId = 100,
                Rect = new RectangleF(0f, 660f, 100f, 20f),
                RowIndex = 0
            }
        ])
        {
            FragmentId = 10,
            Rect = new RectangleF(0f, 650f, 100f, 40f),
            DerivedColumnCount = 1
        };

        var blocks = new List<BlockFragment>
        {
            CreateBlock(1, 10f, 70f),
            table
        };

        var result = paginator.Paginate(blocks, new SizePt(200f, 100f), new Spacing(10f, 10f, 10f, 10f));
        var movedTable = result.Pages[1].Placements.ShouldHaveSingleItem().Fragment.ShouldBeOfType<TableFragment>();

        movedTable.PageNumber.ShouldBe(2);
        movedTable.Rect.Y.ShouldBe(10f);
        movedTable.Rows.ShouldHaveSingleItem().Rect.Y.ShouldBe(20f);
        movedTable.Rows[0].Cells.ShouldHaveSingleItem().Rect.Y.ShouldBe(20f);
        movedTable.Rows[0].Cells[0].Children.ShouldHaveSingleItem().ShouldBeOfType<LineBoxFragment>().Rect.Y.ShouldBe(25f);
    }

    [Fact]
    public void Paginate_WhenUnknownFragmentTypeIsNotRegistered_ShouldThrowWithExtensionGuidance()
    {
        var paginator = new BlockPaginator();
        var blocks = new List<BlockFragment>
        {
            CreateBlock(1, 10f, 70f),
            CreateBlockWithCustomChild<UnregisteredCustomFragment>(2, 650f, 40f)
        };

        var exception = Should.Throw<NotSupportedException>(() =>
            paginator.Paginate(blocks, new SizePt(200f, 100f), new Spacing(10f, 10f, 10f, 10f)));

        exception.Message.ShouldContain(nameof(UnregisteredCustomFragment));
        exception.Message.ShouldContain(nameof(FragmentCoordinateTranslator.RegisterExtensionTranslator));
    }

    [Fact]
    public void Paginate_WhenUnknownFragmentTypeIsRegistered_ShouldUseExtensionTranslator()
    {
        FragmentCoordinateTranslator.RegisterExtensionTranslator<RegisteredCustomFragment>(
            static (fragment, pageNumber, deltaX, deltaY) => new RegisteredCustomFragment
            {
                FragmentId = fragment.FragmentId,
                PageNumber = pageNumber,
                Rect = new RectangleF(
                    fragment.Rect.X + deltaX,
                    fragment.Rect.Y + deltaY,
                    fragment.Rect.Width,
                    fragment.Rect.Height),
                ZOrder = fragment.ZOrder,
                Style = fragment.Style
            });

        var paginator = new BlockPaginator();
        var blocks = new List<BlockFragment>
        {
            CreateBlock(1, 10f, 70f),
            CreateBlockWithCustomChild<RegisteredCustomFragment>(2, 650f, 40f)
        };

        var result = paginator.Paginate(blocks, new SizePt(200f, 100f), new Spacing(10f, 10f, 10f, 10f));
        var movedBlock = result.Pages[1].Placements.ShouldHaveSingleItem().Fragment;
        var translatedCustom = movedBlock.Children.OfType<RegisteredCustomFragment>().ShouldHaveSingleItem();

        translatedCustom.PageNumber.ShouldBe(2);
        translatedCustom.Rect.Y.ShouldBe(15f); // child starts at block.Y + 5f after moving block to page top (10f)
    }

    private static BlockFragment CreateBlock(int id, float y, float height)
    {
        return new BlockFragment
        {
            FragmentId = id,
            Rect = new RectangleF(0f, y, 100f, height)
        };
    }

    private static BlockFragment CreateBlockWithCustomChild<TCustom>(int id, float y, float height)
        where TCustom : LayoutFragment, new()
    {
        var child = new TCustom
        {
            FragmentId = id * 100,
            Rect = new RectangleF(0f, y + 5f, 50f, 10f)
        };

        return new BlockFragment([child])
        {
            FragmentId = id,
            Rect = new RectangleF(0f, y, 100f, height)
        };
    }

    private sealed class UnregisteredCustomFragment : LayoutFragment
    {
    }

    private sealed class RegisteredCustomFragment : LayoutFragment
    {
    }
}
