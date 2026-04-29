using Html2x.LayoutEngine.Box;
using Html2x.LayoutEngine.Models;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Display;

public sealed class ListMarkerPolicyTests
{
    [Fact]
    public void CreateMarker_UnorderedList_ReturnsBulletMarker()
    {
        var list = CreateList(HtmlCssConstants.HtmlTags.Ul);
        var item = new BlockBox(BoxRole.ListItem)
        {
            Parent = list
        };

        var marker = ListMarkerPolicy.CreateMarker(list, item).ShouldNotBeNull();
        marker.TextContent.ShouldBe("\u2022 ");
        marker.Parent.ShouldBeSameAs(item);
    }

    [Fact]
    public void CreateSyntheticMarker_OrderedList_UsesListItemPosition()
    {
        var list = CreateList(HtmlCssConstants.HtmlTags.Ol);
        var first = new BlockBox(BoxRole.ListItem) { Parent = list };
        var second = new BlockBox(BoxRole.ListItem) { Parent = list };
        list.Children.Add(first);
        list.Children.Add(second);

        var marker = ListMarkerPolicy.CreateSyntheticMarker(second).ShouldNotBeNull();
        marker.TextContent.ShouldBe("2. ");
    }

    [Fact]
    public void CreateSyntheticMarker_ExistingMarkerOffset_ReturnsNull()
    {
        var list = CreateList(HtmlCssConstants.HtmlTags.Ul);
        var item = new BlockBox(BoxRole.ListItem)
        {
            Parent = list,
            MarkerOffset = HtmlCssConstants.Defaults.ListMarkerOffsetPt
        };
        list.Children.Add(item);

        var marker = ListMarkerPolicy.CreateSyntheticMarker(item);

        marker.ShouldBeNull();
    }

    private static BlockBox CreateList(string tagName)
    {
        return new BlockBox(BoxRole.Block)
        {
            Element = StyledElementFacts.Create(tagName)
        };
    }
}
