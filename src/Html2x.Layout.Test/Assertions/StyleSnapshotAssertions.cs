using System.Reflection;
using Html2x.Layout.Style;
using Shouldly;

namespace Html2x.Layout.Test.Assertions;

public record StyleSnapshot(
    string Tag,
    ComputedStyle? Style = null,
    List<StyleSnapshot>? Children = null
);

public static class StyleTreeSnapshot
{
    public static StyleSnapshot FromTree(StyleTree tree)
        => tree.Root is null
            ? new StyleSnapshot("empty")
            : FromNode(tree.Root);

    private static StyleSnapshot FromNode(StyleNode node)
        => new(
            Tag: node.Element.TagName.ToLowerInvariant(),
            Style: node.Style,
            Children: node.Children.Select(FromNode).ToList()
        );
}

public static class StyleSnapshotAssertions
{
    public static void ShouldMatch(this StyleSnapshot actual, StyleSnapshot expected)
    {
        // Always verify tag
        actual.Tag.ShouldBe(expected.Tag, $"Tag mismatch at <{expected.Tag}>");

        // Verify styles only if defined
        if (expected.Style is not null)
        {
            AssertComputedStyle(actual.Style, expected.Style);
        }

        // Verify children if defined
        if (!(expected.Children?.Count > 0))
        {
            return;
        }

        actual.Children.Count.ShouldBe(expected.Children.Count,
            $"Expected {expected.Children.Count} children for <{expected.Tag}> but found {actual.Children.Count}");

        for (int i = 0; i < expected.Children.Count; i++)
        {
            actual.Children[i].ShouldMatch(expected.Children[i]);
        }
    }

    private static void AssertComputedStyle(ComputedStyle? actual, ComputedStyle expected)
    {
        if (actual is null)
        {
            throw new Xunit.Sdk.XunitException("Actual style is null");
        }

        var type = typeof(ComputedStyle);
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var expectedValue = prop.GetValue(expected);
            var actualValue = prop.GetValue(actual);

            // Skip if expected value is null or default (float==0, bool==false, string==null/empty)
            if (IsDefaultOrNull(expectedValue))
            {
                continue;
            }

            try
            {
                actualValue.ShouldBeEquivalentTo(expectedValue,
                    $"Mismatch in style property '{prop.Name}'");
            }
            catch (ShouldAssertException)
            {
                // Provide richer error context
                throw new Xunit.Sdk.XunitException(
                    $"Style mismatch at property '{prop.Name}': expected {expectedValue}, actual {actualValue}");
            }
        }
    }

    private static bool IsDefaultOrNull(object? value)
    {
        if (value is null)
            return true;

        var type = value.GetType();

        if (type == typeof(string))
            return string.IsNullOrEmpty((string)value);

        if (type.IsValueType)
        {
            var defaultValue = Activator.CreateInstance(type);
            return value.Equals(defaultValue);
        }

        return false;
    }
}