using Spectre.Console.Cli;

namespace Html2x.TestConsole;

internal static class Program
{
    public static Task<int> Main(string[] args) => 
        new CommandApp<RenderCommand>().RunAsync(args);
}
