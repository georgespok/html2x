using Html2x.LayoutEngine.Box;
using Shouldly;

namespace Html2x.LayoutEngine.Test;

public class StringNormalizerTests
{
    [Theory]
    [InlineData("foo    bar", "foo bar")]
    [InlineData("   leading", "leading")]
    [InlineData("first\nsecond", "first second")]
    public void NormalizeSingleRun_Scenarios(string input, string expected)
    {
        var state = new TextNormalizationState();

        var result = StringNormalizer.NormalizeWhiteSpaceNormal(input, state);

        result.ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(CarryAcrossRunsCases))]
    public void NormalizeAcrossRuns_CarriesPendingSpace(
        string[] chunks,
        string[] expectedOutputs,
        string expectedConcat)
    {
        var state = new TextNormalizationState();
        var results = chunks.Select(chunk => StringNormalizer.NormalizeWhiteSpaceNormal(chunk, state)).ToArray();

        results.ShouldBe(expectedOutputs);
        string.Concat(results).ShouldBe(expectedConcat);
    }

    public static IEnumerable<object[]> CarryAcrossRunsCases()
    {
        yield return new object[]
        {
            new[] { "one ", " two", " three" },
            new[] { "one", " two", " three" },
            "one two three"
        };
    }
}
