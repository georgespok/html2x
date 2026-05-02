using Shouldly;

namespace Html2x.TestConsole.Test;

public sealed class ConsolePathResolverTests
{
    [Fact]
    public void Resolve_ExplicitRelativeOutput_UsesWorkingDirectory()
    {
        var settings = new RenderSettings
        {
            OutputOption = Path.Combine("build", "example.pdf")
        };

        var paths = ConsolePathResolver.Resolve(settings, "input.html", selectedSamplePath: null);

        paths.OutputPath.ShouldBe(Path.GetFullPath(Path.Combine("build", "example.pdf")));
    }

    [Fact]
    public void Resolve_DefaultOutput_UsesTempDirectory()
    {
        var settings = new RenderSettings();

        var paths = ConsolePathResolver.Resolve(settings, "sample.html", selectedSamplePath: null);

        paths.OutputPath.ShouldBe(Path.Combine(Path.GetTempPath(), "sample.pdf"));
    }
}
