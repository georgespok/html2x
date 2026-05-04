using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;

namespace Html2x.LayoutEngine.Test.Architecture;


internal static class ArchitectureTestSetAssertions
{
    public static void ShouldBeSet(this IEnumerable<string> actual, IEnumerable<string> expected)
    {
        actual
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray()
            .ShouldBe(expected
                .OrderBy(static value => value, StringComparer.Ordinal)
                .ToArray());
    }

    public static void ShouldContainSet(this IEnumerable<string> actual, IEnumerable<string> expected)
    {
        var actualValues = actual.ToArray();

        foreach (var expectedValue in expected)
        {
            actualValues.ShouldContain(expectedValue);
        }
    }
}
