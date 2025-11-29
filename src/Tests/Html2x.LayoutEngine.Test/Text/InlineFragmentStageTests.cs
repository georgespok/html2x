using Html2x.Abstractions.Layout.Fragments;
using Html2x.LayoutEngine.Fragment;
using Html2x.LayoutEngine.Fragment.Stages;
using Html2x.LayoutEngine.Models;
using Html2x.LayoutEngine.Test.Builders;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Text;

public class InlineFragmentStageTests
{
    [Fact]
    public void InlineSiblingsWithSameBaseline_AreMergedIntoSingleLine()
    {
        // Arrange: build a single block with sibling inline nodes via the builder helpers
        var boxTree = new BlockBoxBuilder()
            .Block(10, 20, 200, 40, style: new ComputedStyle { FontSizePt = 12 })
                .Inline("This is ", new ComputedStyle { FontSizePt = 12 })
                .Inline("bold", new ComputedStyle { FontSizePt = 12, Bold = true })
                .Inline(" and ", new ComputedStyle { FontSizePt = 12 })
                .Inline("italic", new ComputedStyle { FontSizePt = 12, Italic = true })
                .Inline(" text.", new ComputedStyle { FontSizePt = 12 })
                .Up()
            .BuildTree();

        var state = new FragmentBuildState(boxTree);
        
        // Act
        state = new BlockFragmentStage().Execute(state);
        state = new InlineFragmentStage().Execute(state);

        
        // Assert
        var fragment = state.Fragments.Blocks.ShouldHaveSingleItem();
        var line = fragment.Children.ShouldHaveSingleItem().ShouldBeOfType<LineBoxFragment>();

        line.Runs.Count.ShouldBe(5);
        string.Concat(line.Runs.Select(r => r.Text)).ShouldBe("This is bold and italic text.");

        foreach (var run in line.Runs)
        {
            (run.Origin.Y + run.Ascent).ShouldBe(line.BaselineY, 0.01);
        }
    }
}
