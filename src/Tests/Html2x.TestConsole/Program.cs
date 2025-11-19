using Spectre.Console.Cli;

namespace Html2x.TestConsole;

internal static class Program
{
    public static Task<int> Main(string[] args)
    {
        var app = new CommandApp<RenderCommand>();
        app.Configure(config =>
        {
            config.SetApplicationName("Html2x.TestConsole");
        });

        return app.RunAsync(args);
    }
}
