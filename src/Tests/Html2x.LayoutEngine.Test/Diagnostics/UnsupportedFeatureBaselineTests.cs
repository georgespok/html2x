using Html2x.RenderModel;
using Html2x.Diagnostics.Contracts;
using Html2x.LayoutEngine.Test.TestHelpers;
using Shouldly;
using LayoutFragment = Html2x.RenderModel.Fragment;

namespace Html2x.LayoutEngine.Test.Diagnostics;

public sealed class UnsupportedFeatureBaselineTests
{
    [Fact]
    public async Task Build_FloatLeft_BaselineOmitsFloatedContentAndKeepsFollowingText()
    {
        var result = await GeometryTestHarness.BuildAsync(
            """
            <html>
              <body style='margin: 0;'>
                <div style='margin: 0;'>
                  before
                  <span style='float: left; width: 40px;'>floated</span>
                  after
                </div>
              </body>
            </html>
            """);

        var text = EnumerateText(result.Layout.Pages.SelectMany(static page => page.Children)).ToList();

        text.ShouldContain(static value => value.Contains("before", StringComparison.OrdinalIgnoreCase));
        text.ShouldContain(static value => value.Contains("after", StringComparison.OrdinalIgnoreCase));
        text.ShouldNotContain(static value => value.Contains("floated", StringComparison.OrdinalIgnoreCase));
        AssertUnsupportedMode(result.Diagnostics, "float");
    }

    [Fact]
    public async Task Build_PositionAbsolute_BaselineTreatsElementAsNormalBlockFlow()
    {
        var result = await GeometryTestHarness.BuildAsync(
            """
            <html>
              <body style='margin: 0;'>
                <div style='margin: 0;'>
                  before
                  <div style='position: absolute; left: 40px; top: 20px; margin: 0;'>absolute</div>
                  after
                </div>
              </body>
            </html>
            """);

        var lines = EnumerateLines(result.Layout.Pages.SelectMany(static page => page.Children)).ToList();
        var before = FindLine(lines, "before");
        var absolute = FindLine(lines, "absolute");
        var after = FindLine(lines, "after");

        absolute.Rect.Y.ShouldBeGreaterThan(before.Rect.Y);
        after.Rect.Y.ShouldBeGreaterThan(absolute.Rect.Y);
        AssertUnsupportedMode(result.Diagnostics, "position:absolute");
    }

    [Fact]
    public async Task Build_DisplayFlex_BaselineTreatsContainerAsBlockFlow()
    {
        var result = await GeometryTestHarness.BuildAsync(
            """
            <html>
              <body style='margin: 0;'>
                <div style='display: flex; margin: 0;'>
                  <div style='margin: 0;'>one</div>
                  <div style='margin: 0;'>two</div>
                </div>
              </body>
            </html>
            """);

        var lines = EnumerateLines(result.Layout.Pages.SelectMany(static page => page.Children)).ToList();
        var one = FindLine(lines, "one");
        var two = FindLine(lines, "two");

        two.Rect.Y.ShouldBeGreaterThan(one.Rect.Y);
        AssertUnsupportedMode(result.Diagnostics, "display:flex");
    }

    private static void AssertUnsupportedMode(IReadOnlyList<DiagnosticRecord> diagnostics, string structureKind)
    {
        diagnostics.Any(e =>
            e.Severity == DiagnosticSeverity.Warning &&
            e.Name == "layout/unsupported-mode" &&
            e.Fields["structureKind"] is DiagnosticStringValue { Value: var actualKind } &&
            actualKind == structureKind &&
            e.Fields["reason"] is DiagnosticStringValue { Value: var reason } &&
            !string.IsNullOrWhiteSpace(reason))
            .ShouldBeTrue();
    }

    private static LineBoxFragment FindLine(IEnumerable<LineBoxFragment> lines, string text)
    {
        return lines.First(line => line.Runs.Any(run => run.Text.Contains(text, StringComparison.OrdinalIgnoreCase)));
    }

    private static IEnumerable<string> EnumerateText(IEnumerable<LayoutFragment> fragments)
    {
        foreach (var line in EnumerateLines(fragments))
        {
            foreach (var run in line.Runs)
            {
                yield return run.Text;
            }
        }
    }

    private static IEnumerable<LineBoxFragment> EnumerateLines(IEnumerable<LayoutFragment> fragments)
    {
        foreach (var fragment in fragments)
        {
            foreach (var line in EnumerateLines(fragment))
            {
                yield return line;
            }
        }
    }

    private static IEnumerable<LineBoxFragment> EnumerateLines(LayoutFragment fragment)
    {
        if (fragment is LineBoxFragment line)
        {
            yield return line;
        }

        if (fragment is not BlockFragment block)
        {
            yield break;
        }

        foreach (var child in block.Children)
        {
            foreach (var nestedLine in EnumerateLines(child))
            {
                yield return nestedLine;
            }
        }
    }
}
