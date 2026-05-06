using Shouldly;

namespace Html2x.TestConsole.Test;

public sealed class ConsolePathResolverTests
{
    [Fact]
    public void Resolve_ExplicitRelativeOutput_UsesWorkingDirectory()
    {
        var settings = new RenderSettings
        {
            OutputOption = Path.Combine("build", "all-supported-features.pdf")
        };

        var paths = ConsolePathResolver.Resolve(settings, "input.html", null);

        paths.OutputPath.ShouldBe(Path.GetFullPath(Path.Combine("build", "all-supported-features.pdf")));
    }

    [Fact]
    public void Resolve_DefaultOutput_UsesTempDirectory()
    {
        var settings = new RenderSettings();

        var paths = ConsolePathResolver.Resolve(settings, "sample.html", null);

        paths.OutputPath.ShouldBe(Path.Combine(Path.GetTempPath(), "sample.pdf"));
    }
}